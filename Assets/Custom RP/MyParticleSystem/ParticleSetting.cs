
using UnityEngine;


namespace Common.ParticleSystem
{
    /// <summary>
    /// 用来设置粒子材质用的类，我的打算是用一个类去创造以及设置粒子系统的材质属性
    /// 然后用这个材质在使用时在内存中进行创建使用以及生成
    /// </summary>
    public class ParticleSetting : MonoBehaviour
    {
        public Material setMat;

        /// <summary>        /// 是否移动根据曲线        /// </summary>
        public bool CurveMove;
        /// <summary>        /// 根据的曲线        /// </summary>
        public AnimationCurve widthCurve = AnimationCurve.Linear(0, 0, 1, 1);
        /// <summary>        /// 是否高度应用与曲线        /// </summary>
        public bool CurveHight;
        /// <summary>        /// 是否宽度应用与曲线        /// </summary>
        public bool CurveWidth;

        /// <summary>        /// 大小跟随曲线变化        /// </summary>
        public bool CurveSize;
        public AnimationCurve sizeCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public bool CurveAlpha;
        public AnimationCurve alphaCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private void SetMatValue()
        {
            if (setMat == null) return;
            Vector4[] vector4;

            //设置移动
            vector4 = new Vector4[widthCurve.length];
            for (int i = 0; i < widthCurve.length; i++)
            {
                vector4[i] = new Vector4(widthCurve.keys[i].time, widthCurve.keys[i].value,
                     widthCurve.keys[i].inTangent, widthCurve.keys[i].outTangent);
            }


            if (CurveMove) setMat.EnableKeyword("_CURVE_MOVE");
            else setMat.DisableKeyword("_CURVE_MOVE");

            if (CurveHight) setMat.EnableKeyword("_MOVE_HIGHT");
            else setMat.DisableKeyword("_MOVE_HIGHT");

            if (CurveWidth) setMat.EnableKeyword("_MOVE_WIDTH");
            else setMat.DisableKeyword("_MOVE_WIDTH");

            setMat.SetInt("_MovePointCount", widthCurve.length);
            setMat.SetVectorArray("_MovePointArray", vector4);

            //设置大小
            vector4 = new Vector4[sizeCurve.length];
            for (int i = 0; i < sizeCurve.length; i++)
            {
                vector4[i] = new Vector4(sizeCurve.keys[i].time, sizeCurve.keys[i].value,
                    sizeCurve.keys[i].inTangent, sizeCurve.keys[i].outTangent);
            }

            if (CurveSize)
                setMat.EnableKeyword("_CURVE_SIZE");
            else setMat.DisableKeyword("_CURVE_SIZE");
            setMat.SetInt("_SizePointCount", sizeCurve.length);
            setMat.SetVectorArray("_SizePointArray", vector4);

            //设置透明度
            vector4 = new Vector4[alphaCurve.length];
            for (int i = 0; i < alphaCurve.length; i++)
            {
                vector4[i] = new Vector4(alphaCurve.keys[i].time, alphaCurve.keys[i].value,
                    alphaCurve.keys[i].inTangent, alphaCurve.keys[i].outTangent);
            }

            if (CurveAlpha) setMat.EnableKeyword("_CURVE_ALPHA");
            else setMat.DisableKeyword("_CURVE_ALPHA");
            setMat.SetInt("_AlphaPointCount", alphaCurve.length);
            setMat.SetVectorArray("_AlphaPointArray", vector4);

            //设置位置
            setMat.SetVector("_BeginPos", transform.position);

        }

        private void OnValidate()
        {
            SetMatValue();
        }


        /// <summary>
        /// 开始播放特效
        /// </summary>
        public void BeginEffect()
        {
            SetMatValue();
            ParticleTriangle particleTriangle = new ParticleTriangle();
            for(int i=0; i<transform.childCount; i++)
            {
                particleTriangle.OnBegin(transform.GetChild(i).gameObject, setMat);
            }
        }



        private void Start()
        {
            BeginEffect();
        }

    }
}