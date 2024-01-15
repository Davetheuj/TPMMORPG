using Assets.Scripts;
using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking;
using UnityEngine;


public class Player : Character
{
    public NetworkVariable<FixedString128Bytes> username = new NetworkVariable<FixedString128Bytes>();
    public int posX;
    public int posY;

    public NetworkVariable<int> maxHealth = new NetworkVariable<int>();
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>();


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            PlayerManager.Instance.localPlayer = this;
            ZoneManager.Instance.RequestZoneChangeServerRpc(posX, posY, NetworkManager.Singleton.LocalClientId, true);
        }
        else if (!NetworkManager.Singleton.IsServer)
        {
            ZoneManager.Instance.networkObjectCounter += 1;
            Debug.Log($"{username.Value} increased networkObjectCounter");
        }
    }


    [ClientRpc]
    public void OutputToConsoleClientRPC(string textToOutput, ClientRpcParams clientRpcParams)
    {
        Output.Instance.Log(textToOutput);
    }

    [ClientRpc]
    public void OutputToConsoleClientRPC(string textToOutput)
    {
        Output.Instance.Log(textToOutput);
    }


    [ServerRpc(RequireOwnership = false)]
    public void DealDamageServerRPC(ulong clientIdFrom)
    {
        int damage = UnityEngine.Random.Range(0, 10);
        currentHealth.Value -= damage;
        OutputToConsoleClientRPC($"{username.Value} has taken {damage} damage!");
    }
}
