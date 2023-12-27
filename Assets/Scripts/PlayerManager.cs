using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    public bool isAuthenticated;
    public Player localPlayer;


    private void Awake()
    {
#if DEDICATED_SERVER
Destroy(this);
#else
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;

        }
#endif
    }

    private void Start()
    {
       //localPlayer.transform.parent = ZoneManager.Instance.zones[localPlayer.posX, localPlayer.posY].gameObject.transform;
    }

}
