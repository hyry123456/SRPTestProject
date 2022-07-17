#ifndef CUSTOM_PARTICLE_INPUT
#define CUSTOM_PARTICLE_INPUT

#include "../../../ShaderLibrary/Common.hlsl"


TEXTURE2D(_MainTex);
TEXTURE2D(_DistortionTex);
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
	float4 _MainTex_ST;
    float4 _ParticleColor;

    #ifdef _DISTORTION
        float _DistortionSize;
        float _DistortionBlend;
    #endif

    float _NearFadeDistance;
    float _NearFadeRange;
    float _SoftParticlesDistance;
    float _SoftParticlesRange;
    
    float4 _MovePointArray[10];
    int _MovePointCount;
    
    float4 _SizePointArray[10];
    int _SizePointCount;
    
    float4 _AlphaPointArray[10];
    int _AlphaPointCount;

    float4 _BeginPos;

	float4 _MaxDistance;
	float4 _MoveSpeed;
	float _ParticleLength;
	float _RowCount;
	float _ColumnCount;

CBUFFER_END


struct FragInput{
    float4 uv0 : TEXCOORD0;
    #ifdef _ANIMATE_UV
        float4 uv1 : TEXCOORD1;
    #endif
    float4 pos : SV_POSITION;
    float time : TEXCOORD2;
};

float4 GetBaseColor(FragInput i, Fragment fragment){
    float4 col0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv0.xy) * _ParticleColor;
    #ifdef _ANIMATE_UV
        float4 col1= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv1.xy) * _ParticleColor;
        float4 col = lerp(col0, col1, i.uv0.z);
    #else
        float4 col = col0;
    #endif

    float nearAttenuation = 1;

    nearAttenuation = (fragment.depth - _NearFadeDistance) / _NearFadeRange;
    col0.a *= saturate(nearAttenuation);

    float depthDelta = fragment.bufferDepth - fragment.depth;	//获得深度差
    nearAttenuation = (depthDelta - _SoftParticlesDistance) / _SoftParticlesRange;	//进行透明控制
    col0.a *= saturate(nearAttenuation);	//透明度赋值
    return col0;
}


void SetUV(inout FragInput i, float2 uv, float4 topAndButtom, float interpolation, float time){

    i.uv0.xy = uv + topAndButtom.zw;
    i.uv0.x /= _RowCount;
    i.uv0.y /= _ColumnCount;
    i.uv0.z = interpolation;

    #ifdef _ANIMATE_UV
        i.uv1.xy = uv + topAndButtom.xy;
        i.uv1.x /= _RowCount;
        i.uv1.y /= _ColumnCount;
        i.uv1.zw = uv;
    #endif
}

#ifdef _DISTORTION
    float2 GetDistortion(FragInput i, float baseAlpha){
        float4 col0 = SAMPLE_TEXTURE2D(_DistortionTex, sampler_MainTex, i.uv0.xy) * _ParticleColor;
        #ifdef _ANIMATE_UV
            float4 col1= SAMPLE_TEXTURE2D(_DistortionTex, sampler_MainTex, i.uv1.xy) * _ParticleColor;
            float4 col = lerp(col0, col1, i.uv0.z);
        #else
            float4 col = col0;
        #endif

        return col.xy * baseAlpha * _DistortionSize;
    }

    float GetDistortionBlend(){
        return _DistortionBlend;
    }
#endif


#endif