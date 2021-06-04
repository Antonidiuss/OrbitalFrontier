using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DrawLine : MonoBehaviour 
{
    public GameObject Line;
    TrailRenderer trailRenderer;

    //origin is vector3 due to deleting and we save only position
    //destination could move, so we need gameobject
    public Vector3 origin;
    public GameObject destination;
    public Vector3 center;

    Vector3 centerOfSphere;
    float radiusOfTheSphere;

    public int TrailTimeToLive = 4;
    public float targetedTimeOfLine = 4;
    public float biasOfTime = 2;
    float journeyTime = 6; //How many seconds animation lasts
    float startTime;

    Vector3 randomBias; // a lil bit random for line so it could deviate from sphere

    //I need somehow to return control over camera after animation ends. I decide use simple delegate
    public delegate void functionWhenEnds();
    public functionWhenEnds function; 



    private bool isFuncOfReturnCalled = false; //Do not allows function to be called more than once.
    void Awake()
    {
        trailRenderer = Line.GetComponent<TrailRenderer>();

    }

    public void SetSphereParameters(Vector3 z, float r)
    {
        centerOfSphere = z;
        radiusOfTheSphere = r;
    }

    public GameObject GetLine() // for animation
    {
        return Line;
    }
    public void newRender(Vector3 origin, GameObject destination, Action act)
    {
        this.origin = origin;
        this.destination = destination;
        startTime = Time.unscaledTime;


        function = new functionWhenEnds(act);
        isFuncOfReturnCalled = false;

        SetLinePosition(origin);
        randomBias = new Vector3(Random.insideUnitCircle.x, Random.insideUnitCircle.y, Random.insideUnitCircle.x);

        //decide add random to time of animation cause it is more cooler than awaiting right time in case of random ways; 
        journeyTime = targetedTimeOfLine + Random.Range(0, biasOfTime*2) - biasOfTime;

    }

    //this little trick was intended to fix bug when first movement is ilustrated, but it doesn't work.
    public void SetLinePosition(Vector3 vector3)
    {
        trailRenderer.time = 0;
        Line.transform.position = vector3;
        trailRenderer.time = TrailTimeToLive;
    }


    //Я могу сделать алгоритм, что выполняет ТЗ точь в точь (хотя этот тоже идет сквозь центры мишеней) и тоже может считаться правильным 
    //"Для облегчения поиска следующей цели следует указать путь к ней кривой(произвольного цвета) 
    //которая проходит через центры мишеней." Это нужно сделать граф, по нему алгоритм поиска пути, потом сделать прыжки трейла по каждой вершине. 
    //Но я решил, что раз это не настоящее ТЗ (а еще и тестовое), то могу отклониться в угоду варианта, что немного проще в исполнении. 
    void Update()
    {
        if (destination != null)
        {
            //declare values
            Vector3 destinationPos = destination.transform.position;

            //got center
            center = (origin + destinationPos) * 0.5f;  //should minus radius

            //these three lines calculating bias from "forward line from A to B" to "work on sphere coordinates" + adding some Random  
            Vector3 vectorFromCenterSphToMidWay = center - centerOfSphere;
            float bias = radiusOfTheSphere - vectorFromCenterSphToMidWay.magnitude;
            Vector3 vectorBias = center - vectorFromCenterSphToMidWay.normalized * bias / 2 + randomBias * radiusOfTheSphere;
            //Vector3 vectorBias = center;


            //Interpolate over the arc relative to center
            Vector3 origRelCenter = origin - vectorBias;
            Vector3 destRelCenter = destinationPos - vectorBias;

            //The fraction of the animation that has happened so far is
            // equal to the elapsed time divided by the desired time for
            // the total journey
            float fracComplete = (Time.unscaledTime - startTime) / journeyTime;

            //slerp = lerp for spheres
            Vector3 pointOriginLongLine = Vector3.Slerp(origRelCenter, destRelCenter, fracComplete);

            Line.transform.position = pointOriginLongLine + vectorBias;

            if (!isFuncOfReturnCalled && Time.unscaledTime - startTime > journeyTime * 0.95) //double "&" allways for optimization if no init in "if"
            {
                function(); //Return camera control
                isFuncOfReturnCalled = true;
            }
        }
    }
}
