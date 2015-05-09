using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridUtils
{
    public static Material baseMaterial;
    public static Color[] BaseColors = new Color[] { Color.red, Color.magenta, Color.blue, Color.cyan, Color.gray, Color.white };

    static float[] cubeVerts = new float[]
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
    static int[] polyIdx = new int[24] { 0, 1, 3, -3, 2, 3, 5, -5, 4, 5, 7, -7, 6, 7, 1, -1, 1, 7, 5, -4, 6, 0, 2, -5 };
    static int[] cubeTris;
    static Vector3[] cubeVertices;

    static Vector3[] newVertices;
    static Vector2[] newUV;
    static int[] newTriangles;

    public static GridPos WorldToGrid(Vector3 worldPosition, int gridSize)
    {
        var x = Mathf.RoundToInt(worldPosition.x / gridSize);
        var y = Mathf.RoundToInt(worldPosition.y / gridSize);
        var z = Mathf.RoundToInt(worldPosition.z / gridSize);
        return new GridPos(x, y, z);
    }

    public static Vector3[] CreateVertices(Vector3 scale)
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

    public static int[] CreateTriangles()
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

    public static GameObject CreateRenderCube(GridPos pos, Vector3 scale, Color color, GameObject parent)
    {
        newVertices = CreateVertices(scale);
        newTriangles = CreateTriangles();

        var obj = new GameObject("MeshObject");
        obj.AddComponent<MeshRenderer>();
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<BoxCollider>();
        obj.transform.position = new Vector3(pos.x, pos.y, pos.z);

        obj.GetComponent<BoxCollider>().size = new Vector3(scale.x * 2, scale.y * 2, scale.z * 2);

        var m = new Mesh();
        obj.GetComponent<MeshFilter>().mesh = m;
        m.vertices = newVertices;
        m.triangles = newTriangles;
        //m.uv = UVDATA


        obj.GetComponent<MeshRenderer>().material = baseMaterial;
        obj.GetComponent<MeshRenderer>().material.color = color;

        obj.GetComponent<MeshRenderer>().enabled = false;
        obj.transform.parent = parent.transform;
        
        //obj.GetComponent<MeshRenderer>().enabled = false;
        return obj;
    } 

}
