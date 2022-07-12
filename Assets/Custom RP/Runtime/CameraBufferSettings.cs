/// <summary>	/// 摄像机的Buffer设置类，这个定义是给场景用的，并没有控制单个摄像机	/// </summary>
[System.Serializable]
public struct CameraBufferSettings {

	/// <summary>	/// 是否允许HDR	/// </summary>
	public bool allowHDR;

	/// <summary>
	/// 控制场景中是否需要拷贝一些纹理，Reflection是给反射探针的摄像机用的，一般也不需要给这些摄像机处理这种数据吧
	/// </summary>
	public bool copyColor, copyColorReflection, copyDepth, copyDepthReflection;
}