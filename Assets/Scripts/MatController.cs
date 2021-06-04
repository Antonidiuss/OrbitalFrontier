using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatController : MonoBehaviour
{
    //for Singletone
    static MatController instance;


    //Materials for targets. 
    //midIn - center, not outlined
    //midOut - center, outlined
    //sideIn - side, not outlined
    //sideOut - side, outlined

    Material midIn, midOut, sideIn, sideOut;


    private void Awake()
    {
        //pattern Singleton
        if (instance == null)
            instance = this;

        //initiate material
        midIn = Resources.Load<Material>("TargetMatInside");
        midOut = Resources.Load<Material>("OutlineMid");
        sideIn = Resources.Load<Material>("TargetOutMat");
        sideOut = Resources.Load<Material>("OutlineOut");
    }


    Material GetMaterial(bool isMid, bool isOutlined)
    {
        //minimum 3 ifs
        if(isMid)
        {
            if(isOutlined)
                  return midOut;
            return midIn;
        }
        if (isOutlined)
             return sideOut;
        return sideIn;
    }

    //static method
    public static Material GetMaterial_Static(bool isMid, bool isOutlined)
    {
        return instance.GetMaterial(isMid, isOutlined);
    }
}
