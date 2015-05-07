using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Policy;
using UnityEditor;


[Serializable]
public class GridPos
{
    public int x { get; set; }
    public int y { get; set; }
    public int z { get; set; }

    public GridPos(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public bool Equals(GridPos pos)
    {
        if ((x == pos.x) && (y == pos.y) && (z == pos.z))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public GridPos Delta(GridPos newPos)
    {
        GridPos g = new GridPos(
            -(x - newPos.x),
            -(y - newPos.y),
            -(z - newPos.z));
        return g;
    }

    public string ToKeyString()
    {
        return String.Format("{0},{1},{2}", x, y, z);
    }
}
