#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

float3 IncomingLight (Surface surface, Light light) {
	#ifdef _USE_BSDF
		float dotV = dot(surface.normal, light.direction);
		float y = (dotV + surface.wrap) / (1 + surface.wrap);
		return saturate( (y + y * (1 - y) * surface.shiftColor) * light.attenuation ) * light.color;
	#else
		return
			saturate(dot(surface.normal, light.direction) * light.attenuation) * 
			light.color;
	#endif
}

#ifdef _USE_BSDF

	float3 TransferLight(Surface surface, Light light){
		float3 backLightDir = surface.normal * surface.distorion + light.direction;


		float fLTDot = pow(saturate( dot(surface.viewDirection, -backLightDir)), surface.power ) 
			* surface.scale * light.attenuation;

		return fLTDot * surface.transferColor * light.color ;
	}
#endif

float3 GetLighting (Surface surface, BRDF brdf, Light light) {
	#ifdef _USE_BSDF
		return ( IncomingLight(surface, light) + TransferLight(surface, light) ) * DirectBRDF(surface, brdf, light);
	#else
		return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
	#endif
}

bool RenderingLayersOverlap (Surface surface, Light light) {
	return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

float3 GetLighting (Surface surfaceWS, BRDF brdf, GI gi) {
	ShadowData shadowData = GetShadowData(surfaceWS);
	shadowData.shadowMask = gi.shadowMask;
	
	float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		Light light = GetDirectionalLight(i, surfaceWS, shadowData);
		if (RenderingLayersOverlap(surfaceWS, light)) {
			color += GetLighting(surfaceWS, brdf, light);
		}
	}
	
	#if defined(_LIGHTS_PER_OBJECT)
		for (int j = 0; j < min(unity_LightData.y, 8); j++) {
			int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
			Light light = GetOtherLight(lightIndex, surfaceWS, shadowData);
			if (RenderingLayersOverlap(surfaceWS, light)) {
				color += GetLighting(surfaceWS, brdf, light);
			}
		}
	#else
		for (int j = 0; j < GetOtherLightCount(); j++) {
			Light light = GetOtherLight(j, surfaceWS, shadowData);
			if (RenderingLayersOverlap(surfaceWS, light)) {
				color += GetLighting(surfaceWS, brdf, light);
			}
		}
	#endif

	return color;
}

//是由兰伯特光进行默认采样，只进行光照计算的采样
float3 GetLightingDiffuse(Surface surface, GI gi){
	ShadowData shadowData = GetShadowData(surface);
	shadowData.shadowMask = gi.shadowMask;
	float3 color = 0;

	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		Light light = GetDirectionalLight(i, surface, shadowData);
		color += IncomingLight(surface, light);
	}

	for (int j = 0; j < GetOtherLightCount(); j++) {
		Light light = GetOtherLight(j, surface, shadowData);
		color += IncomingLight(surface, light);
	}

	return color;
}

//因为是视线方向作为法线的，因此要注意相反的情况，但是相反应该要也可以看见
float3 BulkIncomingLight(Light light, float3 viewDirection, float g){
	float cosTheta = saturate( dot(-viewDirection, light.direction) );
	return 1 / (4 * 3.14) * (1 - g * g) / pow(abs( 1 + g * g - 2 * g * cosTheta ), 1.5) * light.attenuation * light.color;
}

//获得体积光的光照计算方式
float3 GetBulkLighting(float3 worldPos, float3 viewDirection, float g = 0){
	ShadowData shadowData = GetShadowDataByPosition(worldPos);
	shadowData.shadowMask = GetDiffuseShadowMask();

	float3 color = 0;

	//for (int i = 0; i < GetDirectionalLightCount(); i++) {
	//	Light light = GetDirectionalLightByPosition(i, worldPos, shadowData);
	//	color += BulkIncomingLight(light, viewDirection, g);
	//}

	for (int i = 0; i < GetOtherLightCount(); i++) {
		Light light = GetOtherLightByPosition(i, worldPos, shadowData);
		color += BulkIncomingLight(light, viewDirection, g);
	}

	return color;
}

#endif