
using UnityEngine;
using UnityEngine.Rendering;
using CustomRP.GPUPipeline;

/// <summary>
/// 一些特效后处理，也就是不是真正的后处理，但是使用一些方式将特效混合在目前的纹理上
/// </summary>
public class PostFXEffect
{
    enum Pass { 
		Copy,
		WaterBlend,
		BilateralFilter,
		BilateralFilterDepth
	}

	/// <summary>	/// 判断是否至此直接拷贝纹理，因为在一些平台不支持直接拷贝纹理，需要使用Draw来代替拷贝	/// </summary>
	static bool copyTextureSupported =
		SystemInfo.copyTextureSupport > CopyTextureSupport.None;
	public bool IsActive => setting != null;

	static int waterDepthMapId = Shader.PropertyToID("_ParticleWaterDepthTex"),
		waterWidthMapId = Shader.PropertyToID("_ParticleWaterWidthTex"),
		waterNormalMapId = Shader.PropertyToID("_ParticleWaterNormalTex"),
		bilaterFilterFactorId = Shader.PropertyToID("_BilaterFilterFactor"),
		blurRadiusId = Shader.PropertyToID("_BlurRadius"),
		waterReflectId = Shader.PropertyToID("_WaterReflectCube"),
		waterColorId = Shader.PropertyToID("_WaterColor"),
		waterDataId = Shader.PropertyToID("_WaterData");


	static int fxEffectSourceId = Shader.PropertyToID("_PostFxEffectSource"),
		fxEffectSource2Id = Shader.PropertyToID("_PostFxEffectSource2"),
		temRT1 = Shader.PropertyToID("_PostFxEffectTempRT1"),
		temRT2 = Shader.PropertyToID("_PostFxEffectTempRT2"),
		temRT3 = Shader.PropertyToID("_PostFxEffectTempRT3");

	static ShaderTagId
		waterWidthId = new ShaderTagId("WaterWidth"),
		waterNormalAndDepth = new ShaderTagId("WaterDepthAndNormal");

	int currentDepthId;

    const string bufferName = "Post FX Effect";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;
    Camera camera;
	PostFXEffectSetting setting;

	CullingResults cullingResults;


	/// <summary>	/// 准备后处理前的数据准备	/// </summary>
	public void Setup(
		ScriptableRenderContext context, Camera camera, PostFXEffectSetting setting,
		ref CullingResults cullingResults, int currentDepthId
		)
	{
		this.context = context;
		this.camera = camera;
        this.setting = setting;
		this.cullingResults = cullingResults;
		this.currentDepthId = currentDepthId;
	}

	/// <summary>	/// 后处理的渲染方法，以传入纹理的ID，进行渲染到摄像机的操作	/// </summary>
	/// <param name="sourceId">根据的原纹理</param>
	/// <param name="destId">目标的渲染纹理</param>
	public void Render(RenderTargetIdentifier sourceId,
		RenderTargetIdentifier dest)
	{
		buffer.BeginSample(bufferName);
        //判断是否要添加粒子水效果
        if (setting.particleWater.useParticleWater)
        {
            RenderWater(sourceId, dest);
        }
        else
        {
            if (copyTextureSupported)
            {
                buffer.CopyTexture(sourceId, dest);
            }
            else
            {
                Draw(sourceId, dest, Pass.Copy);
            }
        }
        buffer.EndSample(bufferName);
		ExecuteBuffer();
	}

