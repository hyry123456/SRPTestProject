
using UnityEngine;

public class Noise : MonoBehaviour
{
    public int size = 512;
    public RenderTexture renderTexture;
    public ComputeShader shader;
    public int octave = 3;
    public float texSize = 32;

    int kernel;
    void Start()
    {
        ChangeTexture();
    }

    void ChangeTexture()
    {
        //if (renderTexture == null) renderTexture = new RenderTexture(size, size, 0);
        renderTexture.Release();
        renderTexture = new RenderTexture(size, size, 0);

        kernel = shader.FindKernel("CurlNoise3D");
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        shader.SetTexture(kernel, "Result", renderTexture);
        shader.SetInt("textureSize", size);
        shader.SetFloat("texSize", texSize);
        shader.SetInt("octave", octave);
        shader.Dispatch(kernel, size / 32 + 1, size / 32 + 1, 1);
    }

    private void OnValidate()
    {
        ChangeTexture();
    }

}
