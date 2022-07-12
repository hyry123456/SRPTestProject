﻿using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject {

	[SerializeField]
	Shader shader = default;

	/// <summary>	/// Bloom参数设置	/// </summary>
	[System.Serializable]
	public struct BloomSettings {

		/// <summary>		/// 渐变等级		/// </summary>
		[Range(0f, 16f)]
		public int maxIterations;

		/// <summary>		/// Bloom进行到最小的像素，像素量小于该值就不进行下一步		/// </summary>
		[Min(1f)]
		public int downscaleLimit;

		/// <summary>		/// 是否使用三线性插值		/// </summary>
		public bool bicubicUpsampling;

		/// <summary>		/// Bloom的分割线		/// </summary>
		[Min(0f)]
		public float threshold;

		/// <summary>		/// 分割线下降的剧烈程度，不是直接截断		/// </summary>
		[Range(0f, 1f)]
		public float thresholdKnee;

		/// <summary>		/// Bloom叠加在主纹理的强度		/// </summary>
		[Min(0f)]
		public float intensity;

		/// <summary>		/// 是否使用范围颜色限制，即进行颜色限制时采样了周围的颜色		/// </summary>
		public bool fadeFireflies;

		/// <summary>		/// Bloom的混合模式，是添加还是lerp混合		/// </summary>
		public enum Mode { Additive, Scattering }

		public Mode mode;

		/// <summary>		/// lerp混合的比例		/// </summary>
		[Range(0.05f, 0.95f)]
		public float scatter;
	}

	[SerializeField]
	BloomSettings bloom = new BloomSettings {
		scatter = 0.7f,
		maxIterations = 1,
		downscaleLimit = 30,
		threshold = 1,
		thresholdKnee = 0.2f,
		intensity = 0.5f,
	};

	public BloomSettings Bloom => bloom;


	/// <summary>	/// 颜色值调整	/// </summary>
	[Serializable]
	public struct ColorAdjustmentsSettings {

		/// <summary>		/// 曝光度		/// </summary>
		public float postExposure;

		/// <summary>		/// 对比度		/// </summary>
		[Range(-100f, 100f)]
		public float contrast;

		/// <summary>		/// 颜色过滤		/// </summary>
		[ColorUsage(false, true)]
		public Color colorFilter;

		/// <summary>		/// 色相转移		/// </summary>
		[Range(-180f, 180f)]
		public float hueShift;

		/// <summary>		/// 饱和度		/// </summary>
		[Range(-100f, 100f)]
		public float saturation;
	}

	[SerializeField]
	ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings {
		colorFilter = Color.white
	};

	public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;

	/// <summary>	/// 白平衡阐述控制	/// </summary>
	[Serializable]
	public struct WhiteBalanceSettings {

		/// <summary>		/// 色温以及色温强度		/// </summary>
		[Range(-100f, 100f)]
		public float temperature, tint;
	}

	[SerializeField]
	WhiteBalanceSettings whiteBalance = default;

	public WhiteBalanceSettings WhiteBalance => whiteBalance;

	/// <summary>	/// 色调分离	/// </summary>
	[Serializable]
	public struct SplitToningSettings {

		/// <summary>		/// 阴影颜色和高亮颜色		/// </summary>
		[ColorUsage(false)]
		public Color shadows, highlights;

		/// <summary>		/// 平衡度		/// </summary>
		[Range(-100f, 100f)]
		public float balance;
	}

	[SerializeField]
	SplitToningSettings splitToning = new SplitToningSettings {
		shadows = Color.gray,
		highlights = Color.gray
	};

	public SplitToningSettings SplitToning => splitToning;

	/// <summary>	/// 通道重映射	/// </summary>
	[Serializable]
	public struct ChannelMixerSettings {
		/// <summary>		/// 三个通道要映射到的数值		/// </summary>
		public Vector3 red, green, blue;
	}

	[SerializeField]
	ChannelMixerSettings channelMixer = new ChannelMixerSettings {
		red = Vector3.right,
		green = Vector3.up,
		blue = Vector3.forward
	};

	public ChannelMixerSettings ChannelMixer => channelMixer;


	/// <summary>	/// 三重色调分离	/// </summary>
	[Serializable]
	public struct ShadowsMidtonesHighlightsSettings {

		[ColorUsage(false, true)]
		public Color shadows, midtones, highlights;

		[Range(0f, 2f)]
		public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
	}

	[SerializeField]
	ShadowsMidtonesHighlightsSettings
		shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings {
			shadows = Color.white,
			midtones = Color.white,
			highlights = Color.white,
			shadowsEnd = 0.3f,
			highlightsStart = 0.55f,
			highLightsEnd = 1f
		};

	public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights =>
		shadowsMidtonesHighlights;
	
	/// <summary>	/// HDR映射方式	/// </summary>
	[Serializable]
	public struct ToneMappingSettings {

		public enum Mode { None, ACES, Neutral, Reinhard }

		public Mode mode;
	}

	[SerializeField]
	ToneMappingSettings toneMapping = default;

	public ToneMappingSettings ToneMapping => toneMapping;

	[NonSerialized]
	Material material;

	/// <summary>	/// 体积光计算，这个是真的奢侈	/// </summary>
	[Serializable]
	public struct BulkLight
	{
		public bool useBulkLight;
		[Range(0, 100f)]
		public float shrinkRadio;
		[Range(10, 100)]
		public int circleCount;
		[Range(0, 1)]
		public float scatterRadio;
		public float checkDistance;
		public Texture fogTex;

		public float fogMaxHeight;
		public float fogMinHeight;

		[Range(0, 1)]
		public float fogMaxDepth;
		[Range(0, 1)]
		public float fogMinDepth;

		[Range(0.001f, 3)]
		public float fogDepthFallOff;
		[Range(0.001f, 3)]
		public float fogPosYFallOff;

    }

	public BulkLight bulkLight = new BulkLight
	{
		useBulkLight = false,
		shrinkRadio = 0.00005f,
		checkDistance = 100,
		circleCount = 64,
		fogMaxHeight = 100,
		fogMinHeight = 0,
		fogMaxDepth = 1,
		fogMinDepth = 0.2f,
	};


	public Material Material {
		get {
			if (material == null && shader != null) {
				material = new Material(shader);
				material.hideFlags = HideFlags.HideAndDontSave;
			}
			return material;
		}
	}
}