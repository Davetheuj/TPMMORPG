using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Networking;

#if DEDICATED_SERVER
using Unity.Services.Core;
using Unity.Services.Multiplay;
#endif

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }



    private float autoAllocateTimer = 999999999f;
    private bool alreadyAutoAllocated = false;
#if DEDICATED_SERVER
    private static IServerQueryHandler serverQueryHandler;
#endif

    public GameObject playerPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
#if DEDICATED_SERVER
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        InitializeUnityAuthentication();
#else
        Destroy(this);
#endif
    }

    private async void InitializeUnityAuthentication()
    {
#if UNITY_EDITOR || LOCAL_CLIENT
        NetworkManager.Singleton.StartServer();
    }
#elif DEDICATED_SERVER && !UNITY_EDITOR
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.Log("!!!INITIALIZING UNITY SERVICES!!!");
            InitializationOptions initializationOptions = new InitializationOptions();
            await UnityServices.InitializeAsync(initializationOptions);

            MultiplayEventCallbacks multiplayEventCallbacks = new MultiplayEventCallbacks();
            multiplayEventCallbacks.Allocate += MultiplayEventCallbacks_Allocate;
            multiplayEventCallbacks.Deallocate += MultiplayEventCallbacks_Deallocate;
            multiplayEventCallbacks.Error += MultiplayEventCallbacks_Error;
            multiplayEventCallbacks.SubscriptionStateChanged += MultiplayEventCallbacks_SubscriptionStateChanged;
            IServerEvents serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(multiplayEventCallbacks);

            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(10, "Vale of Dreams", "Classic", "1", "Valgnar");

            var serverConfig = MultiplayService.Instance.ServerConfig;
            StartAllocation();

            if (serverConfig.AllocationId != "")
            {
                //Already allocated
                MultiplayEventCallbacks_Allocate(new MultiplayAllocation("", serverConfig.ServerId, serverConfig.AllocationId));

            }
        }
        else
        {
            Debug.Log("!!!UNITY SERVICES ALREADY INITIALIZED!!!");
            var serverConfig = MultiplayService.Instance.ServerConfig;
            if (serverConfig.AllocationId != "")
            {
                //Already allocated
                MultiplayEventCallbacks_Allocate(new MultiplayAllocation("", serverConfig.ServerId, serverConfig.AllocationId));
            }
        }

}
#else
}
#endif
    private void StartAllocation()
    {
        Debug.Log("Calling request to create allocation");
        string keyId = "599988c3-846a-46e1-9710-3271de086ebc";
        string keySecret = "Oo0gxIwE6CdMh4jq_ayvzn5UWXI7bYXT";
        byte[] keyByteArray = Encoding.UTF8.GetBytes(keyId + ":" + keySecret);
        string keyBase64 = Convert.ToBase64String(keyByteArray);

        string projectId = "fa02b1e6-8039-4b35-b9ba-3d5b628fa004";
        string environmentId = "639afd8d-8ead-486e-b1c7-b6d02491f7e5";
        string fleetId = "09280211-6646-4aeb-b661-6320b3729111";

        string url = $"https://services.api.unity.com/auth/v1/token-exchange?projectId={projectId}&environmentId={environmentId}";

        string bearerToken = "";

        string jsonRequestBody = JsonUtility.ToJson(new TokenExchangeRequest
        {
            scopes = new[] { "multiplay.allocations.create" },
        });

        WebRequests.PostJson(url,
        (UnityWebRequest unityWebRequest) => {
            unityWebRequest.SetRequestHeader("Authorization", "Basic " + keyBase64);
        },
        jsonRequestBody,
        (string error) => {
            Debug.Log("Error Bearer Token Request: " + error);
        },
        (string json) =>
        {
            Debug.Log("Successful Bearer Token Request: " + json);
            TokenExchangeResponse tokenExchangeResponse = JsonConvert.DeserializeObject<TokenExchangeResponse>(json);
            bearerToken = tokenExchangeResponse.accessToken;

            string url2 = $"https://multiplay.services.api.unity.com/v1/allocations/projects/{projectId}/environments/{environmentId}/fleets/{fleetId}/allocations";
            Debug.Log("Starting allocation request");
            WebRequests.PostJson(url2,
            (UnityWebRequest unityWebRequest2) => {
                unityWebRequest2.SetRequestHeader("Authorization", "Bearer " + bearerToken);
            },
            JsonConvert.SerializeObject(new QueueAllocationRequest
            {
                allocationId = "09d523cf-0284-4940-9624-f0fa3437ae3e",
                restart = false,
                buildConfigurationId = 1247807,
                regionId = "cd83869f-96e0-40a6-9f54-2740c9b938f2",
                payload = ""
            }),
            (string error) => {
                Debug.Log("Error Allocation Request: " + error);
                Debug.Log(JsonConvert.SerializeObject(new QueueAllocationRequest
                {
                    allocationId = "09d523cf-0284-4940-9624-f0fa3437ae3e",
                    buildConfigurationId = 1247807,
                    regionId = "3425325",
                }));
            },
            (string json) => {
                Debug.Log("Success Allocation Request: " + json);
            }
            );
        });




    }
