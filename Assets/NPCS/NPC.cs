using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NPC : Character
{

    public string npcName;
    
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>();


  public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"{npcName} increased networkObjectCounter");
            ZoneManager.Instance.networkObjectCounter += 1;
        }
    }
}
