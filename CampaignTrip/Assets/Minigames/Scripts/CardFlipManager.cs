﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0618
public class CardFlipManager : MinigameManager
{
	public Sprite winningSprite;
	public Sprite failSprite;
	public List<FlippableCard> cards;

    public static CardFlipManager GetInstance()
    {
        return Instance as CardFlipManager;
    }

    protected override IEnumerator HandlePlayers(List<PersistentPlayer> randomPlayers)
	{
		List<FlippableCard> randomCards = new List<FlippableCard>(cards);
		//Fisher-Yates Shuffle
		for (var i = 0; i < randomCards.Count - 1; i++)
		{
			int randomNum = Random.Range(i, randomCards.Count);
			//now swap them
			FlippableCard tmp = randomCards[i];
			randomCards[i] = randomCards[randomNum];
			randomCards[randomNum] = tmp;
		}

		List<PersistentPlayer> randomPlayersCopy = new List<PersistentPlayer>(randomPlayers);
		var playerWhoPicksTheCard = randomPlayersCopy[0].connectionToClient;
		randomPlayersCopy.RemoveAt(0);
		yield return new WaitUntil(() => playerWhoPicksTheCard.isReady);
		//give playerWhoPicksTheCard authority to flip the cards
		foreach (FlippableCard card in cards)
			card.GetComponent<NetworkIdentity>().AssignClientAuthority(playerWhoPicksTheCard);
		FlippableCard winningCard = randomCards[0];
		randomCards.Remove(winningCard);
		winningCard.isWinner = true;

		yield return new WaitForSecondsRealtime(.5f);//make sure everyones here

		while (randomPlayersCopy.Count != 0)
		{
			PersistentPlayer p = randomPlayersCopy[0];
			yield return new WaitUntil(() => p.connectionToClient.isReady);
			
			//select a number of cards based on how many players have yet to get their cards
			for(int cardsToReveal = randomCards.Count / randomPlayersCopy.Count; cardsToReveal > 0; cardsToReveal--)
			{
				randomCards[0].TargetFlip(p.connectionToClient);
				randomCards.RemoveAt(0);
			}

			randomPlayersCopy.Remove(p);
		}
	}
}
