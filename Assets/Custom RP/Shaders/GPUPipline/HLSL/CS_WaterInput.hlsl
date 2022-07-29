#ifndef GPUPIPELINE_PARTICLE_WATER_INCLUDE
#define GPUPIPELINE_PARTICLE_WATER_INCLUDE

#include "../../../ShaderLibrary/Common.hlsl"

TEXTURE2D(_MainTex);
TEXTURE2D(_DistortionTex);
SAMPLER(sampler_MainTex);

//由于本身是使用DrawProcedural，没有必要GPU实例化
CBUFFER_START(UnityPerMaterial)
	float4 _MainTex_ST;
    float4 _ParticleColor;
    //偏移主纹理
    #ifdef _DISTORTION
        float _DistortionSize;
        float _DistortionBlend;
    #endif

    //软粒子
    #ifdef _SOFT_PARTICLE
        float _SoftParticlesDistance;
        float _SoftParticlesRange;
    #endif

    //近平面透明
    #ifdef _NEAR_ALPHA
        float _NearFadeDistance;
        float _NearFadeRange;
    #endif

    int _RowCount;
    int _ColCount;
    float _ParticleSize;

CBUFFER_END

struct WaterParticleData {
    float4 random;          //xyzw都是随机数，w用来确定死亡时间
    int index;             //状态标记，确定是否存活
    float3 worldPos;        //当前位置
    float alpha;           //颜色值，包含透明度
    float size;             //粒子大小
    float3 nowSpeed;        //xyz是当前速度
};

StructuredBuffer<WaterParticleData> _WaterParticleBuffer;         //单个粒子的buffer数据

//顶点生成平面需要赋值的数据
struct PointToQuad {
    float3 worldPos;
    float size;
    float alpha;
};

struct FragInput
{
    float alpha : VAR_COLOR;
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 center : VAR_SPHERE_CENTER;
    float3 worldPos : VAR_WORLD;
};

//封装点生成面
void outOnePoint(inout TriangleStream<FragInput> tristream, PointToQuad i) 
{
    FragInput o[4] = (FragInput[4])0;

    float3 worldVer = i.worldPos;
    float paritcleLen = i.size * _ParticleSize;
    float3 sphereCenter = worldVer - UNITY_MATRIX_V[2].xyz * paritcleLen;

    float3 worldPos = worldVer + -unity_MatrixV[0].xyz * paritcleLen + -unity_MatrixV[1].xyz * paritcleLen;
    o[0].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    o[0].uv = float2(0, 0);
    o[0].alpha = i.alpha;
    o[0].center = sphereCenter;
    o[0].worldPos = worldPos;

    worldPos = worldVer + UNITY_MATRIX_V[0].xyz * -paritcleLen
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[1].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    o[1].alpha = i.alpha;
    o[1].uv = float2(1, 0);
    o[1].center = sphereCenter;
    o[1].worldPos = worldPos;

    worldPos = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen
        + UNITY_MATRIX_V[1].xyz * -paritcleLen;
    o[2].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    o[2].alpha = i.alpha;
    o[2].uv = float2(0, 1);
    o[2].center = sphereCenter;
    o[2].worldPos = worldPos;

    worldPos = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[3].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    o[3].alpha = i.alpha;
    o[3].uv = float2(1, 1);
    o[3].center = sphereCenter;
    o[3].worldPos = worldPos;

    tristream.Append(o[1]);
    tristream.Append(o[2]);
    tristream.Append(o[0]);
    tristream.RestartStrip();

    tristream.Append(o[1]);
    tristream.Append(o[3]);
    tristream.Append(o[2]);
    tristream.RestartStrip();
}

float4 GetBaseColor(FragInput i){
    float4 color1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    return color1 * i.alpha;
}

float ChangeAlpha(Fragment fragment){
    float reAlpha = 1;

    #ifdef _NEAR_ALPHA
        reAlpha = saturate( (fragment.depth - _NearFadeDistance) / _NearFadeRange );
    #endif

    #ifdef _SOFT_PARTICLE
        float depthDelta = fragment.bufferDepth - fragment.depth;	//获得深度差
        reAlpha *= (depthDelta - _SoftParticlesDistance) / _SoftParticlesRange;	//进行透明控制
    #endif
    return saturate( reAlpha );
}


#endif