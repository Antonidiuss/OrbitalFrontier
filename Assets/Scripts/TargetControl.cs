using UnityEngine;

public class TargetControl : MonoBehaviour
{
    //instead of carring refs of each child to destroy i decide to subscribe them to event cause why not?
    public delegate void HitAction();
    public event HitAction explodeThisTarget;

    GameObject Center;
    onAnyTargetDestroy eventToInvoke;

    public bool HitIntoMid = false;
    public bool isOutlined = false;

    //function for complicated movement
    public void SetParent(Transform transform)
    {
        transform.parent = transform;
    }

    //in case if I want to change dirrection
    public void SetCenter(GameObject go) 
    {
        Center = go;
    }
    
    private void LateUpdate()
    {
        if (Center != null)
            transform.LookAt(Center.transform); // in case of movement I do not expect that they will rotate wrong, but it is additional 
        else
            transform.LookAt(Vector3.zero);
    }


    //function called when player hits part of target
    public void HitTarget(bool isMiddleHit)
    {
        HitIntoMid = isMiddleHit;
        explodeThisTarget.Invoke();
        eventToInvoke.Invoke(this);
    }

    public void SetEvent(onAnyTargetDestroy ev)
    {
        eventToInvoke = ev;
    }

    public void SetOutline(bool isOutline)
    {
        isOutlined = isOutline;
        //could be cycle for each child, but I decide to be simpler
        transform.GetChild(0).GetComponent<TargetPart>().ChangeMat(isOutline);
        transform.GetChild(1).GetComponent<TargetPart>().ChangeMat(isOutline);

    }
}
