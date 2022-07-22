using UnityEditor;
using UnityEngine;

namespace CustomRP.GPUPipeline
{ 
    public class ClustEditor : Editor 
    {
        static string FindTagName = "GPU Pipline";

        [MenuItem("ClustObject/Create Mesh")]
        public static void CreateMeshTex()
        {
            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(FindTagName);
            if (gameObjects == null || gameObjects.Length == 0) return;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                //ClustControl clustControl = gameObjects[i].GetComponent<ClustControl>();
                ClustBase clustBase = gameObjects[i].GetComponent<ClustBase>();
                if (clustBase == null) continue;
                Mesh createMesh = clustBase.createMesh;
                if (createMesh == null) continue;
                GameObject createFather = new GameObject("ClustFather");
                createFather.tag = FindTagName;
                ClustBase fatherClust = createFather.AddComponent<ClustBase>();
                fatherClust.controlObjects = new GameObject[clustBase.createCount * clustBase.createCount];
                for (int j = 0; j < clustBase.createCount; j++)
                {
                    for (int k = 0; k < clustBase.createCount; k++)
                    {
                        GameObject go = new GameObject((j * clustBase.createCount + k).ToString());
                        go.AddComponent<MeshFilter>().mesh = createMesh;
                        go.transform.position = new Vector3(j, 0, k);
                        fatherClust.controlObjects[j * clustBase.createCount + k] = go;
                        go.transform.parent = createFather.transform;
                    }

                }
            }
        }
    }
}