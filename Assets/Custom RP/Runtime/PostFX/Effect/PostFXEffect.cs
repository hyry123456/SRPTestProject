
using UnityEngine;
using UnityEngine.Rendering;
using CustomRP.GPUPipeline;

/// <summary>
/// һЩ��Ч����Ҳ���ǲ��������ĺ�������ʹ��һЩ��ʽ����Ч�����Ŀǰ��������
/// </summary>
public class PostFXEffect
{
    enum Pass { 
		Copy,
		WaterBlend,
		BilateralFilter,
		BilateralFilterDepth
	}

	/// <summary>	/// �ж��Ƿ�����ֱ�ӿ���������Ϊ��һЩƽ̨��֧��ֱ�ӿ���������Ҫʹ��Draw�����濽��	/// </summary>
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


	/// <summary>	/// ׼������ǰ������׼��	/// </summary>
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

	/// <summary>	/// �������Ⱦ�������Դ��������ID��������Ⱦ��������Ĳ���	/// </summary>
	/// <param name="sourceId">���ݵ�ԭ����</param>
	/// <param name="destId">Ŀ�����Ⱦ����</param>
	public void Render(RenderTargetIdentifier sourceId,
		RenderTargetIdentifier dest)
	{
		buffer.BeginSample(bufferName);
        //�ж��Ƿ�Ҫ�������ˮЧ��
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
	/// ��Ⱦˮ�������һ�ϵ���������
	/// </summary>
	/// <param name="sourceId">ԭ����</param>
	/// <param name="destId">Ŀ������</param>
	void RenderWater(RenderTargetIdentifier sourceId, RenderTargetIdentifier destId)
    {
        //����ȾWidthͼ
        buffer.GetTemporaryRT(waterWidthMapId, camera.pixelWidth, camera.pixelHeight,
            0, FilterMode.Bilinear, RenderTextureFormat.R8);

        //���ÿ��ͼ
        buffer.SetRenderTarget(waterWidthMapId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, true, Color.clear);
        ExecuteBuffer();
		//DrawVisibleGeometry(setting.particleWater.renderingLayerMask, waterWidthId);
		DrawWidth();


        //��������Լ�����
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

	/// <summary>	/// ��װһ��Bufferд�뺯�����������	/// </summary>
	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}
