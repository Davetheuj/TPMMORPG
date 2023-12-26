using Unity.Netcode;
using UnityEngine;

public class InputManager : MonoBehaviour
{

    public TMPro.TMP_InputField inputField;

    // Start is called before the first frame update
    void Start()
    {
#if DEDICATED_SERVER
        Destroy(this);
#endif
        inputField.Select();
        inputField.onFocusSelectAll = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (inputField.text.Length < 1)
            {
                inputField.ActivateInputField();
                return;
            }
            ProcessAction(inputField.text);
            inputField.ActivateInputField();
        }      
    }

    void ProcessAction(string action)
    {
        action = action.Trim();
        string actionL = action.ToLower();

        if (actionL.StartsWith("look"))
        {
            LookAction(actionL);
        }
        else if (actionL.StartsWith("walk"))
        {
            WalkAction(actionL);
        }
        else //Chat to be sent in game
        {
            Output.Instance.Log(inputField.text);
            inputField.text = "";
        }       
    }

    private void LookAction(string action)
    {
        if (action.Contains("here"))
        {
            PlayerManager.Instance.zoneManager.OutputZoneInformation();
        }
        else
        {
            Output.Instance.Log("Invalid input - did you mean 'Look here' or 'Look North'?");
        }
        inputField.text = "";
    }

    private void WalkAction(string action)
    {

        Player localPlayer = PlayerManager.Instance.localPlayer;
        if (action.Contains("east"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX + 1, localPlayer.posY, NetworkManager.Singleton.LocalClientId);
        }
       else if (action.Contains("west"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX - 1, localPlayer.posY, NetworkManager.Singleton.LocalClientId);
        }
        else if (action.Contains("north"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX, localPlayer.posY + 1, NetworkManager.Singleton.LocalClientId);
        }
        else if (action.Contains("south"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX, localPlayer.posY - 1, NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            Output.Instance.Log("Invalid input - did you mean 'Walk North' or 'Walk East?");
        }
        inputField.text = "";
    }



}
