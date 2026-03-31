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
    [SerializeField, Range(1, 10)]
    protected int worleyTiling = 1;
    [SerializeField, Range(5, 20)]
    protected int simplexTiling = 10;
    [SerializeField, Range(0, 1)]
    protected float noiseMix = 1;
    [SerializeField, Range(1f, 3)]
    protected float minDistMultiplier = 1.35f;
    [SerializeField, Range(0f, 2f)]
    protected float slice = 0f;

    [SerializeField]
    protected Shader previewShader;
    private Material previewMaterial;

    [SerializeField]
    protected ComputeShader noiseCompute;
    private RenderTexture noiseTexture;

    private int currentTextureResolution = 0;
    private float currentMinDistMultiplier = 0;

    private float currentWorleyTiling = 0;
    private float currentSimplexTiling = 0;
    private float currentNoiseMix = 0;

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

        if (noiseTexture == null || currentTextureResolution != textureResolution)
        {
            CreateRenderTexture(textureResolution);
        }
        if (currentTextureResolution != textureResolution || currentWorleyTiling != worleyTiling 
            || currentMinDistMultiplier != minDistMultiplier || currentSimplexTiling != simplexTiling || currentNoiseMix != noiseMix)
        {
            currentTextureResolution = textureResolution;
            currentWorleyTiling = worleyTiling;
            currentSimplexTiling = simplexTiling;
            currentNoiseMix = noiseMix;
            currentMinDistMultiplier = minDistMultiplier;

            DispatchNoiseCompute();
        }

        previewMaterial.SetTexture("_VolumeTex", noiseTexture);
        previewMaterial.SetFloat("_Slice", slice);

        Graphics.Blit(_source, _destination, previewMaterial);
    }

    void CreateRenderTexture(int _textureSize)
    {
        if (noiseTexture != null)
        {
            noiseTexture.Release();
            noiseTexture = null;
        }

        noiseTexture = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.RFloat);

        noiseTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        noiseTexture.volumeDepth = _textureSize;

        noiseTexture.enableRandomWrite = true;
        noiseTexture.wrapMode = TextureWrapMode.Repeat;

        noiseTexture.Create();
    }
    void DispatchNoiseCompute()
    {
        int kernelIndex = noiseCompute.FindKernel("CSGetWorley");

        noiseCompute.SetTexture(kernelIndex, "Result", noiseTexture);
        noiseCompute.SetFloat("_MinDistMultiplier", currentMinDistMultiplier);

        noiseCompute.SetFloat("_WorleyTiling", currentWorleyTiling);
        noiseCompute.SetFloat("_SimplexTiling", currentSimplexTiling);
        noiseCompute.SetFloat("_NoiseMix", currentNoiseMix);

        noiseCompute.SetFloat("_Resolution", currentTextureResolution);

        int threadsCount = Mathf.CeilToInt(currentTextureResolution / 4f);
        noiseCompute.Dispatch(kernelIndex, threadsCount, threadsCount, threadsCount);
    }
}
