using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpText : MonoBehaviour
{
    public static PopUpText instance; //to get from each part of application

    public GameObject textPrefab;
    public float speed = 50;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    IEnumerator PopText(GameObject go)
    {
        float d = 1f;
        //could be Lerp. Took my old code. From pluses - no flaw in form of attitude to time
        for (float ft = 1f; ft >= 0; ft -= 0.02f)
        {
            d += 0.02f;


            go.GetComponent<RectTransform>().localScale = new Vector3(d, d, d);

            Text meshPro = go.GetComponent<Text>();


            meshPro.color = new Color32(255, 255, 255, (byte)(ft * 255));


            yield return null;

        }

        Destroy(go);
    }


    //simple Instantiating of prefab
    void NewPopUp(string str)
    {
        GameObject go = Instantiate(textPrefab, this.transform);
        go.GetComponent<Text>().text = str;
        go.GetComponent<RectTransform>().localPosition = new Vector3(0, -100, 0);
        go.transform.SetParent(gameObject.transform);
        StartCoroutine(PopText(go));
    }

    public static void NewPopUp_Static(string str)
    {
        instance.NewPopUp(str);
    }
}
