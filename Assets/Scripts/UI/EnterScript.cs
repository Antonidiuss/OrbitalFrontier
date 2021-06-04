using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterScript : MonoBehaviour
{
    bool isStarted = false;

    private void Update()
    {
        if (!isStarted)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Mouse0) || Input.touchCount > 0)
            {
                OnAnyEnter();
                isStarted = true;
            }
        }
    }

    void OnAnyEnter()
    {
        SceneManager.LoadScene("MainScene");
    }
}
