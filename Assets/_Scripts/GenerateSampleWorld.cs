using UnityEngine;
using System.Collections;


public class GenerateSampleWorld : MonoBehaviour
{
    private const int gridMinSize = 100;

    void Start()
    {
        CreateWorldLayers();
    }

    private GameObject CreateWorldCube(GridPos pos, Vector3 scale, Color color)
    {
        //newVertices = createVertices(scale);
        //newTriangles = createTriangles();

        var obj = new GameObject("MeshObject");
        obj.AddComponent<MeshRenderer>();
        obj.AddComponent<MeshFilter>();
        obj.transform.position = new Vector3(pos.x, pos.y, pos.z);

        var m = new Mesh();
        obj.GetComponent<MeshFilter>().mesh = m;
        //m.vertices = newVertices;
        //m.triangles = newTriangles;
        //m.uv = UVDATA


        //obj.GetComponent<MeshRenderer>().material = baseMaterial;
        obj.GetComponent<MeshRenderer>().material.color = color;
        return obj;
    }

    void CreateWorldLayers()
    {
        int colorCounter = 0;
        GridPos pos = GridUtils.WorldToGrid(transform.position, gridMinSize);
        GridPos vpos;
        GridPos gpos;


        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    gpos = new GridPos(pos.x + x, pos.y + y, pos.z + z);

                    vpos = new GridPos(pos.x + x * gridMinSize * 9, pos.y + y * gridMinSize * 9, pos.z + z * gridMinSize * 9);
                    //L3Dict.Add(gpos.ToKeyString(), createRenderCube(vpos, new Vector3(45f, 0.1f, 45f), baseColors[colorCounter % 2 + 4]));

                    vpos = new GridPos(pos.x + x * gridMinSize * 3, pos.y + y * gridMinSize * 3, pos.z + z * gridMinSize * 3);
                    //L2Dict.Add(gpos.ToKeyString(), createRenderCube(vpos, new Vector3(15f, 1.5f, 15f), baseColors[colorCounter % 2 + 2]));

                    vpos = new GridPos(pos.x + x * gridMinSize, pos.y + y * gridMinSize, pos.z + z * gridMinSize);
                    //L1Dict.Add(gpos.ToKeyString(), createRenderCube(vpos, new Vector3(5f, 3f, 5f), baseColors[colorCounter++ % 2]));
                }
            }
        }
    }
}
