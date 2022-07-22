using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
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
        public int createCount = 100;

        public Stack<ClustData> loadClustStack = new Stack<ClustData>();


        private uint[] argsArray = new uint[4] { 0, 0, 0, 0};    //标识buffer需要的数据

        private ComputeBuffer cullResult;
        private ComputeBuffer lightCullResult;
        private ComputeBuffer argsBuffer;       //绘制时需要的标识buffer
        private ComputeBuffer positionBuffer;
        private ComputeBuffer normalBuffer;
        private ComputeBuffer uvBuffer;
        private ComputeBuffer tangentBuffer;
        private ComputeBuffer boundsBuffer;

        private bool isInsert;

        public int allClusterCount;
        [SerializeField]
        private int nowClusterIndex;



        //定义需要传入的数据，这些数据一般没有必要改变，只是名称而已
        int inputPosId = Shader.PropertyToID("_InputPoss"),
            inputNorId = Shader.PropertyToID("_InputNormals"),
            inputTanId = Shader.PropertyToID("_InpoutTangents"),
            inputUv0Id = Shader.PropertyToID("_InputUv0s"),
            inputBoundId = Shader.PropertyToID("_InputBounds"),
            cullResultId = Shader.PropertyToID("_CullResult"),
            clustLengthId = Shader.PropertyToID("_ClustLength"),
            planesId = Shader.PropertyToID("_Planes"),
            projectMatrixId = Shader.PropertyToID("_ProjectMatrix"),
            lightCullResultId = Shader.PropertyToID("_LightCullResult");

        protected int Cam_Cull_kernel;            //摄像机视锥体剔除的方法编号
        protected int Pro_Cull_kernel;            //投影矩阵剔除的方法编号
        //protected int Cam_Light_Cull_kernel;      //包含灯光剔除的视锥体剔除方法编号



        private void Awake()
        {
            if (!gameObject.activeSelf || !this.enabled || compute == null) return;
            //List<ClustData> clustData =
            //    ClustDataLoad.LoadClust(Application.streamingAssetsPath + "/ClustData/" + saveName);
            //UpdateBuffer(clustData);
            //triangles = ClustDataLoad.GetAllTriangle(clustData);

            //clustData.Clear();

            //获取对应的CS函数，建议重写时使用的名称也是一致的，方便改
            Cam_Cull_kernel = compute.FindKernel("ViewCulling");
            Pro_Cull_kernel = compute.FindKernel("ProjectMatrixCulling");
            //Cam_Light_Cull_kernel = compute.FindKernel("ViewCullingWithLighting");

            ReadyBuffer();

            CoroutinesStack coroutines = CoroutinesStack.Instance;  //呼叫一下，防止报错
            AsyncLoad.Instance.AddAction(AsynLoadData);
        }

        private void Start()
        {
            if (showMat == null) return;
            if (showMat.renderQueue >= 3000)     //处于透明队列，插入到透明物体栈
                GPUPipelineDrawStack.Instance.InsertRender(this, true);
            else
                GPUPipelineDrawStack.Instance.InsertRender(this, false);
            isInsert = true;
        }

        private void OnDisable()
        {
            if (isInsert)
            {
                if (showMat.renderQueue >= 3000)
                    GPUPipelineDrawStack.Instance.RemoveRender(this, true);
                else GPUPipelineDrawStack.Instance.RemoveRender(this, false);
                isInsert = false;
            }
            boundsBuffer?.Dispose();
            cullResult?.Dispose();
            argsBuffer?.Dispose();
            lightCullResult?.Dispose();
            positionBuffer?.Dispose();
            normalBuffer?.Dispose();
            uvBuffer?.Dispose();
            tangentBuffer?.Dispose();
        }

        private void AsynLoadData()
        {
            string path = GetSavePath();
            StringBuilder str = new StringBuilder();

            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //还没有读取的文件内容长度
                long leftLength = file.Length;
                int readSize = 10240;
                //创建接收文件内容的字节数组
                byte[] buffer = new byte[readSize];
                //每次读取的最大字节数
                int maxLength = buffer.Length;
                //每次实际返回的字节数长度
                int num = 0;
                //文件开始读取的位置
                long fileStart = 0;

                while (leftLength > 0 && AsyncLoad.Instance.IsRunning)
                {
                    //设置文件流的读取位置，一开始从0位置读取
                    file.Position = fileStart;

                    // 当读取的位置没有缓存buff的最后
                    if (leftLength < maxLength)
                    {
                        num = file.Read(buffer, 0, Convert.ToInt32(leftLength));
                    }
                    else
                    {
                        num = file.Read(buffer, 0, maxLength);
                    }
                    if (num == 0)
                    {
                        break;
                    }
                    // 读取的游标往后移动
                    fileStart += num;
                    // 需要读取的长度减少
                    leftLength -= num;

                    str.Append(Encoding.Default.GetString(buffer));
                    ReadyStringBuilder(ref str);

                    // 坑在这里，如果最后一次读取的长度没有超过1024，则buffer中会残留上一次读取的内容，导致            
                    //最后获取的内容出错，所以每次读取后都要清空数组，使用Array.Clear方法
                    Array.Clear(buffer, 0, readSize);
                }
            }
        }

        private void ReadyStringBuilder(ref StringBuilder stringBuilder)
        {
            LinkedList<string> list = new LinkedList<string>();
            string str = stringBuilder.ToString();
            int i, next = 0;
            for (i = str.IndexOf('<'); i < str.Length && i != -1;)
            {
                next = str.IndexOf('>', i);
                if (next == -1)
                {
                    if (i > 1) stringBuilder.Remove(0, i - 1);     //移除已经处理过的数据
                    break;
                }

                if (next - 1 - i > 1)
                {
                    string str2 = str.Substring(i + 1, next - 1 - i);
                    list.AddLast(str2);
                }
                i = str.IndexOf('<', next);
            }
            if (next != -1)
            {
                stringBuilder.Remove(0, next);
            }

            foreach (string index in list)
            {
                lock (loadClustStack)
                {
                    loadClustStack.Push(
                    ClustDataLoad.LoadOneClustData(index)
                    );
                }
            }
            BeginLoad();
        }

        /// <summary>        /// 协程显示加载数据        /// </summary>
        private bool CoroutineLoadCluster()
        {
            if (loadClustStack.Count == 0) return false;
            for(int i=0; i<10 && loadClustStack.Count != 0; i++)
            {
                ClustData clustData = loadClustStack.Pop();
                InsertClusterBuffer(clustData);
            }

            if (loadClustStack.Count > 0)
                return false;           //还没结束
            isLoading = false;
            return true;
        }

        bool isLoading = false;
        private void BeginLoad()
        {
            if (isLoading) return;
            isLoading = true;
            CoroutinesStack.Instance.AddReadyAction(CoroutineLoadCluster);
        }


        /// <summary>        /// 初始化buffer数据，也就是根据目前的Cluster数据初始化数组        /// </summary>
        private void ReadyBuffer()
        {
            argsBuffer?.Release();
            argsBuffer = new ComputeBuffer(1, argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsArray[0] = 1;
            argsArray[1] = (uint)allClusterCount * 12;
            argsArray[2] = 0;
            argsArray[3] = 0;
            argsBuffer.SetData(argsArray);

            boundsBuffer?.Release();
            boundsBuffer = new ComputeBuffer(allClusterCount, sizeof(float) * (2 * 3 + 1));

            positionBuffer?.Release();
            positionBuffer = new ComputeBuffer(allClusterCount * 48, sizeof(float) * 3);

            normalBuffer?.Release();
            normalBuffer = new ComputeBuffer(allClusterCount * 48, sizeof(float) * 3);

            tangentBuffer?.Release();
            tangentBuffer = new ComputeBuffer(allClusterCount * 48, sizeof(float) * 4);

            uvBuffer?.Release();
            uvBuffer = new ComputeBuffer(allClusterCount * 48, sizeof(float) * 2);

            cullResult?.Release();
            //声明buffer中的数据数量，设置的数据是剔除矩阵的数据大小，声明buffer为添加buffer
            //暂时裁剪结果的数据大小和三角形的数据大小一致，之后需要修改，添加光照数据
            cullResult = new ComputeBuffer(allClusterCount * 12, sizeof(float) * 4 * (3 + 3 + 2 + 4), ComputeBufferType.Append);

            //lightCullResult?.Release();
            //lightCullResult = new ComputeBuffer(allClusterCount * 32, 4 * (3 * (3 + 3 + 4 + 2) + 2), ComputeBufferType.Append);
            nowClusterIndex = 0;
        }

        private void InsertClusterBuffer(ClustData clustData)
        {
            if (clustData.Positions == null)
            {
                Debug.Log("Clust出错");
                return;
            }
            if (nowClusterIndex == allClusterCount)
            {
                Debug.LogError("数量出错");
                return;
            }
            BoundsData[] bounds = new BoundsData[1];
            bounds[0].boundMin = clustData.boundMin;
            bounds[0].boundMax = clustData.boundMax;
            bounds[0].isLive = 1;
            boundsBuffer.SetData(bounds, 0, nowClusterIndex, 1);

            positionBuffer.SetData(clustData.Positions, 0, nowClusterIndex * 48, 48);
            normalBuffer.SetData(clustData.Normals, 0, nowClusterIndex * 48, 48);
            tangentBuffer.SetData(clustData.Tangents, 0, nowClusterIndex * 48, 48);
            uvBuffer.SetData(clustData.UV0s, 0, nowClusterIndex * 48, 48);

            nowClusterIndex++;
        }


        public override void DrawClustByCamera(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Camera camera)
        {
            if (drawType == ClustDrawType.Shadow)
            {
                DrawCulledClustDataByCamera(context, buffer, (int)drawType, camera);
            }
            else
            {
                DrawCulledClustDataByCamera(context, buffer, (int)drawType, camera);

                //DrawClustWithLightingCullByCamera(context, buffer, 2, camera);
            }
        }

        public override void DrawClustByProjectMatrix(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Matrix4x4 projectMatrix)
        {
            DrawCullingDataByProjectMatrix(context, buffer, (int)drawType, projectMatrix);
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
            if (buffer == null || showMat == null || compute == null)
            {
                return;
            }

            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputBoundId, boundsBuffer);   //传递所有边框数据
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputPosId, positionBuffer);
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputNorId, normalBuffer);
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputUv0Id, uvBuffer);
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputTanId, tangentBuffer);

            buffer.SetComputeBufferCounterValue(cullResult, 0);                              //初始化结果栈
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, cullResultId, cullResult);   //设置剔除结果
            buffer.SetComputeMatrixParam(compute, projectMatrixId, projectMatrix);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);      //设置数据范围
            buffer.DispatchCompute(compute, Pro_Cull_kernel, (boundsBuffer.count / 64) + 1, 1, 1);
            showMat.SetBuffer(cullResultId, cullResult);
            buffer.CopyCounterValue(cullResult, argsBuffer, sizeof(uint));

            showMat.SetBuffer(cullResultId, cullResult);

            buffer.DrawProceduralIndirect(Matrix4x4.identity, showMat, subPass, MeshTopology.Points, argsBuffer);
            ExecuteBuffer(ref buffer, context);
        }

        //protected void DrawClustWithLightingCullByCamera(ScriptableRenderContext context,
        //    CommandBuffer buffer, int subPass, Camera camera)
        //{
        //    if (compute == null || showMat == null || camera == null) return;

        //    //获得裁剪数据
        //    Vector4[] planes = ViewCulling.GetFrustumPlane(camera);
        //    buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel, inputBoundId, boundsBuffer);   //传递所有边框数据
        //    buffer.SetComputeBufferCounterValue(lightCullResult, 0);                          //初始化结果栈
        //    buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel,
        //        lightCullResultId, lightCullResult);                            //设置剔除输出结果位置
        //    buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //设置最大值
        //    buffer.SetComputeVectorArrayParam(compute, planesId, planes);
        //    buffer.DispatchCompute(compute, Cam_Light_Cull_kernel, (boundsBuffer.count / 64) + 1, 1, 1);

        //    buffer.CopyCounterValue(lightCullResult, argsBuffer, sizeof(uint));
        //    showMat.SetBuffer(lightCullResultId, lightCullResult);

        //    buffer.DrawProceduralIndirect(Matrix4x4.identity, showMat, subPass,
        //        MeshTopology.Points, argsBuffer);
        //    ExecuteBuffer(ref buffer, context);
        //}


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
            if (compute == null || showMat == null || camera == null) return;
            if (boundsBuffer == null || boundsBuffer.count == 0) return;

            //获得裁剪数据
            Vector4[] planes = ViewCulling.GetFrustumPlane(camera);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputBoundId, boundsBuffer);   //传递所有边框数据
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputPosId, positionBuffer);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputNorId, normalBuffer);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputUv0Id, uvBuffer);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputTanId, tangentBuffer);

            buffer.SetComputeBufferCounterValue(cullResult, 0);                          //初始化结果栈
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, cullResultId, cullResult);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //设置最大值
            buffer.SetComputeVectorArrayParam(compute, planesId, planes);
            buffer.DispatchCompute(compute, Cam_Cull_kernel, (boundsBuffer.count / 64) + 1, 1, 1);

            buffer.CopyCounterValue(cullResult, argsBuffer, sizeof(uint));
            showMat.SetBuffer(cullResultId, cullResult);

            buffer.DrawProceduralIndirect(Matrix4x4.identity, showMat, subPass, MeshTopology.Points, argsBuffer);

            ExecuteBuffer(ref buffer, context);
        }

    }

}
