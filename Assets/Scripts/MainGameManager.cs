using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using csDelaunay;
using System;
using Random = UnityEngine.Random;
using System.IO;

public class onAnyTargetDestroy : UnityEvent<TargetControl> { }



[Serializable]
public class ParametersToJSON
{
    public int countOfTargets;
    public float radius;
    public float minDistance ; 
    public float rotateSpeed;
    public float howManySecondAddToTimer;
}


public class HighscoreToJSON
{
    public List<HighscoreData> data;
}

[Serializable]
public class HighscoreData
{
    public float time;
    public string name;
}


public class MainGameManager : MonoBehaviour
{
    [HideInInspector]
    public static onAnyTargetDestroy onAnyTargetDestroyEvent = new onAnyTargetDestroy();

    //just for reset and exit game
    static MainGameManager instance;

    class point //not a struct cause we will modify parameters
    {
        public Vector3 pos;
        public Vector2f circlePos;
        public Site site;
        public TargetControl targetControl;

        public point(Vector3 pos, TargetControl tc)
        {
            targetControl = tc;
            this.pos = pos;
            circlePos = new Vector2f(pos.x, pos.z);
        }
    }
 


    [Header("Init game parameters")]
    public int countOfTargets = 20;
    public float radius = 20f;
    public float minDistance = 3f; //min distance between targets when generated
    public float rotateSpeed = 2f;
    public float howManySecondAddToTimer = 10f;

    public GameObject Center;
    public Player player;
    public GameObject targetPrefab;


    [Header("Game parameters")]
    public bool isRotating = false;
    [Header("Score")]
    public int Score = 0;


    GameObject guideText;
    DrawLine drawLine;

    //private float timeStampOfStart;
    private float timeStampUntil; //for timer
    private float timeStampStart; //for endgame result
    private float ifTimeStopBias = 0; //When we stop time we are adding bias
    private float RotateDelta => rotateSpeed * Time.deltaTime;
    private bool isLastGameWinned = false;

    List<HighscoreData> highscoreList = new List<HighscoreData>();

    List<point> listOfPoints = new List<point>();
    Dictionary<Vector2f, Site> sites;
    
    point goalTarget;
    Vector3 oldPos; //for animation line from old target to new
    Vector3 previousLineOfSight;


    bool gameIsGo = false;



    private void Awake()
    {
        //init
        if (instance == null)
            instance = this;


        drawLine = GetComponent<DrawLine>();
        guideText = GameObject.Find("GuidingText");
    }

    private void Start()
    {
        UITimer.getTimeEvent_static().AddListener(OnTimeEnd);

        LoadParameters();
        LoadScore();

        drawLine.SetSphereParameters(Center.transform.position, radius);
        onAnyTargetDestroyEvent.AddListener(TargetDestroyed);


        InitGame();
        //Start look into target

    }



    private void FixedUpdate() //fixed cause physics and Time.timescale
    {
        if (isRotating)
            Center.transform.Rotate(Vector3.up * RotateDelta); 
    }



    private void Update()
    {
        //check if aim over target
        if (!gameIsGo)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
            RaycastHit hit;
            
            Physics.Raycast(ray, out hit);
           
            if(hit.transform)
                if (hit.collider.gameObject.GetComponent<TargetPart>())
                    if(hit.collider.gameObject.GetComponent<TargetPart>().getTargetControl() == goalTarget.targetControl)
                        StartGame();
        }
    }



