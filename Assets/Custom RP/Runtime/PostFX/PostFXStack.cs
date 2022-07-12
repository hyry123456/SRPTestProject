using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack {

	enum Pass {
		BloomAdd,
		BloomHorizontal,
		BloomPrefilter,
		BloomPrefilterFireflies,
		BloomScatter,
		BloomScatterFinal,
		BloomVertical,
		Copy,
		ColorGradingNone,
		ColorGradingACES,
		ColorGradingNeutral,
		ColorGradingReinhard,
		Final,
		BulkLight,
	}

	const string bufferName = "Post FX";

	const int maxBloomPyramidLevels = 16;

	int
		bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
		bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
		bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
		bloomResultId = Shader.PropertyToID("_BloomResult"),
		bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
		fxSourceId = Shader.PropertyToID("_PostFXSource"),
		fxSource2Id = Shader.PropertyToID("_PostFXSource2");

	int
		colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
		colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
		colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC"),
		colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
		colorFilterId = Shader.PropertyToID("_ColorFilter"),
		whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),
		splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows"),
		splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights"),
		channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed"),
		channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen"),
		channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue"),
		smhShadowsId = Shader.PropertyToID("_SMHShadows"),
		smhMidtonesId = Shader.PropertyToID("_SMHMidtones"),
		smhHighlightsId = Shader.PropertyToID("_SMHHighlights"),
		smhRangeId = Shader.PropertyToID("_SMHRange");

	int
		finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
		finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

	int bulkLightMapId = Shader.PropertyToID("_BulkLightTex"),
		bulkLightTempMapId = Shader.PropertyToID("_BulkLightTempTex"),
		bulkLightShrinkRadio = Shader.PropertyToID("_BulkLightShrinkRadio"),
		bulkLightSampleCount = Shader.PropertyToID("_BulkSampleCount"),
		bulkLightScatterRadio = Shader.PropertyToID("_BulkLightScatterRadio"),
		bulkLightCheckMaxDistance = Shader.PropertyToID("_BulkLightCheckMaxDistance"),

		bulkFogMaxHight = Shader.PropertyToID("_FogMaxHight"),
		bulkFogMinHight = Shader.PropertyToID("_FogMinHight"),
		bulkFogMaxDepth = Shader.PropertyToID("_FogMaxDepth"),
		bulkFogMinDepth = Shader.PropertyToID("_FogMinDepth"),

		bulkFogDepthFallOff = Shader.PropertyToID("_FogDepthFallOff"),
		//bulkFogHeightFallOff = Shader.PropertyToID("_FogHightFallOff"),
		bulkFogPosYFallOff = Shader.PropertyToID("_FogPosYFallOff");

		//bulkFogIntensity = Shader.PropertyToID("_FogIntensity");


	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	ScriptableRenderContext context;

	Camera camera;

	PostFXSettings settings;

	int bloomPyramidId;

	bool useHDR;

	int colorLUTResolution;

	public bool IsActive => settings != null;

	CameraSettings.FinalBlendMode finalBlendMode;

	public PostFXStack () {
		bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
		for (int i = 1; i < maxBloomPyramidLevels * 2; i++) {
			Shader.PropertyToID("_BloomPyramid" + i);
		}
	}

	/// <summary>	/// 准备后处理前的数据准备	/// </summary>
	public void Setup (
		ScriptableRenderContext context, Camera camera, PostFXSettings settings,
		bool useHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode
	) {
		this.finalBlendMode = finalBlendMode;
		this.colorLUTResolution = colorLUTResolution;
		this.useHDR = useHDR;
		this.context = context;
		this.camera = camera;
		this.settings =
			camera.cameraType <= CameraType.SceneView ? settings : null;
		ApplySceneViewState();
	}

	/// <summary>	/// 后处理的渲染方法，以传入纹理的ID，进行渲染到摄像机的操作	/// </summary>
	/// <param name="sourceId">根据的原纹理</param>
	public void Render (int sourceId) {
        if (settings.bulkLight.useBulkLight)
        {
            DoBulkLight(sourceId);
        }
        else
        {
            buffer.GetTemporaryRT(bulkLightMapId, 1, 1,
                0, FilterMode.Bilinear, RenderTextureFormat.Default);
        }

        if (DoBloom(sourceId)) {
			DoColorGradingAndToneMapping(bloomResultId);
			buffer.ReleaseTemporaryRT(bloomResultId);
		}
		else {
			DoColorGradingAndToneMapping(sourceId);
		}

        buffer.ReleaseTemporaryRT(bulkLightMapId);

        context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	/// <summary>	/// 进行Bloom操作	/// </summary>
	/// <returns>是否成功进行Bloom</returns>
	bool DoBloom (int sourceId) {
		BloomSettings bloom = settings.Bloom;
		int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
		
		if (
			bloom.maxIterations == 0 || bloom.intensity <= 0f ||
			height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2
		) {
			return false;
		}

		buffer.BeginSample("Bloom");
		Vector4 threshold;
		threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
		threshold.y = threshold.x * bloom.thresholdKnee;
		threshold.z = 2f * threshold.y;
		threshold.w = 0.25f / (threshold.y + 0.00001f);
		threshold.y -= threshold.x;
		buffer.SetGlobalVector(bloomThresholdId, threshold);		//Bloom参数准备

		RenderTextureFormat format = useHDR ?
			RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
		buffer.GetTemporaryRT(
			bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
		);
		Draw(
			sourceId, bloomPrefilterId, bloom.fadeFireflies ?
				Pass.BloomPrefilterFireflies : Pass.BloomPrefilter
		);
		width /= 2;
		height /= 2;

		int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;
		int i;
		//逐个循环，进行逐级高斯模糊
		for (i = 0; i < bloom.maxIterations; i++) {
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit) {
				break;
			}
			int midId = toId - 1;
			buffer.GetTemporaryRT(
				midId, width, height, 0, FilterMode.Bilinear, format
			);
			buffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Bilinear, format
			);
			//水平以及垂直高斯模糊
			Draw(fromId, midId, Pass.BloomHorizontal);
			Draw(midId, toId, Pass.BloomVertical);
			//模糊后减少纹理像素大小
			fromId = toId;
			toId += 2;
			width /= 2;
			height /= 2;
		}

		buffer.ReleaseTemporaryRT(bloomPrefilterId);
		buffer.SetGlobalFloat(
			bloomBicubicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f
		);

		//控制纹理的混合模式，是使用渐变混合还是直接相加
		Pass combinePass, finalPass;
		float finalIntensity;
		if (bloom.mode == BloomSettings.Mode.Additive) {
			combinePass = finalPass = Pass.BloomAdd;
			buffer.SetGlobalFloat(bloomIntensityId, 1f);
			finalIntensity = bloom.intensity;
		}
		else {
			combinePass = Pass.BloomScatter;
			finalPass = Pass.BloomScatterFinal;
			buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
			finalIntensity = Mathf.Min(bloom.intensity, 1f);
		}

		//循环混合到最上级纹理上
		if (i > 1) {
			buffer.ReleaseTemporaryRT(fromId - 1);
			toId -= 5;
			for (i -= 1; i > 0; i--) {
				buffer.SetGlobalTexture(fxSource2Id, toId + 1);
				Draw(fromId, toId, combinePass);
				buffer.ReleaseTemporaryRT(fromId);
				buffer.ReleaseTemporaryRT(toId + 1);
				fromId = toId;
				toId -= 2;
			}
		}
		else {
			buffer.ReleaseTemporaryRT(bloomPyramidId);
		}
		buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
		buffer.SetGlobalTexture(fxSource2Id, sourceId);
		buffer.GetTemporaryRT(
			bloomResultId, camera.pixelWidth, camera.pixelHeight, 0,
			FilterMode.Bilinear, format
		);

		//绘制到目标纹理上
		Draw(fromId, bloomResultId, finalPass);
		buffer.ReleaseTemporaryRT(fromId);
		buffer.EndSample("Bloom");
		return true;
	}

	/// <summary>	/// 颜色调整参数赋值	/// </summary>
	void ConfigureColorAdjustments () {
		ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
		buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
			Mathf.Pow(2f, colorAdjustments.postExposure),
			colorAdjustments.contrast * 0.01f + 1f,
			colorAdjustments.hueShift * (1f / 360f),
			colorAdjustments.saturation * 0.01f + 1f
		));
		buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
	}

	/// <summary>	/// 白平衡	/// </summary>
	void ConfigureWhiteBalance () {
		WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
		buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
			whiteBalance.temperature, whiteBalance.tint
		));
	}

	/// <summary>	/// 色调分离	/// </summary>
	void ConfigureSplitToning () {
		SplitToningSettings splitToning = settings.SplitToning;
		Color splitColor = splitToning.shadows;
		splitColor.a = splitToning.balance * 0.01f;
		buffer.SetGlobalColor(splitToningShadowsId, splitColor);
		buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
	}

	/// <summary>	/// 通道重映射	/// </summary>
	void ConfigureChannelMixer () {
		ChannelMixerSettings channelMixer = settings.ChannelMixer;
		buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
		buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
		buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
	}

	/// <summary>	/// 三重阴影映射	/// </summary>
	void ConfigureShadowsMidtonesHighlights () {
		ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
		buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
		buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
		buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
		buffer.SetGlobalVector(smhRangeId, new Vector4(
			smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
		));
	}

	/// <summary>	/// 进行色彩分级以及渲染到摄像机上	/// </summary>
	/// <param name="sourceId">源纹理</param>
	void DoColorGradingAndToneMapping (int sourceId) {
		ConfigureColorAdjustments();
		ConfigureWhiteBalance();
		ConfigureSplitToning();
		ConfigureChannelMixer();
		ConfigureShadowsMidtonesHighlights();

		int lutHeight = colorLUTResolution;
		int lutWidth = lutHeight * lutHeight;
		buffer.GetTemporaryRT(
			colorGradingLUTId, lutWidth, lutHeight, 0,
			FilterMode.Bilinear, RenderTextureFormat.DefaultHDR
		);
		buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
			lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)
		));

		ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
		Pass pass = Pass.ColorGradingNone + (int)mode;
		buffer.SetGlobalFloat(
			colorGradingLUTInLogId, useHDR && pass != Pass.ColorGradingNone ? 1f : 0f
		);
		//绘制查找集纹理
		Draw(sourceId, colorGradingLUTId, pass);

		buffer.SetGlobalVector(colorGradingLUTParametersId,
			new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f)
		);
        buffer.SetGlobalTexture(fxSource2Id, bulkLightMapId);
        //绘制最终图像
        DrawFinal(sourceId);
		buffer.ReleaseTemporaryRT(colorGradingLUTId);
	}

	/// <summary>	/// 进行体积光和雾效的混合	/// </summary>
	/// <param name="sourceId"></param>
	void DoBulkLight(int sourceId)
    {
		buffer.BeginSample("BulkLight");
        buffer.GetTemporaryRT(bulkLightMapId, (int)(camera.pixelWidth * 0.5f), (int)(camera.pixelHeight * 0.5),
            0, FilterMode.Bilinear, RenderTextureFormat.Default);
        buffer.GetTemporaryRT(bulkLightTempMapId, (int)(camera.pixelWidth * 0.5f), (int)(camera.pixelHeight * 0.5),
            0, FilterMode.Bilinear, RenderTextureFormat.Default);

		buffer.SetGlobalFloat(bulkLightShrinkRadio, settings.bulkLight.shrinkRadio / 100000f);
		buffer.SetGlobalInt(bulkLightSampleCount, settings.bulkLight.circleCount);
		buffer.SetGlobalFloat(bulkLightScatterRadio, settings.bulkLight.scatterRadio);
		buffer.SetGlobalFloat(bulkLightCheckMaxDistance, settings.bulkLight.checkDistance);

		buffer.SetGlobalFloat(bulkFogMaxHight, settings.bulkLight.fogMaxHeight);
		buffer.SetGlobalFloat(bulkFogMinHight, settings.bulkLight.fogMinHeight);
		buffer.SetGlobalFloat(bulkFogMaxDepth, settings.bulkLight.fogMaxDepth);
		buffer.SetGlobalFloat(bulkFogMinDepth, settings.bulkLight.fogMinDepth);

		buffer.SetGlobalFloat(bulkFogDepthFallOff, settings.bulkLight.fogDepthFallOff);
		//buffer.SetGlobalFloat(bulkFogHeightFallOff, settings.bulkLight.fogHeightOff);
		buffer.SetGlobalFloat(bulkFogPosYFallOff, settings.bulkLight.fogPosYFallOff);

		//buffer.SetGlobalFloat(bulkFogIntensity, settings.bulkLight.fogIntensity);

		DrawQueue(settings.bulkLight.fogTex, bulkLightMapId, Pass.BulkLight);

		Draw(bulkLightMapId, bulkLightTempMapId, Pass.BloomHorizontal);
		Draw(bulkLightTempMapId, bulkLightMapId, Pass.BloomVertical);

		buffer.ReleaseTemporaryRT(bulkLightTempMapId);

		buffer.EndSample("BulkLight");
    }

	void DrawQueue(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(
			to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.DrawProcedural(
			Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Quads, 6
		);
	}

	void Draw (RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass) {
		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(
			to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.DrawProcedural(
			Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Triangles, 3
		);
	}

	/// <summary>	/// 绘制最终纹理，渲染至摄像机的实际看到的纹理中	/// </summary>
	/// <param name="from">根据的纹理</param>
	void DrawFinal (RenderTargetIdentifier from) {
		buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
		buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);
		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(
			BuiltinRenderTextureType.CameraTarget,
			finalBlendMode.destination == BlendMode.Zero ?
				RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
			RenderBufferStoreAction.Store
		);
		buffer.SetViewport(camera.pixelRect);
		buffer.DrawProcedural(
			Matrix4x4.identity, settings.Material,
			(int)Pass.Final, MeshTopology.Triangles, 3
		);
	}
}