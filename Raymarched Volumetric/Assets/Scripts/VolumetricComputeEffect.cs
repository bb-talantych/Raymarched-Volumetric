using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class VolumetricComputeEffect : MonoBehaviour
{
    public Light sun;

    public Color extinctionColor = new Color(1, 0, 0, 1);
    public Color scateringColor = new Color(1, 1, 1, 1);

    public Vector3 spherePosition = new Vector3(0, 0, - 10);
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
    protected ComputeShader volumetricCompute;
    private RenderTexture computeOutput = null;

    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
        if (!volumetricCompute)
        {
            Graphics.Blit(_source, _destination);
            return;
        }
        if(!sun)
        {
            Graphics.Blit(_source, _destination);
            return;
        }

        ////////////////////////////
        int width = _source.width;
        int height = _source.height;

        if (computeOutput == null)
        {
            CreateRenderTexture(ref computeOutput, width, height);
        }

        DispatchComputeShader(width, height);

        Graphics.Blit(computeOutput, _destination);
    }

    void CreateRenderTexture(ref RenderTexture _renderTexture, int _width, int _height)
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            _renderTexture = null;
        }

        _renderTexture = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGBFloat);
        _renderTexture.enableRandomWrite = true;

        _renderTexture.Create();
    }
    void DispatchComputeShader(int _width, int _height)
    {
        int kernelIndex = volumetricCompute.FindKernel("CS_RaymarchVolumetric");

        volumetricCompute.SetTexture(kernelIndex, "Result", computeOutput);
        volumetricCompute.SetVector("_SpherePosition", spherePosition);
        volumetricCompute.SetFloat("_SphereRadius", sphereRadius);
        volumetricCompute.SetVector("_ScreenResolution", new Vector2(_width, _height));
        volumetricCompute.SetTexture(kernelIndex, "_CameraDepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
        Matrix4x4 viewProjMatrix = projMatrix * Camera.main.worldToCameraMatrix;
        volumetricCompute.SetMatrix("_CameraProjectionToWorld", Matrix4x4.Inverse(viewProjMatrix));
        volumetricCompute.SetVector("_WorldSpaceCameraPos", Camera.main.transform.position);
        volumetricCompute.SetVector("_AbsorptionCoef", absorptionCoef * absorptionMultipier);
        volumetricCompute.SetVector("_ScatteringCoef", scatteringCoef * scatteringMultipier);
        volumetricCompute.SetFloat("_VolumeDensity", volumeDensity);
        volumetricCompute.SetFloat("_StepSize", stepSize);
        volumetricCompute.SetFloat("_LightStepSize", lightStepSize);
        volumetricCompute.SetFloat("_DensityFalloff", densityFalloff);
        volumetricCompute.SetFloat("_AsymmetryFactor", asymmetryFactor);
        volumetricCompute.SetVector("_SunDirection", sun.transform.forward);
        volumetricCompute.SetVector("_SunColor", sun.color);

        int threadsCountX = Mathf.CeilToInt(_width / 8f);
        int threadsCountY = Mathf.CeilToInt(_height / 8f);
        volumetricCompute.Dispatch(kernelIndex, threadsCountX, threadsCountY, 1);
    }
}
