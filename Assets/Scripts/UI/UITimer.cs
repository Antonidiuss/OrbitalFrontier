using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TimeOut : UnityEvent { }
public class UITimer : MonoBehaviour
{
    [HideInInspector]
    static TimeOut TimeOutEvent = new TimeOut(); //event for MainGame. Should be into Time cause timer ends

    public static UITimer instance;

    float timeStampOfStart = 0;
    float timeUntilEnd = 0;
    bool isTimerActive = false;

    Text textTimerUntil;
    Text textFullTime;

    CanvasGroup canvasGroup;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        init();
    }
    private void init()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        textTimerUntil = transform.GetChild(0).GetComponent<Text>();
        textFullTime = transform.GetChild(1).GetComponent<Text>();
    }

    private void Update()
    {
        if (isTimerActive)
        {
            timeUntilEnd -= Time.deltaTime;
            UpdateText();
            CheckIfTimeEnd();
        }
    }

    void UpdateText()
    {
        textTimerUntil.text = (timeUntilEnd).ToString("#.00");
        textFullTime.text = (Time.time - timeStampOfStart).ToString("#.00");
    }

    void CheckIfTimeEnd()
    {
        if (timeUntilEnd <= 0)
        {
            TimeOutEvent.Invoke();
        }
    }

    void setActive(bool b)
    {
        isTimerActive = b;
    }

    public static void setActive_static(bool b)
    {
        instance.setActive(b);
    }

    float getTime()
    {
        return Time.time - timeStampOfStart;
    }

    public static float getTime_Static()
    {
        return instance.getTime();
    }    


    void SetTime(float biasOfUntil)
    {
        timeStampOfStart = Time.time;
        timeUntilEnd = biasOfUntil;
    }

    public static void SetTime_static(float biasOfUntil)
    {
        instance.SetTime(biasOfUntil);
    }

    void AddTime(float bias)
    {
        timeUntilEnd += bias;
    }

    public static void AddTime_static(float bias)
    {
        instance.AddTime(bias);
    }

    public void setAlpha(bool doAppear)
    {
        if (doAppear)
            canvasGroup.alpha = 1;
        else
            canvasGroup.alpha = 0;
    }

    public static void setAlpha_static(bool doAppear)
    {
        instance.setAlpha(doAppear);
    }

    public static TimeOut getTimeEvent_static()
    {
        return TimeOutEvent;
    }
}
