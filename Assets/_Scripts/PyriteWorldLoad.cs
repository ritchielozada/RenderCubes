using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pyrite3D;
using Pyrite3D.Model;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Debug = UnityEngine.Debug;

public class PyriteWorldLoad : MonoBehaviour
{
    public GameObject CameraRig;
    public string ModelVersion = "V1";
    public GameObject WorldLocatorCube;
    public GameObject LocatorCube;
    public int DetailLevel = 3;    
    public GameObject PlaceHolderCube;
    public string PyriteServer;
    public string SetName;
    public int MaxListCount = 50;

    public bool EnableDebugLogs = false;
    public bool EnableDebugLogs2 = true;
    public bool UseCameraDetection = false;
    public bool UseEbo = true;
    public bool UseUnlitShader = true;
    public bool UseWww = false;

    private Vector3 tempPosition;
    private PyriteCube cubeCamPos;
    private PyriteCube cubeCamPosNew;
    private PyriteQuery pyriteQuery;
    private PyriteSetVersionDetailLevel pyriteLevel;
    private bool DataReady = false;

    Dictionary<string, CubeTracker> cubeDict = new Dictionary<string, CubeTracker>();
    Queue<CubeTracker> cubeQueue = new Queue<CubeTracker>();
    LinkedList<CubeTracker> cubeList = new LinkedList<CubeTracker>();

    void Start()
    {
        DebugLog("+Start()");
        StartCoroutine(Load());
        DebugLog("-Start()");
    }
    
    void Update()
    {        
        if (DataReady)
        {
            cubeCamPosNew = pyriteLevel.GetCubeForWorldCoordinates(CameraRig.transform.position);
            if (!cubeCamPos.Equals(cubeCamPosNew))
            {
                //Debug.Log("NEW CUBE POSITION");
                cubeCamPos = cubeCamPosNew;
                LoadCamCubes();
            }

            var planePoint = CameraRig.transform.position;
            planePoint.y = 0f;
            Debug.DrawLine(CameraRig.transform.position, planePoint, Color.green, 0f, true);
        }
    }

    protected void DebugLog(string fmt, params object[] args)
    {
        if (EnableDebugLogs2)
        {
            var content = string.Format(fmt, args);
            Debug.LogFormat("{0}", content);
        }
    }

    private PyriteCube CreateCubeFromCubeBounds(CubeBounds cubeBounds)
    {
        return new PyriteCube
        {
            X = (int)cubeBounds.BoundingBox.Min.x,
            Y = (int)cubeBounds.BoundingBox.Min.y,
            Z = (int)cubeBounds.BoundingBox.Min.z
        };
    }

