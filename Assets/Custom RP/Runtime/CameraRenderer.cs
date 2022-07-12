using UnityEngine;
using UnityEngine.Rendering;
using CustomRP.Clust;

/// <summary>/// 单个摄像级的渲染类，对该摄像机进行渲染	/// </summary>
public partial class CameraRenderer {

	/// <summary>	/// 定义buffer名称，用来方便Framer Debug中进行查看行为	/// </summary>
	const string bufferName = "Render Camera";

	/// <summary>
	/// 设置渲染的对应Pass的名称，教程上说是需要加Tags的，但我发现不用也行，不知道判定的具体原理是什么鬼，
	/// 但是对于所有Unlit的Shader都是能用的
	/// </summary>
	static ShaderTagId
		unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),	//支持Unlit
		litShaderTagId = new ShaderTagId("CustomLit");          //支持自定义的Pass

	// 一些通用且永久的Shader实例化编号，用编号来指代以及传递数据
	static int
		//渲染到的目标颜色图(经过后处理后再输出到摄像机的对应纹理上)
		colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment"),
		//渲染时需要的深度buffer位置，因为需要深度数据，所以提出来方便使用
		depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment"),
		colorTextureId = Shader.PropertyToID("_CameraColorTexture"),        //拷贝的颜色纹理id
		depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),        //拷贝的深度纹理id
		sourceTextureId = Shader.PropertyToID("_SourceTexture"),
		srcBlendId = Shader.PropertyToID("_CameraSrcBlend"),
		dstBlendId = Shader.PropertyToID("_CameraDstBlend"),
		timeId = Shader.PropertyToID("_Time"),
		inverseVPMatrixId = Shader.PropertyToID("_InverseVPMatrix"),
		postEffectTargetId = Shader.PropertyToID("_PostEffectTarget");		//添加混合特效的目标纹理

	/// <summary>	/// 默认摄像机设置模式，如果目前渲染的摄像机没有计算机设置数据，就用该模式处理	/// </summary>
	static CameraSettings defaultCameraSettings = new CameraSettings();

	/// <summary>	/// 判断是否至此直接拷贝纹理，因为在一些平台不支持直接拷贝纹理，需要使用Draw来代替拷贝	/// </summary>
	static bool copyTextureSupported =
		SystemInfo.copyTextureSupport > CopyTextureSupport.None;

	/// <summary>	/// 创建一个Buffer，将我们需要的行为放在该Buffer中	/// </summary>
	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	#region 由渲染管线对象中传入的数据以及我们运算中需要的数据位置
	/// <summary>	/// 行为存储块	/// </summary>
	ScriptableRenderContext context;
	/// <summary>	/// 这次赋值渲染的camera	/// </summary>
	Camera camera;
	/// <summary>	///裁剪数据	/// </summary>
	CullingResults cullingResults;
	/// <summary>	///光照控制类	/// </summary>
	Lighting lighting = new Lighting();	
	/// <summary>	/// 后处理栈对象	/// </summary>
	PostFXStack postFXStack = new PostFXStack();
	/// <summary>	/// 后处理特效	/// </summary>
	PostFXEffect postFXEffect = new PostFXEffect();
	/// <summary>	/// 是否使用HDR	/// </summary>
	bool useHDR;

	/// <summary>	/// 用来判断是否使用一些纹理图片	/// </summary>
	bool useColorTexture, useDepthTexture, useIntermediateBuffer;
	/// <summary>	/// 在摄像机渲染时需要的材质，用来拷贝一些buffer数据	/// </summary>
	Material material;
	/// <summary>	/// 丢失时使用的纹理，用来防止没有创建深度纹理时出现bug 	/// </summary>
	Texture2D missingTexture;
    #endregion

    public CameraRenderer (Shader shader) {
		//创建一个材质用来渲染buffer
		material = CoreUtils.CreateEngineMaterial(shader);
		//创建一个丢失纹理
		missingTexture = new Texture2D(1, 1) {
			hideFlags = HideFlags.HideAndDontSave,
			name = "Missing"
		};
		//赋值像素，因为就只有1个像素
		missingTexture.SetPixel(0, 0, Color.white * 0.5f);
		//进行变化，不这么调用好像会没有影响
		missingTexture.Apply(true, true);
	}

	public void Dispose () {
		//释放材质
		CoreUtils.Destroy(material);
		//释放纹理
		CoreUtils.Destroy(missingTexture);
	}

	public void Render (
		ScriptableRenderContext context, Camera camera,
		CameraBufferSettings bufferSettings,
		bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject,
		ShadowSettings shadowSettings, PostFXSettings postFXSettings,
		int colorLUTResolution, PostFXEffectSetting postFXEffectSetting
	) {
		this.context = context;
		this.camera = camera;

		//获取该摄像机的渲染设置
		var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
		//如果没有渲染设置就使用默认模式
		CameraSettings cameraSettings =
			crpCamera ? crpCamera.Settings : defaultCameraSettings;

		//反射探针摄像机只用关注场景是否支持
		if (camera.cameraType == CameraType.Reflection) {
			useColorTexture = bufferSettings.copyColorReflection;
			useDepthTexture = bufferSettings.copyDepthReflection;
		}
		else {
			useColorTexture = bufferSettings.copyColor && cameraSettings.copyColor;
			useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
		}
		//需要替换后处理就替换后处理
		if (cameraSettings.overridePostFX) {
			postFXSettings = cameraSettings.postFXSettings;
		}

		PrepareBuffer();
        PrepareForSceneWindow();    //场景摄像机操作
        
		if (!Cull(shadowSettings.maxDistance)) {	//摄像机剔除准备
			return;		//准备失败就退出
		}
		//判断场景设置以及单个摄像机设置
		useHDR = bufferSettings.allowHDR && camera.allowHDR;

		buffer.BeginSample(SampleName);
		ExecuteBuffer();

		//if (drawClustData == null)
		//	drawClustData = new DrawClustData(clustSetting.clustShader);
		//drawClustData.ReadyClust(context, clustSetting.clustShader);

		//准备灯光数据，在灯光数据中会进行阴影数据准备
		lighting.Setup(
			context, cullingResults, shadowSettings, useLightsPerObject,
			cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1
		);
		//后处理数据准备
		postFXStack.Setup(
			context, camera, postFXSettings, useHDR, colorLUTResolution,
			cameraSettings.finalBlendMode
		);
		//后处理特效准备
		postFXEffect.Setup( context, camera, postFXEffectSetting, ref cullingResults, depthAttachmentId);

		buffer.EndSample(SampleName);
		//本摄像机渲染准备
		Setup();

        ClustDrawStack.Instance.DrawClustData(context, buffer, ClustDrawType.Simple, camera);

        //绘制所有可见图元
        DrawVisibleGeometry(
			useDynamicBatching, useGPUInstancing, useLightsPerObject,
			cameraSettings.renderingLayerMask
		);

        DrawUnsupportedShaders();   //绘制不支持的纹理

        if (postFXEffect.IsActive)
        {
            buffer.GetTemporaryRT(postEffectTargetId, camera.pixelWidth, camera.pixelHeight,
                0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            ExecuteBuffer();
            postFXEffect.Render(colorAttachmentId, postEffectTargetId);

            if (copyTextureSupported)
            {       //支持拷贝就直接拷贝
                buffer.CopyTexture(postEffectTargetId, colorAttachmentId);
            }
            else
            {                           //不支持拷贝的话就用Shader复制
                Draw(postEffectTargetId, colorAttachmentId);
            }
            buffer.ReleaseTemporaryRT(postEffectTargetId);
            ExecuteBuffer();
        }

        DrawGizmosBeforeFX();       //在后处理前准备一下Gizmos需要的数据
        if (postFXStack.IsActive)
        {       //支持后处理才进行后处理
                //执行后处理
            postFXStack.Render(colorAttachmentId);
        }
        //如果摄像机的目标纹理被替换过
        else if (useIntermediateBuffer)
        {
            DrawFinal(cameraSettings.finalBlendMode);
			ExecuteBuffer();
        }
        DrawGizmosAfterFX();    //绘制最终的Gizmos效果
		Cleanup();				//清除数据
		Submit();
	}

	/// <summary>	/// 执行摄像机数据进行阴影剔除，将不需要的部分进行剔除	/// </summary>
	/// <param name="maxShadowDistance">剔除距离</param>
	/// <returns>是否成功剔除</returns>
	bool Cull (float maxShadowDistance) {
		//执行剔除，有可能剔除失败，因为要进行区分
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			//控制阴影距离
			p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
			//赋值裁剪结果
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	/// <summary>	/// 渲染开始的准备方法  	/// </summary>
	void Setup () {

		//准备摄像机数据，剔除之类的
		context.SetupCameraProperties(camera);
		//获得清理buffer的模式
		CameraClearFlags flags = camera.clearFlags;

		//判断是否使用分开纹理，也就是将颜色盒深度图分开存储
		useIntermediateBuffer =
			useColorTexture || useDepthTexture || postFXStack.IsActive;
		//使用分开存储就分开存储，不使用时会直接渲染到摄像机的目标纹理上，也就不会有影响了
		if (useIntermediateBuffer) {
			if (flags > CameraClearFlags.Color) {
				flags = CameraClearFlags.Color;
			}
			//获取颜色图
			buffer.GetTemporaryRT(
				colorAttachmentId, camera.pixelWidth, camera.pixelHeight,
				0, FilterMode.Bilinear, useHDR ?
					RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
			);
			//获取深度图
			buffer.GetTemporaryRT(
				depthAttachmentId, camera.pixelWidth, camera.pixelHeight,
				32, FilterMode.Point, RenderTextureFormat.Depth
			);
			//设置渲染目标，分开设置，让深度和颜色渲染到我们想要的位置
			buffer.SetRenderTarget(
				colorAttachmentId,
				RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
				depthAttachmentId,
				RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
			);
		}

		//清除上一帧的数据
		buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth,
			flags == CameraClearFlags.Color,
			flags == CameraClearFlags.Color ?
				camera.backgroundColor.linear : Color.clear
		);
		buffer.BeginSample(SampleName);
		//设置一下默认全局纹理
		buffer.SetGlobalTexture(colorTextureId, missingTexture);
		buffer.SetGlobalTexture(depthTextureId, missingTexture);

		Vector4 time = new Vector4(Time.time / 20, Time.time, Time.time * 2, Time.time * 3);
		//设置游戏时间
		buffer.SetGlobalVector(timeId, time);

		Matrix4x4 matrix4X4 = camera.projectionMatrix * camera.worldToCameraMatrix;
		buffer.SetGlobalMatrix(inverseVPMatrixId, matrix4X4.inverse);

		ExecuteBuffer();
	}

	/// <summary>	/// 清除使用过的数据，因为纹理图大多数都是在内存中的，因此需要我们手动释放	/// </summary>
	void Cleanup () {
		lighting.Cleanup();		//灯光数据清除
		if (useIntermediateBuffer) {		//如果使用了中间纹理，需要将使用过的都清除一遍
			buffer.ReleaseTemporaryRT(colorAttachmentId);
			buffer.ReleaseTemporaryRT(depthAttachmentId);
			if (useColorTexture) {
				buffer.ReleaseTemporaryRT(colorTextureId);
			}
			if (useDepthTexture) {
				buffer.ReleaseTemporaryRT(depthTextureId);
			}
		}
	}

	/// <summary>
	/// 提交方法，所有Context数据录入后需要提交才可以正常执行
	/// </summary>
	void Submit () {
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
	}

	/// <summary>	/// 封装一个Buffer写入函数，方便调用	/// </summary>
	void ExecuteBuffer () {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	/// <summary>
	/// 绘制所有可以看见的图元，包括可以看见的和不可以看见的
	/// </summary>
	/// <param name="useDynamicBatching">是否使用动态批处理</param>
	/// <param name="useGPUInstancing">是否使用GPU实例化</param>
	/// <param name="useLightsPerObject">是否使用逐物体光照</param>
	/// <param name="renderingLayerMask">渲染的遮罩层,确定该渲染哪个物体</param>
	void DrawVisibleGeometry (
		bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject,
		int renderingLayerMask
	) {
		//判断灯光数据模式，是否要开启逐物体光照
		PerObjectData lightsPerObjectFlags = useLightsPerObject ?
			PerObjectData.LightData | PerObjectData.LightIndices :
			PerObjectData.None;
		//设置该摄像机的物体排序模式，目前是渲染普通物体，因此用一般排序方式
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque
		};
		//设置绘制参数，对于通用物体需要全部都打开，一般来说
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		) {
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing,
			perObjectData =
				PerObjectData.ReflectionProbes |
				PerObjectData.Lightmaps | PerObjectData.ShadowMask |
				PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
				PerObjectData.LightProbeProxyVolume |
				PerObjectData.OcclusionProbeProxyVolume |
				lightsPerObjectFlags
		};
		//支持自定义Pass
        drawingSettings.SetShaderPassName(1, litShaderTagId);

		//渲染一般物体
        var filteringSettings = new FilteringSettings(
			RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask
		);
		//进行渲染的执行方法
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		//绘制天空盒
		context.DrawSkybox(camera);
		if (useColorTexture || useDepthTexture) {
			//拷贝目前的buffer数据
			CopyAttachments();
		}

		//准备渲染透明物体
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		//渲染方法
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	/// <summary>
	/// 复制颜色图或深度图，一般要在透明物体前渲染，因为透明物体不写入深度，在后面惠子可能会由问题
	/// </summary>
	void CopyAttachments () {
		if (useColorTexture) {	//使用颜色纹理就需要复制颜色纹理
			//准备一张新的颜色纹理，进行数据存储
			buffer.GetTemporaryRT(
				colorTextureId, camera.pixelWidth, camera.pixelHeight,
				0, FilterMode.Bilinear, useHDR ?
					RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
			);
			if (copyTextureSupported) {		//支持拷贝就直接拷贝
				buffer.CopyTexture(colorAttachmentId, colorTextureId);
			}
			else {							//不支持拷贝的话就直接用Shader复制
				Draw(colorAttachmentId, colorTextureId);
			}
		}
		if (useDepthTexture) {	//使用深度纹理
			//准备深度纹理
			buffer.GetTemporaryRT(
				depthTextureId, camera.pixelWidth, camera.pixelHeight,
				32, FilterMode.Point, RenderTextureFormat.Depth
			);
			if (copyTextureSupported) {		//拷贝深度纹理
				buffer.CopyTexture(depthAttachmentId, depthTextureId);
			}
			else {			//绘制深度纹理
				Draw(depthAttachmentId, depthTextureId, true);
			}
		}

		//再设置渲染目标，之后要渲染透明物体了
		if (!copyTextureSupported) {
			buffer.SetRenderTarget(
				colorAttachmentId,
				RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
				depthAttachmentId,
				RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
			);
		}
		ExecuteBuffer();
	}

	/// <summary>
	/// 进行纹理绘制
	/// </summary>
	/// <param name="from">根据纹理</param>
	/// <param name="to">目标纹理</param>
	/// <param name="isDepth">是否为深度</param>
	void Draw (
		RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false
	) {
		buffer.SetGlobalTexture(sourceTextureId, from);
		buffer.SetRenderTarget(
			to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		//调用对应的Pass进行处理
		buffer.DrawProcedural(
			Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3
		);
	}

	/// <summary>
	/// 将纹理根据混合模式渲染到目标位置
	/// </summary>
	/// <param name="finalBlendMode">混合模式</param>
	void DrawFinal (CameraSettings.FinalBlendMode finalBlendMode) {
		buffer.SetGlobalFloat(srcBlendId, (float)finalBlendMode.source);
		buffer.SetGlobalFloat(dstBlendId, (float)finalBlendMode.destination);
		buffer.SetGlobalTexture(sourceTextureId, colorAttachmentId);
		buffer.SetRenderTarget(
			BuiltinRenderTextureType.CameraTarget,
			finalBlendMode.destination == BlendMode.Zero ?
				RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
			RenderBufferStoreAction.Store
		);
		buffer.SetViewport(camera.pixelRect);
		buffer.DrawProcedural(
			Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3
		);
		buffer.SetGlobalFloat(srcBlendId, 1f);
		buffer.SetGlobalFloat(dstBlendId, 0f);
	}
}