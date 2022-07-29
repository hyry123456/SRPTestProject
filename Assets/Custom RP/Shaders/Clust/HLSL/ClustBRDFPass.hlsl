#ifndef CLUST_BRDF_PASS_INCLUDED
#define CLUST_BRDF_PASS_INCLUDED

#include "../../../ShaderLibrary/Surface.hlsl"
#include "../../../ShaderLibrary/Shadows.hlsl"
#include "../../../ShaderLibrary/Light.hlsl"
#include "../../../ShaderLibrary/BRDF.hlsl"
#include "../../../ShaderLibrary/GI.hlsl"
#include "../../../ShaderLibrary/Lighting.hlsl"

#include "ClustBRDFInput.hlsl"

struct Varyings {
	float4 positionCS_SS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float3 normalWS : VAR_NORMAL;
	float2 baseUV : VAR_BASE_UV;

	#if defined(_NORMAL_MAP)
		float4 tangentWS : VAR_TANGENT;
	#endif
	#if defined(_DETAIL_MAP)
		float2 detailUV : VAR_DETAIL_UV;
	#endif
};

void ClustBRDFVertex(inout uint instanceID : SV_InstanceID)
{

}

[maxvertexcount(6)]
void ClustBRDFPassGemo(point uint instanceID[1] : SV_InstanceID, inout TriangleStream<Varyings> tristream)
{
	Varyings o[4] = (Varyings[4])0;
	Quards quard = _CullResult[instanceID[0]];
	o[0].positionWS = quard.position0;
	o[1].positionWS = quard.position1;
	o[2].positionWS = quard.position2;
	o[3].positionWS = quard.position3;

	o[0].normalWS = quard.normal0;
	o[1].normalWS = quard.normal1;
	o[2].normalWS = quard.normal2;
	o[3].normalWS = quard.normal3;

	o[0].baseUV = TransformBaseUV(quard.uv0_0);
	o[1].baseUV = TransformBaseUV(quard.uv0_1);
	o[2].baseUV = TransformBaseUV(quard.uv0_2);
	o[3].baseUV = TransformBaseUV(quard.uv0_3);

	o[0].positionCS_SS = TransformWorldToHClip(o[0].positionWS);
	o[1].positionCS_SS = TransformWorldToHClip(o[1].positionWS);
	o[2].positionCS_SS = TransformWorldToHClip(o[2].positionWS);
	o[3].positionCS_SS = TransformWorldToHClip(o[3].positionWS);

	#if defined(_NORMAL_MAP)
		o[0].tangentWS = quard.tangent0;
		o[1].tangentWS = quard.tangent1;
		o[2].tangentWS = quard.tangent2;
		o[3].tangentWS = quard.tangent3;
	#endif
	#if defined(_DETAIL_MAP)
		o[0].detailUV = TransformDetailUV(quard.uv0_0);
		o[1].detailUV = TransformDetailUV(quard.uv0_1);
		o[2].detailUV = TransformDetailUV(quard.uv0_2);
		o[3].detailUV = TransformDetailUV(quard.uv0_3);
	#endif

	tristream.Append(o[0]);
	tristream.Append(o[1]);
	tristream.Append(o[2]);
	tristream.RestartStrip();
	tristream.Append(o[3]);
	tristream.Append(o[1]);
	tristream.Append(o[0]);
	tristream.RestartStrip();

}


//整个流程是模拟Lit的，毕竟没什么好改的
float4 ClustBRDFFragment(Varyings input) : SV_TARGET{
	InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
	#if defined(_MASK_MAP)
		config.useMask = true;
	#endif
	#if defined(_DETAIL_MAP)
		config.detailUV = input.detailUV;
		config.useDetail = true;
	#endif
	
	float4 base = GetBase(config);
	#if defined(_CLIPPING)
		clip(base.a - GetCutoff(config));
	#endif
	
	Surface surface;
	surface.position = input.positionWS;
	#if defined(_NORMAL_MAP)
		surface.normal = NormalTangentToWorld(
			GetNormalTS(config), input.normalWS, input.tangentWS
		);
		surface.interpolatedNormal = input.normalWS;
	#else
		surface.normal = normalize(input.normalWS);
		surface.interpolatedNormal = surface.normal;
	#endif

	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(config);
	surface.occlusion = GetOcclusion(config);
	surface.smoothness = GetSmoothness(config);
	surface.fresnelStrength = GetFresnel(config);
	surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
	surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
	
	#if defined(_PREMULTIPLY_ALPHA)
		BRDF brdf = GetBRDF(surface, true);
	#else
		BRDF brdf = GetBRDF(surface);
	#endif

	// GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    GI gi = GetDiffuseGI(surface);
	float3 color = GetLighting(surface, brdf, gi);
	color += GetEmission(config);
	return float4(color, GetFinalAlpha(surface.alpha));
}

#endif