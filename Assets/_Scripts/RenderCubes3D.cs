using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Policy;
using UnityEditor;

public class RenderCubes3D : MonoBehaviour
{
    public int GridMinSize = 50;

    public Material BaseMaterial;
    public GameObject LocatorCube;
    public bool EnabledTracking = false;

    private GridPos prevGridPosL1;
    private GridPos newGridPosL1;
    private GridPos prevGridPosL2;
    private GridPos newGridPosL2;
    private GridPos prevGridPosL3;
    private GridPos newGridPosL3;
    
    private GameObject playerObject;
    private GameObject worldObject;

    private Dictionary<String, GameObject> L1Dict = new Dictionary<String, GameObject>();
    private Dictionary<String, GameObject> L2Dict = new Dictionary<String, GameObject>();
    private Dictionary<String, GameObject> L3Dict = new Dictionary<String, GameObject>();



    // Use this for initialization
    void Start()
    {        
        playerObject = GameObject.FindGameObjectWithTag("Player");
        //CreateCubeLayers(playerObject.transform.position);

        prevGridPosL1 = GridUtils.WorldToGrid(playerObject.transform.position, GridMinSize);
        newGridPosL1 = GridUtils.WorldToGrid(playerObject.transform.position, GridMinSize);
        prevGridPosL2 = GridUtils.WorldToGrid(playerObject.transform.position, GridMinSize * 3);
        newGridPosL2 = GridUtils.WorldToGrid(playerObject.transform.position, GridMinSize * 3);
        prevGridPosL3 = GridUtils.WorldToGrid(playerObject.transform.position, GridMinSize * 9);
        newGridPosL3 = GridUtils.WorldToGrid(playerObject.transform.position, GridMinSize * 9);
    }

