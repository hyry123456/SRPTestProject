using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    struct NoiseParticleData
    {
        public Vector4 random;          //xyz是随机数，w是目前存活时间
        public Vector2Int index;             //状态标记，x是当前编号，y是是否存活
        public Vector3 worldPos;        //当前位置
        public Vector4 uvTransData;     //uv动画需要的数据
        public float interpolation;    //插值需要的数据
        public Vector4 color;           //颜色值，包含透明度
        public float size;             //粒子大小
        public Vector3 nowSpeed;        //xyz是当前速度，w是存活时间
        public float liveTime;         //最多存活时间
    }

    public class ParticleNoise : GPUPipelineBase
    {
        public ComputeShader computeShader;
        public ComputeBuffer particleBuffer;

        [Range(0.01f, 6.18f)]
        public float arc = 0;
        [Range(1, 8)]
        public int octave;
        public float frequency;
        [Range(0.0001f, 1f)]
        public float intensity = 0.1f;
        public int particleCount = 1000;
        public int particleOutCount = 100;
        public float radius = 1;
        public int rowCount = 1;
        public int colCount = 1;
        public Vector2 lifeTimeRange = new Vector2(0, 1);
        public Material material;
        public AnimationCurve sizeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [GradientUsage(true)]
        public Gradient colorWithLive;

        private int kernel;
        private bool isInsert;
        public int arrive;
        private float arriveF = 0;

        private int arcId = Shader.PropertyToID("_Arc"),
            radiusId = Shader.PropertyToID("_Radius"),
            octaveId = Shader.PropertyToID("_Octave"),
            frequencyId = Shader.PropertyToID("_Frequency"),
            arriveIndexId = Shader.PropertyToID("_ArriveIndex"),
            timeId = Shader.PropertyToID("_Time"),
            uvCountId = Shader.PropertyToID("_UVCount"),
            rowCountId = Shader.PropertyToID("_RowCount"),
            colCountId = Shader.PropertyToID("_ColCount"),
            colorsId = Shader.PropertyToID("_Colors"),
            alphasId = Shader.PropertyToID("_Alphas"),
            particleCountId = Shader.PropertyToID("_ParticleCount"),
            sizesId = Shader.PropertyToID("_Sizes"),
            intensityId = Shader.PropertyToID("_Intensity"),
            particleBufferId = Shader.PropertyToID("_ParticleNoiseBuffer");

        Vector3 GetSphereBeginPos(Vector4 random)
        {
            float u = Mathf.Lerp(0, arc, random.x);
            float v = Mathf.Lerp(0, arc, random.y);
            return new Vector3(radius * Mathf.Cos(u),
                radius * Mathf.Sin(u) * Mathf.Cos(v), radius * Mathf.Sin(u) * Mathf.Sin(v));
        }

        private void Awake()
        {
            if (computeShader == null || material == null) return;
            kernel = computeShader.FindKernel("NoiseParticle");
        }

        private void OnEnable()
        {
            ReadyBuffer();
            if (material.renderQueue >= 3000)
                GPUPipelineDrawStack.Instance.InsertRender(this, true);
            else
                GPUPipelineDrawStack.Instance.InsertRender(this, false);
            isInsert = true;
            SetUnUpdateData();
        }

        private void Update()
        {
            arriveF += Time.deltaTime;
            int add = (int)(arriveF / (1.0f / particleOutCount));
            if(add > 0)
            {
                arrive += add;
                arriveF = 0;
                arrive %= 1000000007;
            }

            SetUpdateData();
            computeShader.Dispatch(kernel, (particleCount / 64) + 1, 1, 1);
        }

        private void OnDisable()
        {
            if (isInsert)
            {
                if (material.renderQueue >= 3000)
                    GPUPipelineDrawStack.Instance.RemoveRender(this, true);
                else
                    GPUPipelineDrawStack.Instance.RemoveRender(this, false);
                isInsert=false;
            }

            particleBuffer?.Release();
        }

        private void OnValidate()
        {
            if (isInsert)
            {
                ReadyBuffer();
                SetUnUpdateData();
            }
        }

        private void ReadyBuffer()
        {
            particleBuffer?.Release();
            particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * (4 + 2 + 3 + 4 + 1 + 4 + 1 + 3 + 1));
            NoiseParticleData[] noiseParticleData = new NoiseParticleData[particleCount];
            for(int i=0; i<particleCount; i++)
            {
                Vector4 vector4 = new Vector4(Random.value, Random.value, Random.value, 0);
                Vector3 pos = GetSphereBeginPos(vector4);
                noiseParticleData[i] = new NoiseParticleData
                {
                    random = vector4,
                    index = new Vector2Int(i, -1),
                    worldPos = pos,
                    liveTime = Mathf.Lerp(lifeTimeRange.x, lifeTimeRange.y, Random.value),
                };
            }
            particleBuffer.SetData(noiseParticleData);
            arrive = 0;

        }

        private void SetUnUpdateData()
        {
            computeShader.SetInts(uvCountId, new int[2] { rowCount, colCount });
            computeShader.SetInt(particleCountId, particleCount);

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

            computeShader.SetFloat(arcId, arc);
            computeShader.SetFloat(radiusId, radius);
            computeShader.SetInt(octaveId, octave);
            computeShader.SetFloat(frequencyId, frequency);
            computeShader.SetFloat(intensityId, intensity);
        }

        private void SetUpdateData()
        {
            computeShader.SetBuffer(kernel, particleBufferId, particleBuffer);

            //设置时间
            computeShader.SetVector(timeId, new Vector4(Time.time, Time.deltaTime));
            //设置枚举位置
            computeShader.SetInt(arriveIndexId, arrive);
        }

        public override void DrawClustByCamera(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Camera camera)
        {
            material.SetBuffer(particleBufferId, particleBuffer);
            material.SetInt(rowCountId, rowCount);
            material.SetInt(colCountId, colCount);
            buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Points,
                1, particleCount);
            ExecuteBuffer(ref buffer, context);
        }

        public override void DrawClustByProjectMatrix(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Matrix4x4 projectMatrix)
        {
            return;
        }
    }
}