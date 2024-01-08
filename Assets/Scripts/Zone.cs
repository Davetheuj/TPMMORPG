using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Zone : NetworkBehaviour
{
    public string zoneName;
    public string description;
    public int posX;
    public int posY;
#if !DEDICATED_SERVER
    public Sprite zoneImage;
#endif
}
