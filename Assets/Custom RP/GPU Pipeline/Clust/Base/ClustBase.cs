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
        public Mesh createMesh;             //���ڴ�������Meshʱʹ�õ�Mesh����
        public int createCount = 100;

        public Stack<ClustData> loadClustStack = new Stack<ClustData>();


        private uint[] argsArray = new uint[4] { 0, 0, 0, 0};    //��ʶbuffer��Ҫ������

        private ComputeBuffer cullResult;
        private ComputeBuffer lightCullResult;
        private ComputeBuffer argsBuffer;       //����ʱ��Ҫ�ı�ʶbuffer
        private ComputeBuffer positionBuffer;
        private ComputeBuffer normalBuffer;
        private ComputeBuffer uvBuffer;
        private ComputeBuffer tangentBuffer;
        private ComputeBuffer boundsBuffer;

        private bool isInsert;

        public int allClusterCount;
        [SerializeField]
        private int nowClusterIndex;



        //������Ҫ��������ݣ���Щ����һ��û�б�Ҫ�ı䣬ֻ�����ƶ���
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

        protected int Cam_Cull_kernel;            //�������׶���޳��ķ������
        protected int Pro_Cull_kernel;            //ͶӰ�����޳��ķ������
        //protected int Cam_Light_Cull_kernel;      //�����ƹ��޳�����׶���޳��������



        private void Awake()
        {
            if (!gameObject.activeSelf || !this.enabled || compute == null) return;
            //List<ClustData> clustData =
            //    ClustDataLoad.LoadClust(Application.streamingAssetsPath + "/ClustData/" + saveName);
            //UpdateBuffer(clustData);
            //triangles = ClustDataLoad.GetAllTriangle(clustData);

            //clustData.Clear();

            //��ȡ��Ӧ��CS������������дʱʹ�õ�����Ҳ��һ�µģ������
            Cam_Cull_kernel = compute.FindKernel("ViewCulling");
            Pro_Cull_kernel = compute.FindKernel("ProjectMatrixCulling");
            //Cam_Light_Cull_kernel = compute.FindKernel("ViewCullingWithLighting");

            ReadyBuffer();

            CoroutinesStack coroutines = CoroutinesStack.Instance;  //����һ�£���ֹ����
            AsyncLoad.Instance.AddAction(AsynLoadData);
        }

        private void Start()
        {
            if (showMat == null) return;
            if (showMat.renderQueue >= 3000)     //����͸�����У����뵽͸������ջ
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
                //��û�ж�ȡ���ļ����ݳ���
                long leftLength = file.Length;
                int readSize = 10240;
                //���������ļ����ݵ��ֽ�����
                byte[] buffer = new byte[readSize];
                //ÿ�ζ�ȡ������ֽ���
                int maxLength = buffer.Length;
                //ÿ��ʵ�ʷ��ص��ֽ�������
                int num = 0;
                //�ļ���ʼ��ȡ��λ��
                long fileStart = 0;

                while (leftLength > 0 && AsyncLoad.Instance.IsRunning)
                {
                    //�����ļ����Ķ�ȡλ�ã�һ��ʼ��0λ�ö�ȡ
                    file.Position = fileStart;

                    // ����ȡ��λ��û�л���buff�����
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
                    // ��ȡ���α������ƶ�
                    fileStart += num;
                    // ��Ҫ��ȡ�ĳ��ȼ���
                    leftLength -= num;

                    str.Append(Encoding.Default.GetString(buffer));
                    ReadyStringBuilder(ref str);

                    // �������������һ�ζ�ȡ�ĳ���û�г���1024����buffer�л������һ�ζ�ȡ�����ݣ�����            
                    //����ȡ�����ݳ�������ÿ�ζ�ȡ��Ҫ������飬ʹ��Array.Clear����
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
                    if (i > 1) stringBuilder.Remove(0, i - 1);     //�Ƴ��Ѿ������������
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

        /// <summary>        /// Э����ʾ��������        /// </summary>
        private bool CoroutineLoadCluster()
        {
            if (loadClustStack.Count == 0) return false;
            for(int i=0; i<10 && loadClustStack.Count != 0; i++)
            {
                ClustData clustData = loadClustStack.Pop();
                InsertClusterBuffer(clustData);
            }

            if (loadClustStack.Count > 0)
                return false;           //��û����
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


        /// <summary>        /// ��ʼ��buffer���ݣ�Ҳ���Ǹ���Ŀǰ��Cluster���ݳ�ʼ������        /// </summary>
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
            //����buffer�е��������������õ��������޳���������ݴ�С������bufferΪ���buffer
            //��ʱ�ü���������ݴ�С�������ε����ݴ�Сһ�£�֮����Ҫ�޸ģ���ӹ�������
            cullResult = new ComputeBuffer(allClusterCount * 12, sizeof(float) * 4 * (3 + 3 + 2 + 4), ComputeBufferType.Append);

            //lightCullResult?.Release();
            //lightCullResult = new ComputeBuffer(allClusterCount * 32, 4 * (3 * (3 + 3 + 4 + 2) + 2), ComputeBufferType.Append);
            nowClusterIndex = 0;
        }

        private void InsertClusterBuffer(ClustData clustData)
        {
            if (clustData.Positions == null)
            {
                Debug.Log("Clust����");
                return;
            }
            if (nowClusterIndex == allClusterCount)
            {
                Debug.LogError("��������");
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
        /// ͨ���������޳����Ƶ����壬��������Ĵ���ʽ��
        /// �޳��󲻽��������޳�������ֻ����׶���޳������������Ҳ��û�м����������ݣ�
        /// һ�������಻��Ҫ��д������Ϊ����Ҫ���Ǽ��ϻ᷽��һ��
        /// </summary>
        /// <param name="context">�ύ��Ҫ��context</param>
        /// <param name="buffer">���Ƶ���buffer����</param>
        /// <param name="subPass">���õ�Pass</param>
        /// <param name="projectMatrix">�޳�ʹ�õ������</param>
        protected virtual void DrawCullingDataByProjectMatrix(ScriptableRenderContext context,
            CommandBuffer buffer, int subPass, Matrix4x4 projectMatrix)
        {
            if (buffer == null || showMat == null || compute == null)
            {
                return;
            }

            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputBoundId, boundsBuffer);   //�������б߿�����
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputPosId, positionBuffer);
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputNorId, normalBuffer);
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputUv0Id, uvBuffer);
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputTanId, tangentBuffer);

            buffer.SetComputeBufferCounterValue(cullResult, 0);                              //��ʼ�����ջ
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, cullResultId, cullResult);   //�����޳����
            buffer.SetComputeMatrixParam(compute, projectMatrixId, projectMatrix);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);      //�������ݷ�Χ
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

        //    //��òü�����
        //    Vector4[] planes = ViewCulling.GetFrustumPlane(camera);
        //    buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel, inputBoundId, boundsBuffer);   //�������б߿�����
        //    buffer.SetComputeBufferCounterValue(lightCullResult, 0);                          //��ʼ�����ջ
        //    buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel,
        //        lightCullResultId, lightCullResult);                            //�����޳�������λ��
        //    buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //�������ֵ
        //    buffer.SetComputeVectorArrayParam(compute, planesId, planes);
        //    buffer.DispatchCompute(compute, Cam_Light_Cull_kernel, (boundsBuffer.count / 64) + 1, 1, 1);

        //    buffer.CopyCounterValue(lightCullResult, argsBuffer, sizeof(uint));
        //    showMat.SetBuffer(lightCullResultId, lightCullResult);

        //    buffer.DrawProceduralIndirect(Matrix4x4.identity, showMat, subPass,
        //        MeshTopology.Points, argsBuffer);
        //    ExecuteBuffer(ref buffer, context);
        //}


        /// <summary>
        /// ͨ��������޳����Ƶ����壬�������ʽ��������ģ�
        /// �޳������������޳�������ֻ����׶���޳������������Ҳ��û�м����������ݣ�
        /// һ�������಻��Ҫ��д������Ϊ����Ҫ���Ǽ��ϻ᷽��һ��
        /// </summary>
        /// <param name="context">�ύ��Ҫ��context</param>
        /// <param name="buffer">���Ƶ���buffer����</param>
        /// <param name="subPass">���õ�Pass</param>
        /// <param name="camera">�޳�ʹ�õ������</param>
        protected virtual void DrawCulledClustDataByCamera(ScriptableRenderContext context,
            CommandBuffer buffer, int subPass, Camera camera)
        {
            if (compute == null || showMat == null || camera == null) return;
            if (boundsBuffer == null || boundsBuffer.count == 0) return;

            //��òü�����
            Vector4[] planes = ViewCulling.GetFrustumPlane(camera);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputBoundId, boundsBuffer);   //�������б߿�����
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputPosId, positionBuffer);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputNorId, normalBuffer);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputUv0Id, uvBuffer);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputTanId, tangentBuffer);

            buffer.SetComputeBufferCounterValue(cullResult, 0);                          //��ʼ�����ջ
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, cullResultId, cullResult);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //�������ֵ
            buffer.SetComputeVectorArrayParam(compute, planesId, planes);
            buffer.DispatchCompute(compute, Cam_Cull_kernel, (boundsBuffer.count / 64) + 1, 1, 1);

            buffer.CopyCounterValue(cullResult, argsBuffer, sizeof(uint));
            showMat.SetBuffer(cullResultId, cullResult);

            buffer.DrawProceduralIndirect(Matrix4x4.identity, showMat, subPass, MeshTopology.Points, argsBuffer);

            ExecuteBuffer(ref buffer, context);
        }

    }

}
