using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    /// <summary>
    /// ���뵽Clust��Ⱦջ�Ļ��࣬Ҳ��ģ���࣬�ܹ�֧��һ����ȫ��̬���������Clust���ƣ�
    /// �����Ҫ��չһЩ��֮���Clust�����Լ̳и���ͳһʵ��
    /// </summary>
    public abstract class GPUPipelineBase : MonoBehaviour
    {
        public string saveName;

        /// <summary>
        /// ��ȡ�洢·�������Խ���ͬ���͵�2�����ļ��ڲ�ͬ��·��
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
        /// ���𱣴�ĺ����������Ҫ�޸ı������ݣ�������д�ú���,
        /// �洢˳�򣺶��㡢�߿򡢷��ߡ����ߡ�uv
        /// </summary>
        /// <param name="mesh">����ĸ���Mesh</param>
        /// <param name="transform">��Ӧ��ģ�͵�transform</param>
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
        /// ��������д���ClustBase�����һ���Ӷ���ʱ���õķ�����
        /// Ŀǰ�Ƕ�ÿһ��ģ����ȡ���MeshFilter���д洢��
        /// Ҳ����һ��һ����Clust���зֿ��洢������б�Ҫ������д�ú�������ʹ��MeshFilter����Ϊ���ݴ洢�ĸ���
        /// </summary>
        /// <param name="game">�������һ�����壬���п����кܶ��cluster��</param>
        /// <param name="clustBase">�������ClustBase���</param>
        /// <returns>����������ɵ�����Clust���ı�</returns>
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
        /// �������һ�������Ƿ�Ҫ���п��ƣ������Щ�������Ⱦ�Ƚ����⣬
        /// ����ͨ����д�÷�����֤�����������ʱ�ļ�����
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