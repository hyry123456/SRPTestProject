using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    /// <summary>
    /// 传入到Clust渲染栈的基类，也是模板类，能够支持一般完全静态的物体进行Clust绘制，
    /// 如果需要拓展一些树之类的Clust，可以继承该类统一实现
    /// </summary>
    public abstract class GPUPipelineBase : MonoBehaviour
    {
        public string saveName;

        /// <summary>
        /// 获取存储路径，可以将不同类型的2进制文件在不同的路径
        /// </summary>
        public virtual string GetSavePath()
        {
            return null;
        }
        public GameObject[] controlObjects;

        protected void ExecuteBuffer(ref CommandBuffer buffer, ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public abstract void DrawClustByCamera(ScriptableRenderContext context,
            CommandBuffer buffer, ClustDrawType drawType, Camera camera);

        public abstract void DrawClustByProjectMatrix(ScriptableRenderContext context,
            CommandBuffer buffer, ClustDrawType drawType, Matrix4x4 projectMatrix);

        /// <summary>
        /// 负责保存的函数，如果需要修改保存数据，可以重写该函数,
        /// 存储顺序：顶点、边框、法线、切线、uv
        /// </summary>
        /// <param name="mesh">保存的根据Mesh</param>
        /// <param name="transform">对应的模型的transform</param>
        public virtual string ReadyMeshData(Mesh mesh, Transform transform)
        {
            Vector3 boundMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 boundMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            //StringBuilder context = new StringBuilder("vertices=");
            StringBuilder context = new StringBuilder("");
            //Vertices
            for (int i = 0; i < 48; i++)
            {
                Vector3 temp = transform.TransformPoint(mesh.vertices[i]);
                boundMax = GetBounds(boundMax, temp, true);
                boundMin = GetBounds(boundMin, temp, false);
                context.Append(Vertex3ToString(temp) + "|");
            }
            context.Append("\n");

            //context.Append("triangles=");
            //
            //for (int i = 0; i < mesh.triangles.Length; i++)
            //{
            //    context.Append(mesh.triangles[i].ToString() + "|");
            //}
            //context.Append("\n");

            //context.Append("bounds=");
            //Bounds
            context.Append(Vertex3ToString(boundMin) + "|");
            context.Append(Vertex3ToString(boundMax) + "|");
            context.Append("\n");

            //context.Append("normals=");
            //Normals
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                Vector3 temp = transform.TransformDirection(mesh.normals[i]);
                context.Append(Vertex3ToString(temp) + "|");
            }
            context.Append("\n");

            //context.Append("tanges=");
            //Tangents
            for (int i = 0; i < mesh.tangents.Length; i++)
            {
                Vector4 temp = mesh.tangents[i];
                Vector3 tangensWorldDir = transform.TransformDirection((Vector3)temp);
                temp.x = tangensWorldDir.x; temp.y = tangensWorldDir.y; temp.z = tangensWorldDir.z;
                context.Append(Vector4ToString(temp) + "|");
            }
            context.Append("\n");

            //context.Append("uv0s=");
            //UV0s
            for (int i = 0; i < mesh.uv.Length; i++)
            {
                Vector2 temp = mesh.uv[i];
                context.Append(Vector2ToString(temp) + "|");
            }
            context.Append("\n");

            return context.ToString();
        }

        /// <summary>
        /// 保存过程中处理ClustBase管理的一个子对象时调用的方法，
        /// 目前是对每一个模型提取其的MeshFilter进行存储，
        /// 也就是一个一个的Clust进行分开存储，如果有必要可以重写该函数，不使用MeshFilter来作为数据存储的根据
        /// </summary>
        /// <param name="game">被管理的一个物体，其中可能有很多个cluster块</param>
        /// <param name="clustBase">父对象的ClustBase组件</param>
        /// <returns>这个对象生成的所有Clust块文本</returns>
        public virtual string ReadyData(GameObject game, GPUPipelineBase clustBase, out int size)
        {
            Transform transform = game.transform;
            StringBuilder context = new StringBuilder("");
            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                context.Append("<");
                Transform child = transform.GetChild(i);
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    context.Append(">\n");
                    continue;
                }
                Mesh mesh = meshFilter.sharedMesh;
                context.Append(clustBase.ReadyMeshData(mesh, child.transform));

                context.Append(">\n");
                count++;
            }
            size = count;
            return context.ToString();
        }

        /// <summary>
        /// 用来检查一个物体是否要进行控制，如果有些物体的渲染比较特殊，
        /// 可以通过重写该方法保证输入控制物体时的兼容性
        /// </summary>
        public virtual bool CheckNeedControl(GameObject game)
        {
            MeshFilter mesh = game.GetComponent<MeshFilter>();
            if (mesh != null)
                return true;
            return false;
        }

        protected static string Vertex3ToString(Vector3 vector3)
        {
            return vector3.x.ToString() + "," + vector3.y.ToString() + "," + vector3.z.ToString();
        }

        protected static string Vector4ToString(Vector4 vector4)
        {
            return vector4.x.ToString() + "," + vector4.y.ToString() + "," + vector4.z.ToString() + "," + vector4.w.ToString();
        }
        protected static string Vector2ToString(Vector2 vector2)
        {
            return vector2.x.ToString() + "," + vector2.y.ToString();
        }

        protected static Vector3 GetBounds(Vector3 bound, Vector3 compareVe, bool isBig)
        {
            if (isBig)
            {
                bound.x = Mathf.Max(bound.x, compareVe.x);
                bound.y = Mathf.Max(bound.y, compareVe.y);
                bound.z = Mathf.Max(bound.z, compareVe.z);
            }
            else
            {
                bound.x = Mathf.Min(bound.x, compareVe.x);
                bound.y = Mathf.Min(bound.y, compareVe.y);
                bound.z = Mathf.Min(bound.z, compareVe.z);
            }
            return bound;
        }



    }
}