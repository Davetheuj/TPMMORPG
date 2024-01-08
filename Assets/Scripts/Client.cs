using Assets;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Networking;

public class Client : MonoBehaviour
{

    private void Start()
    {
#if DEDICATED_SERVER
        Destroy(this);
#else
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        InitializeConnection();
#endif

    }

    private void OnTransportFailure()
    {
        Debug.Log("Transport Failure in the Network Manager");
        Output.Instance.Log("Transport failure detected!");
    }

    private void OnClientDisconnected(ulong obj)
    {
        Debug.Log("Client Disconnected! " + obj);
    }

    private void OnClientConnected(ulong obj)
    {
        Debug.Log("Client Connected! " + obj);
        Output.Instance.Log("Welcome to Valgnar!");
    }

   
    private void InitializeConnection()
    {
#if LOCAL_CLIENT || UNITY_WEBGL
        NetworkManager.Singleton.StartClient();
#else
        string keyId = "eadd3cca-a705-4336-95be-4f82f5fe6c69";
        string keySecret = "D6U0AqkAXib8C_iW0d-DDSfJ_7zETFJo";
        byte[] keyByteArray = Encoding.UTF8.GetBytes(keyId+":"+keySecret);
        string keyBase64 = Convert.ToBase64String(keyByteArray);

        string projectId = "fa02b1e6-8039-4b35-b9ba-3d5b628fa004";
        string environmentId = "639afd8d-8ead-486e-b1c7-b6d02491f7e5";
        string url = $"https://services.api.unity.com/multiplay/servers/v1/projects/{projectId}/environments/{environmentId}/servers";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Basic " + keyBase64);
        StartCoroutine(GetServer(request, server => {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(server.ip, (ushort)server.port, "0.0.0.0");
            NetworkManager.Singleton.StartClient();
        }));
#endif
    }

    IEnumerator GetServer(UnityWebRequest request, Action<Server> callback = null)
    {
        Output.Instance.Log("Locating Valgnar...");
            // Request and wait for the desired page.
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                Output.Instance.Log("Successful web request.");
                ListServers listServers = JsonUtility.FromJson<ListServers>("{\"serverList\":" + request.downloadHandler.text + "}");
                Debug.Log("Connecting to: " + listServers.serverList[0].ip);
                callback.Invoke(listServers.serverList[0]);
                    break;

                default:
                Output.Instance.Log(request.downloadHandler.error);
                break;
            }
     
    }

}
