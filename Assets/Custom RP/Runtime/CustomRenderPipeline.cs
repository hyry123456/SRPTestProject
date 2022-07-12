using UnityEngine;
using UnityEngine.Rendering;
using CustomRP.Clust;

/// <summary>
/// 自定义渲染管线对象，这个是实际上的总调用函数，Unity会将函数块以及所有的摄像机穿进来，
/// 首先我们在Context中写入要执行的内容，提交后就会执行了，一般是一个摄像机一个摄像机执行的
/// </summary>
public partial class CustomRenderPipeline : RenderPipeline {
	/// <summary>	/// 单个摄像机的渲染对象，是摄像机渲染的实际类，传入camera和contex进行渲染	/// </summary>
	CameraRenderer renderer;

    #region 预制件传入数据的存储位置

    CameraBufferSettings cameraBufferSettings;

	bool useDynamicBatching, useGPUInstancing, useLightsPerObject;

	ShadowSettings shadowSettings;
	PostFXSettings postFXSettings;
	PostFXEffectSetting postFXEffectSettings;

	int colorLUTResolution;

    #endregion

    public CustomRenderPipeline (
		CameraBufferSettings cameraBufferSettings,
		bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
		bool useLightsPerObject, ShadowSettings shadowSettings,
		PostFXSettings postFXSettings, int colorLUTResolution, Shader cameraRendererShader,
		PostFXEffectSetting postFXEffectSettings
	) {
		//赋值传入数据，以及准备需要的数据
		this.colorLUTResolution = colorLUTResolution;
		//this.allowHDR = allowHDR;
		this.cameraBufferSettings = cameraBufferSettings;
		this.postFXSettings = postFXSettings;
		this.shadowSettings = shadowSettings;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.useLightsPerObject = useLightsPerObject;
		this.postFXEffectSettings = postFXEffectSettings;

		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
		GraphicsSettings.lightsUseLinearIntensity = true;
		InitializeForEditor();          //重写生成光照贴图
		renderer = new CameraRenderer(cameraRendererShader);
	}

	protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
		foreach (Camera camera in cameras) {
			//调用实际渲染类，对单个摄像机进行渲染
			renderer.Render(
				context, camera, cameraBufferSettings,
				useDynamicBatching, useGPUInstancing, useLightsPerObject,
				shadowSettings, postFXSettings, colorLUTResolution, postFXEffectSettings
			);
		}
	}

	protected override void Dispose (bool disposing) {
		base.Dispose(disposing);
		DisposeForEditor();			//释放重写生成光照贴图
		renderer.Dispose();
	}
}