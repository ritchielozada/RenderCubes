﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Policy;
using UnityEditor;

public class RenderCubes3D : MonoBehaviour
{
    private const int gridMinSize = 10;
    Color[] baseColors = new Color[] { Color.red, Color.magenta, Color.blue, Color.cyan, Color.gray, Color.white };
    private float[] cubeVerts = new float[]
    {
           -1f, -1f, 1f, 
            1f, -1f, 1f, 
            -1f, 1f, 1f, 
            1f, 1f, 1f, 
            -1f, 1f, -1f, 
            1f, 1f, -1f, 
            -1f, -1f, -1f, 
            1f, -1f, -1f
    };
    private int[] polyIdx = new int[24] { 0, 1, 3, -3, 2, 3, 5, -5, 4, 5, 7, -7, 6, 7, 1, -1, 1, 7, 5, -4, 6, 0, 2, -5 };
    private int[] cubeTris;
    private Vector3[] cubeVertices;

    Vector3[] newVertices;
    Vector2[] newUV;
    int[] newTriangles;


    public Material baseMaterial;

    private GridPos prevGridPosL1;
    private GridPos newGridPosL1;
    private GridPos prevGridPosL2;
    private GridPos newGridPosL2;
    private GridPos prevGridPosL3;
    private GridPos newGridPosL3;
    
    private GameObject playerObject;

    private Dictionary<String, GameObject> L1Dict = new Dictionary<String, GameObject>();
    private Dictionary<String, GameObject> L2Dict = new Dictionary<String, GameObject>();
    private Dictionary<String, GameObject> L3Dict = new Dictionary<String, GameObject>();



    // Use this for initialization
    void Start()
    {
        CreateCubeLayers(transform.position);
        playerObject = GameObject.FindGameObjectWithTag("Player");

        prevGridPosL1 = WorldToGrid(playerObject.transform.position, gridMinSize);
        newGridPosL1 = WorldToGrid(playerObject.transform.position, gridMinSize);
        prevGridPosL2 = WorldToGrid(playerObject.transform.position, gridMinSize * 3);
        newGridPosL2 = WorldToGrid(playerObject.transform.position, gridMinSize * 3);
        prevGridPosL3 = WorldToGrid(playerObject.transform.position, gridMinSize * 9);
        newGridPosL3 = WorldToGrid(playerObject.transform.position, gridMinSize * 9);
    }

