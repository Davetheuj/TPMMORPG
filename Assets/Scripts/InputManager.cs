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
            ProcessCommand(inputField.text);
            inputField.ActivateInputField();
        }      
    }

    void ProcessCommand(string command)
    {
        command = command.Trim().ToLower();

        if (command.StartsWith("look"))
        {
            LookCommand(command);
        }
        else if (command.StartsWith("walk"))
        {
            WalkCommand(command);
        }
        else if (command.StartsWith("help"))
        {
            HelpCommand(command);
        }
        else //Chat to be sent in game
        {
            Output.Instance.Log($"<color=#FACA59>[{PlayerManager.Instance.localPlayer.username.Value}] {inputField.text}");
            ZoneManager.Instance.SendPlayerMessageToZoneServerRpc($"[{PlayerManager.Instance.localPlayer.username.Value}] {inputField.text}", NetworkManager.Singleton.LocalClientId);
        }
        inputField.text = "";
    }

    private void LookCommand(string command)
    {
        string output = $"<color=#77FB59>{command}</color>";
        Output.Instance.Log(output);
        Player localPlayer = PlayerManager.Instance.localPlayer;
        if (command.Contains("here"))
        {
            ZoneManager.Instance.OutputCurrentPlayerZoneInformation();
        }
        else if (command.Contains("east"))
        {
            ZoneManager.Instance.OutputZoneInformation(localPlayer.posX+1, localPlayer.posY);
        }
        else if (command.Contains("west"))
        {
            ZoneManager.Instance.OutputZoneInformation(localPlayer.posX - 1, localPlayer.posY);
        }
        else if (command.Contains("north"))
        {
            ZoneManager.Instance.OutputZoneInformation(localPlayer.posX, localPlayer.posY + 1);
        }
        else if (command.Contains("south"))
        {
            ZoneManager.Instance.OutputZoneInformation(localPlayer.posX, localPlayer.posY - 1);
        }
        else
        {
            Output.Instance.Log("Invalid input - did you mean 'Look here' or 'Look North'?");
        }
    }

    private void HelpCommand(string command)
    {
        string output = $"<color=#77FB59>{command}</color>";
        output += "\n----------COMMANDS----------" +
            "\n\n<command [variable]> : description : example" +
            "\n\n<look [direction]> : Obtains zone information in the specified direction, including the current zone with 'here': look here" +
            "\n\n<walk [direction]> : Moves your player in the specified direction : walk north";
        
        Output.Instance.Log(output);
    }

    private void WalkCommand(string command)
    {
        string output = $"<color=#77FB59>{command}</color>";
        Output.Instance.Log(output);
        Player localPlayer = PlayerManager.Instance.localPlayer;
        if (command.Contains("east"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX + 1, localPlayer.posY, NetworkManager.Singleton.LocalClientId);
        }
       else if (command.Contains("west"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX - 1, localPlayer.posY, NetworkManager.Singleton.LocalClientId);
        }
        else if (command.Contains("north"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX, localPlayer.posY + 1, NetworkManager.Singleton.LocalClientId);
        }
        else if (command.Contains("south"))
        {
            ZoneManager.Instance.RequestZoneChangeServerRpc(localPlayer.posX, localPlayer.posY - 1, NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            Output.Instance.Log("Invalid input - did you mean 'Walk North' or 'Walk East?");
        }
    }



}
