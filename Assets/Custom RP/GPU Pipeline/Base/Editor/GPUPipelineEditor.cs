using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static GPUPipelineSaveStruct;

namespace CustomRP.GPUPipeline
{
    public class GPUPipelineEditor : Editor
    {
        static string FindTagName = "GPU Pipline";
        private static List<Triangle> GetAllTriangles(Mesh mesh, GPUPipelineSaveStruct clustSaveStruct)
        {
            List<Triangle> triangles = new List<Triangle>();
            if (mesh == null)
            {
                Debug.Log("mesh is null");
                return null;
            }

            clustSaveStruct.pointsArray = mesh.vertices;
            clustSaveStruct.normalsArray = mesh.normals;
            clustSaveStruct.uv0sArray = mesh.uv;
            clustSaveStruct.tangesArray = mesh.tangents;
            int[] tris = mesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Triangle triangle = new Triangle();
                triangle.points = new int[3];
                triangle.points[0] = tris[i];
                triangle.points[1] = tris[i + 1];
                triangle.points[2] = tris[i + 2];

                triangles.Add(triangle);
            }

            return triangles;
        }

        /// <summary>
        /// 将相邻的三角面转化为四遍形面
        /// </summary>
        /// <param name="triangles">所有的三角形数据</param>
        private static List<Quardrangle> GetAllQuard(List<Triangle> triangles)
        {
            Triangle[] trianglesArray = triangles.ToArray();
            List<Quardrangle> intersectQuard = new List<Quardrangle>();
            for (int i = 0; i < trianglesArray.Length; i++)
            {
                if (trianglesArray[i].isUse) continue;
                for (int j = 0; j < trianglesArray.Length && !trianglesArray[i].isUse; j++)
                {
                    if (trianglesArray[j].isUse) continue;
                    if (trianglesArray[i].IsBorderUpon(trianglesArray[j]))
                    {
                        trianglesArray[i].isUse = true;
                        trianglesArray[j].isUse = true;
                        Quardrangle quard = new Quardrangle();
                        quard.isUse = false;
                        quard.triangle1 = trianglesArray[i];
                        quard.triangle2 = trianglesArray[j];
                        intersectQuard.Add(quard);
                        break;
                    }
                }
            }

            for (int i = 0; i < trianglesArray.Length; i++)
            {
                if (!trianglesArray[i].isUse)
                {
                    Quardrangle triangle = new Quardrangle();
                    triangle.triangle1 = trianglesArray[i];
                    triangle.triangle2 = trianglesArray[i];
                    intersectQuard.Add(triangle);
                }
            }

            return intersectQuard;
        }


        private static void ClipQuardrangleMeshByClust(List<Quardrangle> quard, 
            GameObject father, GPUPipelineSaveStruct clustSaveStruct)
        {
            Quardrangle[] quardrangles1 = quard.ToArray();

            //检查用的四边形分割
            //for (int i = 0; i < quard.Count; i++)
            //{
            //    GameObject go = new GameObject(count.ToString());
            //    go.transform.parent = father.transform;
            //    AddMeshTemp(go, quard[i]);
            //    count++;
            //}
            int count = 0;

            for (int i = 0; i < quardrangles1.Length; i++)
            {
                List<Quardrangle> intersectQuard = new List<Quardrangle>();
                if (quardrangles1[i].isUse) continue;
                quardrangles1[i].isUse = true;
                intersectQuard.Add(quardrangles1[i]);

                KeyValuePair<float, int>[] pair = new KeyValuePair<float, int>[quardrangles1.Length];
                for (int j = 0; j < quardrangles1.Length; j++)
                {
                    if (quardrangles1[j].isUse)
                    {
                        pair[j] = new KeyValuePair<float, int>(
                            float.MaxValue, j);
                    }
                    else
                    {
                        pair[j] = new KeyValuePair<float, int>(
                            quardrangles1[i].GetDistance(quardrangles1[j], clustSaveStruct), j);
                    }
                }
                Array.Sort(pair, (a, b) =>
                {
                    if (b.Key > a.Key) return -1;
                    else if (b.Key == a.Key) return 0;
                    else return 1;
                });


                foreach (KeyValuePair<float, int> index in pair)
                {
                    if (quardrangles1[index.Value].isUse) continue;
                    if (intersectQuard.Count == 16) break;
                    intersectQuard.Add(quardrangles1[index.Value]);
                    quardrangles1[index.Value].isUse = true;
                }


                GameObject gameObject = new GameObject(count.ToString());
                count++;
                gameObject.transform.parent = father.transform;
                gameObject.transform.position = father.transform.position;
                gameObject.transform.localScale = new Vector3(1, 1, 1);     //大小设置为和父亲一样即可
                gameObject.transform.rotation = father.transform.rotation;
                //Debug.Log(intersectQuard.Count + "Intet Count");
                AddMesh(gameObject, intersectQuard, clustSaveStruct);
                intersectQuard.Clear();
            }
        }

