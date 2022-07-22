#ifndef CLUST_INPUT_INCLUDED
#define CLUST_INPUT_INCLUDED

struct Quards {
    float3 position0;
    float3 position1;
    float3 position2;
    float3 position3;

    float3 normal0;
    float3 normal1;
    float3 normal2;
    float3 normal3;

    float2 uv0_0;
    float2 uv0_1;
    float2 uv0_2;
    float2 uv0_3;

    float4 tangent0;
    float4 tangent1;
    float4 tangent2;
    float4 tangent3;
};

struct LightingTriangle {
    float3 point0;
    float3 normal0;
    float4 tangen0;
    float2 uv0_0;

    float3 point1;
    float3 normal1;
    float4 tangen1;
    float2 uv0_1;

    float3 point2;
    float3 normal2;
    float4 tangen2;
    float2 uv0_2;

    int lightArray0;
    int lightArray1;
};

//buffer数据，生成的根据
StructuredBuffer<Quards> _CullResult;
StructuredBuffer<LightingTriangle> _LightCullResult;

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
//因为实际上这些材质都只会渲染一次，就不进行实例化设置了
CBUFFER_START(UnityPerMaterial)
	float4 _BaseMap_ST;
	float4 _BaseColor;
	float _Cutoff;
CBUFFER_END


#endif
