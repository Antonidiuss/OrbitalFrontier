using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TargetPart : MonoBehaviour
{
    TargetControl targetControl;
    public bool isMiddle;
    public bool isPlanet = false;


    //initiate GO
    private void Awake()
    {
        if(!isPlanet)
            targetControl = transform.parent.GetComponent<TargetControl>();

        //bug at Unity that new nonintantiatet compute shader overrides old
        shader = (ComputeShader)Instantiate(Resources.Load("MeshExplosion")); 
    }

    public void Start()
    {
        if (InitData())
        {
            InitShader();
        }

    }

    private void OnEnable()
    {
        if(!isPlanet)
            targetControl.explodeThisTarget += Explode; //adding trigger to explode by command of control
    }

    private void OnDisable()
    {
        if (!isPlanet)
            targetControl.explodeThisTarget -= Explode; //prevent null exception
    }

    public TargetControl getTargetControl()
    {
        return targetControl;
    }

    //method called when target is hitted
    public void HitThisTarget()
    {
        if (!isPlanet)
            targetControl.HitTarget(isMiddle);
        else
        {
            PopUpText.NewPopUp_Static("Uh no... What have you done?");
            Explode();
        }
    }

    //call animation of destroying
    public void Explode()
    {
        StartCoroutine(SplitMesh(true));
    }

    //Chnge material onto outlined or not.
    public void ChangeMat(bool isOutline)
    {
        GetComponent<MeshRenderer>().material = MatController.GetMaterial_Static(isMiddle, isOutline);
    }




    #region shader

    ComputeShader shader;
    [Range(0.5f, 3.0f)]
    public float radius;
    public float bias = 2;
    bool doExplosion = false;
    float timeStamp = 0;
    Mesh[] meshesForShader;

    int kernelHandle;

    int[] trianglesArray;
    Vertex[] vertexArray;
    Vertex[] initialArray;
    ComputeBuffer vertexBuffer;
    ComputeBuffer initialBuffer;


    private bool InitData() //defense from null reference
    {
        kernelHandle = shader.FindKernel("CSMain");

        if (GetComponent<MeshFilter>() == null)
        {
            Debug.Log("No MeshFilter found");
            return false;
        }

        return true;
    }

    private void InitShader()
    {
        shader.SetFloat("radius", radius);
        shader.SetFloat("bias", bias);
    }


    private void InitGPUBuffers() //initiating GPU buffers...
    {
        vertexBuffer = new ComputeBuffer(vertexArray.Length, sizeof(float) * 6); //6 cause two vectors3. 2 vectors * 3 floats = 6 floats.
        vertexBuffer.SetData(vertexArray);

        initialBuffer = new ComputeBuffer(initialArray.Length, sizeof(float) * 6);
        initialBuffer.SetData(initialArray);

        shader.SetBuffer(kernelHandle, "vertexBuffer", vertexBuffer);
        shader.SetBuffer(kernelHandle, "initialBuffer", initialBuffer);
    }


    //struct for buffer;
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;

        public Vertex(Vector3 p, Vector3 n)
        {
            position.x = p.x;
            position.y = p.y;
            position.z = p.z;
            normal.x = n.x;
            normal.z = n.z;
            normal.y = n.y;
        }
    }




    private void Update()
    {
        if (doExplosion & Time.timeScale != 0)
        {

#if UNITY_EDITOR
            InitShader();
#endif

            shader.SetFloat("delta", Time.time); //pass time
            shader.Dispatch(kernelHandle, vertexArray.Length, 1, 1); //Dispatch. count equal to count of triangles

            GetVerticesFromGPU(); //get results of math
        }
    }


    void GetVerticesFromGPU()
    {
        vertexBuffer.GetData(vertexArray);
        Vector3[] vertices = new Vector3[vertexArray.Length];
        Vector3[] normals = new Vector3[vertexArray.Length];



        for (int i = 0; i < trianglesArray.Length; i++)
        {
            vertices[i] = vertexArray[i].position;
            normals[i] = vertexArray[i].normal;

        }
        
        //create vector and set once is faster than call each time
        for (int i = 0; i < trianglesArray.Length; i += 3)
        {
            Vector3[] newNormal = new Vector3[3] { normals[i], normals[i + 1], normals[i + 2] };
            meshesForShader[i / 3].SetNormals(newNormal);
            Vector3[] newVerts = new Vector3[3] { vertices[i], vertices[i+1], vertices[i+2]};
            meshesForShader[i / 3].SetVertices(newVerts);
        }
        

    }


    public void MakeExplosion() //set data for shader
    {
        timeStamp = Time.time;
        shader.SetFloat("timeStamp", timeStamp);

        doExplosion = true;
    }

    #endregion



    //This Enumerator is not mine, but this code is so cool and simple and fully understand it. I like that is is universal. 
    //But cause it is not optimized I decide to use GPU instead CPU 
    //I took it from https://answers.unity.com/questions/1006318/script-to-break-mesh-into-smaller-pieces.html . I found it while made one of my past projects, but didn't used it
    //This function creates triangle gameobjects with rigidbody for each triangle into mesh.
    private IEnumerator SplitMesh(bool destroy)
    {
        //null check
        if (GetComponent<MeshFilter>() == null || GetComponent<SkinnedMeshRenderer>() == null)
        {
            yield return null;
        }

        //working collider does not allow particles to fall
        if (GetComponent<Collider>())
        {
            GetComponent<Collider>().enabled = false;
        }

        //get form of object
        Mesh M = new Mesh();
        if (GetComponent<MeshFilter>())
        {
            M = GetComponent<MeshFilter>().mesh;
        }
        else if (GetComponent<SkinnedMeshRenderer>())
        {
            M = GetComponent<SkinnedMeshRenderer>().sharedMesh;
        }

        //store materials, cause particles should be same material
        Material[] materials = new Material[0];
        if (GetComponent<MeshRenderer>())
        {
            materials = GetComponent<MeshRenderer>().materials;
        }
        else if (GetComponent<SkinnedMeshRenderer>())
        {
            materials = GetComponent<SkinnedMeshRenderer>().materials;
        }

        Vector3[] verts = M.vertices;
        Vector3[] normals = M.normals;
        Vector2[] uvs = M.uv; //uv for planet

        trianglesArray = M.GetTriangles(0);

        //Verts to verts of triangles
        Vector3[] triangleVerts = VertsToTrianVerts(verts, trianglesArray); //0 cause we use only one submesh
        Vector3[] triangleNormals = VertsToTrianVerts(normals, trianglesArray); //0 cause we use only one submesh


        //init shader
        if (InitData())
        {
            InitShader();
            vertexArray = new Vertex[triangleVerts.Length];
            initialArray = new Vertex[triangleVerts.Length];

            for (int i = 0; i < triangleVerts.Length; i++)
            {
                
                //Vertex v1 = new Vertex(verts[index], normals[index], uvs[index]);
                Vertex v1 = new Vertex(triangleVerts[i], triangleNormals[i]);
                vertexArray[i] = v1;
                //Vertex v2 = new Vertex(verts[index], normals[index], uvs[index]);
                Vertex v2 = new Vertex(triangleVerts[i], triangleNormals[i]);
                initialArray[i] = v2;
            } 
        }

        //get full ammount of meshes and initiate array of meshes
        int countOfMeshes = 0;
        for (int submesh = 0; submesh < M.subMeshCount; submesh++)
        {
            countOfMeshes += M.GetTriangles(submesh).Length / 3;
        }
        meshesForShader = new Mesh[countOfMeshes];


        int idOfMesh = 0; //to synchronize  mesh number and triangles
        for (int submesh = 0; submesh < M.subMeshCount; submesh++)
        {

            int[] indices = M.GetTriangles(submesh);

            for (int i = 0; i < indices.Length; i += 3) //+3 in case of triangle
            {
                Vector3[] newVerts = new Vector3[3];
                Vector3[] newNormals = new Vector3[3];
                Vector2[] newUvs = new Vector2[3];
                for (int n = 0; n < 3; n++) //get parameters of triangle
                {
                    int index = indices[i + n];
                    newVerts[n] = verts[index];
                    newNormals[n] = normals[index];
                    newUvs[n] = uvs[index]; //uv for planet
                }




                //Create mesh same as triangle
                Mesh mesh = new Mesh();
                meshesForShader[idOfMesh] = mesh;
                idOfMesh++; //we could simply increment cause we will use same pattern of allocating when we will transfer to and from gpu
                mesh.vertices = newVerts;
                mesh.normals = newNormals;
                mesh.uv = newUvs;

                //into new mesh there are two meshes. One like original and another is back side.
                mesh.triangles = new int[] { 0, 1, 2, 2, 1, 0 };

                //creating gameObject
                GameObject GO = new GameObject("Triangle " + (i / 3));
                GO.transform.parent = GameObject.Find("ThrashToDelete").transform; //keep hierarchy clear and nice
                GO.layer = 17; //particle layer = 17
                GO.transform.position = transform.position;
                GO.transform.rotation = transform.rotation;
                GO.AddComponent<MeshRenderer>().material = materials[submesh]; //this is hard line of code.
                GO.AddComponent<MeshFilter>().mesh = mesh; //this is hard line of code. 
               

                
                GO.transform.localScale = transform.localScale;

                //old simulatng explosion of object
                //Vector3 explosionPos = new Vector3(transform.position.x + Random.Range(-0.5f, 0.5f), transform.position.y + Random.Range(0f, 0.5f), transform.position.z + Random.Range(-0.5f, 0.5f));
                //GO.AddComponent<Rigidbody>().AddExplosionForce(Random.Range(300, 500), explosionPos, 5); //one of causes why I dicide to optimize.
                
                Destroy(GO, 2 + Random.Range(0.0f, 2.0f)); //cleae gibs
            }
        }


        
        InitGPUBuffers(); //init gpu
        MakeExplosion(); //set explosion data

        //remove renderer of original
        GetComponent<Renderer>().enabled = false;

        yield return new WaitForSeconds(6.0f);
        if (destroy == true)
        {
            Destroy(gameObject);
        }
    }

    Vector3[] VertsToTrianVerts(Vector3[] verts, int[] triangles) //many triangles could reffers to one vertex. This function returns array of vertex parameter where each trianle reffers to one vertex parameter
    {
        Vector3[] res = new Vector3[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            res[i] = verts[triangles[i]];
        }

        return res;
    }



    void OnDestroy() //keep clean
    {
        if (vertexBuffer != null)
        {
            vertexBuffer.Dispose();
            initialBuffer.Dispose();
        }
    }
}
