Shader "Custom RP/Tree" {
	
	Properties {
		[Header(Base Setting)]
		[Space(10)]
		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)

		[Header(Clip Setting)]
		[Space(5)]
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0

		[Header(Shadow Setting)]
		[Space(5)]
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0

		[Toggle(_USE_BSDF)] _UseBSDF("Use BSDF", Float) = 1

		[Header(BSSDF Setting)]
		[Space(5)]
		[Toggle(_MASK_MAP)] _MaskMapToggle ("Mask Map", Float) = 0
		[NoScaleOffset] _MaskMap("Mask (MODS)", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Occlusion ("Occlusion", Range(0, 1)) = 1
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
		_Fresnel ("Fresnel", Range(0, 1)) = 1
		_Wrap("Wrap", Range(0,1)) = 0
		_ShiftColor ("Shift Color", Color) = (1,1,1,1)

		[Header(BTDF Setting)]
		[Space(5)]
		_FLTDistorion("FLTP Distortion", Range(0, 2)) = 1
		_ILTPower("LTP Power", Range(1, 10)) = 1
		_FLTScale ("FLT Scale", Range(0.01, 5)) = 1
		_WidthControl ("Width Control", Range(0, 1)) = 1
		_TransferColor ("Transfer Color", Color) = (1,1,1,1)

		[Header(Normal Setting)]
		[Space(5)]
		[Toggle(_NORMAL_MAP)] _NormalMapToggle ("Normal Map", Float) = 0
		[NoScaleOffset] _NormalMap("Normals", 2D) = "bump" {}
		_NormalScale("Normal Scale", Range(0, 1)) = 1

		[Header(Emission Setting)]
		[Space(5)]
		[NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
		[HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)

		[Header(Detail Setting)]
		[Space(5)]
		[Toggle(_DETAIL_MAP)] _DetailMapToggle ("Detail Maps", Float) = 0
		_DetailMap("Details", 2D) = "linearGrey" {}
		[NoScaleOffset] _DetailNormalMap("Detail Normals", 2D) = "bump" {}
		_DetailAlbedo("Detail Albedo", Range(0, 1)) = 1
		_DetailSmoothness("Detail Smoothness", Range(0, 1)) = 1
		_DetailNormalScale("Detail Normal Scale", Range(0, 1)) = 1
		
		[Header(Pass Setting)]
		[Space(5)]
		[Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0         //透明度影响diffuse值，放一下怕忘了

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1

		[HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
		[HideInInspector] _TranslucencyColor("TranslucenctyColor", Color) = (0.5, 0.5, 0.5, 1.0)
		[HideInInspector] _TranslucencyViewDependency("TranslucenctyColor", Float) = 1
		[HideInInspector] _ShadowStrength("_ShadowStrength", Float) = 1
	}
	
	SubShader {
		HLSLINCLUDE
		#include "../../ShaderLibrary/Common.hlsl"
		#include "../BaseHLSL/LitInput.hlsl"
		ENDHLSL

		Pass {
			Tags {
				"LightMode" = "CustomLit"
			}

			Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
			ZWrite [_ZWrite]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma shader_feature _PREMULTIPLY_ALPHA
			#pragma shader_feature _MASK_MAP
			#pragma shader_feature _NORMAL_MAP
			#pragma shader_feature _DETAIL_MAP
			#pragma shader_feature _USE_BSDF

			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
			#pragma multi_compile _ _LIGHTS_PER_OBJECT
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_instancing


			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "HLSL/TreeLeafPass.hlsl"
			// #include "../HLSL/LitPass.hlsl"
			ENDHLSL
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "HLSL/TreeShadowCasterPass.hlsl"
			ENDHLSL
		}

		// Pass {
		// 	Tags {
		// 		"LightMode" = "Meta"
		// 	}

		// 	Cull Off

		// 	HLSLPROGRAM
		// 	#pragma target 3.5
		// 	#pragma vertex MetaPassVertex
		// 	#pragma fragment MetaPassFragment
		// 	#include "../HLSL/MetaPass.hlsl"
		// 	ENDHLSL
		// }
	}

	CustomEditor "CustomShaderGUI"
}