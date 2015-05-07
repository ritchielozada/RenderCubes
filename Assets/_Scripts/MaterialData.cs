using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Policy;
using UnityEditor;

[Serializable]
public class MaterialData
{
    public Material material { get; set; }
    public Color color { get; set; }
}
