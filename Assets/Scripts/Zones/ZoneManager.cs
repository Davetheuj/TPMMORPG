using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ZoneManager : NetworkBehaviour
{

    public Zone[,] zones;
    public int networkObjectCounter = 0;
    public GameObject zoneImage;
    
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
        if (zones[posX, posY] == null)
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().OutputToConsoleClientRPC("You are unable to move into the requested zone!", CreateClientRpcParams(clientId));
            return;
        }

       else if (!NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkObject>().TrySetParent(zones[posX, posY].transform))
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().OutputToConsoleClientRPC("You are unable to move into the requested zone!", CreateClientRpcParams(clientId));
            return;
        }

        
        Player requestedPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
        int initialX = requestedPlayer.posX;
        int initialY = requestedPlayer.posY;

        //Setting the directional text for notifications
        string directionFrom = "";
        string directionTo = "";
        
        if(initialX > posX)
        {
            directionFrom = "East";
            directionTo = "West";
        }
        else if( initialX < posX)
        {
            directionFrom = "West";
            directionTo = "East";
        }
        else if (initialY < posY)
        {
            directionFrom = "South";
            directionTo = "North";
        }
        else 
        {
            directionFrom = "North";
            directionTo = "South";
        }



        //Hide Npcs and Objects in the old zone from the local client
        foreach (InteractableNetworkedObject no in zones[initialX, initialY].GetComponentsInChildren<InteractableNetworkedObject>())
        {
            if (no.OwnerClientId == clientId)
            {
                continue;
            }
            if (no.NetworkObject.IsNetworkVisibleTo(clientId))
            {
                Debug.Log($"Hiding {no.name} from LocalCLient({clientId})");
                no.NetworkObject.NetworkHide(clientId);
            }
        }

            //SHOW NETWORK OBJECTS IN THE NEW ZONE TO THE PLAYER THAT HAS JOINED
            int networkObjectCounter = 0;
        foreach (InteractableNetworkedObject no in zones[posX, posY].GetComponentsInChildren<InteractableNetworkedObject>())
        {
            if (no.NetworkObject.OwnerClientId == clientId)
            {
                continue;
            }
            Debug.Log(no.name);
            if (!no.NetworkObject.IsNetworkVisibleTo(clientId))
            {
                networkObjectCounter++;
                no.NetworkObject.NetworkShow(clientId);
            }
        }
        //NOTIFY PLAYERS IN THE OLD ZONE THE PLAYER HAS LEFT
        List<ulong> playersToNotifyLeft = new List<ulong>();
        foreach (Player player in zones[initialX, initialY].GetComponentsInChildren<Player>())
        {
            if(player.OwnerClientId == clientId)
            {
                continue;
            }

            if (requestedPlayer.NetworkObject.IsNetworkVisibleTo(player.OwnerClientId))
            {
                requestedPlayer.NetworkObject.NetworkHide(player.OwnerClientId); //hides the local player from the other clients
                playersToNotifyLeft.Add(player.OwnerClientId);
            }
        }

        //MAKE NEW PLAYER VISIBLE TO EXISTING PLAYERS IN THE ZONE
        List<ulong> playersToNotifyJoin = new List<ulong>();
        foreach (Player player in zones[posX, posY].GetComponentsInChildren<Player>())
        {
            if (player.OwnerClientId == clientId)
            {
                continue;
            }
            requestedPlayer.NetworkObject.NetworkShow(player.OwnerClientId);
            playersToNotifyJoin.Add(player.OwnerClientId);
        }


        NotifyPlayersOfJoinClientRpc(requestedPlayer.username.Value.ToString(), directionFrom, CreateClientRpcParams(playersToNotifyJoin.ToArray()));
        RemovePlayerFromRoomClientRpc(requestedPlayer.username.Value.ToString(), directionTo, CreateClientRpcParams(playersToNotifyLeft.ToArray()));

        requestedPlayer.posX = posX;
        requestedPlayer.posY = posY;
        OutputZoneInformationClientRpc(networkObjectCounter, posX, posY, CreateClientRpcParams(clientId));
    }

    [ClientRpc]
    private void NotifyPlayersOfJoinClientRpc(string playerJoinName, string direction, ClientRpcParams clientRpcParams)
    {
        Output.Instance.Log($"{playerJoinName} has entered the zone from the {direction}.");
    }

    public void OutputCurrentPlayerZoneInformation()
    {
#if !DEDICATED_SERVER
        zoneImage.GetComponent<Image>().sprite = zones[PlayerManager.Instance.localPlayer.posX, PlayerManager.Instance.localPlayer.posY].zoneImage;
#endif

        //Description
        string output = "";
        output += zones[PlayerManager.Instance.localPlayer.posX, PlayerManager.Instance.localPlayer.posY].description;

        //PLAYERS     
        Player[] tempPlayers = zones[PlayerManager.Instance.localPlayer.posX, PlayerManager.Instance.localPlayer.posY].GetComponentsInChildren<Player>();
        if (tempPlayers.Length > 1)
        {
            output += "\n\n----------PLAYERS----------";
            foreach (Player player in tempPlayers)
            {
                if (player.username != PlayerManager.Instance.localPlayer.username)
                {
                    output += $"\n{player.username.Value}";
                }
            }
        }

        //NPCs     
        NPC[] npcs = zones[PlayerManager.Instance.localPlayer.posX, PlayerManager.Instance.localPlayer.posY].GetComponentsInChildren<NPC>();
        if (npcs.Length > 1)
        {
            output += "\n\n----------NPCs----------";
            foreach (NPC npc in npcs)
            {    
               output += $"\n{npc.npcName}";          
            }
        }

        Output.Instance.Log(output);
    }

    public void OutputZoneInformation(int x, int y)
    {
        string output = "";
        if (zones[x, y] == null)
        {
            output += "You see vast swirling mists and the great cosmic dark speckled with hot jewels from the fire of the gods flung out as if from a burning brazier!";
        }
        else if (
            (x > zones.GetLength(0) - 1) ||
            (x < 0) ||
            (y > zones.GetLength(1) - 1) ||
            (y < 0))
        {
            output += "You see vast swirling mists and the great cosmic dark speckled with hot jewels from the fire of the gods flung out as if from a burning brazier!";
        }
        else
        {
            output += zones[x, y].zoneName;
        }
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
    public void RemovePlayerFromRoomClientRpc(string playerLeftName, string direction, ClientRpcParams clientRpcParams)
    {
        Output.Instance.Log($"{playerLeftName} has exited the zone to the {direction}.");
    }


    [ServerRpc(RequireOwnership = false)]
    public void SendPlayerMessageToZoneServerRpc(string messageToSend, ulong clientId)
    {

        Player sentPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
        int posX = sentPlayer.posX;
        int posY = sentPlayer.posY;

        List<ulong> playersToSendMessage = new List<ulong>();
        foreach (Player player in zones[posX, posY].GetComponentsInChildren<Player>())
        {
            if (player.OwnerClientId == clientId)
            {
                continue;
            }
            playersToSendMessage.Add(player.OwnerClientId);
        }
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().OutputToConsoleClientRPC(messageToSend, CreateClientRpcParams(playersToSendMessage.ToArray()));
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
        OutputCurrentPlayerZoneInformation();

    }

    public Player[] GetPlayersInZone(int x, int y)
    {
        return zones[x, y].GetComponentsInChildren<Player>();
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