    IEnumerator Load()
    {
        DebugLog("START-PyriteQuery.LoadAll()");
        tempPosition = transform.position;
        transform.position = Vector3.zero;
        pyriteQuery = new PyriteQuery(this, SetName, ModelVersion, PyriteServer);
        yield return StartCoroutine(pyriteQuery.LoadAll());
        DebugLog("END-PyriteQuery.LoadAll()");
        DebugLog("Pyrite Detail Levels Count: " + pyriteQuery.DetailLevels.Count);

        pyriteLevel = pyriteQuery.DetailLevels[DetailLevel];
        //var centerVector = pyriteLevel.ModelBoundsMax - pyriteLevel.ModelBoundsMin;
        //var region = pyriteLevel.Octree.Octants;

        var setSize = pyriteLevel.SetSize;
        Debug.Log("Set Size " + setSize);
        //var camPos = new Vector3(setSize.x/2, setSize.y/2, 1.5f);
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingBox(centerPos, centerPos));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingSphere(centerPos, setSize.x / 6f));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingSphere(new Vector3(4.5f, 4.5f, 0f), 1f));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingBox(new Vector3(3, 3, 0), new Vector3(3f, 3f, 0f)));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new Microsoft.Xna.Framework.BoundingBox(new Vector3(3, 3, 0), new Vector3(3f, 3f, 0f)));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new Microsoft.Xna.Framework.BoundingSphere(new Vector3(3, 3, 0), 1f));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingSphere(centerVector, 2f));

        cubeCamPos = pyriteLevel.GetCubeForWorldCoordinates(CameraRig.transform.position);
        LoadCamCubes();


        var worldObject = new GameObject("WorldParent") as GameObject;
        worldObject.transform.position = Vector3.zero;
        worldObject.transform.rotation = Quaternion.identity;
        foreach (var i in pyriteLevel.Octree.AllItems())
        {
            var pCube = CreateCubeFromCubeBounds(i);
            var cubePos = pyriteLevel.GetWorldCoordinatesForCube(pCube);
            var loc = Instantiate(WorldLocatorCube, cubePos, Quaternion.identity) as GameObject;
            loc.transform.localScale = new Vector3(
                      pyriteLevel.WorldCubeScale.x,
                      pyriteLevel.WorldCubeScale.z,
                      pyriteLevel.WorldCubeScale.y);
            loc.transform.parent = worldObject.transform;
        }

        //var octCubes2 = pyriteLevel.Octree.AllItems();
        //foreach (var octCube in octCubes2)
        //{
        //    var pCube = CreateCubeFromCubeBounds(octCube);
        //    var cubePos = pyriteLevel.GetWorldCoordinatesForCube(pCube);
        //    var loc = Instantiate(LocatorCube, cubePos, Quaternion.identity) as GameObject;
        //    loc.transform.parent = gameObject.transform;
        //}

        transform.position = tempPosition;
        DataReady = true;
    }


    void LoadCamCubes()
    {        
        Debug.Log(String.Format("Cube: ({0},{1},{2})", cubeCamPos.X, cubeCamPos.Y, cubeCamPos.Z));        
        var cubeCamVector = new Vector3(cubeCamPos.X + 0.5f, cubeCamPos.Y + 0.5f, cubeCamPos.Z + 0.5f);

        var minVector = cubeCamVector - Vector3.one;
        var maxVector = cubeCamVector + Vector3.one;        
        //minVector.z = 0f; // HACK:  Hit the ground to see reference

        var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingBox(minVector, maxVector));

        int cubeCounter = 0;
        foreach (var i in octIntCubes)
        {
            cubeCounter++;
            var pCube = CreateCubeFromCubeBounds(i.Object);
            var cubePos = pyriteLevel.GetWorldCoordinatesForCube(pCube);
            
            // Setup object at cube location
            if(cubeDict.ContainsKey(pCube.GetKey()))
            {
                var cube = cubeDict[pCube.GetKey()];
                cubeList.Remove(cube);
                cubeList.AddFirst(cube);
                if (!cube.Active)
                {                    
                    cube.Active = true;                    
                    // TODO: Re-activate cube
                }
            }
            else
            {
                CubeTracker ct;
                // TODO: Create GameObject
                
                var gObj = Instantiate(LocatorCube, cubePos, Quaternion.identity) as GameObject;
                if(cubeList.Count < MaxListCount)
                {                    
                    ct = new CubeTracker(pCube.GetKey(), null);
                }
                else
                {
                    // Reuse Last CubeTracker
                    Debug.Log("Reusing Cube");
                    ct = cubeList.Last.Value;
                    cubeList.RemoveLast();
                    cubeDict.Remove(ct.DictKey);
                    ct.DictKey = pCube.GetKey();
                    
                    // TODO: Reassign GameObject Content instead of destroying
                    Destroy(ct.gameObject);
                    
                    if(ct.Active)
                    {
                        Debug.Log("ALERT: Active Object in List Tail");
                    }
                }
                gObj.transform.parent = gameObject.transform;            
                ct.gameObject = gObj;                
                ct.Active = true;
                cubeList.AddFirst(ct);
                cubeDict.Add(ct.DictKey, ct);
            }
        }
        Debug.Log(String.Format("CubeCounter: {0}  CubeList/Dict: {1}/{2}", cubeCounter, cubeList.Count, cubeDict.Count));
        
        foreach (var q in cubeList.Skip(cubeCounter).TakeWhile(q => q.Active))
        {
            q.Active = false;
        }
    }
}
