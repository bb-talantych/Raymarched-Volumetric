using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CameraCommon;
using System.Runtime.InteropServices;
using System;
using Unity.VisualScripting.Dependencies.Sqlite;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class VolumetricComputeEffect : MonoBehaviour
{
    [Header("Components")]
    public Light sun;
    public Material skybox;

    [Header("Sphere Parameters")]
    public Vector3 spherePosition = new Vector3(0, 0, 0);
    [Min(0.1f)]
    public float sphereRadius = 7f;

    [Header("Raymarching Parameters")]
    [Range(0.01f, 2f)]
    public float stepSize = 0.1f;
    [Range(0.01f, 2f)]
    public float lightStepSize = 0.2f;
    [Range(0, 5f)]
    public int downsamplingItterations = 2;

    [Header("Noise Parameters")]
    [SerializeField, Range(64, 256)]
    protected int textureResolution = 64;
    [SerializeField, Range(1, 10)]
    protected int worleyTiling = 2;
    [SerializeField, Range(5, 20)]
    protected int noiseOffsetTiling = 10;
    [SerializeField, Range(0, 1)]
    protected float noiseMix = 0.585f;
    [SerializeField, Range(1f, 3)]
    protected float minDistMultiplier = 2.35f;

    private int currentTextureResolution = 0;
    private float currentMinDistMultiplier = 0;

    private float currentWorleyTiling = 0;
    private float currentNoiseOffsetTiling = 0;
    private float currentNoiseMix = 0;

    [Header("Volumetric Parameters")]
    public bool animate = false;
    public Vector3 animationDir = new Vector3(0.15f, 0, 0.25f);
    [Min(0f)]
    public float absorptionMultipier = 0f;
    [Min(0f)]
    public Vector3 absorptionCoef = Vector3.one;
    [Min(0f)]
    public float scatteringMultipier = 2f;
    [Min(0f)]
    public Vector3 scatteringCoef = new Vector3(0.7f, 1f, 1.7f);
    [Min(0f)]
    public float volumeDensity = 1f;
    public enum PhaseFunctions
    {
        Isotropic,
        HenyeyGreenstein,
        Rayleigh,
        Schlick
    }
    public PhaseFunctions phase = PhaseFunctions.HenyeyGreenstein;
    [Range(-1f, 1f)]
    public float asymmetryFactor = 0.154f;
    [Range(0f, 1f)]
    public float densityFalloff = 0.8f;
    [Min(0f)]
    public float multiScatteredLightMultiplier = 1.55f;
    [Range(0f, 1f)]
    public float powderPower = 1f;
    [Min(1f)]
    public float powderExponent = 3f;
    public enum AmbientSettings
    {
        Regular,
        ApplyPowder
    }
    public AmbientSettings ambientSetting = AmbientSettings.Regular;
    [Range(0f, 1f)]
    public float ambientPower = 0.365f;

    [SerializeField, HideInInspector]
    protected ComputeShader volumetricCompute;
    private RenderTexture computeColor = null;
    private RenderTexture computeTransmittance = null;
    private RenderTexture computeColor_FullRez = null;
    private RenderTexture computeTransmittance_FullRez = null;

    [SerializeField, HideInInspector]
    protected Shader volumetricCompositeShader;
    private Material volumetricCompositeMaterial;

    [SerializeField, HideInInspector]
    protected ComputeShader noiseCompute;
    [SerializeField, HideInInspector]
    protected Texture noiseOffsetTex;
    private RenderTexture noiseTexture;

    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
        if (!IsSetup())
        {
            Graphics.Blit(_source, _destination);
            return;
        }
        if (!IsNoiseTextureReady())
        {
            Create3DRenderTexture(ref noiseTexture,textureResolution);
        }
        if (!IsNoise3DReady())
        {
            currentTextureResolution = textureResolution;
            currentWorleyTiling = worleyTiling;
            currentNoiseOffsetTiling = noiseOffsetTiling;
            currentNoiseMix = noiseMix;
            currentMinDistMultiplier = minDistMultiplier;

            DispatchNoiseCompute();
        }


        ////////////////////////////
        int width = _source.width;
        int height = _source.height;
        CreateTempRT(ref computeColor_FullRez, width, height);
        CreateTempRT(ref computeTransmittance_FullRez, width, height);

        int computeWidth = width;
        int computeHeight = height;
        for (int i = 0; i < downsamplingItterations; i++)
        {
            if ((computeHeight / 2) % 2.0f != 0)
                break;

            computeWidth /= 2;
            computeHeight /= 2;
        }

        CreateTempRT(ref computeColor, computeWidth, computeHeight);
        CreateTempRT(ref computeTransmittance, computeWidth, computeHeight);

        DispatchVolumetricCompute(computeWidth, computeHeight);

        Graphics.Blit(computeColor, computeColor_FullRez);
        Graphics.Blit(computeTransmittance, computeTransmittance_FullRez);

        RenderTexture.ReleaseTemporary(computeColor);
        RenderTexture.ReleaseTemporary(computeTransmittance);

        volumetricCompositeMaterial.SetTexture("_VolumetricColor", computeColor_FullRez);
        volumetricCompositeMaterial.SetTexture("_VolumetricTransmittance", computeTransmittance_FullRez);

        Graphics.Blit(_source, _destination, volumetricCompositeMaterial);

        RenderTexture.ReleaseTemporary(computeColor_FullRez);
        RenderTexture.ReleaseTemporary(computeTransmittance_FullRez);
    }

    #region Checking Bools
    bool IsSetup()
    {
        if (!volumetricCompute)
            return false;
        if (!sun)
            return false;
        if (!noiseCompute)
            return false;
        if (!BB_Rendering.ShaderMaterialReady(volumetricCompositeShader, ref volumetricCompositeMaterial))
            return false;

        return true;
    }
    bool IsNoiseTextureReady()
    {
        if (noiseTexture == null)
            return false;
        if (currentTextureResolution != textureResolution)
            return false;

        return true;
    }
    bool IsNoise3DReady()
    {
        if (currentTextureResolution != textureResolution)
            return false; 
        if (currentWorleyTiling != worleyTiling)
            return false;
        if (currentMinDistMultiplier != minDistMultiplier)
            return false;
        if (currentNoiseOffsetTiling != noiseOffsetTiling)
            return false;
        if (currentNoiseMix != noiseMix)
            return false;

        return true;
    }

    #endregion

    #region Noise Compute Stuff
    void Create3DRenderTexture(ref RenderTexture _renderTexture, int _textureSize)
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            _renderTexture = null;
        }

        _renderTexture = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.RFloat);

        _renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        _renderTexture.volumeDepth = _textureSize;

        _renderTexture.enableRandomWrite = true;
        _renderTexture.wrapMode = TextureWrapMode.Repeat;

        _renderTexture.Create();
    }
    void DispatchNoiseCompute()
    {
        int kernelIndex = noiseCompute.FindKernel("CSGetWorley");

        noiseCompute.SetTexture(kernelIndex, "Result", noiseTexture);
        noiseCompute.SetFloat("_MinDistMultiplier", currentMinDistMultiplier);

        noiseCompute.SetFloat("_WorleyTiling", currentWorleyTiling);
        noiseCompute.SetFloat("_NoiseOffsetTiling", currentNoiseOffsetTiling);
        noiseCompute.SetFloat("_NoiseMix", currentNoiseMix);

        noiseCompute.SetTexture(kernelIndex, "_NoiseOffsetTex", noiseOffsetTex);

        noiseCompute.SetFloat("_Resolution", currentTextureResolution);

        int threadsCount = Mathf.CeilToInt(currentTextureResolution / 4f);
        noiseCompute.Dispatch(kernelIndex, threadsCount, threadsCount, threadsCount);
    }

    #endregion
    void CreateTempRT(ref RenderTexture _renderTexture, int _width, int _height)
    {
        if (_renderTexture != null)
        {
            RenderTexture.ReleaseTemporary(_renderTexture);
        }

        _renderTexture = RenderTexture.GetTemporary(_width, _height, 0, RenderTextureFormat.ARGBFloat);
        _renderTexture.enableRandomWrite = true;
    }
    void SetPhaseKeyword()
    {
        volumetricCompute.DisableKeyword("PHASE_HG");
        volumetricCompute.DisableKeyword("PHASE_REYLEIGH");
        volumetricCompute.DisableKeyword("PHASE_SCHLICK");
        switch (phase)
        {
            case PhaseFunctions.HenyeyGreenstein:
                volumetricCompute.EnableKeyword("PHASE_HG");
                break;
            case PhaseFunctions.Rayleigh:
                volumetricCompute.EnableKeyword("PHASE_REYLEIGH");
                break;
            case PhaseFunctions.Schlick:
                volumetricCompute.EnableKeyword("PHASE_SCHLICK");
                break;
        }
    }
    void SetAmbientKeyword()
    {
        volumetricCompute.DisableKeyword("APPLY_POWDER_TO_AMBIENT");
        switch (ambientSetting)
        {
            case AmbientSettings.ApplyPowder:
                volumetricCompute.EnableKeyword("APPLY_POWDER_TO_AMBIENT");
                break;
        }
    }
    void DispatchVolumetricCompute(int _width, int _height)
    {
        SetPhaseKeyword();
        SetAmbientKeyword();

        int kernelIndex = volumetricCompute.FindKernel("CS_RaymarchVolumetric");

        volumetricCompute.SetTexture(kernelIndex, "VolumetricColor", computeColor);
        volumetricCompute.SetTexture(kernelIndex, "VolumetricTransmittance", computeTransmittance);

        volumetricCompute.SetVector("_ScreenResolution", new Vector2(_width, _height));
        volumetricCompute.SetVector("_WorldSpaceCameraPos", Camera.main.transform.position);
        
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
        Matrix4x4 viewProjMatrix = projMatrix * Camera.main.worldToCameraMatrix;
        volumetricCompute.SetMatrix("_CameraProjectionToWorld", Matrix4x4.Inverse(viewProjMatrix));
        volumetricCompute.SetTexture(kernelIndex, "_CameraDepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));

        volumetricCompute.SetVector("_SunDirection", sun.transform.forward);
        volumetricCompute.SetVector("_SunColor", sun.color);

        volumetricCompute.SetVector("_SpherePosition", spherePosition);
        volumetricCompute.SetFloat("_SphereRadius", sphereRadius);

        volumetricCompute.SetFloat("_StepSize", stepSize);
        volumetricCompute.SetFloat("_LightStepSize", lightStepSize);

        volumetricCompute.SetTexture(kernelIndex, "_NoiseTex", noiseTexture);
        
        volumetricCompute.SetVector("_AbsorptionCoef", absorptionCoef * absorptionMultipier);
        volumetricCompute.SetVector("_ScatteringCoef", scatteringCoef * scatteringMultipier);
        
        volumetricCompute.SetFloat("_VolumeDensity", volumeDensity);
        volumetricCompute.SetFloat("_AsymmetryFactor", asymmetryFactor);
        volumetricCompute.SetFloat("_DensityFalloff", densityFalloff);
        
        volumetricCompute.SetFloat("_MultiScatteredLightMultiplier", multiScatteredLightMultiplier);
        volumetricCompute.SetFloat("_PowderPower", powderPower);
        volumetricCompute.SetFloat("_PowderExponent", powderExponent);
        
        volumetricCompute.SetFloat("_AmbientPower", ambientPower);

        volumetricCompute.SetTexture(kernelIndex, "_Skybox", skybox.GetTexture("_Tex"));

        Vector3 finalAnimDir = animate ? animationDir : Vector3.zero;
        volumetricCompute.SetVector("_AnimationDir", finalAnimDir);
        volumetricCompute.SetFloat("_FrameTime", Time.time);

        int threadsCountX = Mathf.CeilToInt(_width / 8f);
        int threadsCountY = Mathf.CeilToInt(_height / 8f);
        volumetricCompute.Dispatch(kernelIndex, threadsCountX, threadsCountY, 1);
    }
}
