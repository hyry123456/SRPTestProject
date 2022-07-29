using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    public struct WaterGroupData
    {
        public Vector4 plane;       //判断用的平面，xyz=normal，w=D
        public Vector2 lifeTime;     //存活时间，x=beginTime，y=endTime
        public Vector3 beginDir;    //初始方向
        public Vector3 beginPos;    //初始位置
    };

    struct WaterParticleData
    {
        public Vector4 random;          //xyzw都是随机数，w用来确定死亡时间
        public int index;             //状态标记，确定是否存活
        public Vector3 worldPos;        //当前位置
        public float alpha;           //颜色值，包含透明度
        public float size;             //粒子大小
        public Vector3 nowSpeed;        //xyz是当前速度
    };

    public class NoiseWater : DrawWaterBase
    {
        public ComputeShader computeShader;
        private ComputeBuffer particleBuffer;
        private ComputeBuffer groupBuffer;

        //初始化
        [SerializeField]
        private int particleCount = 64000;   //每组粒子数量
        int groupCount;

        [SerializeField]
        private Material material;
        [SerializeField]
        private AnimationCurve sizeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField]
        [GradientUsage(true)]
        private Gradient colorWithLive;

        [SerializeField]
        float rayDis = 10;      //射线距离
        [SerializeField]
        LayerMask rayLayer;     //可以射中的层
        [SerializeField]
        float outTime = 3;      //水额外存活的时间
        Vector3 prePosition;

        //更新
        [Range(1, 8), SerializeField]
        int octave = 1;
        [SerializeField]
        float frequency = 1;
        [Min(0.1f), SerializeField]
        float intensity = 0.5f;

        private int kernel_Updata;
        private int kernel_FixUpdata;
        private WaterGroupData[] groups;
        private bool isInsert;

        private int
            waterGroupId = Shader.PropertyToID("_WaterGroupBuffer"),
            waterParticleId = Shader.PropertyToID("_WaterParticleBuffer"),
            timeId = Shader.PropertyToID("_Time"),
            sizesId = Shader.PropertyToID("_Sizes"),
            alphasId = Shader.PropertyToID("_Alphas"),
            noiseDataId = Shader.PropertyToID("_NoiseData"),
            particleCountId = Shader.PropertyToID("_ParticleCount");

        private void Awake()
        {
            if (computeShader == null || material == null) return;
            kernel_Updata = computeShader.FindKernel("Water_PerFrame");
            kernel_FixUpdata = computeShader.FindKernel("Water_PerFixFrame");
        }

        private void OnEnable()
        {
            WaterDrawStack.Instance.InsertRender(this);
            isInsert = true;

            ReadyBuffer();
            SetUnUpdateData();
        }

        private void OnValidate()
        {
            if (isInsert)
            {
                SetUnUpdateData();
            }
        }

        private void OnDisable()
        {
            if (isInsert)
            {
                WaterDrawStack.Instance.RemoveRender(this);
                isInsert = false;
            }

            particleBuffer?.Dispose();
            groupBuffer?.Dispose();
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
            particleCount -= particleCount % 64;
            particleBuffer = new ComputeBuffer(particleCount ,
                sizeof(float) * (4 + 1 + 3 + 1 + 1 + 3));
            WaterParticleData[] waterParticles = new WaterParticleData[particleCount];
            for(int i=0; i<particleCount; i++)
            {
                waterParticles[i] = new WaterParticleData()
                {
                    random = new Vector4(Random.value, Random.value, Random.value, Random.value),
                    index = -1,
                };
            }
            particleBuffer.SetData(waterParticles);
        }

        /// <summary>        /// 加载每一组粒子的初始化数据        /// </summary>
        private void ReadyInitialParticle()
        {
            groupBuffer?.Dispose();
            groupCount = particleCount / 64;
            groupBuffer = new ComputeBuffer(groupCount,
                sizeof(float) * (4 + 2 + 3 + 3));
            groups = new WaterGroupData[groupCount];
            for (int i = 0; i < groupCount; i++)
            {

                groups[i] = new WaterGroupData
                {
                    lifeTime = new Vector2(-100, -100)
                };
            }

            groupBuffer.SetData(groups);
        }

        private void SetUnUpdateData()
        {
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
        }

        private void Update()
        {
            SetUpdateData();
            computeShader.Dispatch(kernel_Updata, groupCount, 1, 1);
        }

        private void SetUpdateData()
        {
            computeShader.SetBuffer(kernel_Updata, waterParticleId, particleBuffer);
            computeShader.SetBuffer(kernel_Updata, waterGroupId, groupBuffer);

            //设置时间
            computeShader.SetVector(timeId, new Vector4(Time.time, Time.deltaTime, Time.fixedDeltaTime));
            computeShader.SetVector(noiseDataId, new Vector3(octave, frequency, intensity));
        }

        private void SetFixUpdateData()
        {
            computeShader.SetBuffer(kernel_FixUpdata, waterParticleId, particleBuffer);
            computeShader.SetBuffer(kernel_FixUpdata, waterGroupId, groupBuffer);

            //设置时间
            computeShader.SetVector(timeId, new Vector4(Time.time, Time.deltaTime, Time.fixedDeltaTime));
        }

        private void FixedUpdate()
        {
            RunWater();
            SetFixUpdateData();
            computeShader.Dispatch(kernel_FixUpdata, groupCount, 1, 1);
        }

        public int circulatePos = 0;
        /// <summary>       /// 运行液体喷射的方法，返回被射中的物体        /// </summary>
        public GameObject RunWater()
        {
            circulatePos %= groupCount;
            if (groups[circulatePos].lifeTime.y > Time.time)
                return null;

            RaycastHit raycastHit;
            Vector3 upDir = transform.forward * rayDis;
            Vector3 upPos = transform.position;
            Vector3 veTemp = Vector3.zero;
            float upTime = 0;
            float buttonY = 0;

            //向上时执行上抛，否则直接向下确定位置
            if (transform.forward.y >= 0)
            {
                upTime = upDir.y / 9.8f;

                //确定第一条射线数据
                upDir.y = 0;
                upPos.y += 0.5f * 9.8f * upTime * upTime;
                upPos += upDir * upTime;
                veTemp = upPos - transform.position;
                //第一条线射中目标，检查是否上方有东西阻挡
                if (Physics.Raycast(transform.position, veTemp, out raycastHit, veTemp.magnitude, rayLayer))
                {
                    SetRayPoint(raycastHit, rayDis * transform.forward, upTime, circulatePos);
                    circulatePos++;
                    //Debug.DrawLine(transform.position, raycastHit.point, Color.red);
                    return raycastHit.collider.gameObject;
                }
            }
            if (prePosition != transform.position)
            {
                //默认底部高度
                buttonY = transform.position.y - 1;

                if (Physics.Raycast(transform.position, Vector3.down, out raycastHit, float.MaxValue, rayLayer))
                {
                    buttonY = raycastHit.point.y;
                }
            }

            float s = upPos.y - buttonY;

            float downTime = Mathf.Sqrt(
                2 * (0.5f * 9.8f * Mathf.Pow(upDir.y / 9.8f, 2) + s) / 9.8f
                ) - Mathf.Abs(upDir.y) / 9.8f;
            upDir.y = 0;
            Vector3 downPos = downTime * upDir + transform.position;
            downPos.y = buttonY;


            //第二条射线默认无限距离，往尽头射
            if (Physics.Raycast(upPos, downPos - upPos, out raycastHit, float.MaxValue, rayLayer))
            {
                SetRayPoint(raycastHit, rayDis * transform.forward, downTime + upTime, circulatePos);
                circulatePos++;
                //Debug.DrawLine(upPos, raycastHit.point, Color.black);

                return raycastHit.collider.gameObject;
            }

            //第二条也没有中,就在空中结束吧
            raycastHit.point = downPos;
            raycastHit.normal = Vector3.up;
            SetRayPoint(raycastHit, rayDis * transform.forward, upTime, circulatePos);
            circulatePos++;
            //Debug.Log("Two Point");

            return null;
        }


        private void SetRayPoint(RaycastHit raycastHit, Vector3 beginSpeed, float moveTime, int index)
        {
            groups[index].lifeTime.x = Time.time;
            groups[index].lifeTime.y = Time.time + moveTime + outTime;
            groups[index].beginDir = beginSpeed;
            groups[index].beginPos = transform.position;
            groups[index].plane = GetPlane(raycastHit.normal, raycastHit.point);
            groupBuffer.SetData(groups, index, index, 1);
        }

        private Vector4 GetPlane(Vector3 normal, Vector3 point)
        {
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
        }


        public override void DrawWater(ScriptableRenderContext context, CommandBuffer buffer, WaterDrawMode drawType)
        {
            material.SetBuffer(waterParticleId, particleBuffer);

            buffer.DrawProcedural(Matrix4x4.identity, material, (int)drawType, MeshTopology.Points,
                1, particleCount);
            ExecuteBuffer(ref buffer, context);
        }
    }
}