#region Game Functions
    public void InitGame()
    {
        //set parameters
        WinLoose.setAlphaPanel_static(false);
        UITimer.setAlpha_static(false);
        Cursor.lockState = CursorLockMode.Locked; 
        isLastGameWinned = false;
        player.SetFullLock(false);
        CreatePointsForTargets(); //make list of points 
        CreateVoronoi(); //make links btw points
        ChooseTargetAndRemoveOldGoal(true, false); //make first target

        OutlinePoint(goalTarget, true);

        UITimer.setAlpha_static(false);
        guideText.SetActive(true);
        //rotate camera to near of first point (do not work)
        player.LookAt(goalTarget.targetControl.transform.position + new Vector3(2, 2, 2));
        //ready to start

        AnimateWay(new Vector3(0,0.5f,0), goalTarget.targetControl.gameObject);
    }

    public void StartGame()
    {
        guideText.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; //after first start
        UITimer.setAlpha_static(true);
        UITimer.setActive_static(true);
        UITimer.SetTime_static(howManySecondAddToTimer);
        gameIsGo = true;
        player.setAbleToShoot(true);
        StartTime();
    }


    //end game could be into two situations. Or victory. Or game is stopped and can't be beated.
    void EndGame(bool win)
    {
        Cursor.lockState = CursorLockMode.None;
        gameIsGo = false;
        player.setAbleToShoot(false);
        UITimer.setActive_static(false);
        WinLoose.setAlphaPanel_static(true);
        player.SetFullLock(true);
        isLastGameWinned = win;
        if (win)
        {

            WinLoose.setAlphaInputField_static(true);
            highscoreList.Add(new HighscoreData() { time = float.Parse(((UITimer.getTime_Static()).ToString("#.00"))), name = "Enter name" });
            WinLoose.LoadHighscore_static(highscoreList);
        }
        else
        {
            WinLoose.setAlphaInputField_static(false);
            WinLoose.LoadHighscore_static(highscoreList);

        }
    }

    void renameTMPHighscore()
    {
        renameTMPHighscore("Enter name", WinLoose.getInputText_static());
        
    }
    void renameTMPHighscore(string whatRename, string toWhatRename)
    {
        foreach (HighscoreData h in highscoreList)
        {
            if (h.name == whatRename)
                h.name = toWhatRename;
        }
        WinLoose.clearInputText_static();
    }
    public static void RestartGame_static()
    {        
        instance.RestartGame();
    }

    void RestartGame()
    {
        if (isLastGameWinned) //if into list are new name
        {
            renameTMPHighscore();
            SaveScore();
        }
        ClearGame();
        InitGame();
    }

    void ClearGame()
    {
        Score = 0;
        isRotating = false;
        foreach (point p in listOfPoints)
        {
            Destroy(p.targetControl.gameObject);
        }
        listOfPoints.Clear();
    }

    public void ExitGame()
    {
        if (isLastGameWinned) //if into list are new name
        {
            renameTMPHighscore();
            SaveScore();
        }
        Application.Quit();
    }
    
    public static void ExitGame_Static()
    {
        instance.ExitGame();
    }

    void OnTimeEnd()
    {
        EndGame(false);
    }

    void StartTime()
    {
        Time.timeScale = 1;
    }

    void StopTime()
    {
        Time.timeScale = 0;
    }

    void ContinueTime()
    {
        Time.timeScale = 1;
    }

#endregion

#region Generate points

    //get list of positions onto sphere
    void CreatePointsForTargets()
    {
        List<Vector3> points = new List<Vector3>();
        Center.transform.rotation = Quaternion.Euler(0,0,0); //reset center rotation
        //I know that there are more elegant 
        for (int i = 0; i < countOfTargets; i++)
        {
            Vector3 XYZ = Vector3.zero;
            bool isUnic = false;
            int countOfTries = 100, d = 0;

            while (!isUnic)
            {
                XYZ = GetPointOnSphere(); //Get random point
                isUnic = true;
                foreach (Vector3 pointObject in points)
                {
                    if ((XYZ-pointObject).magnitude < minDistance) //check if it isn't close enough then it shoud repeat
                    {
                        isUnic = false; //marker that should repeat
                        break; //break cause no sense in futher checks
                    }
                }

                d++; //special index of number of tries. Cause if there are no possible space - you should out of cycle
                if(d > countOfTries)
                {
                    XYZ = Vector3.zero;
                    Debug.Log("There is no empty space. Increase radius or reduce count.");
                    break;
                }
            }

            if (XYZ == Vector3.zero) //If there are no space
                break;  //then stop creating

            GameObject go = Instantiate(targetPrefab, XYZ, Quaternion.identity); //Instate target and introduse it to system
            go.transform.parent = Center.transform;
            listOfPoints.Add(new point(XYZ, go.GetComponent<TargetControl>()));
            go.GetComponent<TargetControl>().SetEvent(onAnyTargetDestroyEvent);
            points.Add(XYZ);
        }
        Center.transform.rotation = Quaternion.Euler(90, 0, 0); //return rotated angle
        if (listOfPoints.Count < 10)
        { 
            //Something to prevent this cause game cannot be beated 
        }
    }

    Vector3 GetPointOnSphere()
    {
        //get position on circle without height
        Vector2 XZ = Random.insideUnitCircle * radius;
        float height = Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(XZ.magnitude, 2));


        //add bias by center
        Vector3 XYZ = Center.transform.position + new Vector3(XZ.x, height, XZ.y);
        return XYZ;
    }

