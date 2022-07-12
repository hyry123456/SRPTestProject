using System;
using UnityEngine.Rendering;

/// <summary>
/// 单个摄像机的设置类，用来判断单个摄像机需要执行的行为，与CameraBufferSettings相对应
/// </summary>
[Serializable]
public class CameraSettings {

	/// <summary>
	/// 渲染是否需要拷贝深度纹理以及颜色纹理，因为不能方便直接采样使用中的图片，
	/// 因此先复制一张出来，再进行处理，如果没有开启就没有该纹理
	/// </summary>
	public bool copyColor = true, copyDepth = true;

	/// <summary>	/// 渲染掩码，用来控制摄像机渲染的物体	/// </summary>
	[RenderingLayerMaskField]
	public int renderingLayerMask = -1;
	/// <summary>	/// 是否遮罩灯光	/// </summary>
	public bool maskLights = false;
	/// <summary>	/// 是否需要覆盖原有的场景后处理设置	/// </summary>
	public bool overridePostFX = false;
	/// <summary>	/// 重写的根据后处理组件	/// </summary>
	public PostFXSettings postFXSettings = default;

	/// <summary>	/// 纹理的混合模式控制，这个控制的是摄像机的纹理混合	/// </summary>
	[Serializable]
	public struct FinalBlendMode {

		public BlendMode source, destination;
	}

	public FinalBlendMode finalBlendMode = new FinalBlendMode {
		source = BlendMode.One,
		destination = BlendMode.Zero
	};
}