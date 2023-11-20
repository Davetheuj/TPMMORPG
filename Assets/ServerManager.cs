using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }



    private float autoAllocateTimer = 999999999f;
    private bool alreadyAutoAllocated = false;
    private static IServerQueryHandler serverQueryHandler;

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

            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(10, "Vale of Dreams", "Classic", "1.0", "Valgnar");

            var serverConfig = MultiplayService.Instance.ServerConfig;
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

    private void MultiplayEventCallbacks_Allocate(MultiplayAllocation obj)
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

        NetworkManager.Singleton.StartServer();

        //await MultiplayService.Instance.ReadyServerForPlayersAsync();


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
    }

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
}
