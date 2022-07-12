#ifndef CLUST_BRDF_LIGHT_CULL_PASS_INCLUDED
#define CLUST_BRDF_LIGHT_CULL_PASS_INCLUDED

#include "../../../ShaderLibrary/Surface.hlsl"
#include "../../../ShaderLibrary/Shadows.hlsl"
#include "../../../ShaderLibrary/Light.hlsl"
#include "../../../ShaderLibrary/BRDF.hlsl"
#include "../../../ShaderLibrary/GI.hlsl"
#include "HLSL/ClustLighting.hlsl"

#include "ClustBRDFInput.hlsl"

struct Varyings {
	float4 positionCS_SS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float3 normalWS : VAR_NORMAL;
	float2 baseUV : VAR_BASE_UV;
    int lightArray0 : LIGHTING_ARRAY0;
    int lightArray1 : LIGHTING_ARRAY1;

	#if defined(_NORMAL_MAP)
		float4 tangentWS : VAR_TANGENT;
	#endif
	#if defined(_DETAIL_MAP)
		float2 detailUV : VAR_DETAIL_UV;
	#endif
};

Varyings ClustBRDFVertex(uint id : SV_VertexID, 
    uint instanceID : SV_InstanceID)
{
    Varyings o = (Varyings)0;
    //获得该实例的对应三角形数据
    LightingTriangle tri = _LightCullResult[instanceID];
    if (id == 0) {
        o.positionWS = tri.point0;
        o.positionCS_SS = TransformWorldToHClip(o.positionWS);
        o.baseUV = TransformBaseUV(tri.uv0_0);
        o.normalWS = tri.normal0;

	    #if defined(_NORMAL_MAP)
            o.tangentWS = tri.tangen0;
        #endif
        #if defined(_DETAIL_MAP)
            o.detailUV = TransformDetailUV(tri.uv0_0);
        #endif
    }
    if (id == 1) {
        o.positionWS = tri.point1;
        o.positionCS_SS = TransformWorldToHClip(o.positionWS);
        o.baseUV = TransformBaseUV(tri.uv0_1);
        o.normalWS = tri.normal1;

	    #if defined(_NORMAL_MAP)
            o.tangentWS = tri.tangen1;
        #endif
        #if defined(_DETAIL_MAP)
            o.detailUV = TransformDetailUV(tri.uv0_1);
        #endif
    }
    if (id == 2) {
        o.positionWS = tri.point2;
        o.positionCS_SS = TransformWorldToHClip(o.positionWS);
        o.baseUV = TransformBaseUV(tri.uv0_2);
        o.normalWS = tri.normal2;

	    #if defined(_NORMAL_MAP)
            o.tangentWS = tri.tangen2;
        #endif
        #if defined(_DETAIL_MAP)
            o.detailUV = TransformDetailUV(tri.uv0_2);
        #endif
    }

    o.lightArray0 = tri.lightArray0;
    o.lightArray1 = tri.lightArray1;

    return o;
}

//整个流程是模拟Lit的，毕竟没什么好改的
float4 ClustBRDFFragment (Varyings input) : SV_TARGET {
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

    GI gi = GetDiffuseGI(surface);
	float3 color = GetLighting(surface, brdf, gi, input.lightArray0, input.lightArray1);
	color += GetEmission(config);
	return float4(color, GetFinalAlpha(surface.alpha));
}
#endif