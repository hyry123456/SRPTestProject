using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    struct ParticleData
    {
        public Vector4 random;
        public Vector2Int index;
        public Vector3 worldPos;
        public Vector4 uvTransData;
        public float interpolation;
        public Vector4 color;
        public float size;
    }
    struct ParticleGroupData
    {
        public Vector3 beginPos;
        public float lifeTime;
    };

    public class ParticleSimple : GPUPipelineBase
    {
        ComputeBuffer particleBuffer;
        ComputeBuffer origenBuffer;         //根据的buffer

        int kernelId;
        int childCount = 0;

        int particleBufferId = Shader.PropertyToID("_ParticleBuffer"),
            particleGroupBufferId = Shader.PropertyToID("_ParticleGroupBuffer"),
            groupDataId = Shader.PropertyToID("_GroupData"),
            arriveIndexId = Shader.PropertyToID("_ArriveIndex"),
            speedStartId = Shader.PropertyToID("_SpeedStart"),
            speedEndId = Shader.PropertyToID("_SpeedEnd"),
            uvCountId = Shader.PropertyToID("_UVCount"),
            rowCountId = Shader.PropertyToID("_RowCount"),
            colCountId = Shader.PropertyToID("_ColCount"),
            timeId = Shader.PropertyToID("_Time"),
            colorsId = Shader.PropertyToID("_Colors"),
            alphasId = Shader.PropertyToID("_Alphas"),
            sizesId = Shader.PropertyToID("_Sizes");
        int arrive;
        float arriveF;

        List<Vector3> beginPoss;

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

        public ParticleSimple()
        {
            saveName = "pathSimple.particle";
        }


        private void Awake()
        {
            if (computeShader == null || material == null) return;
            kernelId = computeShader.FindKernel("ParticleReady");


        }

        private void LoadAllPostion()
        {
            string str = FileFuctions.LoadAllStr(GetSavePath());
            List<string> strs = FileFuctions.ClipByAngleBrackets(str);
            beginPoss = new List<Vector3>(strs.Count);
            foreach(string index in strs)
            {
                string[] clipIndex = index.Split(',');
                beginPoss.Add(new Vector3(float.Parse(clipIndex[0]),
                    float.Parse(clipIndex[1]), float.Parse(clipIndex[2])));
            }
            childCount = beginPoss.Count;
        }

        private void OnEnable()
        {
            ReadyBuffer();
            GPUPipelineDrawStack.Instance.InsertRender(this);
            isInsert = true;
            SetUnUpdateData();
        }

        private void OnValidate()
        {
            //if (isActiveAndEnabled)
            //{
            //    ReadyBuffer();
            //    SetUnUpdateData();
            //}
        }

        private void ReadyBuffer()
        {
            particleBuffer?.Dispose();
            origenBuffer?.Dispose();
            bool isStatic = gameObject.isStatic;
            //静态就加载文件
            if (isStatic)
                LoadAllPostion();
            //非静态加载位置
            else
                childCount = transform.childCount;

            if (childCount == 0) return;


            particleBuffer = new ComputeBuffer(particleSize * childCount, sizeof(float) * (4 + 2 + 3 + 4 + 1 + 4 + 1));
            List<ParticleData> particleDatas = new List<ParticleData>(particleSize * childCount);
            for (int i = 0; i < childCount; i++)
            {
                for (int j = 0; j < particleSize; j++)
                {
                    if (isStatic)
                    {
                        particleDatas.Add(new ParticleData
                        {
                            index = new Vector2Int(j, 0),
                            random = new Vector4(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
                                Random.Range(0.0f, 1.0f), 0),
                            worldPos = beginPoss[i],
                        });
                    }
                    else
                    {
                        particleDatas.Add(new ParticleData
                        {
                            index = new Vector2Int(j, 0),
                            random = new Vector4(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
                            Random.Range(0.0f, 1.0f), 0),
                            worldPos = transform.GetChild(i).position,
                        });
                    }
                }
            }
            particleBuffer.SetData(particleDatas);

            //准备每组的根据数据
            origenBuffer = new ComputeBuffer(childCount, sizeof(float) * (3 + 1));
            List<ParticleGroupData> particleGroupDatas = new List<ParticleGroupData>(childCount);
            for (int i = 0; i < childCount; i++)
            {
                if (isStatic)
                    particleGroupDatas.Add(new ParticleGroupData
                    {
                        beginPos = beginPoss[i],
                        lifeTime = this.liveTime
                    });
                else
                    particleGroupDatas.Add(new ParticleGroupData
                    {
                        beginPos = transform.GetChild(i).position,
                        lifeTime = this.liveTime
                    });

            }
            origenBuffer.SetData(particleGroupDatas);
        }

        private void Update()
        {
            if (childCount == 0)
                return;
            arriveF += Time.deltaTime;
            if (arriveF / (1.0f / particleOutSize) > 1)
            {
                arriveF = 0;
                arrive++;
            }

            SetUpdateData();
            computeShader.Dispatch(kernelId, childCount, (particleSize / 64) + 1, 1);
        }

        private void OnDisable()
        {
            if (isInsert)
            {
                GPUPipelineDrawStack.Instance.RemoveRender(this);
                isInsert = false;
            }
            particleBuffer?.Dispose();
            origenBuffer?.Dispose();
        }

        /// <summary>        /// 设置非时时帧数据        /// </summary>
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
        }

        /// <summary>        /// 设置时时帧数据        /// </summary>
        void SetUpdateData()
        {
            computeShader.SetBuffer(kernelId, particleBufferId, particleBuffer);
            computeShader.SetBuffer(kernelId, particleGroupBufferId, origenBuffer);

            //设置时间
            computeShader.SetVector(timeId, new Vector4(Time.time, Time.deltaTime));
            //设置枚举位置
            computeShader.SetInt(arriveIndexId, arrive);

        }

        public override void DrawClustByCamera(ScriptableRenderContext context, 
            CommandBuffer buffer, ClustDrawType drawType, Camera camera)
        {
            if (childCount == 0) return;
            material.SetBuffer(particleBufferId, particleBuffer);
            material.SetInt(rowCountId, rowCount);
            material.SetInt(colCountId, colCount);
            buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Points,
                1, particleSize * childCount);
            ExecuteBuffer(ref buffer, context);
        }

        public override void DrawClustByProjectMatrix(ScriptableRenderContext context, 
            CommandBuffer buffer, ClustDrawType drawType, Matrix4x4 projectMatrix)
        {
            return; //暂时不绘制阴影
        }

        /// <summary>        /// 设置为只要是个物体就要进行管理，因为我们只需要位置        /// </summary>
        public override bool CheckNeedControl(GameObject game)
        {
            if (game != null && game.activeSelf)
                return true;
            return false;
        }

        public override string GetSavePath()
        {
            return Application.streamingAssetsPath + "/Particle/" + saveName;
        }

        public override string ReadyData(GameObject game, GPUPipelineBase clustBase)
        {
            StringBuilder context = new StringBuilder("");
            context.Append("<");
            context.Append(Vertex3ToString(game.transform.position));
            context.Append(">");
            return context.ToString();
        }
    }
}

