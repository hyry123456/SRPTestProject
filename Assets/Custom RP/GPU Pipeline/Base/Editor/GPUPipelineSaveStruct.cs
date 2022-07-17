using UnityEngine;

public class GPUPipelineSaveStruct
{
    public struct QuardrangleMeshData
    {
        public Vector3[] vects;
        public Vector3[] normals;
        public Vector2[] uv0s;
        public Vector4[] tanges;

        public int[] tris;
    }

    public Vector3[] pointsArray;
    public Vector3[] normalsArray;
    public Vector2[] uv0sArray;
    public Vector4[] tangesArray;

    public struct Triangle
    {
        public int[] points;
        //public Vector3[] normals;
        //public Vector2[] uv0s;
        //public Vector4[] tanges;
       
        public bool isUse;

        public bool IsBorderUpon(Triangle triangle)
        {
            int index = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    if(points[i].Equals( triangle.points[k] ))
                    {
                        index++;
                    }
                }
            }

            if (index >= 2)
                return true;
            return false;
        }

        public Mesh GetMesh(Triangle triangle, Vector3[] pointsArray)
        {
            Mesh mesh = new Mesh();
            Vector3[] vectes = new Vector3[4];
            vectes[0] = pointsArray[points[0]];
            vectes[1] = pointsArray[points[1]];
            vectes[2] = pointsArray[points[2]];

            int[] tris = new int[6];

            int[] vs = GetEqualData(triangle);
            tris[0] = 0;
            tris[1] = 1;
            tris[2] = 2;

            for (int i = 0; i < 3; i++)
            {
                if (vs[i] > 2)
                {
                    vectes[3] = pointsArray[triangle.points[vs[i] - 3]];
                    break;
                }
            }
            tris[3] = (vs[0] > 2) ? 3 : vs[0];
            tris[4] = (vs[1] > 2) ? 3 : vs[1];
            tris[5] = (vs[2] > 2) ? 3 : vs[2];

            mesh.vertices = vectes;
            mesh.triangles = tris;

            return mesh;
        }

        public int[] GetEqualData(Triangle triangle)
        {
            int[] vectes = new int[3];
            vectes[0] = vectes[1] = vectes[2] = -1;

            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (points[i].Equals(triangle.points[k]))
                    {
                        vectes[k] = i;
                        break;
                    }
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (vectes[i] == -1) vectes[i] = i + 3;
            }
            return vectes;
        }

        public Vector3 GetCenter(GPUPipelineSaveStruct clustSaveStruct)
        {
            return (clustSaveStruct.pointsArray[points[0]] +
                clustSaveStruct.pointsArray[points[1]] +
                clustSaveStruct.pointsArray[points[2]]) /3;
        }
    }

    public struct Quardrangle
    {
        public Triangle triangle1;
        public Triangle triangle2;
        public bool isUse;

        public void SetUse()
        {
            isUse = true;
        }

        public bool IsBorderUpon(Quardrangle queue)
        {
            if (triangle1.IsBorderUpon(queue.triangle1) || triangle1.IsBorderUpon(queue.triangle2))
            {
                return true;
            }
            if (triangle2.IsBorderUpon(queue.triangle1) || triangle2.IsBorderUpon(queue.triangle2))
            {
                return true;
            }
            return false;
        }

        public QuardrangleMeshData GetMeshData(GPUPipelineSaveStruct clustSaveStruct)
        {
            Vector3[] vectes = new Vector3[4];
            Vector3[] normals = new Vector3[4];
            Vector4[] tangles = new Vector4[4];
            Vector2[] uv0s = new Vector2[4];

            int index = triangle1.points[0];
            vectes[0] =  clustSaveStruct.pointsArray[index];
            normals[0] = clustSaveStruct.normalsArray[index];
            tangles[0] = clustSaveStruct.tangesArray[index];
            uv0s[0] =    clustSaveStruct.uv0sArray[index];

            index = triangle1.points[1];
            vectes[1] =  clustSaveStruct.pointsArray[index];
            normals[1] = clustSaveStruct.normalsArray[index];
            tangles[1] = clustSaveStruct.tangesArray[index];
            uv0s[1] =    clustSaveStruct.uv0sArray[index];

            index = triangle1.points[2];
            vectes[2] =     clustSaveStruct.pointsArray[index];
            normals[2] =    clustSaveStruct.normalsArray[index];
            tangles[2] =    clustSaveStruct.tangesArray[index];
            uv0s[2] =       clustSaveStruct.uv0sArray[index];

            int[] tris = new int[6];

            int[] vs = triangle1.GetEqualData(triangle2);
            tris[0] = 0;
            tris[1] = 1;
            tris[2] = 2;

            for (int i = 0; i < 3; i++)
            {
                if (vs[i] > 2)
                {
                    index = triangle2.points[vs[i] - 3];
                    vectes[3] =     clustSaveStruct.pointsArray[index];
                    normals[3] =    clustSaveStruct.normalsArray[index];
                    tangles[3] =    clustSaveStruct.tangesArray[index];
                    uv0s[3] =       clustSaveStruct.uv0sArray[index];
                    break;
                }
            }
            tris[3] = (vs[0] > 2) ? 3 : vs[0];
            tris[4] = (vs[1] > 2) ? 3 : vs[1];
            tris[5] = (vs[2] > 2) ? 3 : vs[2];

            QuardrangleMeshData meshData = new QuardrangleMeshData();
            meshData.vects = vectes;
            meshData.tris = tris;
            meshData.normals = normals;
            meshData.tanges = tangles;
            meshData.uv0s = uv0s;

            return meshData;
        }

        public Mesh GetMesh(GPUPipelineSaveStruct clustSaveStruct)
        {
            Vector3[] vectes = new Vector3[4];
            vectes[0] = clustSaveStruct.pointsArray[triangle1.points[0]];
            vectes[1] = clustSaveStruct.pointsArray[triangle1.points[1]];
            vectes[2] = clustSaveStruct.pointsArray[triangle1.points[2]];

            int[] tris = new int[6];

            int[] vs = triangle1.GetEqualData(triangle2);
            tris[0] = 0;
            tris[1] = 1;
            tris[2] = 2;

            for (int i = 0; i < 3; i++)
            {
                if (vs[i] > 2)
                {
                    vectes[3] = clustSaveStruct.pointsArray[triangle2.points[vs[i] - 3]];
                    break;
                }
            }
            tris[3] = (vs[0] > 2) ? 3 : vs[0];
            tris[4] = (vs[1] > 2) ? 3 : vs[1];
            tris[5] = (vs[2] > 2) ? 3 : vs[2];

            Mesh mesh = new Mesh();
            mesh.vertices = vectes;
            mesh.triangles = tris;

            return mesh;
        }

        public float GetDistance(Quardrangle queue, GPUPipelineSaveStruct clustSaveStruct)
        {
            Vector3 center1 = (triangle1.GetCenter(clustSaveStruct) 
                + triangle2.GetCenter(clustSaveStruct)) / 2;
            Vector3 center2 = (queue.triangle1.GetCenter(clustSaveStruct) 
                + queue.triangle2.GetCenter(clustSaveStruct)) / 2;
            return (center2 - center1).sqrMagnitude;
        }
    }
}