        private static bool CheckUpon(List<Quardrangle> intersectQuard, Quardrangle quardrangle)
        {
            for (int i = 0; i < intersectQuard.Count; i++)
            {
                if (quardrangle.IsBorderUpon(intersectQuard[i]))
                    return true;
            }
            return false;
        }

        private static void AddMesh(GameObject game, List<Quardrangle> quardrangles, 
            GPUPipelineSaveStruct clustSaveStruct)
        {
            int count = 16 - quardrangles.Count;
            for (int i = 0; i < count; i++)
            {
                quardrangles.Add(quardrangles[quardrangles.Count - 1]);
            }
            Mesh mesh = new Mesh();
            Vector3[] vecs = new Vector3[64];
            Vector3[] normals = new Vector3[64];
            Vector4[] tangles = new Vector4[64];
            Vector2[] uv0s = new Vector2[64];

            int[] tris = new int[96];
            for (int i = 0; i < quardrangles.Count; i++)
            {
                QuardrangleMeshData quardrangleMeshData = 
                    quardrangles[i].GetMeshData(clustSaveStruct);
                vecs[i * 4 + 0] = quardrangleMeshData.vects[0];
                vecs[i * 4 + 1] = quardrangleMeshData.vects[1];
                vecs[i * 4 + 2] = quardrangleMeshData.vects[2];
                vecs[i * 4 + 3] = quardrangleMeshData.vects[3];

                normals[i * 4 + 0] = quardrangleMeshData.normals[0];
                normals[i * 4 + 1] = quardrangleMeshData.normals[1];
                normals[i * 4 + 2] = quardrangleMeshData.normals[2];
                normals[i * 4 + 3] = quardrangleMeshData.normals[3];

                tangles[i * 4 + 0] = quardrangleMeshData.tanges[0];
                tangles[i * 4 + 1] = quardrangleMeshData.tanges[1];
                tangles[i * 4 + 2] = quardrangleMeshData.tanges[2];
                tangles[i * 4 + 3] = quardrangleMeshData.tanges[3];

                uv0s[i * 4 + 0] = quardrangleMeshData.uv0s[0];
                uv0s[i * 4 + 1] = quardrangleMeshData.uv0s[1];
                uv0s[i * 4 + 2] = quardrangleMeshData.uv0s[2];
                uv0s[i * 4 + 3] = quardrangleMeshData.uv0s[3];

                tris[i * 6 + 0] = i * 4 + quardrangleMeshData.tris[0];
                tris[i * 6 + 1] = i * 4 + quardrangleMeshData.tris[1];
                tris[i * 6 + 2] = i * 4 + quardrangleMeshData.tris[2];
                tris[i * 6 + 3] = i * 4 + quardrangleMeshData.tris[3];
                tris[i * 6 + 4] = i * 4 + quardrangleMeshData.tris[4];
                tris[i * 6 + 5] = i * 4 + quardrangleMeshData.tris[5];
            }

            mesh.vertices = vecs;
            mesh.triangles = tris;
            mesh.uv = uv0s;
            mesh.tangents = tangles;
            mesh.normals = normals;

            MeshFilter meshFilter = game.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
        }