#endregion

#region Voronoi

    void CreateVoronoi()
    {
        List<Vector2f> vectors = new List<Vector2f>();

        foreach(point p in listOfPoints)
        {
            vectors.Add(p.circlePos);
        }

        Rectf bounds = new Rectf(0, 0, 1024, 1024); //canvas for voronoi

        Voronoi voronoi = new Voronoi(vectors, bounds);

        sites = voronoi.SitesIndexedByLocation;
        //edges = voronoi.Edges;


        for(int i = 0; i < listOfPoints.Count; i++)
        {
            listOfPoints[i].site = sites[listOfPoints[i].circlePos];
        }
    }



#endregion


#region Utility functions

    //Compare is points neighbors
    bool isPointsNear(point p1, point p2)
    {
        foreach (Site s in p1.site.NeighborSites())
        {
            if (s == p2.site)
                return true;
        }

        return false;
    }


    point getPointByVector2(Vector2f vector2)
    {
        for (int i = 0; i < listOfPoints.Count; i++)
        {
            if (listOfPoints[i].circlePos == vector2)
                return listOfPoints[i];
        }

        Debug.Log("Did not found compare point.");
        return null;
    }

    void OutlinePoint(point p, bool isOutline)
    {
        p.targetControl.SetOutline(isOutline);
    }

    point getPointByTargetControl(TargetControl tc)
    {
        foreach (point p in listOfPoints)
            if (p.targetControl == tc)
                return p;
        return null; //if no points
    }
#endregion


