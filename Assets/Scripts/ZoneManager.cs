using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ZoneManager : NetworkBehaviour
{

    public Zone[,] zones;
    public int networkObjectCounter = 0;
    
    public static ZoneManager Instance { get; private set; }

    private void Awake()
    {

        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;

            int highestX =0;
            int highestY =0;
            Zone[] tempZones = GetComponentsInChildren<Zone>();
            foreach (Zone zone in tempZones)
            {
                if(zone.posX > highestX)
                {
                    highestX = zone.posX;
                }
                if (zone.posY > highestY)
                {
                    highestY = zone.posY;
                }
            }
            zones = new Zone[highestX+1, highestY+1];
            foreach (Zone zone in tempZones)
            {
                zones[zone.posX, zone.posY] = zone;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong obj)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            return;
        }
        

    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestZoneChangeServerRpc(int posX, int posY, ulong clientId, bool isFirstSpawn = false)
    {
        Debug.Log("ZoneChange Requested");

        if(
            (posX > zones.GetLength(0) - 1) || 
            (posX < 0) || 
            (posY > zones.GetLength(1) - 1) ||
            (posY < 0))
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().OutputToConsoleClientRPC("You are unable to move into the requested zone!", CreateClientRpcParams(clientId));
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkObject>().TrySetParent(zones[posX, posY].transform))
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().OutputToConsoleClientRPC("You are unable to move into the requested zone!", CreateClientRpcParams(clientId));
            return;
        }

        Player requestedPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
        int initialX = requestedPlayer.posX;
        int initialY = requestedPlayer.posY;
       

        //NOTIFY PLAYERS IN THE OLD ZONE THE PLAYER HAS LEFT
        List<ulong> playersToNotifyLeft = new List<ulong>();
        foreach (Player player in zones[initialX, initialY].GetComponentsInChildren<Player>())
        {
            if(player.OwnerClientId == clientId)
            {
                //Debug.Log("Owner Client ID is same as clientId");
                continue;
            }
           
            if (requestedPlayer.NetworkObject.IsNetworkVisibleTo(player.OwnerClientId))
            {
                Debug.Log($"Hiding {player.username.Value} from {player.username.Value}.");
                player.NetworkObject.NetworkHide(clientId);
                requestedPlayer.NetworkObject.NetworkHide(player.OwnerClientId);
                playersToNotifyLeft.Add(player.OwnerClientId);
            }
        }

        //SHOW NETWORK OBJECTS IN THE NEW ZONE TO THE PLAYER THAT HAS JOINED
        int networkObjectCounter = 0;
        foreach (NetworkObject no in zones[posX, posY].GetComponentsInChildren<NetworkObject>())
        {
            Debug.Log(no.name);
            if (!no.IsNetworkVisibleTo(clientId))
            {
                networkObjectCounter++;
                no.NetworkShow(clientId);
            }
        }

            //HIDE NETWORK OBJECTS IN THE OLD ZONE TO THE PLAYER THAT HAS LEFT
        //if (!isFirstSpawn)
        //{
        //    Debug.Log("Hiding NetworkObjects in old zone");
        //    foreach (Player player in zones[initialX, initialY].GetComponentsInChildren<Player>())
        //    {
        //        if (player.OwnerClientId == clientId)
        //        {
        //            Debug.Log("Owner Client ID is same as clientId");
        //            continue;
        //        }
        //        Debug.Log(player.name);
        //        if (player.NetworkObject.IsNetworkVisibleTo(clientId))
        //        {
        //            player.NetworkObject.NetworkHide(clientId);
        //        }
        //    }
        //}


        //MAKE NEW PLAYER VISIBLE TO EXISTING PLAYERS IN THE ZONE
        foreach (Player player in zones[posX, posY].GetComponentsInChildren<Player>())
        {
            if (player.OwnerClientId == clientId)
            {
                continue;
            }
            requestedPlayer.NetworkObject.NetworkShow(player.OwnerClientId);
        }



        RemovePlayerFromRoomClientRpc(requestedPlayer.username.Value.ToString(), zones[initialX,initialY].zoneName, CreateClientRpcParams(playersToNotifyLeft.ToArray()));

        requestedPlayer.posX = posX;
        requestedPlayer.posY = posY;
        OutputZoneInformationClientRpc(networkObjectCounter, posX, posY, CreateClientRpcParams(clientId));
    }

    public void OutputZoneInformation()
    {
        string output = "";
        output += zones[PlayerManager.Instance.localPlayer.posX, PlayerManager.Instance.localPlayer.posY].description;
        output += "\n----------PLAYERS----------";
        foreach (Player player in zones[PlayerManager.Instance.localPlayer.posX, PlayerManager.Instance.localPlayer.posY].GetComponentsInChildren<Player>())
        {
            if (player.username != PlayerManager.Instance.localPlayer.username)
            {
                output += $"\n{player.username.Value}";
            }
        }
        output += $"\n\n----------ITEMS----------";
        output += $"\n\n----------ROUTES----------";
        Output.Instance.Log(output);

    }

    [ClientRpc]
    public void OutputZoneInformationClientRpc(int networkObjectCounter, int posX, int posY, ClientRpcParams clientRpcParams)
    {
        PlayerManager.Instance.localPlayer.posX = posX;
        PlayerManager.Instance.localPlayer.posY = posY;

        Debug.Log($"OutputZoneInformationClientRpc, networkObject counter = {networkObjectCounter}");
        StartCoroutine(WaitForNetworkObjectSyncAndOutputZoneInformation(networkObjectCounter));
    }

    [ClientRpc]
    public void RemovePlayerFromRoomClientRpc(string playerLeftName, string zoneNameLeft, ClientRpcParams clientRpcParams)
    {
        Output.Instance.Log($"{playerLeftName} has exited the zone.");
    }

    private IEnumerator WaitForNetworkObjectSyncAndOutputZoneInformation(int networkObjectCounter)
    {
        this.networkObjectCounter = 0;
        float timer = 0;
        float maxTime = 1;
        while(this.networkObjectCounter < networkObjectCounter && timer < maxTime)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        Debug.Log($"NetworkObjectCondition: {networkObjectCounter < this.networkObjectCounter}");
        Debug.Log($"TimerCondition: {timer < maxTime}");
        OutputZoneInformation();

    }

    public ClientRpcParams CreateClientRpcParams(ulong clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        return clientRpcParams;
    }

    public ClientRpcParams CreateClientRpcParams(ulong[] clientIds)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = clientIds
            }
        };
        return clientRpcParams;
    }

}