        //四边形Mesh生成
        private static void AddMeshTemp(GameObject game, Quardrangle quardrangles, GPUPipelineSaveStruct clustSaveStruct)
        {
            Mesh mesh = quardrangles.GetMesh(clustSaveStruct);

            MeshFilter meshFilter = game.AddComponent<MeshFilter>();

            meshFilter.sharedMesh = mesh;
        }

        public static void ClipOneGameObject(GameObject game)
        {
            if (game == null) return;
            MeshFilter meshFilter = game.GetComponent<MeshFilter>();
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;

            GPUPipelineSaveStruct clustSaveStruct = new GPUPipelineSaveStruct();    //用来存储顶点数据

            List<Triangle> triangles = GetAllTriangles(mesh, clustSaveStruct);

            List<Quardrangle> intersectQuard = GetAllQuard(triangles);

            ClipQuardrangleMeshByClust(intersectQuard, game, clustSaveStruct);
        }

        [MenuItem("ClustObject/Clip")]
        public static void ClipControlObject()
        {
            GameObject[] gamess = GameObject.FindGameObjectsWithTag(FindTagName);
            if (gamess == null || gamess.Length == 0) return;
            for (int i = 0; i < gamess.Length; i++)
            {
                GameObject[] gameObjects = gamess[i].GetComponent<GPUPipelineBase>().controlObjects;
                Debug.Log("clip");

                if (gameObjects == null || gameObjects.Length == 0) return;
                foreach (GameObject games in gameObjects)
                {
                    if (games == null || !games.activeSelf)
                    {
                        continue;
                    }
                    ClipOneGameObject(games);
                    FileFuctions.WriteFile(Application.streamingAssetsPath + 
                        "/ClustLog.txt", gamess[i].name + " : " + games.name);
                }
            }

        }

        [MenuItem("ClustObject/Save")]
        public static void SaveClustData()
        {
            Debug.Log("Save");

            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(FindTagName);
            if (gameObjects == null || gameObjects.Length == 0) return;
            Debug.Log("Save Begin");

            for (int i = 0; i < gameObjects.Length; i++)
            {
                GPUPipelineBase clustBase = gameObjects[i].GetComponent<GPUPipelineBase>();
                //string path = Application.streamingAssetsPath + "/ClustData/" + clustBase.saveName;
                string path = clustBase.GetSavePath();
                if (path == null) continue;         //表示不需要保存
                GameObject[] allObjs = clustBase.controlObjects;
                if (allObjs == null || allObjs.Length == 0) break;

                FileFuctions.WriteFile(path, "");

                foreach (GameObject games in allObjs)
                {
                    if (games == null || !games.activeSelf)
                        continue;

                    FileFuctions.AppandWriteFile(path, clustBase.ReadyData(games, clustBase));
                    FileFuctions.WriteFile(Application.streamingAssetsPath + "/ClustLog.txt", gameObjects[i].name + " : " + games.name);
                }
            }
            Debug.Log("Save Complete");
        }



        //[MenuItem("ClustObject/Create Mesh")]
        //public static void CreateMeshTex()
        //{
        //    GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Clust");
        //    if (gameObjects == null || gameObjects.Length == 0) return;
        //    for (int i = 0; i < gameObjects.Length; i++)
        //    {
        //        //ClustControl clustControl = gameObjects[i].GetComponent<ClustControl>();
        //        GPUPipelineBase clustBase = gameObjects[i].GetComponent<GPUPipelineBase>();
        //        Mesh createMesh = clustBase.createMesh;
        //        if (createMesh == null) continue;
        //        GameObject createFather = new GameObject("ClustFather");
        //        createFather.tag = "Clust";
        //        GPUPipelineBase fatherClust = createFather.AddComponent<GPUPipelineBase>();
        //        fatherClust.controlObjects = new GameObject[10000];
        //        for (int j = 0; j < 100; j++)
        //        {
        //            for (int k = 0; k < 100; k++)
        //            {
        //                GameObject go = new GameObject((j * 100 + k).ToString());
        //                go.AddComponent<MeshFilter>().mesh = createMesh;
        //                go.transform.position = new Vector3(j, 0, k);
        //                fatherClust.controlObjects[j * 100 + k] = go;
        //                go.transform.parent = createFather.transform;
        //            }

