using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking;
using UnityEngine;


public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> username = new NetworkVariable<FixedString128Bytes>("Wistful Whisp");
    public int posX;
    public int posY;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("Network Spawn called for player");
            PlayerManager.Instance.localPlayer = this;
            ZoneManager.Instance.RequestZoneChangeServerRpc(posX, posY, NetworkManager.Singleton.LocalClientId, true);
            //ZoneManager.Instance.RequestZoneInformationServerRpc(PlayerManager.Instance.localPlayer.posX, PlayerManager.Instance.localPlayer.posY, NetworkManager.Singleton.LocalClientId);
        }
        else if (!NetworkManager.Singleton.IsServer)
        {
            ZoneManager.Instance.networkObjectCounter += 1;
        }
    }

    [ClientRpc]
    public void SetPlayerNameClientRPC(string name, ClientRpcParams clientRpcParams)
    {
        this.username.Value = name;
    }

    [ClientRpc]
    public void OutputToConsoleClientRPC(string textToOutput, ClientRpcParams clientRpcParams)
    {
        Output.Instance.Log(textToOutput);
    }

    private void OnTransformParentChanged()
    {
        if (!IsOwner)
        {
            return;
        }

        //PlayerManager.Instance.zoneManager.OutputZoneInformation();
    }

}
