﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static BattlePlayerBase;
using static EnemyBase;

#pragma warning disable 0618, 0649
public class BuffStatTracker : NetworkBehaviour
{
    public static BuffStatTracker Instance { get; private set; }

    [SerializeField] private EnemyDataList enemies;

    private Dictionary<CharacterType, BattleStats> playerStats;
    private Dictionary<EnemyType, BattleStats> enemyStats;
    
    private void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        enemyStats = new Dictionary<EnemyType, BattleStats>();
        playerStats = new Dictionary<CharacterType, BattleStats>();

        foreach (GameObject enemy in enemies.list)
        {
            EnemyBase script = enemy.GetComponent<EnemyBase>();
            BattleStats copy = BattleStats.Copy(script.BattleStats);
            enemyStats.Add(script.enemyType, copy);
        }
    }

    [Server]
    public void ApplyRandomEnemyBuffs()
    {
        EnemyType[] types = (EnemyType[])Enum.GetValues(typeof(EnemyType));
        foreach (EnemyType t in types)
        {
            if (enemyStats.ContainsKey(t))
            {
                /*BattleStats stats = */enemyStats[t].ApplyRandomBuff();
                RpcUpdateEnemyStats(t, enemyStats[t]);
            }
        }
    }
    
    public BattleStats GetPlayerStats(CharacterType type, BattleStats currentStats)
    {
        if (playerStats.ContainsKey(type))
            return playerStats[type];

        playerStats.Add(type, currentStats);
        return currentStats;
    }

    public BattleStats GetEnemyStats(EnemyType type)
    {
        return enemyStats[type];
    }

    [ClientRpc]
    public void RpcUpdatePlayerStats(CharacterType type, BattleStats stats)
    {
        playerStats[type] = stats; 
    }

    [ClientRpc]
    public void RpcUpdateEnemyStats(EnemyType type, BattleStats stats)
    {
        enemyStats[type] = stats; 
    }
}
