using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ClustDataLoad
{
    [System.Serializable]
    public struct ClustData
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Vector2[] UV0s;
        public int[] tris;
        public Vector3 boundMax;
        public Vector3 boundMix;

    }

    public struct Triangle
    {
        public Vector3 point0;
        public Vector3 normal0;
        public Vector4 tangen0;
        public Vector2 uv0_0;

        public Vector3 point1;
        public Vector3 normal1;
        public Vector4 tangen1;
        public Vector2 uv0_1;

        public Vector3 point2;
        public Vector3 normal2;
        public Vector4 tangen2;
        public Vector2 uv0_2;
    }

    public static List<Triangle> GetAllTriangle(List<ClustData> clustDatas)
    {
        List<Triangle> triangles = new List<Triangle>();
        for (int i = 0; i < clustDatas.Count; i++)
        {
            for (int j = 0; j < 96; j += 3)
            {
                Triangle triangle = new Triangle();
                triangle.point0 = clustDatas[i].Positions[clustDatas[i].tris[j]];
                triangle.normal0 = clustDatas[i].Normals[clustDatas[i].tris[j]];
                triangle.uv0_0 = clustDatas[i].UV0s[clustDatas[i].tris[j]];
                triangle.tangen0 = clustDatas[i].Tangents[clustDatas[i].tris[j]];

                triangle.point1 = clustDatas[i].Positions[clustDatas[i].tris[j + 1]];
                triangle.normal1 = clustDatas[i].Normals[clustDatas[i].tris[ j + 1]];
                triangle.uv0_1 = clustDatas[i].UV0s[clustDatas[i].tris[j + 1]];
                triangle.tangen1 = clustDatas[i].Tangents[clustDatas[i].tris[j + 1]];

                triangle.point2 = clustDatas[i].Positions[clustDatas[i].tris[j + 2]];
                triangle.normal2 = clustDatas[i].Normals[clustDatas[i].tris[j + 2]];
                triangle.uv0_2 = clustDatas[i].UV0s[clustDatas[i].tris[j + 2]];
                triangle.tangen2 = clustDatas[i].Tangents[clustDatas[i].tris[j + 2]];

                triangles.Add(triangle);
            }
        }
        return triangles;
    }

    public struct BoundsData
    {
        public Vector3 boundMin;
        public Vector3 boundMax;
    }

    public static List<BoundsData> LoadBoundsData(List<ClustData> clustDatas)
    {
        List<BoundsData> boundsDatas = new List<BoundsData>();
        foreach (ClustData clustData in clustDatas)
        {
            BoundsData boundsData = new BoundsData();
            boundsData.boundMin = clustData.boundMix;
            boundsData.boundMax = clustData.boundMax;
            boundsDatas.Add(boundsData);
        }
        return boundsDatas;
    }

    public static List<Vector3> LoadPositionData(List<ClustData> clustDatas)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (ClustData clustData in clustDatas)
        {
            for(int i=0; i<clustData.Positions.Length; i++)
            {
                positions.Add(clustData.Positions[i]);
            }
        }
        return positions;
    }

    public static List<int> LoadTriangleData(List<ClustData> clustDatas)
    {
        List<int> positions = new List<int>();
        foreach (ClustData clustData in clustDatas)
        {
            for (int i = 0; i < clustData.Positions.Length; i++)
            {
                positions.Add(clustData.tris[i]);
            }
        }
        return positions;
    }

    /// <summary>
    /// Create Mesh By ClustData
    /// </summary>
    public static Mesh GetMeshByClustData(ClustData clustData)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = clustData.Positions;
        mesh.triangles = clustData.tris;
        return mesh;
    }

    /// <summary>
    /// Create Mesh by ClustList, Read All Clust to Create a Mesh
    /// </summary>
    public static Mesh GetAllMesh(List<ClustData> clustDatas)
    {
        Mesh mesh = new Mesh();
        Vector3[] vector3s = new Vector3[clustDatas.Count * 64];
        int[] tris = new int[clustDatas.Count * 96];
        //Debug.Log(clustDatas.Count + "size");
        for(int i=0; i<clustDatas.Count; i++)
        {
            for(int j=0; j<64; j++)
            {
                vector3s[i*64+j] = clustDatas[i].Positions[j];
            }

            for(int j=0; j<96; j++)
            {
                tris[i* 96 + j] = i * 64 + clustDatas[i].tris[j];
            }
            //Debug.Log(i);
        }
        mesh.vertices = vector3s;
        mesh.triangles = tris;
        return mesh;
    }

    /// <summary>
    /// Main Function, Load All Clust Data By file path
    /// </summary>
    public static List<ClustData> LoadClust(string path)
    {
        string allStr = FileFuctions.LoadAllStr(path);
        if (allStr == null)
        {
            Debug.LogError(path+"文件丢失");
            return null;
        }

        List<ClustData> clustDatas = new List<ClustData>();
        List<string> clipClust = FileFuctions.ClipByAngleBrackets(allStr);
        for (int i = 0; i < clipClust.Count; i++)
        {
            clustDatas.Add(LoadOneClustData(clipClust[i]));
        }
        return clustDatas;
    }

    //读取每一行的数据，每一行用\n分割
    private static string[] GetEveryLine(string oneClustData)
    {
        List<string> lines = new List<string>(oneClustData.Split('\n'));
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines.Count <= 1) lines.RemoveAt(i);
        }
        return lines.ToArray();
    }

    private static Vector3[] GetVector3(string strs)
    {
        List<string> positions = new List<string>(strs.Split('|'));
        Vector3[] result = new Vector3[positions.Count];
        for (int i = 0; i < positions.Count - 1; i++)
        {
            string[] vs = positions[i].Split(',');
            result[i] = new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]));
        }
        return result;
    }

    private static Vector2[] GetVector2(string strs)
    {
        List<string> positions = new List<string>(strs.Split('|'));
        Vector2[] result = new Vector2[positions.Count];
        for (int i = 0; i < positions.Count - 1; i++)
        {
            string[] vs = positions[i].Split(',');
            result[i] = new Vector2(float.Parse(vs[0]), float.Parse(vs[1]));
        }
        return result;
    }

    private static Vector4[] GetVector4(string strs)
    {
        List<string> positions = new List<string>(strs.Split('|'));
        Vector4[] result = new Vector4[positions.Count];
        for (int i = 0; i < positions.Count - 1; i++)
        {
            string[] vs = positions[i].Split(',');
            result[i] = new Vector4(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
        }
        return result;
    }

    private static int[] GetTris(string strs)
    {
        List<string> positions = new List<string>(strs.Split('|'));
        int[] result = new int[positions.Count];
        for (int i = 0; i < positions.Count - 1; i++)
        {
            result[i] = int.Parse(positions[i].Trim());
        }
        return result;
    }

    private static ClustData LoadOneClustData(string oneClustData)
    {
        string[] lines = GetEveryLine(oneClustData);
        ClustData clustData = new ClustData();
        for (int i = 0; i < lines.Length; i++)
        {
            string[] line = lines[i].Split('=');

            switch (line[0])
            {
                case "vertices":
                    clustData.Positions = GetVector3(line[1]);
                    break;
                case "triangles":
                    clustData.tris = GetTris(line[1]);
                    break;

                case "bounds":
                    Vector3[] vs = GetVector3(line[1]);
                    clustData.boundMix = vs[0];
                    clustData.boundMax = vs[1];
                    break;

                case "tanges":
                    clustData.Tangents = GetVector4(line[1]);
                    break;

                case "normals":
                    clustData.Normals = GetVector3(line[1]);
                    break;
                case "uv0s":
                    clustData.UV0s = GetVector2(line[1]);
                    break;

            }
        }
        return clustData;
    }
}
