using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WinLoose : MonoBehaviour
{
    public static WinLoose instance;

    CanvasGroup canvasGroup;
    CanvasGroup inputFieldGroup;

    Text WinnerName;
    Text[] table = new Text[10];

    private void Awake()
    {
        if (instance == null)
            instance = this;

        canvasGroup = GetComponent<CanvasGroup>();
        inputFieldGroup = transform.GetChild(3).GetComponent<CanvasGroup>();
        WinnerName = transform.GetChild(3).GetChild(1).GetComponent<Text>();
        for (int i = 0; i < 10; i++)
        {
            table[i] = transform.GetChild(2).GetChild(i).GetComponent<Text>();
        }
    }


    void LoadHighscore(List<HighscoreData> data)
    {
        if (data != null)
        {
            SortHighscore(data);

            //set top 10 to table
            for (int i = 0; i < 10; i++)
            {
                table[i].text = i + 1 + " | " + data[i].time + " | " + data[i].name;
            }
        }
        else
        {

        }
    }

    void SortHighscore(List<HighscoreData> data)
    {
        //simple bubble sort
        for (int i = 0; i < data.Count; i++)
        {
            for (int j = i + 1; j < data.Count; j++)
            {
                if (data[j].time < data[i].time)
                {
                    // Swap
                    HighscoreData tmp = data[i];
                    data[i] = data[j];
                    data[j] = tmp;
                }
            }
        }
    }
    public static void LoadHighscore_static(List<HighscoreData> data)
    {
        instance.LoadHighscore(data);
    }


    void setAlphaPanel(bool doAppear)
    {
        if (doAppear)
        {
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        else
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
    public static void setAlphaPanel_static(bool doAppear)
    {
        instance.setAlphaPanel(doAppear);
    }


    void setAlphaInputField(bool doAppear)
    {
        if (doAppear)
        {
            inputFieldGroup.alpha = 1;
            inputFieldGroup.blocksRaycasts = true;
            inputFieldGroup.interactable = true;
        }
        else
        {
            inputFieldGroup.alpha = 0;
            inputFieldGroup.blocksRaycasts = false;
            inputFieldGroup.interactable = false;
        }
    }
    public static void setAlphaInputField_static(bool doAppear)
        {
            instance.setAlphaInputField(doAppear);
        }


    string getInputText()
    {
        return inputFieldGroup.transform.GetChild(2).GetComponent<Text>().text; //2 cause caret generates
    }

    public static string getInputText_static()
    {
        return instance.getInputText();
    }

    void clearInputText()
    {
        inputFieldGroup.transform.GetChild(1).GetComponent<Text>().text = "AAA";
    }

    public static void clearInputText_static()
    {
        instance.clearInputText();
    }


    //I decide to manualy set button events cause i don't have solid structure and want to do it easier
    public void OnResetClick()
    {
        MainGameManager.RestartGame_static();
    }

    public void OnExitClick()
    {
        MainGameManager.ExitGame_Static();
    }    
}
