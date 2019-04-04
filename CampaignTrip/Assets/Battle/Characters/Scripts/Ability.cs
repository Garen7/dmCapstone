﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleActorBase;

[Serializable]
public class Ability
{
    public AbilityButton AButton { get; set; }
    public bool TargetAll { get { return targetAll; } }
    public int Damage { get { return damage; } }
    public int Duration { get { return duration; } }
    public TargetGroup Targets { get { return targetGroup; } }
    public StatusEffect Applies { get { return applies; } }

    public enum TargetGroup { Override, Ally, Self, AllyAndSelf, Enemy }

    [HideInInspector] public int RemainingCooldown;

    [SerializeField] private string abilityName;
    [SerializeField] private int damage;
    [SerializeField] private int duration;
    [SerializeField] private int cooldown;
    [SerializeField] private bool targetAll;
    [SerializeField] private TargetGroup targetGroup;
    [SerializeField] private StatusEffect applies;
    [SerializeField] private Sprite buttonIcon;

    //TODO:
    [HideInInspector] public bool IsUpgraded;

    public void SetButton(AbilityButton button)
    {
        AButton = button;
        AButton.nameText.text = abilityName;
        if (buttonIcon != null)
        {
            AButton.iconImage.sprite = buttonIcon;
        }
    }

    public void Use()
    {
        BattlePlayerBase.LocalAuthority.CanPlayAbility = false;
        RemainingCooldown = cooldown + 1;
    }

    public void DecrementCooldown()
    {
        if (RemainingCooldown > 0)
            RemainingCooldown--;
        UpdateButtonUI();
    }

    public void UpdateButtonUI()
    {
        if (AButton == null)
            return;
        AButton.button.interactable = (RemainingCooldown <= 0 && BattlePlayerBase.LocalAuthority.CanPlayAbility);
        AButton.UpdateCooldown(RemainingCooldown, cooldown + 1);
    }
}