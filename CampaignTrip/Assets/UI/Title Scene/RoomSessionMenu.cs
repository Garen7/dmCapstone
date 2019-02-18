﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RoomSessionMenu : NavigationMenu
{
    [HideInInspector] public string roomName;
	
    public GameObject rootPlayerPanel;
    public Image characterImage;
    public List<CharacterData> characters;
    public Text className;
	public Text flavorText;
    public Text roomNameText;
	private int characterIndex = 0;

    public override void NavigateTo()
    {
        base.NavigateTo();

        roomNameText.text = roomName;
    }
    
    public override void NavigateFrom()
	{
		base.NavigateFrom();
	}

	#region Buttons

	public void BackButtonClicked()
	{
        if (NetworkWrapper.discovery.isServer)
        {
            NetworkWrapper.discovery.StopBroadcast();
            NetworkWrapper.manager.StopHost();

            //TODO: kick players back to the first menu

            TitleUIManager.Navigate_HostJoinRoomMenu();
        }
        else
        {
            StartCoroutine(ClientLeave());
        }
	}

    public void ReadyButtonClicked()
	{
		Player.localAuthority.isReady = !Player.localAuthority.isReady;
        Player.localAuthority.CmdUpdatePanel(characterIndex, Player.localAuthority.isReady);
		TryStart();
    }

	public void ClassCycleLeftButtonClicked()
	{
		if (characterIndex == 0)
			characterIndex = characters.Count - 1;
		else
			characterIndex--;

		UpdateCharacterPanel();
	}

	public void ClassCycleRightButtonClicked()
	{
		if (characterIndex == (characters.Count - 1))
			characterIndex = 0;
		else
			characterIndex++;

		UpdateCharacterPanel();
	}

	#endregion

	/// <summary>
	/// Changes the icon and description to match the character data
	/// </summary>
	public void UpdateCharacterPanel()
	{
        className.text = characters[characterIndex].name;
		flavorText.text = characters[characterIndex].flavorText;
		characterImage.sprite = characters[characterIndex].icon;

        Player.localAuthority.CmdUpdatePanel(characterIndex, Player.localAuthority.isReady);
	}

	private void TryStart()
	{
		foreach (Player p in Player.players)
			if (!p.isReady)
				return; //someones not ready so don't start
		NetworkWrapper.manager.ServerChangeScene("mainBattleScene");
		CmdRelayStart();
	}

	[Command]
	private void CmdRelayStart()
	{
		RpcRelayStart();
	}

	[ClientRpc]
	private void RpcRelayStart()
	{
		ClientScene.Ready(Player.localAuthority.networkIdentity.connectionToServer);
	}

	private IEnumerator ClientLeave()
	{
		Player.localAuthority.CmdDisconnect();

		//Need to wait a little before disconnecting so we can call the server Command method.
		yield return new WaitForSeconds(0.2f);

		NetworkWrapper.manager.StopClient();
		TitleUIManager.Navigate_HostJoinRoomMenu();
	}
}
