using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public Controller MyController;

    public int SelectedObject = 1;
    public int NbrOfObjects = 2;

    // red
    public Transform RedObject;
    public float redWidth = 0.1f;
    public float RedOffsetForward = 2;
    public float RedShootingTimeInterval = 0.009f;
    private float RedTime = 0;
    private bool RedActive = false;
    private bool PreviousWill = false;
    Mesh redMesh;
    Vector3[] redNewVertices;
    Vector2[] redNewUV;
    int[] redNewTriangles;
    public float redObjectLength = 1.0f;
    public Material leMatériauPourLeRouge;
    private MeshFilter meshFilter;
    private List<Vector3> redMeshVertices;
    private List<int> redTris;

    // blue
    public Transform BlueObject;
    public float BlueOffsetForward = 2;
    public float ProjectileStartSpeed = 2f;
    public float BlueShootingTimeInterval = 0.15f;
    private float BlueTime = 0;
    private List<Vector3> BluePositionList = new List<Vector3>();
    private List<Rigidbody> BlueRigidBodyList = new List<Rigidbody>();
    private List<Transform> BlueTransformList = new List<Transform>();
    private int BlueListLength = 0;
    public float BlueMaxAttractionRange = 50.0f;
    private float BlueMaxAttractionRangeSqrd; // utile pour des calculs de Vector3 optis
    public float BlueAttractionPower = 8.0f;

    void Start()
    {
        BlueMaxAttractionRangeSqrd = BlueMaxAttractionRange * BlueMaxAttractionRange;
    }

    // Update is called once per frame
    void Update()
    {
        // changement d'arme
        if (Input.GetAxis("Mouse ScrollWheel") > 0f) { SelectedObject += 1; }
        if (Input.GetAxis("Mouse ScrollWheel") < 0f) { SelectedObject -= 1; }
        if (SelectedObject > NbrOfObjects) { SelectedObject = 1; }
        if (SelectedObject <= 0) { SelectedObject = NbrOfObjects; }

        // "arme" rouge
        RedTime += Time.deltaTime;
        if (PreviousWill && !MyController.WantsToShoot)
        {
            RedActive = false;
            meshFilter = null;
            redMeshVertices = null;
            redTris = null;
        } // met le RedActive en false au moment où on cesse d'appuyer sur le bouton de la souris
        PreviousWill = MyController.WantsToShoot;
        if (MyController.WantsToShoot && RedTime >= RedShootingTimeInterval && SelectedObject == 1) // objet rouge
        {
            RedTime = 0;
            GenRed();
        }
            

        // "arme bleue"
        BlueTime += Time.deltaTime;
        if (MyController.WantsToShoot && BlueTime >= BlueShootingTimeInterval && SelectedObject == 2) // objet bleu
        {
            BlueTime = 0;
            GenBlue();
        }
        BlueAttraction();
    }

    void GenRed()
    {
        if (!RedActive) // on spawne le mesh et garde son nom
        {
            RedActive = true;

            //4 lignes suivantes d'après Doc Unity = on crée un renderer, on trouve un material, on crée un mesh
            GameObject redThing = new GameObject();
            MeshRenderer meshRenderer = redThing.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = leMatériauPourLeRouge;
            meshFilter = redThing.AddComponent<MeshFilter>();
            Mesh redMesh = new Mesh();
            meshFilter.mesh = redMesh;
            redThing.AddComponent<MeshCollider>();

            //on crée les vertices initiaux : 4 points. on les crée 16 fois chacun pour avoir toutes les faces avec des bonnes normales
            redMeshVertices = new List<Vector3> { };
            for (int i = 0; i < 16; i++)
            {
                redMeshVertices.Add(transform.position + transform.forward * RedOffsetForward); //point A
                redMeshVertices.Add(transform.position + transform.forward * RedOffsetForward + transform.up * redWidth); // point B
                redMeshVertices.Add(transform.position + transform.forward * (RedOffsetForward + redObjectLength) + transform.up * redWidth); // point C
                redMeshVertices.Add(transform.position + transform.forward * (RedOffsetForward + redObjectLength));  // point D
            }
            redMesh.vertices = redMeshVertices.ToArray();
            //Debug.Log("redMeshVertices"+redMeshVertices.Count);
            //on crée les triangles initiaux.
            //tous les vertex dont l'index est congru au même nombre modulo 4 sont à la même position dans l'espace
            redTris = new List<int>
            {
                0 +4*0, 1 +4*0, 2 +4*0, // ABC
                0 +4*1, 2 +4*1, 1 +4*1, // ACB

                0 +4*2, 2 +4*2, 3 +4*0, // ACD
                0 +4*3, 3 +4*1, 2 +4*3  // ADC
            };
            redMesh.triangles = redTris.ToArray();
        }

        if (RedActive) // on ajoute des vertices et des triangles au mesh
        {
            //vertices
            // là encore on spawn 16 fois chaque vertex pour que chaque triangle ait des vertices à lui. Comme ça les normales des vertices des triangles sont bonnes.
            for (int i = 0; i < 16; i++) 
            {
                redMeshVertices.Add(transform.position + transform.forward * RedOffsetForward); //point F
                redMeshVertices.Add(transform.position + transform.forward * RedOffsetForward + transform.up * redWidth); // point G
                redMeshVertices.Add(transform.position + transform.forward * (RedOffsetForward + redObjectLength) + transform.up * redWidth); // point H
                redMeshVertices.Add(transform.position + transform.forward * (RedOffsetForward + redObjectLength));  // point E
            }
            meshFilter.mesh.vertices = redMeshVertices.ToArray();
            //Debug.Log("redMeshVertices " + redMeshVertices.Count);
            //triangles
            //on note ABCD les 4 points du quadrilatère de l'itération précédente et FGHE les 4 points du quadrilatère actuel (ci)
            //schéma dans les fichiers joints
            //on crée des triangles en groupant par 3 les index des vertex
            int ci = redMeshVertices.Count - 64; // ci = currentIteration
            int[] newTris =
            {
                //EFGH
                ci+0 +(4*0), ci+1 +(4*0), ci+2 +(4*0),
                ci+0 +(4*1), ci+2 +(4*1), ci+1 +(4*1),

                ci+0 +(4*2), ci+2 +(4*2), ci+3 +(4*0),
                ci+0 +(4*3), ci+3 +(4*1), ci+2 +(4*3),

                //ADEF
                ci+0 +(4*4), ci+3 +(4*2), ci+3-64 +(4*8), // on spawne 64 points par cycle, donc pour aller chercher les points du cycle précédent on fait -64
                ci+0 +(4*5), ci+3-64 +(4*9), ci+3 +(4*3),

                ci+0 +(4*6), ci+3-64 +(4*10), ci+0-64 +(4*10),
                ci+0 +(4*7), ci+0-64 +(4*11), ci+3-64 +(4*11),

                //AFGB
                ci+1 +(4*2), ci+0 +(4*8), ci+0-64 +(4*12),
                ci+1 +(4*3), ci+0-64 +(4*13), ci+0 +(4*9),

                ci+1 +(4*4), ci+0-64 +(4*14), ci+1-64 +(4*8),
                ci+1 +(4*5), ci+1-64 +(4*9), ci+0-64 +(4*15),

                //GHCB
                ci+2 +(4*4), ci+1 +(4*6), ci+1-64 +(4*10),
                ci+2 +(4*5), ci+1-64 +(4*11), ci+1 +(4*7),

                ci+2 +(4*6), ci+1-64 +(4*12), ci+2-64 +(4*10),
                ci+2 +(4*7), ci+2-64 +(4*11), ci+1-64 +(4*13),

                //CHED
                ci+3 +(4*4), ci+2 +(4*8), ci+2-64 +(4*12),
                ci+3 +(4*5), ci+2-64 +(4*13), ci+2 +(4*9),

                ci+3 +(4*6), ci+2-64 +(4*14), ci+3-64 +(4*12),
                ci+3 +(4*7), ci+3-64 +(4*13), ci+2-64 +(4*15)
            };
            redTris.AddRange(newTris);
            meshFilter.mesh.triangles = redTris.ToArray();
            
        }
            
        //on refait un mesh avec les nouveaux points
        Mesh newRedMesh = new Mesh();
        newRedMesh.vertices = redMeshVertices.ToArray();
        newRedMesh.triangles = redTris.ToArray();

        //uv
        Vector2[] redUV = new Vector2[redMeshVertices.Count];
        for (int i = 0; i < redUV.Length; i++)
        {
            redUV[i] = new Vector2(0, 0);
        }
        newRedMesh.uv = redUV;

        //normales
        Vector3[] redNorm = new Vector3[redMeshVertices.Count];
        for (int i = 0; i < redNorm.Length; i++)
        {
            redNorm[i] = -Vector3.forward;
        }
        newRedMesh.normals = redNorm;
        //newRedMesh.RecalculateNormals();

        meshFilter.mesh = newRedMesh;
        meshFilter.GetComponent<MeshCollider>().sharedMesh = newRedMesh; // collider = mesh
        //Transform proj = GameObject.Instantiate<Transform>(RedObject, transform.position + transform.forward * RedOffsetForward, transform.rotation); // ancien code : spawn de baguettes rouges
    }

    void GenBlue()
    {
        Transform proj = GameObject.Instantiate<Transform>(BlueObject, transform.position + transform.forward * BlueOffsetForward, transform.rotation); // spawn
        Rigidbody proj2 = proj.GetComponent<Rigidbody>();
        proj2.AddForce(transform.forward * ProjectileStartSpeed, ForceMode.Impulse); // impulsion initiale
        // listes constituées pour optimiser BlueAttraction
        BluePositionList.Add(proj.position);
        BlueRigidBodyList.Add(proj2);
        BlueTransformList.Add(proj);
        BlueListLength += 1;
        //Debug.Log("Number of blue objects : " + BlueListLength);
    }

    // les objets bleus s'attirent les uns les autres
    void BlueAttraction()
    {
        // principe : on prend chaque BlueObject de la liste
        // puis pour chaque objet placé après lui dans la liste, on calcule la distance relative
        // on applique alors une force inversement proportionnelle à cette distance aux deux objets

        for (int i = 0; i < BlueListLength; i++) // mise à jour des positions
        {
            BluePositionList[i] = BlueTransformList[i].position;
        }

        for (int i = 0; i < BlueListLength - 1; i++)
        {
            for (int j = i + 1; j < BlueListLength; j++)
            {
                Vector3 Direction = BluePositionList[j] - BluePositionList[i];
                float dist = Vector3.SqrMagnitude(Direction); // dist est le carré de la distance
                if (dist <= BlueMaxAttractionRangeSqrd)
                {
                    dist = Mathf.Sqrt(dist); // dist devient la distance. à 600 objets spawnés, Sqrt dure 5ms/tick soit 10% de la durée du script, 8% de la durée du tick... pas ouf
                    Direction = (Direction).normalized;
                    float Force = BlueAttractionPower / dist;
                    BlueRigidBodyList[i].AddForce(Force * Direction, ForceMode.Force);
                    BlueRigidBodyList[j].AddForce(Force * (-1 * Direction), ForceMode.Force);
                }
            }
        }
    }
}