#ifndef CLUST_BRDF_INPUT_INCLUDED
#define CLUST_BRDF_INPUT_INCLUDED

#include "ClustInput.hlsl"

// TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_EmissionMap);
// SAMPLER(sampler_BaseMap);

TEXTURE2D(_DetailMap);
TEXTURE2D(_DetailNormalMap);
SAMPLER(sampler_DetailMap);

//因为实际上这些材质都只会渲染一次，就不进行实例化设置了
CBUFFER_START(ClustBRDF)
    float4 _DetailMap_ST;
    float4 _EmissionColor;
    float _ZWrite;
    float _Metallic;
    float _Occlusion;
    float _Smoothness;
    float _Fresnel;
    float _DetailAlbedo;
    float _DetailSmoothness;
    float _DetailNormalScale;
    float _NormalScale;

CBUFFER_END

struct InputConfig {
	Fragment fragment;
	float2 baseUV;
	float2 detailUV;
	bool useMask;
	bool useDetail;
};

InputConfig GetInputConfig (float4 positionSS, float2 baseUV, float2 detailUV = 0.0) {
	InputConfig c;
	c.fragment = GetFragment(positionSS);
	c.baseUV = baseUV;
	c.detailUV = detailUV;
	c.useMask = false;
	c.useDetail = false;
	return c;
}

float2 TransformBaseUV (float2 baseUV) {
	return baseUV * _BaseMap_ST.xy + _BaseMap_ST.zw;
}

float2 TransformDetailUV (float2 detailUV) {
	float4 detailST = _DetailMap_ST;
	return detailUV * _DetailMap_ST.xy + _DetailMap_ST.zw;
}

float4 GetMask (InputConfig c) {
	if (c.useMask) {
		return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, c.baseUV);
	}
	return 1.0;
}

float4 GetDetail (InputConfig c) {
	if (c.useDetail) {
		float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, c.detailUV);
		return map * 2.0 - 1.0;
	}
	return 0.0;
}

float4 GetBase (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
	float4 color = _BaseColor;

	if (c.useDetail) {
		float detail = GetDetail(c).r * _DetailAlbedo;
		float mask = GetMask(c).b;
		map.rgb =
			lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
		map.rgb *= map.rgb;
	}
	
	return map * color;
}

float GetCutoff (InputConfig c) {
	return _Cutoff;
}

float3 GetNormalTS (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, c.baseUV);
	float scale = _NormalScale;
	float3 normal = DecodeNormal(map, scale);

	if (c.useDetail) {
		map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, c.detailUV);
		scale = _DetailNormalScale * GetMask(c).b;
		float3 detail = DecodeNormal(map, scale);
		normal = BlendNormalRNM(normal, detail);
	}
	
	return normal;
}

float GetMetallic (InputConfig c) {
	float metallic = _Metallic;
	metallic *= GetMask(c).r;
	return metallic;
}

float GetOcclusion (InputConfig c) {
	float strength = _Occlusion;
	float occlusion = GetMask(c).g;
	occlusion = lerp(occlusion, 1.0, strength);
	return occlusion;
}

float GetSmoothness (InputConfig c) {
	float smoothness = _Smoothness;
	smoothness *= GetMask(c).a;

	if (c.useDetail) {
		float detail = GetDetail(c).b * _DetailSmoothness;
		float mask = GetMask(c).b;
		smoothness =
			lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
	}
	
	return smoothness;
}

float GetFresnel (InputConfig c) {
	return _Fresnel;
}

float3 GetEmission (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, c.baseUV);
	float4 color = _EmissionColor;
	return map.rgb * color.rgb;
}

float GetFinalAlpha (float alpha) {
	return _ZWrite ? 1.0 : alpha;
}

#endif