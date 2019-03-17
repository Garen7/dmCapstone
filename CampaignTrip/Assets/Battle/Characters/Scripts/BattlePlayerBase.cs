﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618, 0649
public abstract class BattlePlayerBase : BattleActorBase
{
    public static BattlePlayerBase LocalAuthority { get { return PersistentPlayer.localAuthority.battlePlayer; } }
    /// <summary>
    /// This value is only maintained on the Server
    /// </summary>
    public static Ability SelectedAbility { get; private set; }
    public static bool AbilityPlayedThisTurn { get; set; }
    public static int PlayersUsingAbility;

    public bool IsUsingAbility { get; private set; }
    
    [SyncVar] public int playerNum;

    [HideInInspector] public PersistentPlayer persistentPlayer;

    [SerializeField] protected Ability ability1;
    [SerializeField] protected Ability ability2;
    [SerializeField] protected Ability ability3;

    protected List<Ability> Abilities;

    public enum TargetMode { Auto, Friend, Foe }

    [Serializable]
    public class Ability
    {
        public Action<BattleActorBase> ExecuteAbility { get; set; }
        public int Damage { get { return damage; } }
        public TargetMode Targets { get { return targets; } }

        [HideInInspector] public int RemainingCooldown;

        [SerializeField] private TargetMode targets;
        [SerializeField] private int cooldown;
        [Tooltip("Also used for heal amount")]
        [SerializeField] private int damage;

        //TODO:
        [HideInInspector] public bool IsUpgraded;

        [SerializeField] private Sprite ButtonIcon;
        [SerializeField] private string AbilityName;
        //END TODO
        
        public void Bind(Action<BattleActorBase> action)
        {
            ExecuteAbility = action;
        }

        public void Use(BattleActorBase target)
        {
            AbilityPlayedThisTurn = true;
            RemainingCooldown = cooldown;
            ExecuteAbility(target);
        }
    }

    #region Initialization

    public override void OnStartClient()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return new WaitUntil(() => BattleController.Instance != null);

        int i = playerNum - 1;
        persistentPlayer = PersistentPlayer.players[i];
        persistentPlayer.battlePlayer = this;

        transform.position = BattleController.Instance.playerSpawnPoints[i];

        Health = maxHealth;
        HealthBar = BattleController.Instance.ClaimPlayerUI(this);
        damagePopup = BattleController.Instance.ClaimDamagePopup();
        
        Abilities = new List<Ability>() { ability1, ability2, ability3 };
        ability1.Bind(Ability1);
        ability2.Bind(Ability2);
        ability3.Bind(Ability3);
    }

    #endregion

    #region Attack

    //override these implementations if the ability is a little more complex than what CmdUseAbility handles
    public virtual void Ability1(BattleActorBase target)
    {
        CmdUseAbility(target.gameObject, 0);
    }

    public virtual void Ability2(BattleActorBase target)
    {
        CmdUseAbility(target.gameObject, 1);
    }

    public virtual void Ability3(BattleActorBase target)
    {
        CmdUseAbility(target.gameObject, 2);
    }

    [Command] //This method is used for abilities with a simple setup (attack all/selected enemy(s)/player(s) and apply some status effect)
    protected void CmdUseAbility(GameObject target, int i)
    {
        
    }

    private void OnMouseUpAsButton()
    {
        if (IsAlive && LocalAuthority.IsUsingAbility && SelectedAbility.Targets == TargetMode.Friend)
        {
            LocalAuthority.OnAbilityTargetChosen(this);
        }
    }

    public void AbilitySelected(int i)
    {
        if (SelectedAbility != null)
        {
            EndAbility();
        }
        else if (!AbilityPlayedThisTurn && Abilities[i].RemainingCooldown <= 0)
        {
            CmdUseAbilityRequest(i);
        }
    }

    [Command]
    private void CmdUseAbilityRequest(int i)
    {
        if (BattleController.Instance.IsPlayerPhase)
        {
            IsUsingAbility = true;
            PlayersUsingAbility++;
            Target_UseAbilityConfirm(persistentPlayer.connectionToClient, i);
        }
    }
    
    [TargetRpc]
    private void Target_UseAbilityConfirm(NetworkConnection conn, int i)
    {
        SelectedAbility = Abilities[i];

        switch (SelectedAbility.Targets)
        {
            case TargetMode.Auto:
                SelectedAbility.Use(null);
                break;
            case TargetMode.Friend:
                ToggleTargetFriends(true);
                break;
            case TargetMode.Foe:
                ToggleTargetFoes(true);
                break;
        }
    }

    private void ToggleTargetFriends(bool active)
    {
        foreach (PersistentPlayer p in PersistentPlayer.players)
        {
            p.battlePlayer.tempAbilityTarget.SetActive(active);
        }
    }

    private void ToggleTargetFoes(bool active)
    {
        foreach (EnemyBase e in BattleController.Instance.aliveEnemies)
        {
            e.tempAbilityTarget.SetActive(active);
        }
    }
    
    public void OnAbilityTargetChosen(BattleActorBase target)
    {
        SelectedAbility.Use(target);
        EndAbility();
    }
    
    protected void EndAbility()
    {
        ToggleTargetFriends(false);
        ToggleTargetFoes(false);
        SelectedAbility = null;
    }

    [Server]
    public void OnPlayerPhaseStart()
    {
        RpcUpdateAttackBlock(attacksPerTurn);
        AbilityPlayedThisTurn = false;
    }

    [Command]
    public void CmdAttack(GameObject target)
    {
        if (attacksRemaining > 0 && BattleController.Instance.IsPlayerPhase)
        {
            RpcAttack();
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            enemy.TakeDamage(basicDamage);
        }
    }

    [ClientRpc]
    private void RpcAttack()
    {
        UpdateAttackBlock(attacksRemaining - 1);
        if (localPlayerAuthority)
        {
            animator.SetTrigger("Attack");
        }
    }

    [ClientRpc]
    private void RpcUpdateAttackBlock(int newAttacksRemaining)
    {
        UpdateAttackBlock(newAttacksRemaining);
    }

    private void UpdateAttackBlock(int newAttacksRemaining)
    {
        attacksRemaining = newAttacksRemaining;
        blockAmount = (int)Mathf.Min((float)attacksRemaining / attacksPerTurn * 100f, 90);

        if (this == LocalAuthority)
        {
            BattleController.Instance.UpdateAttackBlockUI(attacksRemaining, blockAmount);
        }
    }

    #endregion

    #region Damage

    [Server]
    public void TakeDamage(EnemyBase e)
    {
        int blocked = e.BasicDamage * blockAmount / 100;
        int damageTaken = e.BasicDamage - blocked;
        RpcTakeDamage(damageTaken, blocked);
    }

    [ClientRpc]
    private void RpcTakeDamage(int damageTaken, int blocked)
    {
        damagePopup.Display(damageTaken, blocked, uiTransform.position);

        Health = Math.Max(Health - damageTaken, 0);
        HealthBar.SetHealth(Health);
    }

    #endregion
}
#pragma warning restore CS0618