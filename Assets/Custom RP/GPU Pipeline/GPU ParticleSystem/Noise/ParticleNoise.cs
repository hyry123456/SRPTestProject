using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{

    public class ParticleNoise : GPUPipelineBase
    {
        public ComputeShader computeShader;
        private ComputeBuffer particleBuffer;
        private ComputeBuffer initializeBuffer;
        public NoiseData[] noiseDatas;

        //初始化
        [SerializeField]
        private int particleCount = 1000;   //每组粒子数量
        [SerializeField]
        private int particleOutCount = 100; //每秒输出数量
        [SerializeField]
        private bool runInUpdate = true;

        public int ParticleOutCount
        {
            get { return particleOutCount; }
        }

        //输出、着色
        [SerializeField]
        private int rowCount = 1;
        [SerializeField]
        private int colCount = 1;
        [SerializeField]
        private Material material;
        [SerializeField]
        private AnimationCurve sizeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField]
        [GradientUsage(true)]
        private Gradient colorWithLive;
        [SerializeField]
        private Texture mainTexture;

        private int kernel_Updata;
        private int kernel_FixUpdata;
        private bool isInsert;
        public uint arrive;
        private float arriveF = 0;
        public ParticleInitializeData[] init;

        private int 
            timeId = Shader.PropertyToID("_Time"),
            uvCountId = Shader.PropertyToID("_UVCount"),
            rowCountId = Shader.PropertyToID("_RowCount"),
            colCountId = Shader.PropertyToID("_ColCount"),
            colorsId = Shader.PropertyToID("_Colors"),
            alphasId = Shader.PropertyToID("_Alphas"),
            particleCountId = Shader.PropertyToID("_ParticleCount"),
            sizesId = Shader.PropertyToID("_Sizes"),
            mainTexId = Shader.PropertyToID("_MainTex"), 
            texAspectRatioId = Shader.PropertyToID("_TexAspectRatio"), 
            particleBufferId = Shader.PropertyToID("_ParticleNoiseBuffer"),
            initBufferId = Shader.PropertyToID("_InitializeBuffer");


        Vector3 GetSphereBeginPos(Vector4 random, float arc, float radius)
        {
            float u = Mathf.Lerp(0, arc, random.x);
            float v = Mathf.Lerp(0, arc, random.y);
            return new Vector3(radius * Mathf.Cos(u),
                radius * Mathf.Sin(u) * Mathf.Cos(v), radius * Mathf.Sin(u) * Mathf.Sin(v));
        }

        Vector3 GetCubeBeginPos(Vector4 random, Vector3 cubeRange)
        {
            Vector3 beginPos = -cubeRange / 2.0f;
            Vector3 endPos = cubeRange / 2.0f;
            return new Vector3(
                Mathf.Lerp(beginPos.x, endPos.x, random.x),
                Mathf.Lerp(beginPos.y, endPos.y, random.y),
                Mathf.Lerp(beginPos.z, endPos.z, random.z)
                );
        }

        private void Awake()
        {
            if (computeShader == null || material == null) return;
            kernel_Updata = computeShader.FindKernel("Noise_PerFrame");
            kernel_FixUpdata = computeShader.FindKernel("Noise_PerFixFrame");
        }

        private void OnEnable()
        {
            if (material.renderQueue >= 3000)
                GPUPipelineDrawStack.Instance.InsertRender(this, true);
            else
                GPUPipelineDrawStack.Instance.InsertRender(this, false);
            isInsert = true;

            ReadyBuffer();
            SetUnUpdateData();
        }

        private void Update()
        {
            if (runInUpdate)
            {
                arriveF += Time.deltaTime;
                uint add = (uint)(arriveF / (1.0f / particleOutCount));
                if (add > 0)
                {
                    arrive += add;
                    arriveF = 0;
                    arrive %= 1000000007;
                }
            }


            UpdateInitial();

            SetUpdateData();
            computeShader.Dispatch(kernel_Updata, noiseDatas.Length, (particleCount / 64) + 1, 1);
        }

        private void FixedUpdate()
        {
            SetFixUpdateData();
            computeShader.Dispatch(kernel_FixUpdata, noiseDatas.Length, (particleCount / 64) + 1, 1);
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
            initializeBuffer?.Release();
        }

        private void OnValidate()
        {
            if (isInsert)
            {
                SetUnUpdateData();
                ReadyInitialParticle();
            }
        }

        private void ReadyBuffer()
        {
            ReadyPerParticle();
            ReadyInitialParticle();
        }

        /// <summary>        /// 初始化每一个粒子的数据        /// </summary>
        private void ReadyPerParticle()
        {
            particleBuffer?.Release();
            particleBuffer = new ComputeBuffer(particleCount * noiseDatas.Length, 
                sizeof(float) * (4 + 2 + 3 + 4 + 1 + 4 + 1 + 3 + 1));
            NoiseParticleData[] noiseParticleData = new NoiseParticleData[particleCount * noiseDatas.Length];
            for(int j=0; j< noiseDatas.Length; j++)
            {
                NoiseData noiseData = noiseDatas[j];
                Vector3 begin = noiseDatas[j].position.position;
                for (int i = 0; i < particleCount; i++)
                {
                    Vector4 random = new Vector4(Random.value, Random.value, Random.value, 0);
                    Vector3 pos = begin;
                    switch (noiseData.shapeMode)
                    {
                        case InitialShapeMode.Sphere:
                            pos += GetSphereBeginPos(random, noiseData.arc, noiseData.radius); break;
                        case InitialShapeMode.Cube:
                            pos += GetCubeBeginPos(random, noiseData.cubeRange); break;
                    }

                    Vector3 speed = new Vector3(
                        Mathf.Lerp(noiseData.velocityBegin.x, noiseData.velocityEnd.x, random.y),
                        Mathf.Lerp(noiseData.velocityBegin.y, noiseData.velocityEnd.y, random.z),
                        Mathf.Lerp(noiseData.velocityBegin.z, noiseData.velocityEnd.z, random.x)
                    );
                    noiseParticleData[j * particleCount + i] = new NoiseParticleData
                    {
                        random = random,
                        index = new Vector2Int(i - 100, -1),
                        worldPos = pos,
                        liveTime = Mathf.Lerp(noiseData.lifeTime.x, noiseData.lifeTime.y, Random.value),
                        nowSpeed = speed,
                    };
                }
            }

            particleBuffer.SetData(noiseParticleData);
            arrive = 0;
        }

        /// <summary>        /// 加载每一组粒子的初始化数据        /// </summary>
        private void ReadyInitialParticle()
        {
            initializeBuffer?.Dispose();
            initializeBuffer = new ComputeBuffer(noiseDatas.Length, 
                sizeof(float) * (3 + 3 + 3 + 3 + 2 + 3 + 2 + 3 + 3 + 2 + 1));
            init = new ParticleInitializeData[noiseDatas.Length];
            for(int i=0; i< noiseDatas.Length; i++)
            {
                NoiseData noiseData = noiseDatas[i];
                Vector3Int initEnum = Vector3Int.zero;
                initEnum.x = (int)noiseData.shapeMode;
                Vector2 sphere = new Vector2(noiseData.arc, noiseData.radius);
                Vector3 cube = noiseData.cubeRange;
                Vector3 noise = 
                    new Vector3(noiseData.octave, noiseData.frequency, noiseData.intensity);
                Vector3Int outEnum = Vector3Int.zero;
                outEnum.x = (noiseData.isSizeBySpeed) ? (int)noiseData.sizeBySpeedMode : 0;

                init[i] = new ParticleInitializeData
                {
                    beginPos = noiseData.position.position,
                    velocityBeg = noiseData.velocityBegin,
                    velocityEnd = noiseData.velocityEnd,
                    InitEnum = initEnum,
                    sphereData = sphere,
                    cubeRange = cube,
                    lifeTimeRange = noiseData.lifeTime,
                    noiseData = noise,
                    outEnum = outEnum,
                    smoothRange = noiseData.smoothRange,
                    arriveIndex = 0,
                };
            }

            initializeBuffer.SetData(init);
        }

        private void UpdateInitial()
        {
            if (initializeBuffer == null || init == null) return;
            //for(int i=0; i<init.Length; i++)
            //{
            //    init[i].arriveIndex = arrive;
            //}
            initializeBuffer.SetData(init);
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

            material.SetTexture(mainTexId, mainTexture);
            material.SetInt(rowCountId, rowCount);
            material.SetInt(colCountId, colCount);
            float ratio = (mainTexture == null)? 1 : (float)mainTexture.width / mainTexture.height;
            material.SetFloat(texAspectRatioId, ratio);
        }

        private void SetUpdateData()
        {
            computeShader.SetBuffer(kernel_Updata, particleBufferId, particleBuffer);
            computeShader.SetBuffer(kernel_Updata, initBufferId, initializeBuffer);

            //设置时间
            computeShader.SetVector(timeId, new Vector4(Time.time, Time.deltaTime, Time.fixedDeltaTime));
            //设置枚举位置
            //computeShader.SetInt(arriveIndexId, (int)arrive);

            if (runInUpdate)
            {
                for (int i = 0; i < init.Length; i++)
                {
                    init[i].arriveIndex = arrive;
                }
                initializeBuffer.SetData(init);
            }
        }

        private void SetFixUpdateData()
        {
            computeShader.SetBuffer(kernel_FixUpdata, particleBufferId, particleBuffer);
            computeShader.SetBuffer(kernel_FixUpdata, initBufferId, initializeBuffer);

            //设置时间
            computeShader.SetVector(timeId, new Vector4(Time.time, Time.deltaTime, Time.fixedDeltaTime));
        }

        public override void DrawClustByCamera(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Camera camera)
        {
            material.SetBuffer(particleBufferId, particleBuffer);

            buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Points,
                1, particleCount * noiseDatas.Length);
            ExecuteBuffer(ref buffer, context);
        }

        public override void DrawClustByProjectMatrix(ScriptableRenderContext context, CommandBuffer buffer, ClustDrawType drawType, Matrix4x4 projectMatrix)
        {
            return;
        }
    }
}