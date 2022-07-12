
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Effect")]
public class PostFXEffectSetting : ScriptableObject
{

    [SerializeField]
    Shader shader = default;

	[SerializeField]
	public Material material;
	public Material Material
	{
		get
		{
			if (material == null && shader != null 
				|| (material != null && material.shader != shader))
			{
				material = new Material(shader);
				material.hideFlags = HideFlags.HideAndDontSave;
			}
			return material;
		}
	}


	[Serializable]
	public struct ParticleWater
	{
		public bool useParticleWater;

		[ColorUsage(false, true)]
		public Color waterColor;

		/// <summary>	/// 渲染掩码，表示水渲染到的层，之后采样时也是在该层采用的	/// </summary>
		[RenderingLayerMaskField]
		public int renderingLayerMask;

		[Range(0, 0.5f)]
		public float bilaterFilterStrength;

		public float biurRadius;


		[Range(0,1f)]
		public float airRrfraRadio;
		[Range(0,1f)]
		public float waterRrfraRadio;
	}

	[SerializeField]
	public ParticleWater particleWater = new ParticleWater
	{
		waterColor = new Color(0.8f, 0.8f, 1),
		renderingLayerMask = -1,
		biurRadius = -50,
		airRrfraRadio = 0.1f,
		waterRrfraRadio = 0.3f
	};
}
