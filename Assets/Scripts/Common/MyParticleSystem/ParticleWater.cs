
using UnityEngine;

namespace Common.ParticleSystem
{
    /// <summary>
    /// 水模拟粒子系统控制器，需要时时刷新数据，生成顶点
    /// </summary>
    public class ParticleWater : MonoBehaviour
    {

        private MeshFilter meshFilter;
        public Material setMat;
        private MeshRenderer meshRenderer;
        private Vector3 prePosition;
        private float buttonY;

        /// <summary>        /// 循环到的位置        /// </summary>
        public int circulatePos;

        /// <summary>        /// 粒子输出花费时间        /// </summary>
        public float outTime = 0.3f;
        /// <summary>        /// 粒子到达后开始偏移的损耗时间        /// </summary>
        public float offsetTime = 2f;
        /// <summary>        /// 粒子数量，用来一开始创建        /// </summary>
        public int particleSize = 300;

        public float rayDis = 10;

        public LayerMask layer;

        #region CurveDate
        //移动大小曲线
        public bool isOpenMoveSizeCurve = false;
        public AnimationCurve moveSizeCurve = AnimationCurve.Linear(0,0,1,1);



        //偏移大小曲线
        public bool isOpenOffsetSizeCurve = false;
        public AnimationCurve offsetSizeCurve = AnimationCurve.Linear(0, 0, 1, 1);

        //透明曲线,因为透明都在片原着色器使用，因此设置一个就够了
        public bool isOpenAlphaCurve = false;
        public AnimationCurve offsetAlphaCurve = AnimationCurve.Linear(0, 1, 1, 0);

        //移动透明曲线
        public AnimationCurve moveAlphaCurve = AnimationCurve.Linear(0, 0, 1, 1);

        #endregion

        #region MeshDate              
        //Mesh数据设置位置，因为MeshFilter中的mesh设置没有效果，只能将Mesh
        //提出来然后每次设置为后赋值了

        private Mesh mesh;

        private Vector3[] poss;
        private int[] tris;
        private Vector4[] tangents;
        private Vector3[] normals;
        private Vector2[] uv0;
        private Vector2[] uv1;
        private Vector2[] uv2;
        private Vector2[] uv3;
        private Vector2[] uv4;
        private Vector2[] uv5;
        private Vector2[] uv6;
        private Color[] colors;

        #endregion