	/// <summary>
	/// 渲染水的纹理并且混合到主纹理上
	/// </summary>
	/// <param name="sourceId">原纹理</param>
	/// <param name="destId">目标纹理</param>
	void RenderWater(RenderTargetIdentifier sourceId, RenderTargetIdentifier destId)
    {
        //先渲染Width图
        buffer.GetTemporaryRT(waterWidthMapId, camera.pixelWidth, camera.pixelHeight,
            0, FilterMode.Bilinear, RenderTextureFormat.R8);

        //设置宽度图
        buffer.SetRenderTarget(waterWidthMapId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, true, Color.clear);
        ExecuteBuffer();
		//DrawVisibleGeometry(setting.particleWater.renderingLayerMask, waterWidthId);
		DrawWidth();


        //绘制深度以及法线
        buffer.GetTemporaryRT(waterNormalMapId, camera.pixelWidth, camera.pixelHeight,
            0, FilterMode.Bilinear, RenderTextureFormat.Default);
        buffer.GetTemporaryRT(waterDepthMapId, camera.pixelWidth, camera.pixelHeight,
            16, FilterMode.Bilinear, RenderTextureFormat.Depth);

        buffer.SetRenderTarget(waterNormalMapId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            waterDepthMapId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		buffer.ClearRenderTarget(true, true, Color.clear);

		ExecuteBuffer();
        //DrawVisibleGeometry(setting.particleWater.renderingLayerMask, waterNormalAndDepth);
		DrawDepthAndNormal();

        buffer.GetTemporaryRT(temRT1, camera.pixelWidth, camera.pixelHeight,
            0, FilterMode.Bilinear, RenderTextureFormat.R16);
        buffer.GetTemporaryRT(temRT2, camera.pixelWidth, camera.pixelHeight,
            0, FilterMode.Bilinear, RenderTextureFormat.Default);
		buffer.GetTemporaryRT(temRT3, camera.pixelWidth, camera.pixelHeight,
			16, FilterMode.Bilinear, RenderTextureFormat.Depth);

        buffer.SetGlobalFloat(bilaterFilterFactorId, 1.0f - setting.particleWater.bilaterFilterStrength);
        buffer.SetGlobalVector(blurRadiusId, new Vector4(setting.particleWater.biurRadius, 0, 0, 0));
		ExecuteBuffer();

		Draw(waterWidthMapId, temRT1, Pass.BilateralFilter);
        Draw(waterNormalMapId, temRT2, Pass.BilateralFilter);
		Draw(waterDepthMapId, temRT3, Pass.BilateralFilterDepth);

        buffer.SetGlobalFloat(bilaterFilterFactorId, 1.0f - setting.particleWater.bilaterFilterStrength);
        buffer.SetGlobalVector(blurRadiusId, new Vector4(0, setting.particleWater.biurRadius, 0, 0));

        Draw(temRT1, waterWidthMapId, Pass.BilateralFilter);
        Draw(temRT2, waterNormalMapId, Pass.BilateralFilter);
		Draw(temRT3, waterDepthMapId, Pass.BilateralFilterDepth);

		buffer.SetGlobalTexture(fxEffectSourceId, sourceId);
        buffer.SetGlobalTexture(fxEffectSource2Id, currentDepthId);
		buffer.SetGlobalVector(waterDataId, new Vector4(setting.particleWater.airRrfraRadio,
			setting.particleWater.waterRrfraRadio));
		buffer.SetGlobalColor(waterColorId, setting.particleWater.waterColor);

        Draw(sourceId, destId, Pass.WaterBlend);

        buffer.ReleaseTemporaryRT(waterWidthMapId);
        buffer.ReleaseTemporaryRT(waterNormalMapId);
        buffer.ReleaseTemporaryRT(waterDepthMapId);
        buffer.ReleaseTemporaryRT(temRT1);
        buffer.ReleaseTemporaryRT(temRT2);
		buffer.ReleaseTemporaryRT(temRT3);
		ExecuteBuffer();
    }



	void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
	{
		buffer.SetGlobalTexture(fxEffectSourceId, from);
		buffer.SetRenderTarget(
			to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.DrawProcedural(
			Matrix4x4.identity, setting.Material, (int)pass, MeshTopology.Triangles, 3
		);
	}

	void DrawWidth()
    {
		WaterDrawStack.Instance.DrawWaterByMode(context, buffer, WaterDrawMode.Width);
    }

	void DrawDepthAndNormal()
    {
		WaterDrawStack.Instance.DrawWaterByMode(context, buffer, WaterDrawMode.DepthAndNormal);
	}

	/// <summary>	/// 封装一个Buffer写入函数，方便调用	/// </summary>
	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}
