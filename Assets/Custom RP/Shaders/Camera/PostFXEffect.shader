Shader "Unlit/PostFXEffect"
{
    Properties
    {
		_WaterReflectCube ("Reflect Cube", Cube) ="white" {} 
    }

	SubShader {
		Cull Off
		ZTest Always
		ZWrite Off

		Tags{
			"Mode" = "CustomLit"
		}
		
		HLSLINCLUDE
		#include "../../ShaderLibrary/Common.hlsl"

		#include "HLSL/PostFXEffectPass.hlsl"
		ENDHLSL

		Pass {
			Name "Copy"

			Blend [_CameraSrcBlend] [_CameraDstBlend]

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassFragment
			ENDHLSL
		}

        Pass {
			Name "WaterBlend"

			Blend One Zero

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment WaterFragment
			ENDHLSL
		}

		Pass{
			Name "BilateralFilter"
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BilateralFilterFragment
			ENDHLSL
		}

		Pass{
			Name "BilateralFilterDepth"
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BilateralFilterDepthFragment
			ENDHLSL
		}
	}
}
