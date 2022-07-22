#ifndef CLUST_SHADOW_CASTER_PASS_INCLUDED
#define CLUST_SHADOW_CASTER_PASS_INCLUDED

#include "../../../ShaderLibrary/Common.hlsl"
#include "ClustInput.hlsl"

struct Varyings {
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
};

bool _ShadowPancaking;

void ClustShadowPassVertex(inout uint instanceID : SV_InstanceID)
{
    // Varyings o;
    // Triangle tri = cullresult[instanceID];
    // if (id == 0) {
    //     o.positionCS = mul(UNITY_MATRIX_VP, float4(tri.point0, 1));
    //     o.baseUV = tri.uv0_0;
    // }
    // if (id == 1) {
    //     o.positionCS = mul(UNITY_MATRIX_VP, float4(tri.point1, 1));
    //     o.baseUV = tri.uv0_1;
    // }
    // if (id == 2) {
    //     o.positionCS = mul(UNITY_MATRIX_VP, float4(tri.point2, 1));
    //     o.baseUV = tri.uv0_2;
    // }

	// if (_ShadowPancaking) {
	// 	#if UNITY_REVERSED_Z
	// 			o.positionCS.z =
	// 				min(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
	// 	#else
	// 			o.positionCS.z =
	// 				max(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
	// 	#endif
	// }

    // return o;
}

[maxvertexcount(6)]
void ClustShadowPassGemo(point uint instanceID[1] : SV_InstanceID, inout TriangleStream<Varyings> tristream)
{
	Varyings o[4] = (Varyings[4])0;
	Quards quard = _CullResult[instanceID[0]];

	o[0].baseUV = quard.uv0_0;
	o[1].baseUV = quard.uv0_1;
	o[2].baseUV = quard.uv0_2;
	o[3].baseUV = quard.uv0_3;

	o[0].positionCS = TransformWorldToHClip(quard.position0);
	o[1].positionCS = TransformWorldToHClip(quard.position1);
	o[2].positionCS = TransformWorldToHClip(quard.position2);
	o[3].positionCS = TransformWorldToHClip(quard.position3);

	if (_ShadowPancaking) {
		#if UNITY_REVERSED_Z
				o[0].positionCS.z = min(o[0].positionCS.z, o[0].positionCS.w * UNITY_NEAR_CLIP_VALUE);
				o[1].positionCS.z = min(o[1].positionCS.z, o[1].positionCS.w * UNITY_NEAR_CLIP_VALUE);
				o[2].positionCS.z = min(o[2].positionCS.z, o[2].positionCS.w * UNITY_NEAR_CLIP_VALUE);
				o[3].positionCS.z = min(o[3].positionCS.z, o[3].positionCS.w * UNITY_NEAR_CLIP_VALUE);
		#else
				o[0].positionCS.z = max(o[0].positionCS.z, o[0].positionCS.w * UNITY_NEAR_CLIP_VALUE);
				o[1].positionCS.z = max(o[1].positionCS.z, o[1].positionCS.w * UNITY_NEAR_CLIP_VALUE);
				o[2].positionCS.z = max(o[2].positionCS.z, o[2].positionCS.w * UNITY_NEAR_CLIP_VALUE);
				o[3].positionCS.z = max(o[3].positionCS.z, o[3].positionCS.w * UNITY_NEAR_CLIP_VALUE);
		#endif
	}

	tristream.Append(o[0]);
	tristream.Append(o[1]);
	tristream.Append(o[2]);
	tristream.RestartStrip();
	tristream.Append(o[3]);
	tristream.Append(o[1]);
	tristream.Append(o[0]);
	tristream.RestartStrip();

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