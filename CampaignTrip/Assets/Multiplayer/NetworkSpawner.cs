﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618
public class NetworkSpawner : NetworkBehaviour
{
    public static NetworkSpawner Instance;
    
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void NetworkSpawn(GameObject obj)
    {
        if (!NetworkWrapper.IsHost)
            return;

        NetworkServer.Spawn(obj);
    }

    public void NetworkSpawn(GameObject obj, GameObject player)
    {
        if (!NetworkWrapper.IsHost)
            return;
        
        NetworkServer.SpawnWithClientAuthority(obj, player);
    }
}
#pragma warning restore CS0618