using UnityEngine;

namespace Common.ParticleSystem
{
    [System.Serializable]
    public class ParticleTriangle 
    {

        public void OnBegin(GameObject gameObject, Material setMat)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if(meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            meshRenderer.sharedMaterial = setMat;
            meshRenderer.receiveShadows = false;

            Vector3[] poss = new Vector3[3];
            int[] tris = new int[3];
            poss[0] = new Vector3(-100, 0, -100);
            poss[1] = new Vector3(0, 0, 100);
            poss[2] = new Vector3(100, 0, 100);
            tris[0] = 1;
            tris[1] = 2;
            tris[2] = 0;

            Mesh mesh = new Mesh();
            mesh.vertices = poss;
            mesh.triangles = tris;


            meshFilter.mesh = mesh;

        }
    }
}