using UnityEngine;
using System.Collections;
using Pyrite3D;
using Pyrite3D.Model;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Debug = UnityEngine.Debug;

public class PyriteWorldLoad : MonoBehaviour
{
    public GameObject RenderCubes;
    public string ModelVersion = "V1";
    public GameObject WorldLocatorCube;
    public GameObject LocatorCube;
    public int DetailLevel = 3;    
    public GameObject PlaceHolderCube;
    public string PyriteServer;
    public string SetName;

    public bool EnableDebugLogs = false;
    public bool EnableDebugLogs2 = true;
    public bool UseCameraDetection = false;
    public bool UseEbo = true;
    public bool UseUnlitShader = true;
    public bool UseWww = false;

    private Vector3 tempPosition;

    void Start()
    {
        DebugLog("+Start()");
        StartCoroutine(Load());
        DebugLog("-Start()");
    }
    
    void Update()
    {

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

        var pyriteQuery = new PyriteQuery(this, SetName, ModelVersion, PyriteServer);
        yield return StartCoroutine(pyriteQuery.LoadAll());
        DebugLog("END-PyriteQuery.LoadAll()");
        DebugLog("Pyrite: " + pyriteQuery.DetailLevels.Count);

        var pyriteLevel = pyriteQuery.DetailLevels[DetailLevel];

        //var centerVector = pyriteLevel.ModelBoundsMax - pyriteLevel.ModelBoundsMin;
        var region = pyriteLevel.Octree.Octants;

        var setSize = pyriteLevel.SetSize;
        Debug.Log("Set Size " + setSize);
        var centerPos = new Vector3(setSize.x/2, setSize.y/2, 0);
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingBox(centerPos, centerPos));

        var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingSphere(centerPos, 10f));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingSphere(new Vector3(4.5f, 4.5f, 0f), 1f));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingBox(new Vector3(3, 3, 0), new Vector3(3f, 3f, 0f)));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new Microsoft.Xna.Framework.BoundingBox(new Vector3(3, 3, 0), new Vector3(3f, 3f, 0f)));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new Microsoft.Xna.Framework.BoundingSphere(new Vector3(3, 3, 0), 1f));
        //var octIntCubes = pyriteLevel.Octree.AllIntersections(new BoundingSphere(centerVector, 2f));

        

        foreach(var i in octIntCubes)
        {
            var pCube = CreateCubeFromCubeBounds(i.Object);
            var cubePos = pyriteLevel.GetWorldCoordinatesForCube(pCube);
            var loc = Instantiate(LocatorCube, cubePos, Quaternion.identity) as GameObject;            
            loc.transform.parent = gameObject.transform;
        }

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
    }
}
