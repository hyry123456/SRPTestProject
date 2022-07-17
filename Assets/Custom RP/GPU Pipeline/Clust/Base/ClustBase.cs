using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline.Clust
{
    public class ClustBase : GPUPipelineBase
    {
        public ClustBase()
        {
            saveName = "clustSaveData.clustData";
        }
        public override string GetSavePath()
        {
            return Application.streamingAssetsPath + "/ClustData/" + saveName;
        }
        public ComputeShader compute;
        public Material showMat;
        public Mesh createMesh;             //用于大量生成Mesh时使用的Mesh对象

        List<ClustDataLoad.Triangle> triangles;

        private ComputeBuffer cullResult;
        private ComputeBuffer lightCullResult;
        private ComputeBuffer argsMeshBuffer;       //绘制时需要的标识buffer
        private uint[] argsMesh = new uint[5] { 0, 0, 0, 0, 0 };    //标识buffer需要的数据

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer boundsBuffer;

        private Mesh singleMesh;
        private bool isInsert;

        //定义需要传入的数据，这些数据一般没有必要改变，只是名称而已
        int inputPosId = Shader.PropertyToID("inputPos"),
            inputBoundId = Shader.PropertyToID("inputBound"),
            cullresultId = Shader.PropertyToID("cullresult"),
            clustLengthId = Shader.PropertyToID("clustLength"),
            planesId = Shader.PropertyToID("planes"),
            projectMatrixId = Shader.PropertyToID("_ProjectMatrix"),
            lightCullResultId = Shader.PropertyToID("_LightCullResult");

        protected int Cam_Cull_kernel;            //摄像机视锥体剔除的方法编号
        protected int Pro_Cull_kernel;            //投影矩阵剔除的方法编号
        protected int Cam_Light_Cull_kernel;      //包含灯光剔除的视锥体剔除方法编号

        /// <summary>
        /// 更新目前的buffer数据，也就是根据传入值生成对应的buffer,
        /// 需要拓展创建时重写该方法，因为该方法给模型传入的只有顶点、法线、切线、uv0
        /// </summary>
        /// <param name="clustData">传入的模型数据</param>
        protected virtual void UpdateBuffer(List<ClustDataLoad.ClustData> clustData)
        {
            if (clustData == null) return;
            if (singleMesh == null) CreateSingleMesh();
            argsMeshBuffer = new ComputeBuffer(1, argsMesh.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            triangles = ClustDataLoad.GetAllTriangle(clustData);

            argsMesh[0] = singleMesh.GetIndexCount(0);
            argsMesh[1] = (uint)triangles.Count;
            argsMesh[2] = singleMesh.GetIndexStart(0);
            argsMesh[3] = singleMesh.GetBaseVertex(0);
            argsMeshBuffer.SetData(argsMesh);

            if (boundsBuffer != null)
                boundsBuffer.Release();
            boundsBuffer = new ComputeBuffer(clustData.Count, sizeof(float) * 2 * 3);
            boundsBuffer.SetData(ClustDataLoad.LoadBoundsData(clustData));

            if (triangleBuffer != null)
                triangleBuffer.Release();
            triangleBuffer = new ComputeBuffer(triangles.Count, sizeof(float) * 3 * (3 + 3 + 4 + 2));
            triangleBuffer.SetData(triangles);

            if (cullResult != null)
                cullResult.Release();
            //声明buffer中的数据数量，设置的数据是剔除矩阵的数据大小，声明buffer为添加buffer
            //暂时裁剪结果的数据大小和三角形的数据大小一致，之后需要修改，添加光照数据
            cullResult = new ComputeBuffer(triangles.Count, sizeof(float) * 3 * (3 + 3 + 4 + 2), ComputeBufferType.Append);

            if (lightCullResult != null)
                lightCullResult.Release();
            lightCullResult = new ComputeBuffer(triangles.Count, 4 * (3 * (3 + 3 + 4 + 2) + 2), ComputeBufferType.Append);
        }

        /// <summary>        /// 生成一个三角形Mesh，方便之后进行绘制        /// </summary>
        protected void CreateSingleMesh()
        {
            if (singleMesh != null) return;
            singleMesh = new Mesh();
            Vector3[] vector3 = new Vector3[3];
            vector3[0] = Vector3.zero;
            vector3[1] = Vector3.zero;
            vector3[2] = Vector3.zero;

            int[] tri = new int[3];
            tri[0] = 0;
            tri[1] = 1;
            tri[2] = 2;

            singleMesh.vertices = vector3;
            singleMesh.triangles = tri;
        }

        private void Awake()
        {
            if (!gameObject.activeSelf || !this.enabled || compute == null) return;
            List<ClustDataLoad.ClustData> clustData =
                ClustDataLoad.LoadClust(Application.streamingAssetsPath + "/ClustData/" + saveName);
            UpdateBuffer(clustData);
            clustData.Clear();

            //获取对应的CS函数，建议重写时使用的名称也是一致的，方便改
            Cam_Cull_kernel = compute.FindKernel("ViewCulling");
            Pro_Cull_kernel = compute.FindKernel("ProjectMatrixCulling");
            Cam_Light_Cull_kernel = compute.FindKernel("ViewCullingWithLighting");
        }

        private void Start()
        {
            GPUPipelineDrawStack.Instance.InsertRender(this);
            isInsert = true;
        }


        /// <summary>
        /// 通过摄像机剔除绘制的物体，这个处理方式是最基本的，
        /// 剔除不进行其他剔除操作，只有视锥体剔除，输出的数据也不没有加入其他数据，
        /// 一般这种类不需要重写，但是为了需要还是加上会方便一点
        /// </summary>
        /// <param name="context">提交需要的context</param>
        /// <param name="buffer">绘制到的buffer对象</param>
        /// <param name="subPass">调用的Pass</param>
        /// <param name="camera">剔除使用的摄像机</param>
        protected virtual void DrawCulledClustDataByCamera(ScriptableRenderContext context,
            CommandBuffer buffer, int subPass, Camera camera)
        {
            if (triangleBuffer == null || triangleBuffer.count == 0) return;
            if (compute == null || showMat == null || camera == null) return;

            //获得裁剪数据
            Vector4[] planes = ViewCulling.GetFrustumPlane(Camera.main);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputPosId, triangleBuffer);   //传递所有三角形数据
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputBoundId, boundsBuffer);   //传递所有边框数据
            buffer.SetComputeBufferCounterValue(cullResult, 0);                          //初始化结果栈
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, cullresultId, cullResult);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //设置最大值
            buffer.SetComputeVectorArrayParam(compute, planesId, planes);
            buffer.DispatchCompute(compute, Cam_Cull_kernel, (boundsBuffer.count / 32) + 1, 1, 1);

            buffer.CopyCounterValue(cullResult, argsMeshBuffer, sizeof(uint));
            showMat.SetBuffer("cullresult", cullResult);
            buffer.DrawMeshInstancedIndirect(singleMesh, 0, showMat, subPass, argsMeshBuffer);

            ExecuteBuffer(ref buffer, context);
        }

        /// <summary>
        /// 通过矩阵来剔除绘制的物体，是最基本的处理方式，
        /// 剔除后不进行其他剔除操作，只有视锥体剔除，输出的数据也不没有加入其他数据，
        /// 一般这种类不需要重写，但是为了需要还是加上会方便一点
        /// </summary>
        /// <param name="context">提交需要的context</param>
        /// <param name="buffer">绘制到的buffer对象</param>
        /// <param name="subPass">调用的Pass</param>
        /// <param name="projectMatrix">剔除使用的摄像机</param>
        protected virtual void DrawCullingDataByProjectMatrix(ScriptableRenderContext context,
            CommandBuffer buffer, int subPass, Matrix4x4 projectMatrix)
        {
            if (buffer == null || showMat == null || compute == null || triangleBuffer == null)
            {
                return;
            }

            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputPosId, triangleBuffer);   //传递所有三角形数据
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputBoundId, boundsBuffer);   //传递所有边框数据

            buffer.SetComputeBufferCounterValue(cullResult, 0);                              //初始化结果栈
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, cullresultId, cullResult);   //设置剔除结果
            buffer.SetComputeMatrixParam(compute, projectMatrixId, projectMatrix);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);      //设置数据范围
            buffer.DispatchCompute(compute, Pro_Cull_kernel, (boundsBuffer.count / 32) + 1, 1, 1);
            showMat.SetBuffer("cullresult", cullResult);
            buffer.CopyCounterValue(cullResult, argsMeshBuffer, sizeof(uint));

            buffer.DrawMeshInstancedIndirect(singleMesh, 0, showMat, subPass, argsMeshBuffer);
            ExecuteBuffer(ref buffer, context);
        }

        protected void DrawClustWithLightingCullByCamera(ScriptableRenderContext context,
            CommandBuffer buffer, int subPass, Camera camera)
        {
            if (triangleBuffer == null || triangleBuffer.count == 0) return;
            if (compute == null || showMat == null || camera == null) return;

            //获得裁剪数据
            Vector4[] planes = ViewCulling.GetFrustumPlane(Camera.main);
            buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel, inputPosId, triangleBuffer);   //传递所有三角形数据
            buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel, inputBoundId, boundsBuffer);   //传递所有边框数据
            buffer.SetComputeBufferCounterValue(lightCullResult, 0);                          //初始化结果栈
            buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel,
                lightCullResultId, lightCullResult);                            //设置剔除输出结果位置
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //设置最大值
            buffer.SetComputeVectorArrayParam(compute, planesId, planes);
            buffer.DispatchCompute(compute, Cam_Light_Cull_kernel, (boundsBuffer.count / 32) + 1, 1, 1);

            buffer.CopyCounterValue(lightCullResult, argsMeshBuffer, sizeof(uint));
            showMat.SetBuffer(lightCullResultId, lightCullResult);
            buffer.DrawMeshInstancedIndirect(singleMesh, 0, showMat, subPass, argsMeshBuffer);

            ExecuteBuffer(ref buffer, context);
        }

        private void OnDisable()
        {
            if (isInsert)
            {
                GPUPipelineDrawStack.Instance.RemoveRender(this);
                isInsert = false;
            }
            triangleBuffer?.Dispose();
            boundsBuffer?.Dispose();
            cullResult?.Dispose();
            argsMeshBuffer?.Dispose();
            lightCullResult?.Dispose();
        }

        public override void DrawClustByCamera(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Camera camera)
        {
            if (drawType == ClustDrawType.Shadow)
            {
                DrawCulledClustDataByCamera(context, buffer, (int)drawType, camera);
            }
            else
            {
                DrawClustWithLightingCullByCamera(context, buffer, 2, camera);
            }
        }

        public override void DrawClustByProjectMatrix(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Matrix4x4 projectMatrix)
        {
            DrawCullingDataByProjectMatrix(context, buffer, (int)drawType, projectMatrix);
        }
    }

}
