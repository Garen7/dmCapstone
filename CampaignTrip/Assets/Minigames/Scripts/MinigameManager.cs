﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#pragma warning disable 0618
public abstract class MinigameManager : NetworkBehaviour
{
    public static MinigameManager Instance;

    public List<Transform> spawnPoints;

	public int numPlayersWon = 0;
    public float timer = 30;
	public Text winText;

    protected virtual void Start()
    {
        Instance = this;

		numPlayersWon = 0;
        PersistentPlayer.localAuthority.CmdReadyForMinigame();

        if (NetworkWrapper.IsHost)
		{
			List<PersistentPlayer> randomPlayers = new List<PersistentPlayer>(PersistentPlayer.players);
			//Fisher-Yates Shuffle
			for (var i = 0; i < randomPlayers.Count - 1; i++)
			{
				//using a range of i to the size avoids bias
				int randomNum = Random.Range(i, randomPlayers.Count);
				//now swap them
				PersistentPlayer tmp = randomPlayers[i];
				randomPlayers[i] = randomPlayers[randomNum];
				randomPlayers[randomNum] = tmp;
			}

			StartCoroutine(HandlePlayers(randomPlayers));
        }
    }

    private void LateUpdate()
    {
		if (numPlayersWon != 0 && numPlayersWon == PersistentPlayer.players.Count)
		{
            Win();
            BattleController.Instance.UnloadMinigame(true);
		}
        // TODO Add timer for failure
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    protected virtual IEnumerator HandlePlayers(List<PersistentPlayer> randomPlayers)
    {
        yield return new WaitUntil(() => {
            return PersistentPlayer.localAuthority.minigameReady == randomPlayers.Count;
        });
    }
    
    protected abstract void Win();
    protected abstract void Lose();

    [Command]
	public void CmdWin()
	{
		RpcWin();
	}

    [Command]
    public void CmdLose()
    {
        RpcLose();
    }

    [ClientRpc]
	public void RpcWin()
	{
		Win();
	}

    [ClientRpc]
    public void RpcLose()
    {
        Lose();
    }
}