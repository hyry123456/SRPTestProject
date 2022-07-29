#ifndef CUSTOM_POST_FX_EFFECT_INCLUDE
#define CUSTOM_POST_FX_EFFECT_INCLUDE

struct VertexInput{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
};

TEXTURE2D(_PostFxEffectSource);
TEXTURE2D(_PostFxEffectSource2);
TEXTURE2D(_ParticleWaterDepthTex);
TEXTURE2D(_ParticleWaterWidthTex);
TEXTURE2D(_ParticleWaterNormalTex);
TEXTURECUBE(_WaterReflectCube);
SAMPLER(sampler_WaterReflectCube);

float _BilaterFilterFactor;
float4 _PostFxEffectSource_TexelSize;
float4 _BlurRadius;
float4x4 _InverseVPMatrix;
float4 _WaterColor;
float4 _WaterData;

struct Varyings {
	float4 positionCS_SS : SV_POSITION;
	float2 screenUV : VAR_SCREEN_UV;
	float4 interpolatedRay : VAR_INTERPOLATE;
};

Varyings DefaultPassVertex (uint vertexID : SV_VertexID) {
	Varyings output = (Varyings)0;
	output.positionCS_SS = float4(
		vertexID <= 1 ? -1.0 : 3.0,
		vertexID == 1 ? 3.0 : -1.0,
		0.0, 1.0
	);
	output.screenUV = float2(
		vertexID <= 1 ? 0.0 : 2.0,
		vertexID == 1 ? 2.0 : 0.0
	);
	if (_ProjectionParams.x < 0.0) {
		output.screenUV.y = 1.0 - output.screenUV.y;
	}
	return output;
}


float4 CopyPassFragment (Varyings input) : SV_TARGET {
	return SAMPLE_TEXTURE2D_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV, 0);
}

float3 GetWorldPos(float depth, float2 uv){
    #if defined(UNITY_REVERSED_Z)
        depth = 1 - depth;
    #endif
	float4 ndc = float4(uv.x * 2 - 1, uv.y * 2 - 1, depth * 2 - 1, 1);

	float4 worldPos = mul(_InverseVPMatrix, ndc);
	worldPos /= worldPos.w;
	return worldPos.xyz;
}

float3 GetWaterColor(Varyings input, float waterDepth, float3 waterNormal, float waterWidth, float transLight){
	float3 worldPos = GetWorldPos(waterDepth, input.screenUV);

	float3 viewDirection = normalize( _WorldSpaceCameraPos - worldPos );
	float3 reflectDir = normalize(-viewDirection + 2 * waterNormal);	//反射方向
	//反射颜色
	float3 specular = SAMPLE_TEXTURECUBE_LOD( _WaterReflectCube, sampler_WaterReflectCube, reflectDir, 0 ).rgb;
	float2 ofssetUV = (-viewDirection - 0.2 * waterNormal).xy * waterWidth * 0.05 + input.screenUV;
	//折射颜色
	float3 refrColor = SAMPLE_TEXTURE2D_LOD(_PostFxEffectSource, sampler_linear_clamp, ofssetUV, 0).rgb;
	refrColor = lerp(refrColor * _WaterColor.rgb, refrColor, transLight);
	
	float n_0 = pow( (_WaterData.y - _WaterData.x) / (_WaterData.y + _WaterData.x), 2 );

	float fresnel = ( n_0 + (1 - n_0) * pow( 1 - dot(viewDirection, waterNormal), 5 ) ) * waterWidth;

	return (refrColor * (1 - fresnel) + fresnel * specular);
}

float4 WaterFragment (Varyings input) : SV_TARGET{
	float3 currentColor = SAMPLE_TEXTURE2D_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV, 0).rgb;
	float currentDepth = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource2, sampler_point_clamp, input.screenUV, 0);

	float waterWidth =  SAMPLE_TEXTURE2D_LOD(_ParticleWaterWidthTex, sampler_linear_clamp, input.screenUV, 0).r;
	float3 waterViewNormal = normalize( (SAMPLE_TEXTURE2D_LOD(_ParticleWaterNormalTex, sampler_point_clamp, input.screenUV, 0).rgb - 0.5) *2 );
	float3 waterNormal = waterViewNormal;
	float waterDepth = SAMPLE_DEPTH_TEXTURE_LOD(_ParticleWaterDepthTex, sampler_point_clamp, input.screenUV, 0);

	//检查是否需要进行液体模拟
	if(currentDepth >= waterDepth)       //深度检测
		return float4(currentColor, 1);

	float a = pow(2.71, -0.5 * waterWidth);

	float3 color = GetWaterColor(input, waterDepth, waterNormal, waterWidth, a);

	return float4(color, 1);
}

