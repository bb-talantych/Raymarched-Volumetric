using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CameraCommon;
using Unity.VisualScripting;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class VolumetricTestEffect : MonoBehaviour
{
    [SerializeField, Min(64)]
    protected int textureResolution = 128;
    [SerializeField, Range(0.1f, 10f)]
    protected float noiseTiling = 1;

    public Color extinctionColor = new Color(1, 0, 0, 1);
    public Color scateringColor = new Color(1, 1, 1, 1);

    [Min(0.1f)]
    public float sphereRadius = 3f;
    [Range(0f, 1f)]
    public float densityFalloff = 0.8f;

    [Range(0.01f, 2f)]
    public float stepSize = 0.1f;
    [Range(0.01f, 2f)]
    public float lightStepSize = 0.2f;

    [Min(0f)]
    public float absorptionMultipier = 0f;
    [Min(0f)]
    public Vector3 absorptionCoef = Vector3.one;
    [Min(0f)]
    public float scatteringMultipier = 1f;
    [Min(0f)]
    public Vector3 scatteringCoef = new Vector3(0.5f, 1f, 2f);
    [Min(0f)]
    public float volumeDensity = 1f;
    [Range(-1f, 1f)]
    public float asymmetryFactor = -0.35f;

    [SerializeField]
    protected Transform container;
    [SerializeField, HideInInspector]
    protected Shader volumetricShader;
    protected Material volumetricMaterial = null;

    [SerializeField]
    protected ComputeShader noiseCompute;
    private RenderTexture renderTexture;

    private int currentTextureResolution = 0;
    private float currentNoiseTiling = 0;

    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
        if (!BB_Rendering.ShaderMaterialReady(volumetricShader, ref volumetricMaterial))
        {
            Graphics.Blit(_source, _destination);
            return;
        }
        if (!container)
        {
            Graphics.Blit(_source, _destination);
            return;
        }

        ////////////
        if (!noiseCompute)
        {
            Graphics.Blit(_source, _destination);
            return;
        }

        if (renderTexture == null || currentTextureResolution != textureResolution)
        {
            CreateRenderTexture(textureResolution);
        }
        if (currentTextureResolution != textureResolution || currentNoiseTiling != noiseTiling)
        {
            currentTextureResolution = textureResolution;
            currentNoiseTiling = noiseTiling;

            DispatchComputeShader();
        }

        volumetricMaterial.SetTexture("_VolumeTex", renderTexture);

        /////////////

        volumetricMaterial.SetFloat("_SphereRadius", sphereRadius);
        volumetricMaterial.SetFloat("_DensityFalloff", densityFalloff);

        volumetricMaterial.SetFloat("_StepSize", stepSize);
        volumetricMaterial.SetFloat("_LightStepSize", lightStepSize);

        volumetricMaterial.SetColor("_ExtinctionColor", extinctionColor);
        volumetricMaterial.SetColor("_ScateringColor", scateringColor);

        float halfFOVTan = Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        volumetricMaterial.SetFloat("_Half_FOV_Tan", halfFOVTan);

        volumetricMaterial.SetVector("_ContainerPosition", container.position);
        volumetricMaterial.SetVector("_ContainerLocalScale", container.localScale);

        volumetricMaterial.SetVector("_AbsorptionCoef", absorptionCoef * absorptionMultipier);
        volumetricMaterial.SetVector("_ScatteringCoef", scatteringCoef * scatteringMultipier);
        volumetricMaterial.SetFloat("_VolumeDensity", volumeDensity);
        volumetricMaterial.SetFloat("_AsymmetryFactor", asymmetryFactor);

        Graphics.Blit(_source, _destination, volumetricMaterial);
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
        renderTexture.wrapMode = TextureWrapMode.Clamp;

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
