#ifndef CUSTOM_POST_FX_INPUT_INCLUDED
#define CUSTOM_POST_FX_INPUT_INCLUDED

#include "../../ShaderLibrary/Surface.hlsl"
#include "../../ShaderLibrary/Shadows.hlsl"
#include "../../ShaderLibrary/Light.hlsl"
#include "../../ShaderLibrary/BRDF.hlsl"
#include "../../ShaderLibrary/GI.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"

float4x4 _InverseVPMatrix;


//获得主纹理的采用模式，因为需要采集雾效
SAMPLER(sampler_PostFXSource);
TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);

float4 _PostFXSource_TexelSize;

bool _BloomBicubicUpsampling;
float _BloomIntensity;

float4 _BloomThreshold;

float3 GetWorldPos(float depth, float2 uv){
    #if defined(UNITY_REVERSED_Z)
        depth = 1 - depth;
    #endif
	float4 ndc = float4(uv.x * 2 - 1, uv.y * 2 - 1, depth * 2 - 1, 1);

	float4 worldPos = mul(_InverseVPMatrix, ndc);
	worldPos /= worldPos.w;
	return worldPos.xyz;
}


float _BulkLightShrinkRadio;
int _BulkSampleCount;
float _BulkLightScatterRadio;
float _BulkLightCheckMaxDistance;

float _FogMaxHight;
float _FogMinHight;
float _FogMaxDepth;
float _FogMinDepth;

float _FogDepthFallOff;
float _FogPosYFallOff;

float3 GetFog(float depth, float3 worldPos){
    float lineardDepth = Linear01Depth(depth, _ZBufferParams);

    float depthX = 1 - saturate( (_FogMaxDepth - lineardDepth) / ( _FogMaxDepth - _FogMinDepth ) );

    float depthRadio = pow(depthX, _FogDepthFallOff);

    float posX = saturate( (_FogMaxHight - worldPos.y) / ( _FogMaxHight - _FogMinHight ) );

    float posYRadio = pow(posX, _FogPosYFallOff);

    return posYRadio * depthRadio ;
}

float3 GetBulkLight(float depth, float2 uv){

    float3 worldPos = GetWorldPos(saturate( depth ), uv);
    float3 startPos = 0;
    #if defined(UNITY_REVERSED_Z)
        startPos = GetWorldPos(1, uv);
    #else
        startPos = GetWorldPos(0, uv);
    #endif

    float3 direction = normalize(worldPos - startPos);

    float dis = length(worldPos - startPos);

    float m_length = min(_BulkLightCheckMaxDistance, dis);
    float perNodeLength = m_length / _BulkSampleCount;
    float3 currentPoint = startPos;
    float3 viewDirection = normalize(_WorldSpaceCameraPos - worldPos);
    
    float3 color = 0;
    
    for(int i=0; i<_BulkSampleCount; i++){
        currentPoint += direction * perNodeLength;
        float3 tempPosition = currentPoint + direction * (InterleavedGradientNoise(currentPoint.xy, 0) - 0.5) * perNodeLength;
        color += GetBulkLighting(tempPosition, viewDirection, _BulkLightScatterRadio);
    }

    color *= m_length * _BulkLightShrinkRadio ;

    float3 fog = GetFog(depth, worldPos);

    color = lerp(fog.r, color, Luminance(color));

    return color ;
}


#endif