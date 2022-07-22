using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    [System.Serializable]
    public struct ParticlePerObject        //当个物体的数据
    {
        public Vector3 beginPos;
        public int arriveIndex;
    }

    public class ParticleGroup : GPUPipelineBase
    {
        ComputeBuffer particleBuffer;
        ComputeBuffer origenBuffer;         //根据的buffer

        int kernelId;

        int particleBufferId = Shader.PropertyToID("_ParticleBuffer"),
            particleGroupBufferId = Shader.PropertyToID("_ParticleGroupBuffer"),
            groupDataId = Shader.PropertyToID("_GroupData"),
            lifeTimeId = Shader.PropertyToID("_LifeTime"),
            speedStartId = Shader.PropertyToID("_SpeedStart"),
            speedEndId = Shader.PropertyToID("_SpeedEnd"),
            uvCountId = Shader.PropertyToID("_UVCount"),
            rowCountId = Shader.PropertyToID("_RowCount"),
            colCountId = Shader.PropertyToID("_ColCount"),
            timeId = Shader.PropertyToID("_Time"),
            colorsId = Shader.PropertyToID("_Colors"),
            alphasId = Shader.PropertyToID("_Alphas"),
            sizesId = Shader.PropertyToID("_Sizes");

        bool isInsert;

        public int particleSize = 1000;
        public int particleOutSize = 100;
        public Material material;
        public ComputeShader computeShader;

        public Vector3 speedStart;
        public Vector3 speedEnd;
        public float liveTime = 5.0f;
        public int rowCount = 1;
        public int colCount = 1;
        public AnimationCurve sizeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [GradientUsage(true)]
        public Gradient colorWithLive;


        /// <summary>
        /// 注意，这个是粒子组，也就是说所有粒子每一组是独立的，不想Simple一样统一管理的，
        /// 因此需要根据每帧传入的数据进行对根据buffer的刷新
        /// </summary>
        public void ReadyBuffer(ParticlePerObject[] objects)
        {
            particleBuffer?.Dispose();
            origenBuffer?.Dispose();

            particleBuffer = new ComputeBuffer(objects.Length * particleSize, 
                sizeof(float) * (4 + 2 + 3 + 4 + 1 + 4 + 1));
            List<ParticleData> particleDatas = new List<ParticleData>(particleSize * objects.Length);

            for (int i = 0; i < objects.Length; i++)
            {
                for (int j = 0; j < particleSize; j++)
                {
                    particleDatas.Add(new ParticleData
                    {
                        index = new Vector2Int(j, 0),
                        random = new Vector4(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
                            Random.Range(0.0f, 1.0f), 0),
                        worldPos = objects[i].beginPos,
                    });
                }
            }
            particleBuffer.SetData(particleDatas);

            origenBuffer = new ComputeBuffer(objects.Length, sizeof(float) * (3 + 1));
            origenBuffer.SetData(objects);

            if (material.renderQueue >= 3000)
                GPUPipelineDrawStack.Instance.InsertRender(this, true);
            else
                GPUPipelineDrawStack.Instance.InsertRender(this, false);
            isInsert = true;

            SetUnUpdateData();

        }

        /// <summary>
        /// 更新根据buffer，也就是每一组的数据，注意要先准备过后才调用该函数
        /// </summary>
        public void UpdatePerObjectsBuffer(ParticlePerObject[] objects)
        {
            origenBuffer?.Dispose();
            origenBuffer = new ComputeBuffer(objects.Length, sizeof(float) * (3 + 1));
            origenBuffer.SetData(objects);
        }

        public override void DrawClustByCamera(ScriptableRenderContext context, 
            CommandBuffer buffer, ClustDrawType drawType, Camera camera)
        {
            if (origenBuffer.count == 0) return;
            material.SetBuffer(particleBufferId, particleBuffer);
            material.SetInt(rowCountId, rowCount);
            material.SetInt(colCountId, colCount);
            buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Points,
                1, particleSize * origenBuffer.count);
            ExecuteBuffer(ref buffer, context);
        }

        public override void DrawClustByProjectMatrix(ScriptableRenderContext context, 
            CommandBuffer buffer, ClustDrawType drawType, Matrix4x4 projectMatrix)
        {
            return;
        }

        protected virtual void Awake()
        {
            if (computeShader == null || material == null) return;
            kernelId = computeShader.FindKernel("ParticleGroupReady");
        }

        void SetUnUpdateData()
        {
            computeShader.SetVector(speedStartId, speedStart);
            computeShader.SetVector(speedEndId, speedEnd);
            computeShader.SetInts(uvCountId, new int[2] { rowCount, colCount });
            computeShader.SetInt(groupDataId, particleSize);

            Vector4[] sizes = new Vector4[sizeCurve.length];
            for (int i = 0; i < sizeCurve.length; i++)
            {
                sizes[i] = new Vector4(sizeCurve.keys[i].time, sizeCurve.keys[i].value,
                    sizeCurve.keys[i].inTangent, sizeCurve.keys[i].outTangent);
            }
            computeShader.SetVectorArray(sizesId, sizes);

            GradientAlphaKey[] gradientAlphas = colorWithLive.alphaKeys;
            Vector4[] alphas = new Vector4[gradientAlphas.Length];
            for (int i = 0; i < gradientAlphas.Length; i++)
            {
                alphas[i] = new Vector4(gradientAlphas[i].alpha,
                    gradientAlphas[i].time);
            }
            computeShader.SetVectorArray(alphasId, alphas);

            GradientColorKey[] gradientColorKeys = colorWithLive.colorKeys;
            Vector4[] colors = new Vector4[gradientColorKeys.Length];
            for (int i = 0; i < gradientColorKeys.Length; i++)
            {
                colors[i] = gradientColorKeys[i].color;
                colors[i].w = gradientColorKeys[i].time;
            }
            computeShader.SetVectorArray(colorsId, colors);

            computeShader.SetFloat(lifeTimeId, liveTime);
        }

        void SetUpdateData()
        {
            computeShader.SetBuffer(kernelId, particleBufferId, particleBuffer);
            computeShader.SetBuffer(kernelId, particleGroupBufferId, origenBuffer);

            //设置时间
            computeShader.SetVector(timeId, new Vector4(Time.time, Time.deltaTime));

        }

        private void Update()
        {
            if (particleBuffer == null) return;

            SetUpdateData();
            computeShader.Dispatch(kernelId, origenBuffer.count, (particleSize / 64) + 1, 1);
        }

        private void OnDisable()
        {
            if (isInsert)
            {
                if (material.renderQueue >= 3000)
                    GPUPipelineDrawStack.Instance.RemoveRender(this, true);
                else
                    GPUPipelineDrawStack.Instance.RemoveRender(this, false);
                isInsert = false;
            }
            particleBuffer?.Dispose();
            origenBuffer?.Dispose();
        }
    }
}
