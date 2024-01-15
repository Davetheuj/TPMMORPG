using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Zone : MonoBehaviour
{
    public string zoneName;
    public string description;
    public int posX;
    public int posY;

#if !DEDICATED_SERVER
    public Sprite zoneImage;
#endif


public GameObject[] networkPrefabs;

#if DEDICATED_SERVER
    
    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;     
    }

    private void OnServerStarted()
    {
        foreach (GameObject prefab in networkPrefabs)
        {
            GameObject obj = Instantiate(prefab);
            obj.GetComponent<NetworkObject>().Spawn();
            obj.GetComponent<NetworkObject>().TrySetParent(gameObject.transform);
        }
    }

#endif

}
