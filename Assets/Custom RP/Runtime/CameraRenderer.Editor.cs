using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer {

	partial void DrawGizmosBeforeFX ();

	partial void DrawGizmosAfterFX ();
	
	partial void DrawUnsupportedShaders ();

	partial void PrepareForSceneWindow ();

	partial void PrepareBuffer ();

#if UNITY_EDITOR

	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	static Material errorMaterial;

	string SampleName { get; set; }

	partial void DrawGizmosBeforeFX () {
		if (Handles.ShouldRenderGizmos()) {
			if (useIntermediateBuffer) {
				Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
				ExecuteBuffer();
			}
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
		}
	}

	partial void DrawGizmosAfterFX () {
		if (Handles.ShouldRenderGizmos()) {
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	/// <summary>	/// 给不支持的纹理绘制一下报错效果	/// </summary>
	partial void DrawUnsupportedShaders () {
		if (errorMaterial == null) {
			errorMaterial =
				new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		) {
			overrideMaterial = errorMaterial
		};
		for (int i = 1; i < legacyShaderTagIds.Length; i++) {
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}
		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	/// <summary>	/// 让UI能够在创建摄像机中显示，方便调整位置	/// </summary>
	partial void PrepareForSceneWindow () {
		if (camera.cameraType == CameraType.SceneView) {	//只有场景摄像机才开启该操作
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}

	/// <summary>	/// 准备性能分析器分析一段代码的性能	/// </summary>
	partial void PrepareBuffer () {
		Profiler.BeginSample("Editor Only");
		buffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}

#else

	const string SampleName = bufferName;

#endif
}