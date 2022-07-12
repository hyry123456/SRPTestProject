#ifndef CLUST_SHADOW_CASTER_PASS_INCLUDED
#define CLUST_SHADOW_CASTER_PASS_INCLUDED

#include "../../../ShaderLibrary/Common.hlsl"
#include "ClustInput.hlsl"

struct Varyings {
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
};

Varyings ClustShadowPassVertex(uint id : SV_VertexID, 
    uint instanceID : SV_InstanceID)
{
    Varyings o;
    Triangle tri = cullresult[instanceID];
    if (id == 0) {
        o.positionCS = mul(UNITY_MATRIX_VP, float4(tri.point0, 1));
        o.baseUV = tri.uv0_0;
    }
    if (id == 1) {
        o.positionCS = mul(UNITY_MATRIX_VP, float4(tri.point1, 1));
        o.baseUV = tri.uv0_1;
    }
    if (id == 2) {
        o.positionCS = mul(UNITY_MATRIX_VP, float4(tri.point2, 1));
        o.baseUV = tri.uv0_2;
    }

	#if UNITY_REVERSED_Z
		o.positionCS.z =
			min(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
	#else
		o.positionCS.z =
			max(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
	#endif

    return o;
}

void ClustShadowPassFragment (Varyings input) {
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	float4 baseColor = _BaseColor;
	float4 base = baseMap * baseColor;
	#if defined(_SHADOWS_CLIP)
		clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	#elif defined(_SHADOWS_DITHER)
		float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
		clip(base.a - dither);
	#endif
}


#endif