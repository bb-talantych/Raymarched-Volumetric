using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CameraCommon;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class VolumetricComputeEffect : MonoBehaviour
{
    public Light sun;

    public Vector3 spherePosition = new Vector3(0, 0, -7);
    [Min(0.1f)]
    public float sphereRadius = 3f;
    [Range(0, 5f)]
    public int downsamplingItterations = 0;
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
    public float asymmetryFactor = 0;

    [SerializeField]
    protected ComputeShader volumetricCompute;
    private RenderTexture computeColor = null;
    private RenderTexture computeTransmittance = null;
    private RenderTexture computeColor_FullRez = null;
    private RenderTexture computeTransmittance_FullRez = null;
    [SerializeField]
    protected Shader volumetricCompositeShader;
    private Material volumetricCompositeMaterial;

    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
        if (!volumetricCompute)
        {
            Graphics.Blit(_source, _destination);
            return;
        }
        if (!BB_Rendering.ShaderMaterialReady(volumetricCompositeShader, ref volumetricCompositeMaterial))
        {
            Graphics.Blit(_source, _destination);
            return;
        }
        if (!sun)
        {
            Graphics.Blit(_source, _destination);
            return;
        }

        ////////////////////////////
        int width = _source.width;
        int height = _source.height;
        CreateTempRT(ref computeColor_FullRez, width, height);
        CreateTempRT(ref computeTransmittance_FullRez, width, height);

        int computeWidth = width;
        int computeHeight = height;
        for(int i = 0; i < downsamplingItterations; i++)
        {
            computeWidth /= 2;
            computeHeight /= 2;
        }
        CreateTempRT(ref computeColor, computeWidth, computeHeight);
        CreateTempRT(ref computeTransmittance, computeWidth, computeHeight);

        DispatchComputeShader(computeWidth, computeHeight);

        Graphics.Blit(computeColor, computeColor_FullRez);
        Graphics.Blit(computeTransmittance, computeTransmittance_FullRez);

        volumetricCompositeMaterial.SetTexture("_VolumetricColor", computeColor_FullRez);
        volumetricCompositeMaterial.SetTexture("_VolumetricTransmittance", computeTransmittance_FullRez);

        Graphics.Blit(_source, _destination, volumetricCompositeMaterial);
    }

    void CreateTempRT(ref RenderTexture _renderTexture, int _width, int _height)
    {
        if (_renderTexture != null)
        {
            RenderTexture.ReleaseTemporary(_renderTexture);
        }

        _renderTexture = RenderTexture.GetTemporary(_width, _height, 0, RenderTextureFormat.ARGBFloat);
        _renderTexture.enableRandomWrite = true;
    }
    void DispatchComputeShader(int _width, int _height)
    {
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

        volumetricCompute.SetFloat("_DensityFalloff", densityFalloff);
        volumetricCompute.SetFloat("_StepSize", stepSize);
        volumetricCompute.SetFloat("_LightStepSize", lightStepSize);
        volumetricCompute.SetVector("_AbsorptionCoef", absorptionCoef * absorptionMultipier);
        volumetricCompute.SetVector("_ScatteringCoef", scatteringCoef * scatteringMultipier);
        volumetricCompute.SetFloat("_VolumeDensity", volumeDensity);
        volumetricCompute.SetFloat("_AsymmetryFactor", asymmetryFactor);

        int threadsCountX = Mathf.CeilToInt(_width / 8f);
        int threadsCountY = Mathf.CeilToInt(_height / 8f);
        volumetricCompute.Dispatch(kernelIndex, threadsCountX, threadsCountY, 1);
    }
}
