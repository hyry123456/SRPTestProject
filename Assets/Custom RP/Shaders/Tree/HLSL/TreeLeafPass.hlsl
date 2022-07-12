#ifndef TREE_LEAF_PASS_INCLUDED
#define TREE_LEAF_PASS_INCLUDED

//树渲染不需要GI，因为没有环境贴图

#include "../../ShaderLibrary/Surface.hlsl"
#include "../../ShaderLibrary/Shadows.hlsl"
#include "../../ShaderLibrary/Light.hlsl"
#include "../../ShaderLibrary/BRDF.hlsl"
#include "../../ShaderLibrary/GI.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"
#include "../../ShaderLibrary/MyTerrain.hlsl"


// struct Attributes {
// 	float3 positionOS : POSITION;
// 	float3 normalOS : NORMAL;
// 	float4 tangentOS : TANGENT;
// 	float2 baseUV : TEXCOORD0;
//     float2 texcoord1 : TEXCOORD1;
//     float4 color : COLOR;
// 	// GI_ATTRIBUTE_DATA
// 	UNITY_VERTEX_INPUT_INSTANCE_ID
// };

struct Varyings_TREE {
	float4 positionCS_SS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float3 normalWS : VAR_NORMAL;
	#if defined(_NORMAL_MAP)
		float4 tangentWS : VAR_TANGENT;
	#endif
	float2 baseUV : VAR_BASE_UV;
	#if defined(_DETAIL_MAP)
		float2 detailUV : VAR_DETAIL_UV;
	#endif
	// GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings_TREE LitPassVertex (Attributes_full input) {
	Varyings_TREE output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	// TRANSFER_GI_DATA(input, output);
    // input.positionOS = AnimateVertex (input.positionOS, input.normalOS, float4(input.color.xy, input.texcoord1.xy)).xyz;

    TreeVertLeaf(input);

	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.positionCS_SS = TransformWorldToHClip(output.positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS.xyz);
	#if defined(_NORMAL_MAP)
		output.tangentWS = float4(
			TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w
		);
	#endif
	output.baseUV = TransformBaseUV(input.texcoord0.xy);
	#if defined(_DETAIL_MAP)
		output.detailUV = TransformDetailUV(input.texcoord0.xy);
	#endif
	return output;
}

float4 LitPassFragment (Varyings_TREE input) : SV_TARGET {
	UNITY_SETUP_INSTANCE_ID(input);
	InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
	ClipLOD(config.fragment, unity_LODFade.x);
	
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
		surface.interpolatedNormal = -input.normalWS;
	#else
		surface.normal = normalize(input.normalWS);
		surface.interpolatedNormal = -surface.normal;
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

	#ifdef _USE_BSDF
        surface.wrap = GetWrap();
        surface.shiftColor = GetShifColor();
        surface.distorion = GetDistorion();
        surface.power = GetPower();
        surface.scale = GetScale();
        surface.width = GetWidth();
        surface.transferColor = GetTransferColor();
	#endif

	#if defined(_PREMULTIPLY_ALPHA)
        BRDF brdf = GetBRDF(surface, true);
	#else
        BRDF brdf = GetBRDF(surface, true);
	#endif

    // GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    GI gi = GetDiffuseGI(surface);
    float3 color = GetLighting(surface, brdf, gi);

	color += GetEmission(config);
	return float4(color, GetFinalAlpha(surface.alpha));
}


#endif