    void CreateCubeLayers(Vector3 targetPosition)
    {
        int colorCounter = 0;
        GridPos pos = WorldToGrid(targetPosition, gridMinSize);
        GridPos vpos;
        GridPos gpos;


        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {                                        
                    //Debug.Log(String.Format("{0} {1} {2}", gpos.x, gpos.y, gpos.z));
                    
                    gpos = new GridPos(pos.x + x, pos.y + y, pos.z + z);

                    vpos = new GridPos(pos.x + x * gridMinSize * 9, pos.y + y * gridMinSize * 9, pos.z + z * gridMinSize * 9);
                    L3Dict.Add(gpos.ToKeyString(), createRenderCube(vpos, new Vector3(45f, 0.1f, 45f), baseColors[colorCounter % 2 + 4]));

                    vpos = new GridPos(pos.x + x * gridMinSize * 3, pos.y + y * gridMinSize * 3, pos.z + z * gridMinSize * 3);
                    L2Dict.Add(gpos.ToKeyString(), createRenderCube(vpos, new Vector3(15f, 1.5f, 15f), baseColors[colorCounter % 2 + 2]));
                                        
                    vpos = new GridPos(pos.x + x * gridMinSize, pos.y + y * gridMinSize, pos.z + z * gridMinSize);
                    L1Dict.Add(gpos.ToKeyString(), createRenderCube(vpos, new Vector3(5f, 3f, 5f), baseColors[colorCounter++ % 2]));
                }
            }
        }        
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
        newGridPosL = WorldToGrid(playerObject.transform.position, gridSize);
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
        if(Input.GetKeyDown(KeyCode.Keypad8))
        {
            playerObject.transform.Translate(new Vector3(0f, gridMinSize, gridMinSize));
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            playerObject.transform.Translate(new Vector3(0f, -gridMinSize, -gridMinSize));
        }


        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            playerObject.transform.Translate(new Vector3(gridMinSize, gridMinSize, gridMinSize));
        }
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            playerObject.transform.Translate(new Vector3(-gridMinSize, -gridMinSize, -gridMinSize));
        }

        UpdateGridCubes(ref prevGridPosL1, ref newGridPosL1, gridMinSize, L1Dict, 1);
        UpdateGridCubes(ref prevGridPosL2, ref newGridPosL2, gridMinSize * 3, L2Dict, 2);
        UpdateGridCubes(ref prevGridPosL3, ref newGridPosL3, gridMinSize * 9, L3Dict, 3);
    }


    private GameObject createRenderCube(GridPos pos, Vector3 scale, Color color)
    {
        newVertices = createVertices(scale);
        newTriangles = createTriangles();

        var obj = new GameObject("MeshObject");
        obj.AddComponent<MeshRenderer>();
        obj.AddComponent<MeshFilter>();
        obj.transform.position = new Vector3(pos.x, pos.y, pos.z);

        var m = new Mesh();
        obj.GetComponent<MeshFilter>().mesh = m;
        m.vertices = newVertices;
        m.triangles = newTriangles;


        obj.GetComponent<MeshRenderer>().material = baseMaterial;
        obj.GetComponent<MeshRenderer>().material.color = color;
        return obj;
    }

    private Vector3[] createVertices(Vector3 scale)
    {
        int idx;
        var vt = new List<Vector3>();

        // Order Vertices per QUAD - 0, 2, 1, 3
        for (var i = 0; i < polyIdx.Length; i += 4)
        {
            idx = (polyIdx[i] >= 0) ? polyIdx[i] * 3 : ((polyIdx[i] * -1) - 1) * 3;
            vt.Add(new Vector3(-(float)cubeVerts[idx] * scale.x, (float)cubeVerts[idx + 1] * scale.y, (float)cubeVerts[idx + 2] * scale.z));

            idx = (polyIdx[i + 2] >= 0) ? polyIdx[i + 2] * 3 : ((polyIdx[i + 2] * -1) - 1) * 3;
            vt.Add(new Vector3(-(float)cubeVerts[idx] * scale.x, (float)cubeVerts[idx + 1] * scale.y, (float)cubeVerts[idx + 2] * scale.z));

            idx = (polyIdx[i + 1] >= 0) ? polyIdx[i + 1] * 3 : ((polyIdx[i + 1] * -1) - 1) * 3;
            vt.Add(new Vector3(-(float)cubeVerts[idx] * scale.x, (float)cubeVerts[idx + 1] * scale.y, (float)cubeVerts[idx + 2] * scale.z));

            idx = (polyIdx[i + 3] >= 0) ? polyIdx[i + 3] * 3 : ((polyIdx[i + 3] * -1) - 1) * 3;
            vt.Add(new Vector3(-(float)cubeVerts[idx] * scale.x, (float)cubeVerts[idx + 1] * scale.y, (float)cubeVerts[idx + 2] * scale.z));
        }
        return vt.ToArray();
    }

    private int[] createTriangles()
    {
        var vl = new List<int>();

        //Create sequence based on Quads creating 2 triagles in this vertex sequence { 0, 1, 2, 0, 3, 1 }
        for (int i = 0; i < 24; i += 4)
        {
            vl.Add(i);
            vl.Add(i + 1);
            vl.Add(i + 2);

            vl.Add(i);
            vl.Add(i + 3);
            vl.Add(i + 1);
        }
        return vl.ToArray();
    }

    // Grid Handling Functions
    private GridPos WorldToGrid(Vector3 worldPosition, int gridSize)
    {
        var x = Mathf.RoundToInt(worldPosition.x / gridSize);
        var y = Mathf.RoundToInt(worldPosition.y / gridSize);
        var z = Mathf.RoundToInt(worldPosition.z / gridSize);
        return new GridPos(x, y, z);
    }
}
