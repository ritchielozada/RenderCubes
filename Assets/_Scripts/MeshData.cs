using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Policy;
using UnityEditor;

[Serializable]
public class MeshData
{
    public Vector3[] vertices { get; set; }
    public int[] triangles { get; set; }

    public Vector2[] uv{ get; set; }
}
