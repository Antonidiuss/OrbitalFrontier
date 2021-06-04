using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    #region FPS Camera Control
    public float mouseSense = 100f;
    float xRotatiom;

    #endregion




    bool isLockToLookAt = false;
    bool isLockFully = false;
    bool isAbleToShoot = false;

    public GameObject DummyForCamera;
    public Joystick joystick;
    Camera camera;
    GameObject targetGO;
    
    
    float yBias = 0;
    private bool isJoystickLetGo;

    public bool JoystickWasLetGo { get; private set; }

    private void Awake()
    {
        camera = Camera.main;

        //some parameters relied on platform. Could be special serializeble class, but it is not serious project
        mouseSense = 125f;
#if UNITY_ANDROID
        joystick.gameObject.SetActive(true);
        mouseSense = 75f;
#endif
    }



    void Update()
    {
#if UNITY_ANDROID
        Cursor.lockState = CursorLockMode.None; //the костыль. Фиксится раскидыванием параметра в мейн гейм менеджере в нужные места
#endif

        if (isLockFully)
        {
            //nothing
        }
        else if(isLockToLookAt)
        {
            LookAt(targetGO); //just look
        }
        else
        {

            //input for pc
#if UNITY_STANDALONE_WIN
            //rotate

            

            float mouseX = Input.GetAxis("Mouse X") * mouseSense * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSense * Time.deltaTime;

            xRotatiom -= mouseY;
            xRotatiom = Mathf.Clamp(xRotatiom, -90f, 90f);

            camera.transform.localRotation = Quaternion.Euler(-xRotatiom, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
            transform.eulerAngles =new Vector3(0, transform.eulerAngles.y, 0);


            //shoot
            if (Input.GetKeyDown(KeyCode.Mouse0) && isAbleToShoot)
            {
                Shoot();
            }
#endif



            //and for android
#if UNITY_ANDROID

            //enable joystik and button
            
            
            //event for "When joystick is released"
            if (isJoystickLetGo)
            {
                if (joystick.Direction.magnitude > 0.01)
                {
                    isJoystickLetGo = false;
                    JoystickWasLetGo = false;
                }
            }
            else
            {
                if (joystick.Direction.magnitude < 0.01)
                {
                    isJoystickLetGo = true;
                }

                if (JoystickWasLetGo == false)
                {
                    if (isJoystickLetGo == true)
                    {
                        JoystickWasLetGo = true;
                        if (isAbleToShoot)
                            Shoot();
                    }
                }
            }

            //get input
            float mouseX = joystick.Horizontal * mouseSense * Time.deltaTime;
            float mouseY = -joystick.Vertical * mouseSense * Time.deltaTime;

            xRotatiom -= mouseY;
            xRotatiom = Mathf.Clamp(xRotatiom, -90f, 90f);

            camera.transform.localRotation = Quaternion.Euler(-xRotatiom, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

            
#endif
        }
    }

    void Shoot()
    {
        // Create a ray from the camera going through the middle of your screen
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        // Check whether your are pointing to something so as to adjust the direction

        Physics.Raycast(ray, out hit);            

        if(hit.transform)
        {
            if(hit.transform.GetComponent<TargetPart>())
                hit.transform.GetComponent<TargetPart>().HitThisTarget();
        }
    }

    public void setAbleToShoot(bool boo)
    {
        isAbleToShoot = boo;
    }

    public void SetControl(bool islock)
    {
        isLockToLookAt = islock;
    }

    public void SetFullLock(bool doLock)
    {
        isLockFully = doLock;
    }

    public void StartLookAt(GameObject go)
    {
        targetGO = go;
    }

    //Look! Polymorphism! :p
    public void LookAt(GameObject go)
    {
        camera.gameObject.transform.LookAt(go.transform);
    }

    public void LookAt(Vector3 vector3)
    {
        //camera.gameObject.transform.eulerAngles = vector3;
    }


    public Vector3 getLineOfSight()
    {
        return camera.gameObject.transform.rotation.eulerAngles;
    }
}