half LinearRgbToLuminance(half3 linearRgb)
{
    return dot(linearRgb, half3(0.2126729f,  0.7151522f, 0.0721750f));
}

float CompareColor(float4 col1, float4 col2)
{
	float l1 = LinearRgbToLuminance(col1.rgb);
	float l2 = LinearRgbToLuminance(col2.rgb);
	return smoothstep(_BilaterFilterFactor, 1.0, 1.0 - abs(l1 - l2));
}

float4 BilateralFilterFragment (Varyings input) : SV_TARGET{
	float2 delta = _PostFxEffectSource_TexelSize.xy * _BlurRadius.xy;
	//采集Normal的颜色值
	float4 col =   SAMPLE_TEXTURE2D(_PostFxEffectSource, sampler_linear_clamp, input.screenUV);
	float4 col0a = SAMPLE_TEXTURE2D(_PostFxEffectSource, sampler_linear_clamp, input.screenUV - delta);
	float4 col0b = SAMPLE_TEXTURE2D(_PostFxEffectSource, sampler_linear_clamp, input.screenUV + delta);
	float4 col1a = SAMPLE_TEXTURE2D(_PostFxEffectSource, sampler_linear_clamp, input.screenUV - 2.0 * delta);
	float4 col1b = SAMPLE_TEXTURE2D(_PostFxEffectSource, sampler_linear_clamp, input.screenUV + 2.0 * delta);
	float4 col2a = SAMPLE_TEXTURE2D(_PostFxEffectSource, sampler_linear_clamp, input.screenUV - 3.0 * delta);
	float4 col2b = SAMPLE_TEXTURE2D(_PostFxEffectSource, sampler_linear_clamp, input.screenUV + 3.0 * delta);
	
	float w = 0.37004405286;
	float w0a = CompareColor(col, col0a) * 0.31718061674;
	float w0b = CompareColor(col, col0b) * 0.31718061674;
	float w1a = CompareColor(col, col1a) * 0.19823788546;
	float w1b = CompareColor(col, col1b) * 0.19823788546;
	float w2a = CompareColor(col, col2a) * 0.11453744493;
	float w2b = CompareColor(col, col2b) * 0.11453744493;
	
	float3 result;
	result = w * col.rgb;
	result += w0a * col0a.rgb;
	result += w0b * col0b.rgb;
	result += w1a * col1a.rgb;
	result += w1b * col1b.rgb;
	result += w2a * col2a.rgb;
	result += w2b * col2b.rgb;
	
	result /= w + w0a + w0b + w1a + w1b + w2a + w2b;

	return float4(result, 1);
}

float CompareDepth(float depth1, float depth2)
{
	return smoothstep(_BilaterFilterFactor, 1.0, 1.0 - abs(depth1 - depth2));
}

//深度图的处理不太一样，因此需要用一个单独的Pass来处理
float BilateralFilterDepthFragment(Varyings input) : SV_DEPTH{
	float2 delta = _PostFxEffectSource_TexelSize.xy * _BlurRadius.xy;

	float depth0 = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV, 0);

	float depth1 = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV - delta, 0);
	float depth2 = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV + delta, 0);
	float depth3 = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV - 2.0 * delta, 0);
	float depth4 = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV + 2.0 * delta, 0);
	float depth5 = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV - 3.0 * delta, 0);
	float depth6 = SAMPLE_DEPTH_TEXTURE_LOD(_PostFxEffectSource, sampler_linear_clamp, input.screenUV + 3.0 * delta, 0);
	
	float w = 0.37004405286;
	float w0a = CompareDepth(depth0, depth1) * 0.31718061674;
	float w0b = CompareDepth(depth0, depth2) * 0.31718061674;
	float w1a = CompareDepth(depth0, depth3) * 0.19823788546;
	float w1b = CompareDepth(depth0, depth4) * 0.19823788546;
	float w2a = CompareDepth(depth0, depth5) * 0.11453744493;
	float w2b = CompareDepth(depth0, depth6) * 0.11453744493;
	
	float result;
	result = w * depth0;
	result += w0a * depth1;
	result += w0b * depth2;
	result += w1a * depth3;
	result += w1b * depth4;
	result += w2a * depth5;
	result += w2b * depth6;
	
	result /= w + w0a + w0b + w1a + w1b + w2a + w2b;

	return result;
}

#endif