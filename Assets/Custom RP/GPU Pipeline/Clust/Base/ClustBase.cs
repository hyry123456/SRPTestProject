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
        public Mesh createMesh;             //���ڴ�������Meshʱʹ�õ�Mesh����

        List<ClustDataLoad.Triangle> triangles;

        private ComputeBuffer cullResult;
        private ComputeBuffer lightCullResult;
        private ComputeBuffer argsMeshBuffer;       //����ʱ��Ҫ�ı�ʶbuffer
        private uint[] argsMesh = new uint[5] { 0, 0, 0, 0, 0 };    //��ʶbuffer��Ҫ������

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer boundsBuffer;

        private Mesh singleMesh;
        private bool isInsert;

        //������Ҫ��������ݣ���Щ����һ��û�б�Ҫ�ı䣬ֻ�����ƶ���
        int inputPosId = Shader.PropertyToID("inputPos"),
            inputBoundId = Shader.PropertyToID("inputBound"),
            cullresultId = Shader.PropertyToID("cullresult"),
            clustLengthId = Shader.PropertyToID("clustLength"),
            planesId = Shader.PropertyToID("planes"),
            projectMatrixId = Shader.PropertyToID("_ProjectMatrix"),
            lightCullResultId = Shader.PropertyToID("_LightCullResult");

        protected int Cam_Cull_kernel;            //�������׶���޳��ķ������
        protected int Pro_Cull_kernel;            //ͶӰ�����޳��ķ������
        protected int Cam_Light_Cull_kernel;      //�����ƹ��޳�����׶���޳��������

        /// <summary>
        /// ����Ŀǰ��buffer���ݣ�Ҳ���Ǹ��ݴ���ֵ���ɶ�Ӧ��buffer,
        /// ��Ҫ��չ����ʱ��д�÷�������Ϊ�÷�����ģ�ʹ����ֻ�ж��㡢���ߡ����ߡ�uv0
        /// </summary>
        /// <param name="clustData">�����ģ������</param>
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
            //����buffer�е��������������õ��������޳���������ݴ�С������bufferΪ���buffer
            //��ʱ�ü���������ݴ�С�������ε����ݴ�Сһ�£�֮����Ҫ�޸ģ���ӹ�������
            cullResult = new ComputeBuffer(triangles.Count, sizeof(float) * 3 * (3 + 3 + 4 + 2), ComputeBufferType.Append);

            if (lightCullResult != null)
                lightCullResult.Release();
            lightCullResult = new ComputeBuffer(triangles.Count, 4 * (3 * (3 + 3 + 4 + 2) + 2), ComputeBufferType.Append);
        }

        /// <summary>        /// ����һ��������Mesh������֮����л���        /// </summary>
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

            //��ȡ��Ӧ��CS������������дʱʹ�õ�����Ҳ��һ�µģ������
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
            if (triangleBuffer == null || triangleBuffer.count == 0) return;
            if (compute == null || showMat == null || camera == null) return;

            //��òü�����
            Vector4[] planes = ViewCulling.GetFrustumPlane(Camera.main);
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputPosId, triangleBuffer);   //������������������
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, inputBoundId, boundsBuffer);   //�������б߿�����
            buffer.SetComputeBufferCounterValue(cullResult, 0);                          //��ʼ�����ջ
            buffer.SetComputeBufferParam(compute, Cam_Cull_kernel, cullresultId, cullResult);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //�������ֵ
            buffer.SetComputeVectorArrayParam(compute, planesId, planes);
            buffer.DispatchCompute(compute, Cam_Cull_kernel, (boundsBuffer.count / 32) + 1, 1, 1);

            buffer.CopyCounterValue(cullResult, argsMeshBuffer, sizeof(uint));
            showMat.SetBuffer("cullresult", cullResult);
            buffer.DrawMeshInstancedIndirect(singleMesh, 0, showMat, subPass, argsMeshBuffer);

            ExecuteBuffer(ref buffer, context);
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
            if (buffer == null || showMat == null || compute == null || triangleBuffer == null)
            {
                return;
            }

            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputPosId, triangleBuffer);   //������������������
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, inputBoundId, boundsBuffer);   //�������б߿�����

            buffer.SetComputeBufferCounterValue(cullResult, 0);                              //��ʼ�����ջ
            buffer.SetComputeBufferParam(compute, Pro_Cull_kernel, cullresultId, cullResult);   //�����޳����
            buffer.SetComputeMatrixParam(compute, projectMatrixId, projectMatrix);
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);      //�������ݷ�Χ
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

            //��òü�����
            Vector4[] planes = ViewCulling.GetFrustumPlane(Camera.main);
            buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel, inputPosId, triangleBuffer);   //������������������
            buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel, inputBoundId, boundsBuffer);   //�������б߿�����
            buffer.SetComputeBufferCounterValue(lightCullResult, 0);                          //��ʼ�����ջ
            buffer.SetComputeBufferParam(compute, Cam_Light_Cull_kernel,
                lightCullResultId, lightCullResult);                            //�����޳�������λ��
            buffer.SetComputeIntParam(compute, clustLengthId, boundsBuffer.count);              //�������ֵ
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
