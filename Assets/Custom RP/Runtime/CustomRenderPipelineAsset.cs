using UnityEngine;
using UnityEngine.Rendering;
using CustomRP.Clust;

/// <summary>
/// 渲染管线预制件构造类，定义了自定义渲染管线的预制件需要的数据以及定义，
/// 同时提供了一些可选开关，决定该干什么不该干什么
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public partial class CustomRenderPipelineAsset : RenderPipelineAsset {

	/// <summary>	/// 场景摄像机设置，默认允许使用HDR	/// </summary>
	[SerializeField]
	CameraBufferSettings cameraBuffer = new CameraBufferSettings {
		allowHDR = true
	};

	/// <summary>	/// 定义一些批处理以及灯光处理的常用数据	/// </summary>
	[SerializeField]
	bool
		useDynamicBatching = true,		//动态批处理
		useGPUInstancing = true,		//GPU实例化
		useSRPBatcher = true,			//SRP批处理
		useLightsPerObject = true;		//逐物体光照

	/// <summary>	/// 阴影设置参数	/// </summary>
	[SerializeField]
	ShadowSettings shadows = default;

	/// <summary	/// 后处理设置组件	/// </summary>
	[SerializeField]
	PostFXSettings postFXSettings = default;

	/// <summary>	/// 特效预制件	/// </summary>
	[SerializeField]
	PostFXEffectSetting postFXEffectSettings = default;

	/// <summary>	/// 设置查找表的像素大小	/// </summary>
	public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 }

	/// <summary>	/// 一般查找表32位就够了	/// </summary>
	[SerializeField]
	ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

	/// <summary>	/// 摄像机渲染需要的Shader	/// </summary>
	[SerializeField]
	Shader cameraRendererShader = default;

	/// <summary>	/// 创建一个渲染预制件返回给Unity处理	/// </summary>
	/// <returns>预制件对象</returns>
	protected override RenderPipeline CreatePipeline () {
		return new CustomRenderPipeline(
			cameraBuffer, useDynamicBatching, useGPUInstancing, useSRPBatcher,
			useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution,
			cameraRendererShader, postFXEffectSettings
		);
	}
}