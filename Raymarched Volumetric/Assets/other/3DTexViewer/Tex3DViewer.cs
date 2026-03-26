using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CameraCommon;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class Tex3DViewer : MonoBehaviour
{
    [SerializeField, Min(64)]
    protected int textureResolution = 128;
    [SerializeField, Range(1f, 10f)]
    protected float noiseTiling = 2;
    [SerializeField, Range(0f, 2f)]
    protected float slice = 0f;

    [SerializeField]
    protected Shader previewShader;
    private Material previewMaterial;

    [SerializeField]
    protected ComputeShader noiseCompute;
    private RenderTexture renderTexture;

    private int currentTextureResolution = 0;
    private float currentNoiseTiling = 0;
    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
        if (!BB_Rendering.ShaderMaterialReady(previewShader, ref previewMaterial))
        {
            Graphics.Blit(_source, _destination);
            return;
        }
        if(!noiseCompute)
        {
            Graphics.Blit(_source, _destination);
            return;
        }

        if (renderTexture == null || currentTextureResolution != textureResolution)
        {
            CreateRenderTexture(textureResolution);
        }
        if(currentTextureResolution != textureResolution || currentNoiseTiling != noiseTiling)
        {
            currentTextureResolution = textureResolution;
            currentNoiseTiling = noiseTiling;

            DispatchComputeShader();
        }

        // temp
        DispatchComputeShader();

        previewMaterial.SetTexture("_VolumeTex", renderTexture);
        previewMaterial.SetFloat("_Slice", slice);

        Graphics.Blit(_source, _destination, previewMaterial);
    }

    void CreateRenderTexture(int _textureSize)
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        renderTexture = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.RFloat);

        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        renderTexture.volumeDepth = _textureSize;

        renderTexture.enableRandomWrite = true;
        renderTexture.wrapMode = TextureWrapMode.Repeat;

        renderTexture.Create();
    }
    void DispatchComputeShader()
    {
        int kernelIndex = noiseCompute.FindKernel("CSGetWorley");

        noiseCompute.SetTexture(kernelIndex, "Result", renderTexture);
        noiseCompute.SetFloat("_NoiseTiling", currentNoiseTiling);

        noiseCompute.SetFloat("_Resolution", currentTextureResolution);

        int threadsCount = Mathf.CeilToInt(currentTextureResolution / 4f);
        noiseCompute.Dispatch(kernelIndex, threadsCount, threadsCount, threadsCount);
    }
}
