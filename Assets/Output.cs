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
        // If there is an instance, and it's not me, delete myself.

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
        StartCoroutine(Test());
    }

    private void Update()
    {
        
            //scrollBar.value = 0;
        
    }
    public void Log(string logMessage)
    {
        textPrefab.GetComponent<TMP_Text>().text = logMessage;
        GameObject newText = Instantiate(textPrefab, contentPanel.transform);

        //contentPanel.GetComponent<ContentSizeFitter>().SetLayoutVertical();
        //newText.GetComponent<TMP_Text>().text = logMessage;
        //newText.transform.SetParent(contentPanel.transform);
        //RectTransform rectTransform = contentPanel.GetComponent<RectTransform>();
        //rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)1, LayoutUtility.GetPreferredSize(rectTransform, 1));

        //Canvas.ForceUpdateCanvases();
        //contentPanel.transform.GetComponent<VerticalLayoutGroup>().enabled = false;
        //contentPanel.transform.GetComponent<VerticalLayoutGroup>().enabled = true;
        //scrollBar.value = 0;

        contentPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(contentPanel.GetComponent<RectTransform>().anchoredPosition.x, contentPanel.GetComponent<RectTransform>().sizeDelta.y / 2); 
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel.transform as RectTransform);

        //LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel.transform as RectTransform);
        //rt.offsetMin = rt.anchorMin - new Vector2(rt.anchorMin.x, (DataManager.CurrentPlayer.FriendRequestsReceived.Count - 10) * 180);
    }

    public IEnumerator Test()
    {
        while (true)
        {
            Debug.Log("Printing!");
            Instance.Log(DateTime.Now.ToString());

            yield return new WaitForSeconds(1);
        }
    }
}