        //        }
        //    }
        //}

        [MenuItem("ClustObject/Remove Null Control")]
        public static void RemoveNullControl()
        {
            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(FindTagName);
            if (gameObjects == null || gameObjects.Length == 0) return;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                GPUPipelineBase clustBase = gameObjects[i].GetComponent<GPUPipelineBase>();
                List<GameObject> controls = new List<GameObject>();
                for(int j=0; j<clustBase.controlObjects.Length; j++)
                {
                    if (clustBase.controlObjects[j] != null)
                        controls.Add(clustBase.controlObjects[j]);
                }
                
                clustBase.controlObjects = controls.ToArray();
            }
        }

        //用来将所有有MeshFilter组建的物体都变为控制物体
        [MenuItem("ClustObject/Add All Child In Control")]
        public static void AddChild()
        {
            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(FindTagName);
            if (gameObjects == null || gameObjects.Length == 0) return;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                GPUPipelineBase clustBase = gameObjects[i].GetComponent<GPUPipelineBase>();
                if (clustBase == null) continue;
                GameObject[] meshFilter = SearchAllChildHaveMeshFilter(gameObjects[i], clustBase);
                clustBase.controlObjects = meshFilter;
            }
        }

        private static GameObject[] SearchAllChildHaveMeshFilter(GameObject game, GPUPipelineBase pipeBase)
        {
            Queue<Transform> allTransform = new Queue<Transform>();
            List<GameObject> meshFilterList = new List<GameObject>();
            for(int i=0; i<game.transform.childCount; i++)
            {
                if (!game.transform.GetChild(i).gameObject.activeSelf) continue;
                allTransform.Enqueue(game.transform.GetChild(i));
            }


            while (allTransform.Count != 0)
            {
                Transform nowIndexx = allTransform.Dequeue();
                if (!nowIndexx.gameObject.activeSelf) continue;

                for (int i = 0; i < nowIndexx.childCount; i++)
                {
                    allTransform.Enqueue(nowIndexx.GetChild(i));
                }
                //MeshFilter mesh = nowIndexx.GetComponent<MeshFilter>();
                //if (mesh != null)
                //    meshFilterList.Add(nowIndexx.gameObject);
                if (pipeBase.CheckNeedControl(nowIndexx.gameObject))
                {
                    meshFilterList.Add(nowIndexx.gameObject);
                }

            }
            return meshFilterList.ToArray();
        }

        //用来将所有控制的子物体的裁剪数据都清除的方法
        [MenuItem("ClustObject/Remove Control Child Obj")]
        public static void RemoveControlObjChild()
        {
            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(FindTagName);
            if (gameObjects == null || gameObjects.Length == 0) return;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (!gameObjects[i].activeSelf) continue;
                GPUPipelineBase clustBase = gameObjects[i].GetComponent<GPUPipelineBase>();
                if (clustBase == null || clustBase.controlObjects == null
                    || clustBase.controlObjects.Length == 0)
                {
                    Debug.Log(gameObjects[i].name);
                    continue;
                }
                foreach (GameObject gameObject in clustBase.controlObjects)
                {
                    while (gameObject.transform.childCount != 0)
                    {
                        DestroyImmediate(gameObject.transform.GetChild(0).gameObject);
                    }
                }
            }
        }
    }
}