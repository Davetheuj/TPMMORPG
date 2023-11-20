using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Output : MonoBehaviour
{

    public static Output Instance { get; private set; }

    public GameObject consolePanel;
    public GameObject textPrefab;
    public GameObject contentPanel;
    public Scrollbar scrollBar;

    private void Awake()
    {
#if DEDICATED_SERVER
Destroy(this);
#else
        // If there is an instance, and it's not me, delete myself.

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

    public void Log(string logMessage)
    {
        var time = DateTime.UtcNow;
        textPrefab.GetComponent<TMP_Text>().text = $"[{time.Hour}:{time.Minute}] {logMessage}";
        GameObject newText = Instantiate(textPrefab, contentPanel.transform);
        contentPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(contentPanel.GetComponent<RectTransform>().anchoredPosition.x, contentPanel.GetComponent<RectTransform>().sizeDelta.y / 2); 
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel.transform as RectTransform);

    }

   
}