        private void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            if(meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            else
            {
                meshFilter.sharedMesh.Clear();
                meshFilter.sharedMesh = null;
            }

            meshRenderer = GetComponent<MeshRenderer>();
            if(meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.allowOcclusionWhenDynamic = false;

            prePosition = Vector3.zero;
            buttonY = 0;

            AddMesh();
            SetMatValue();
        }

        private void OnValidate()
        {
            SetMatValue();
        }

        private void SetMatValue()
        {
            if (setMat == null || meshRenderer == null) return;
            if(meshRenderer.material != setMat)
                meshRenderer.material = setMat;

            setMat.SetFloat("_OutTime", outTime);
            setMat.SetFloat("_OffsetTime", offsetTime);

            Vector4[] vector4;

            //设置移动大小
            vector4 = new Vector4[moveSizeCurve.length];
            for (int i = 0; i < moveSizeCurve.length; i++)
            {
                vector4[i] = new Vector4(moveSizeCurve.keys[i].time, moveSizeCurve.keys[i].value,
                     moveSizeCurve.keys[i].inTangent, moveSizeCurve.keys[i].outTangent);
            }
            if (isOpenMoveSizeCurve) setMat.EnableKeyword("_MOVE_SIZE");
            else setMat.DisableKeyword("_MOVE_SIZE");
            setMat.SetInt("_MoveSizePointCount", moveSizeCurve.length);
            setMat.SetVectorArray("_MoveSizePointArray", vector4);

            //设置透明
            if (isOpenAlphaCurve) setMat.EnableKeyword("_CURVE_ALPHA");
            else setMat.DisableKeyword("_CURVE_ALPHA");

            //移动透明
            vector4 = new Vector4[moveAlphaCurve.length];
            for (int i = 0; i < moveAlphaCurve.length; i++)
            {
                vector4[i] = new Vector4(moveAlphaCurve.keys[i].time, moveAlphaCurve.keys[i].value,
                     moveAlphaCurve.keys[i].inTangent, moveAlphaCurve.keys[i].outTangent);
            }
            setMat.SetInt("_MoveAlphaPointCount", moveAlphaCurve.length);
            setMat.SetVectorArray("_MoveAlphaPointArray", vector4);


            //偏移透明
            vector4 = new Vector4[offsetAlphaCurve.length];
            for (int i = 0; i < offsetAlphaCurve.length; i++)
            {
                vector4[i] = new Vector4(offsetAlphaCurve.keys[i].time, offsetAlphaCurve.keys[i].value,
                     offsetAlphaCurve.keys[i].inTangent, offsetAlphaCurve.keys[i].outTangent);
            }
            setMat.SetInt("_OffsetAlphaPointCount", offsetAlphaCurve.length);
            setMat.SetVectorArray("_OffsetAlphaPointArray", vector4);


            //设置大小
            vector4 = new Vector4[offsetSizeCurve.length];
            for (int i = 0; i < offsetSizeCurve.length; i++)
            {
                vector4[i] = new Vector4(offsetSizeCurve.keys[i].time, offsetSizeCurve.keys[i].value,
                    offsetSizeCurve.keys[i].inTangent, offsetSizeCurve.keys[i].outTangent);
            }


            if (isOpenOffsetSizeCurve)
                setMat.EnableKeyword("_OFFSET_SIZE");
            else setMat.DisableKeyword("_OFFSET_SIZE");
            setMat.SetInt("_OffsetSizePointCount", offsetSizeCurve.length);
            setMat.SetVectorArray("_OffsetSizePointArray", vector4);
        }

        private void Update()
        {
            circulatePos %= particleSize;
            if (meshFilter.sharedMesh.tangents[circulatePos].w > Time.time)
                return;

            RaycastHit raycastHit;

            Vector3 upDir = transform.forward * rayDis;
            Vector3 upPos = transform.position;
            Vector3 veTemp = Vector3.zero;
            float upTime = 0;


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
                if (Physics.Raycast(transform.position, veTemp, out raycastHit, veTemp.magnitude, layer))
                {
                    OneRayHit(raycastHit, raycastHit.distance, upTime);
                    Debug.DrawLine(transform.position, raycastHit.point, Color.red);
                    return;
                }
            }
            if(prePosition != transform.position)
            {
                //默认底部高度
                buttonY = transform.position.y - 1;

                if (Physics.Raycast(transform.position, Vector3.down, out raycastHit, float.MaxValue, layer))
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
            if (Physics.Raycast(upPos, downPos - upPos, out raycastHit, float.MaxValue, layer))
            {
                TwoRayHit(raycastHit, upPos, veTemp.magnitude, transform.forward, downTime + upTime);
                Debug.DrawLine(upPos, raycastHit.point, Color.black);

                return;
            }

            //第二条也没有中,就在空中结束吧
            raycastHit.point = downPos;
            raycastHit.normal = Vector3.up;
            TwoRayHit(raycastHit, upPos, veTemp.magnitude, transform.forward, downTime + upTime);

            //Debug.DrawLine(upPos, raycastHit.point, Color.white);
        }


        /// <summary>
        /// 第一条射线射中目标
        /// </summary>
        /// <param name="raycastHit">射中点数据</param>
        /// <param name="dis">理论上的最大距离，也就是本来这条线的长度</param>
        /// <param name="sqrTrue">中间射中了东西，所以确定实际长度</param>
        private void OneRayHit(RaycastHit raycastHit, float dis, float moveTime)
        {
            SetOneRayPoint(raycastHit, dis, moveTime, circulatePos);
            //SetOneRayPoint(raycastHit, dis, moveTime, circulatePos + 1);
            //SetOneRayPoint(raycastHit, dis, moveTime, circulatePos + 2);
            meshFilter.sharedMesh.SetTangents(tangents);
            meshFilter.sharedMesh.SetColors(colors);
            meshFilter.sharedMesh.SetUVs(0, uv0);
            meshFilter.sharedMesh.SetUVs(1, uv1);
            meshFilter.sharedMesh.SetUVs(2, uv2);
            meshFilter.sharedMesh.SetUVs(3, uv3);
            meshFilter.sharedMesh.SetUVs(4, uv4);
            meshFilter.sharedMesh.SetUVs(6, uv6);
            meshFilter.sharedMesh.SetNormals(normals);
            //circulatePos += 3;
            circulatePos += 1;
        }

        private void SetOneRayPoint(RaycastHit raycastHit, float dis, float moveTime, int index)
        {
            //表示这个粒子正在使用中
            tangents[index].w = Time.time + moveTime + outTime + offsetTime;

            //第一条射线射中
            colors[index] = new Color(1, 0, 0, 1);

            //设置起始位置
            SetPos(ref uv0[index], transform.position);
            uv1[index].x = transform.position.z;

            //设置第一条贝塞尔曲线中点
            Vector3 dir = transform.forward * dis * 0.5f + transform.position;
            SetPos(ref uv2[index], dir);
            uv1[index].y = dir.z;


            //第一条线的终点
            SetPos(ref uv3[index], raycastHit.point);
            uv4[index].x = raycastHit.point.z;

            //射中的法线
            normals[index] = raycastHit.normal;

            //设置粒子移动时间
            uv6[index].x = moveTime;
        }

        /// <summary>
        /// 第二条射线射中的情况
        /// </summary>
        /// <param name="raycastHit">射线射中点信息</param>
        /// <param name="upPos">第一条射线的终点</param>
        /// <param name="firstSqrMax">第一条射线长度的平方</param>
        /// <param name="fowardDir">第二条射线开始的方向，设为参数是为了考虑看向下方的情况</param>
        private void TwoRayHit(RaycastHit raycastHit, Vector3 upPos, float dis, Vector3 fowardDir, float moveTime)
        {
            SetTwoRayPoint(raycastHit, upPos, dis, fowardDir, moveTime, circulatePos);
            //SetTwoRayPoint(raycastHit, upPos, dis, fowardDir, moveTime, circulatePos + 1);
            //SetTwoRayPoint(raycastHit, upPos, dis, fowardDir, moveTime, circulatePos + 2);
            circulatePos += 1;
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetUVs(2, uv2);
            mesh.SetUVs(3, uv3);
            mesh.SetUVs(4, uv4);
            mesh.SetUVs(5, uv5);
            mesh.SetUVs(6, uv6);
            mesh.SetTangents(tangents);
            mesh.SetNormals(normals);
        }

        private void SetTwoRayPoint(RaycastHit raycastHit, Vector3 upPos, 
            float dis, Vector3 fowardDir, float moveTime, int index)
        {
            //表示这个粒子正在使用中
            tangents[index].w = Time.time + moveTime + outTime + offsetTime;


            colors[index] = new Color(0f, 1f, 0f, 1f);

            //设置起始位置
            SetPos(ref uv0[index], transform.position);
            uv1[index].x = transform.position.z;

            //设置第一条射线中间位置
            Vector3 dir = transform.forward * dis * 0.5f + transform.position;
            SetPos(ref uv2[index], dir);
            uv1[index].y = dir.z;

            //设置第一条线的终点
            SetPos(ref uv3[index], upPos);
            uv4[index].x = upPos.z;
            //设置第二条线的中间位置，注意，为了方便，这个点的赋值方式有点特别
            fowardDir *= (raycastHit.point - upPos).magnitude * 0.5f;
            fowardDir += upPos;
            SetPos(ref uv5[index], fowardDir);
            uv4[index].y = fowardDir.z;

            //设置第二条线终点
            SetPos(ref tangents[index], raycastHit.point);

            //设置射中点的法线
            normals[index] = raycastHit.normal;

            //设置粒子移动时间
            uv6[index].x = moveTime;

        }

        /// <summary>        /// 将第二个参数的xyz值赋予第一个参数的xyz中，简化上面的函数        /// </summary>
        private void SetPos(ref Vector4 vector, Vector3 vector3)
        {
            vector.x = vector3.x;
            vector.y = vector3.y;
            vector.z = vector3.z;
        }

        /// <summary>        /// 将第二个参数的xyz值赋予第一个参数的xyz中，简化上面的函数        /// </summary>
        private void SetPos(ref Vector2 vector, Vector3 vector3)
        {
            vector.x = vector3.x;
            vector.y = vector3.y;
        }


        /// <summary>
        /// 用来初始化这个生成的顶点，为了画出较好的贝塞尔曲线，
        /// 且保证每一边都是贝塞尔曲线，打算将三个点的数据传入其中
        /// 首先顶点数据依旧用来存储随机数，这个数据是固定不变的，
        /// 第一条射线的起点存储在uv0和uv1(x)中，即begin=(uv0.xy, uv1.x)
        /// 第一条射线的贝塞尔曲线 中点 存储在uv1(y)和uv2中，其中center=(uv2.xy, uv1.y)
        /// 第一条射线的终点[也是第二条射线的起点]在uv3和uv4(x)中，即end=(uv3.xy, uv4.x)
        /// 第二条射线的贝塞尔曲线 中点 存储在uv4(y)和uv5中，其中center=(uv5.xy, uv4.y)
        /// 第二条射线的终点存储在tangent中，其中end=(tangent.xyz)
        /// 射线的最终点的法线存储在normal中，就是normal = normal
        /// 这个点的结束时间存储在tangent.w中，这个也是刷新的根据时间
        /// color存储了这批点的射中类型，x为1时就是第一条射线射中，y为1就是第二条射线射中
        /// 为了物理模拟，移动时间设在uv6的x中
        /// </summary>
        private void AddMesh()
        {
            //表示没有开始循环
            circulatePos = 0;
            particleSize -= particleSize % 3;

            poss = new Vector3[particleSize];
            tris = new int[particleSize];
            tangents = new Vector4[particleSize];
            normals = new Vector3[particleSize];
            uv0 = new Vector2[particleSize];    //Texcoord0
            uv1 = new Vector2[particleSize];    //Texcoord1
            uv2 = new Vector2[particleSize];    //Texcoord2
            uv3 = new Vector2[particleSize];    //Texcoord3
            uv4 = new Vector2[particleSize];    //Texcoord4
            uv5 = new Vector2[particleSize];    //Texcoord5
            uv6 = new Vector2[particleSize];    //Texcoord6
            colors = new Color[particleSize];

            //三个三个的加
            for (int i = 0; i < particleSize; i += 3)
            {
                poss[i] = new Vector3(-100, 0, -100);
                poss[i + 1] = new Vector3(0, 0, 100);
                poss[i + 2] = new Vector3(100, 0, 0);
                tris[i] = i;
                tris[i + 1] = i + 1;
                tris[i + 2] = i + 2;
                //设置结束时间为负数，让Shader知道这个属性没有在使用中，
                //因为只有当前时间在终止时间和终止时间减存活时间之间才会开始运行
                tangents[i] = new Vector4(0, 0, 0, -100);
                tangents[i + 1] = new Vector4(0, 0, 0, -100);
                tangents[i + 2] = new Vector4(0, 0, 0, -100);
                normals[i] = Vector3.zero;
                normals[i + 1] = Vector3.zero;
                normals[i + 2] = Vector3.zero;

                uv0[i] = Vector2.zero;
                uv0[i + 1] = Vector2.zero;
                uv0[i + 2] = Vector2.zero;

                uv1[i + 2] = Vector2.zero;
                uv1[i + 2] = Vector2.zero;
                uv1[i + 2] = Vector2.zero;

                uv2[i] = Vector2.zero;
                uv2[i + 1] = Vector2.zero;
                uv2[i + 2] = Vector2.zero;

                uv3[i] = Vector2.zero;
                uv3[i + 1] = Vector2.zero;
                uv3[i + 2] = Vector2.zero;

                uv4[i] = Vector2.zero;
                uv4[i + 1] = Vector2.zero;
                uv4[i + 2] = Vector2.zero;

                uv5[i] = Vector2.zero;
                uv5[i + 1] = Vector2.zero;
                uv5[i + 2] = Vector2.zero;

                uv6[i] = Vector2.zero;
                uv6[i + 1] = Vector2.zero;
                uv6[i + 2] = Vector2.zero;

                colors[i] = Color.black;
                colors[i + 1] = Color.black;
                colors[i + 2] = Color.black;
            }

            mesh = new Mesh();
            mesh.vertices = poss;
            mesh.triangles = tris;
            mesh.tangents = tangents;
            mesh.uv = uv0;
            mesh.uv2 = uv1;
            mesh.uv3 = uv2;
            mesh.uv4 = uv3;
            mesh.uv5 = uv4;
            mesh.uv6 = uv5;
            mesh.uv7 = uv6;
            mesh.normals = normals;
            mesh.colors = colors;
            meshFilter.mesh = mesh;
        }
    }
}