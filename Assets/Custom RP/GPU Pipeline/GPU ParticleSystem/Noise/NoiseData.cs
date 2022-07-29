using UnityEngine;

namespace CustomRP.GPUPipeline
{
    public struct NoiseParticleData
    {
        public Vector4 random;          //xyz���������w��Ŀǰ���ʱ��
        public Vector2Int index;             //״̬��ǣ�x�ǵ�ǰ��ţ�y���Ƿ���
        public Vector3 worldPos;        //��ǰλ��
        public Vector4 uvTransData;     //uv������Ҫ������
        public float interpolation;    //��ֵ��Ҫ������
        public Vector4 color;           //��ɫֵ������͸����
        public float size;             //���Ӵ�С
        public Vector3 nowSpeed;        //xyz�ǵ�ǰ�ٶȣ�w�Ǵ��ʱ��
        public float liveTime;         //�����ʱ��
    }

    public struct ParticleInitializeData
    {
        public Vector3 beginPos;        //�����������г�ʼλ��
        public Vector3 velocityBeg;     //��ʼ�ٶȷ�Χ
        public Vector3 velocityEnd;     //��ʼ�ٶȷ�Χ
        public Vector3Int InitEnum;     //��ʼ���ĸ��ݱ��
        public Vector2 sphereData;      //��ʼ����������Ҫ������
        public Vector3 cubeRange;       //��ʼ����������ķ�Χ
        public Vector2 lifeTimeRange;   //�������ڵķ�Χ

        public Vector3 noiseData;       //���������ٶ�ʱ��Ҫ������

        public Vector3Int outEnum;      //ȷ�����ʱ�㷨��ö��
        public Vector2 smoothRange;       //���ӵĴ�С��Χ

        public uint arriveIndex;
    };

    public enum SizeBySpeedMode
    {
        TIME = 0,
        X = 1,
        Y = 2,
        Z = 3,
    }

    public enum InitialShapeMode
    {
        Pos = 0,
        Sphere = 1,
        Cube = 2
    }

    /// <summary>   /// �������Ƶ����������ӵ����ݽṹ��    /// </summary>
    [System.Serializable]
    public class NoiseData 
    {
        //��ʼ��
        public InitialShapeMode shapeMode = InitialShapeMode.Pos;
        public Transform position;
        [Range(0.01f, 6.18f)]
        public float arc = 0.1f;              //�������ɷ�Χ
        public float radius = 1;           //Բ��С
        public Vector3 cubeRange;          //���δ�С
        public Vector3 velocityBegin;      //�ٶȷ�Χ
        public Vector3 velocityEnd;
        public Vector2 lifeTime = new Vector2(0.01f, 1);

        //����
        [Range(1, 8)]
        public int octave = 1;
        public float frequency = 1;
        [Min(0.1f)]
        public float intensity = 0.5f;

        //�������
        public bool isSizeBySpeed;
        public SizeBySpeedMode sizeBySpeedMode = SizeBySpeedMode.TIME;
        public Vector2 smoothRange = Vector2.up;


    }
}