#region Functions when target was hitted
    void TargetDestroyed(TargetControl targetControl)
    {
        //if (targetControl.HitIntoMid) //add smthng for this;

        if (listOfPoints.Count < 11 - Score)
        {
            EndGame(false); //cause destroyed to much targets
        }
        else if (targetControl == goalTarget.targetControl)
        {
            UITimer.AddTime_static(howManySecondAddToTimer);
            Score++;
            MessageForPlayer(11 - Score + " targets left!");

            if (Score > 5)
                isRotating = true; //add rotation

            if (Score > 10)
                EndGame(true);

            ChooseTargetAndRemoveOldGoal(false, true);


            AnimateWay(oldPos, goalTarget.targetControl.gameObject);
        }
        else
        {
            MessageForPlayer("It is wrong target!");
            //just remove point
            RemoveTarget(getPointByTargetControl(targetControl));
        }

    }


    void ChooseTargetAndRemoveOldGoal(bool isRandom, bool shouldIDelete) //isRandom = could it be neighbour 
    {
        //temporary list of possible targets
        List<point> PossibleTargets = new List<point>(listOfPoints);

        PossibleTargets.Remove(goalTarget); 

        if (isRandom)
        {
            if (goalTarget != null) //for null exception and if not first try
                if (shouldIDelete)
                    RemoveTarget(goalTarget);

            goalTarget = PossibleTargets[Random.Range(0, PossibleTargets.Count)]; //get random point
        }
        else
        {
            if (goalTarget != null) //for null exception and if not first try
            {
                if (goalTarget.site.Edges.Count >= PossibleTargets.Count)
                {
                    Debug.Log("There are no possible non-neighbour variants.");
                    if(shouldIDelete)
                        RemoveTarget(goalTarget);
                    goalTarget = PossibleTargets[Random.Range(0, PossibleTargets.Count)];//get random point cause every variant is neighbour
                }
                else
                {
                    foreach(Edge e in goalTarget.site.Edges) //remove neighbours from possible list
                    {
                        PossibleTargets.Remove(getPointByVector2(new Vector2f(e.RightSite.x, e.RightSite.y)));
                        PossibleTargets.Remove(getPointByVector2(new Vector2f(e.LeftSite.x, e.LeftSite.y)));
                    }
                    if (shouldIDelete)
                        RemoveTarget(goalTarget); 
                    goalTarget = PossibleTargets[Random.Range(0, PossibleTargets.Count)];
                }
            }
            else
            {
                goalTarget = PossibleTargets[Random.Range(0, PossibleTargets.Count)];//get random point
            }
        }
    }
    void RemoveTarget(point target) //Remove from the list of targets. Need to use into Choose Target.
    {
        if (target == goalTarget)
            oldPos = target.targetControl.transform.position; //for animation line from old target to new

        //Destroy(target.targetControl.gameObject); //no need, cause in Target Control -> TargetPart are destryoing; 
        listOfPoints.Remove(target);
        CreateVoronoi(); //refresh map
    }
    #endregion



    #region JSON and saves //simple JsonUtility

    void LoadParameters()
    {
        try //the костыль for android. Could be fixed, but need more time.
        {
            ParametersToJSON parameters = JsonUtility.FromJson<ParametersToJSON>(File.ReadAllText(Application.streamingAssetsPath + "/parameters.json"));

            countOfTargets = parameters.countOfTargets;
            howManySecondAddToTimer = parameters.howManySecondAddToTimer;
            minDistance = parameters.minDistance;
            radius = parameters.radius;
            rotateSpeed = parameters.rotateSpeed;
        }
        catch
        {
            countOfTargets = 30;
            howManySecondAddToTimer = 10;
            minDistance = 5;
            radius = 20;
            rotateSpeed = 2;
        }
    }
    void SaveParameters()
    {
        try //the костыль for android. Could be fixed, but need more time.
        {
            string jsonSavePath = Application.streamingAssetsPath + "/parameters.json";

            ParametersToJSON toJSON = new ParametersToJSON();
            toJSON.countOfTargets          = countOfTargets;
            toJSON.howManySecondAddToTimer = howManySecondAddToTimer;
            toJSON.minDistance             = minDistance;
            toJSON.radius                  = radius;
            toJSON.rotateSpeed             = rotateSpeed;

            string json = JsonUtility.ToJson(toJSON, true);
            File.WriteAllText(jsonSavePath, json);
        }
        catch
        {

        }
    }

    void SaveScore()
    {
        try //the костыль for android. Could be fixed, but need more time.
        {
            string jsonSavePath = Application.streamingAssetsPath + "/highscore.json";
            HighscoreToJSON highscore = new HighscoreToJSON() { data = highscoreList };
            string json = JsonUtility.ToJson(highscore);
            File.WriteAllText(jsonSavePath, json);
        }
        catch
        {

        }
    }


    void LoadScore()
    {
        try //the костыль for android. Could be fixed, but need more time.
        {
            HighscoreToJSON highscoreJson = JsonUtility.FromJson<HighscoreToJSON>(File.ReadAllText(Application.streamingAssetsPath + "/highscore.json"));
            highscoreList = new List<HighscoreData>(highscoreJson.data);
        }
        catch
        {
            highscoreList = new List<HighscoreData>();
            highscoreList.Add(new HighscoreData() { name = "Mercurial-PC", time = 18.31f });
            highscoreList.Add(new HighscoreData() { name = "Andrew", time = 18.31f });
            highscoreList.Add(new HighscoreData() { name = "Better Andrew", time = 18.31f });
            highscoreList.Add(new HighscoreData() { name = "David", time = 18.31f });
            highscoreList.Add(new HighscoreData() { name = "Dima", time = 18.31f });
            highscoreList.Add(new HighscoreData() { name = "APTEM", time = 38f });
            highscoreList.Add(new HighscoreData() { name = "Yarek", time = 42f });
            highscoreList.Add(new HighscoreData() { name = "Shadow", time = 47f });
            highscoreList.Add(new HighscoreData() { name = "Katherina", time = 48f });
            highscoreList.Add(new HighscoreData() { name = "Ana$tAziA", time = 52f });
            highscoreList.Add(new HighscoreData() { name = "OMEGA", time = 64f });
            highscoreList.Add(new HighscoreData() { name = "Andrew", time = 86f });
        }
    }


#endregion


#region UI functions 
    //here were more functions, but all were revamped
  
    void MessageForPlayer(string text)
    {
        PopUpText.NewPopUp_Static(text);
    } 

#endregion



#region Animate way from target to target

    //Animate draw line from point p1 to transform of GameObject p2
    void AnimateWay(Vector3 p1, GameObject p2)
    {
        StopTime();

        //store line to return
        previousLineOfSight = player.getLineOfSight();

        lockCameraControl();
        player.StartLookAt(drawLine.GetLine());
        //create line
        drawLine.SetLinePosition(p1);

        //start animate line
        drawLine.newRender(p1, p2, unlockCameraControl);

        //control camera through animation - into Player class
        
        //returm camera - into DrawLine class

        

    }

    void lockCameraControl()
    {
        player.SetControl(true);
    }


    void unlockCameraControl() //function called when animation ends 
    {
        OutlinePoint(goalTarget, true);
        ContinueTime();
        player.SetControl(false);
        player.LookAt(previousLineOfSight);
    }

#endregion
}