    public void CreateCubeLayers(Vector3 targetPosition)
    {
        GridUtils.BaseMaterial = BaseMaterial;

        int colorCounter = 0;
        GridPos pos = GridUtils.WorldToGrid(targetPosition, GridMinSize);
        GridPos vpos;
        GridPos gpos;
        float cubesize;

        worldObject = new GameObject("WorldRenderObject");
        worldObject.transform.position = targetPosition;
        GameObject locObj;
        


        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
            {                
                for (int y = -1; y <= 1; y++)
                {                                        
                    //Debug.Log(String.Format("{0} {1} {2}", gpos.x, gpos.y, gpos.z));                    

                    pos = GridUtils.WorldToGrid(targetPosition, GridMinSize * 9);
                    gpos = new GridPos(pos.x + x, pos.y + y, pos.z + z);
                    vpos = new GridPos(gpos.x * GridMinSize * 9, gpos.y * GridMinSize * 9, gpos.z * GridMinSize * 9);
                    cubesize = GridMinSize * 9 / 2f ;
                    
                    //L3Dict.Add(gpos.ToKeyString(), GridUtils.CreateRenderCube(vpos, new Vector3(45f, 0.1f, 45f), GridUtils.BaseColors[colorCounter % 2 + 4], worldObject));
                    L3Dict.Add(gpos.ToKeyString(), GridUtils.CreateRenderCube(vpos, new Vector3(cubesize, cubesize, cubesize), GridUtils.BaseColors[colorCounter % 2 + 4], worldObject));
                    locObj = Instantiate(LocatorCube, vpos.ToVector3(), Quaternion.identity) as GameObject;
                    locObj.transform.parent = worldObject.transform;

                    pos = GridUtils.WorldToGrid(targetPosition, GridMinSize * 3);
                    gpos = new GridPos(pos.x + x, pos.y + y, pos.z + z);
                    vpos = new GridPos(gpos.x * GridMinSize * 3, gpos.y * GridMinSize * 3, gpos.z * GridMinSize * 3);
                    cubesize = GridMinSize * 3 / 2f;
                    
                    //L2Dict.Add(gpos.ToKeyString(), GridUtils.CreateRenderCube(vpos, new Vector3(15f, 1f, 15f), GridUtils.BaseColors[colorCounter % 2 + 2], worldObject));
                    L2Dict.Add(gpos.ToKeyString(), GridUtils.CreateRenderCube(vpos, new Vector3(cubesize, cubesize, cubesize), GridUtils.BaseColors[colorCounter % 2 + 2], worldObject));
                    locObj = Instantiate(LocatorCube, vpos.ToVector3(), Quaternion.identity) as GameObject;
                    locObj.transform.parent = worldObject.transform;

                    pos = GridUtils.WorldToGrid(targetPosition, GridMinSize);
                    gpos = new GridPos(pos.x + x, pos.y + y, pos.z + z);
                    vpos = new GridPos(gpos.x * GridMinSize, gpos.y * GridMinSize, gpos.z * GridMinSize);
                    cubesize = GridMinSize / 2f;
                    
                    //L1Dict.Add(gpos.ToKeyString(), GridUtils.CreateRenderCube(vpos, new Vector3(5f, 2f, 5f), GridUtils.BaseColors[colorCounter++ % 2], worldObject));
                    L1Dict.Add(gpos.ToKeyString(), GridUtils.CreateRenderCube(vpos, new Vector3(cubesize, cubesize, cubesize), GridUtils.BaseColors[colorCounter++ % 2], worldObject));
                    locObj = Instantiate(LocatorCube, vpos.ToVector3(), Quaternion.identity) as GameObject;
                    locObj.transform.parent = worldObject.transform;
                }
            }
        }
        EnabledTracking = true;

    }

    IEnumerator MoveObjectToPosition(GameObject obj, Vector3 targetPosition, float step, float height)
    {
        float dstep = step * Time.deltaTime / 10;
        height += 50;
        //float dstep = step;

        //Vector3 key1 = new Vector3(obj.transform.position.x, height, obj.transform.position.z);
        //Vector3 key2 = new Vector3(targetPosition.x, height, targetPosition.z);

        Vector3 key1 = obj.transform.position + new Vector3(height, 0f, 0f);
        Vector3 key2 = targetPosition + new Vector3(height, 0f, 0f);


        while ((obj.transform.position - key1).sqrMagnitude > 0.5f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, key1, dstep);
            yield return null;
        }
        while ((obj.transform.position - key2).sqrMagnitude > 0.5f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, key2, dstep);
            yield return null;
        }
        while ((obj.transform.position - targetPosition).sqrMagnitude > 0.5f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, targetPosition, dstep);
            yield return null;
        }
        obj.transform.position = targetPosition;
        yield return null;
    }


    IEnumerator TransitionRenderCube(GameObject obj, int level, GridPos gPos)
    {
        // STORE CUBE DATA
        //
        // Check if in Cache 
        // -- Set cache entry as MRU
        // -- NOT: Create new cache entry, remove LRU entry if needed

        // LOAD CUBE DATA
        //
        // Check if in Cache
        // -- Load entry, set as MRU
        // -- NOT: Load from Web

        yield return null;
    }


    void UpdateGridCubes(ref GridPos prevGridPosL, ref GridPos newGridPosL, int gridSize, Dictionary<String, GameObject> LDict, int level)
    {
        newGridPosL = GridUtils.WorldToGrid(playerObject.transform.position, gridSize);
        if (!prevGridPosL.Equals(newGridPosL))
        {
            GridPos deltaPos = prevGridPosL.Delta(newGridPosL);
            GridPos vPosL;
            string vPosKeyL;
            GameObject gObj;
            Vector3 newVector3;
            GridPos newGridPos;

            if (deltaPos.x != 0)
            {
                for (var i = -1; i <= 1; i++)
                {
                    for (var j = -1; j <= 1; j++)
                    {
                        vPosL = new GridPos(prevGridPosL.x - deltaPos.x, prevGridPosL.y + i, prevGridPosL.z + j);
                        vPosKeyL = vPosL.ToKeyString();

                        if (LDict.ContainsKey(vPosKeyL))
                        {
                            gObj = LDict[vPosKeyL];
                            LDict.Remove(vPosKeyL);

                            newGridPos = new GridPos(newGridPosL.x + deltaPos.x, prevGridPosL.y + i, prevGridPosL.z + j);

                            newVector3 = new Vector3(newGridPos.x * gridSize, newGridPos.y * gridSize, newGridPos.z * gridSize);
                            gObj.transform.position = newVector3;                            
                            LDict.Add(newGridPos.ToKeyString(), gObj);
                            StartCoroutine(TransitionRenderCube(gObj, level, newGridPos));
                        }
                        else
                        {
                            Debug.Log("KEY NOT FOUND :" + vPosKeyL);
                        }
                    }
                }
            }

            if (deltaPos.z != 0)
            {
                for (var i = -1 + deltaPos.x; i <= 1 + deltaPos.x; i++)
                {
                    for (var j = -1; j <= 1; j++)
                    {
                        vPosL = new GridPos(prevGridPosL.x + i, prevGridPosL.y + j, prevGridPosL.z - deltaPos.z);
                        vPosKeyL = vPosL.ToKeyString();

                        if (LDict.ContainsKey(vPosKeyL))
                        {
                            gObj = LDict[vPosKeyL];
                            LDict.Remove(vPosKeyL);

                            newGridPos = new GridPos(prevGridPosL.x + i, prevGridPosL.y + j, newGridPosL.z + deltaPos.z);

                            newVector3 = new Vector3(newGridPos.x * gridSize, newGridPos.y * gridSize, newGridPos.z * gridSize);
                            gObj.transform.position = newVector3;                            
                            LDict.Add(newGridPos.ToKeyString(), gObj);                            
                            StartCoroutine(TransitionRenderCube(gObj, level, newGridPos));
                        }
                        else
                        {
                            Debug.Log("KEY NOT FOUND :" + vPosKeyL);
                        }
                    }
                }
            }

            if (deltaPos.y != 0)
            {
                for (var i = -1 + deltaPos.x; i <= 1 + deltaPos.x; i++)
                {
                    for (var j = -1 + deltaPos.z; j <= 1 + deltaPos.z; j++)
                    {
                        vPosL = new GridPos(prevGridPosL.x + i, prevGridPosL.y - deltaPos.y, prevGridPosL.z + j);
                        vPosKeyL = vPosL.ToKeyString();

                        if (LDict.ContainsKey(vPosKeyL))
                        {
                            gObj = LDict[vPosKeyL];
                            LDict.Remove(vPosKeyL);

                            newGridPos = new GridPos(prevGridPosL.x + i, newGridPosL.y + deltaPos.y, prevGridPosL.z + j);

                            newVector3 = new Vector3(newGridPos.x * gridSize, newGridPos.y * gridSize, newGridPos.z * gridSize);
                            gObj.transform.position = newVector3;                            
                            LDict.Add(newGridPos.ToKeyString(), gObj);
                            StartCoroutine(TransitionRenderCube(gObj, level, newGridPos));
                        }
                        else
                        {
                            Debug.Log("KEY NOT FOUND :" + vPosKeyL);
                        }
                    }
                }
            }
            prevGridPosL = newGridPosL;
        }
    }

    void Update()
    {
        if (EnabledTracking)
        {
            if (Input.GetKeyDown(KeyCode.Keypad8))
            {
                playerObject.transform.Translate(new Vector3(0f, GridMinSize, GridMinSize));
            }
            if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                playerObject.transform.Translate(new Vector3(0f, -GridMinSize, -GridMinSize));
            }


            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                playerObject.transform.Translate(new Vector3(GridMinSize, GridMinSize, GridMinSize));
            }
            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                playerObject.transform.Translate(new Vector3(-GridMinSize, -GridMinSize, -GridMinSize));
            }

            UpdateGridCubes(ref prevGridPosL1, ref newGridPosL1, GridMinSize, L1Dict, 1);
            UpdateGridCubes(ref prevGridPosL2, ref newGridPosL2, GridMinSize * 3, L2Dict, 2);
            UpdateGridCubes(ref prevGridPosL3, ref newGridPosL3, GridMinSize * 9, L3Dict, 3);
        }
    }
}
