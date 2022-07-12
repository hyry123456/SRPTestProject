#ifndef CLUST_LIGHTING_INCLUDE
#define CLUST_LIGHTING_INCLUDE


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

float3 GetLighting (Surface surfaceWS, BRDF brdf, GI gi, int lightArray0, int lightArray1) {
	// return lightArray0;
	ShadowData shadowData = GetShadowData(surfaceWS);
	shadowData.shadowMask = gi.shadowMask;
	
	float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
	}

	int j;
    for ( j = 0; j < 32 && j < GetOtherLightCount(); j++) {
        //按位与为0，也就是不使用该灯光
        if( ( !(lightArray0 >> j) & 1) ) continue;
        Light light = GetOtherLight(j, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }

    for ( j = 32; j < 64  && j < GetOtherLightCount(); j++) {
        if( ( !(lightArray0 >> (j - 32)) & 1) ) continue;
        Light light = GetOtherLight(j, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }

	return color;
}

#endif