#if DEDICATED_SERVER
    private void MultiplayEventCallbacks_SubscriptionStateChanged(MultiplayServerSubscriptionState obj)
    {
        Debug.Log("!!!MPEC_SubStateChanged!!!");
    }

    private void MultiplayEventCallbacks_Error(MultiplayError obj)
    {
        Debug.Log("!!!MPEC_Error!!!");
    }

    private void MultiplayEventCallbacks_Deallocate(MultiplayDeallocation obj)
    {
        Debug.Log("!!!MPEC_Dealloc!!!");
    }

    private async void MultiplayEventCallbacks_Allocate(MultiplayAllocation obj)
    {
        Debug.Log("!!!MPEC_Allocate!!!");

        if (alreadyAutoAllocated)
        {
            Debug.Log("!!!Already auto allocated!!!!");
            return;
        }

        alreadyAutoAllocated = true;

        var serverConfig = MultiplayService.Instance.ServerConfig;
        Debug.Log($"Server ID: {serverConfig.ServerId}");
        Debug.Log($"AllocationID: {serverConfig.AllocationId}");
        Debug.Log($"Port: {serverConfig.Port}");
        Debug.Log($"QueryPort: {serverConfig.QueryPort}");
        Debug.Log($"Log Directory: {serverConfig.ServerLogDirectory}");
        string ipv4Address = "0.0.0.0";
        ushort port = serverConfig.Port;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipv4Address, port, "0.0.0.0");
       // NetworkManager.Singleton.GetComponent<UnityTransport>().so
        NetworkManager.Singleton.StartServer();

        await MultiplayService.Instance.ReadyServerForPlayersAsync();

    }

    private void OnTransportFailure()
    {
        Debug.Log("Transport Failure in the Network Manager");
    }

    private void OnClientDisconnected(ulong obj)
    {
        Debug.Log("Client Disconnected! " + obj);
    }

    private void OnClientConnected(ulong obj)
    {
        Debug.Log("Client Connected! " + obj);
        GameObject playerObj = Instantiate(playerPrefab);
        Player player = playerObj.GetComponent<Player>();
        playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(obj);
        playerObj.GetComponent<NetworkObject>().NetworkShow(obj);
        player.currentHealth.Value = 100;
        player.maxHealth.Value = 100;
        player.username.Value = $"Player {obj}";
        // player.transform.parent = ZoneManager.Instance.zones[player.posX, player.posY].transform;
    }
#endif

#if DEDICATED_SERVER
    private void Update()
    {
        if(serverQueryHandler != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                serverQueryHandler.CurrentPlayers = (ushort)NetworkManager.Singleton.ConnectedClientsIds.Count;
            }
            serverQueryHandler.UpdateServerCheck();
        }
    }


    
    
#endif
}

[Serializable]
 public class TokenExchangeResponse
{
    public string accessToken;
}


[Serializable]
public class TokenExchangeRequest
{
    public string[] scopes;
}

[Serializable]
public class QueueAllocationRequest
{
    public string allocationId;
    public int buildConfigurationId;
    public string payload;
    public string regionId;
    public bool restart;
}
