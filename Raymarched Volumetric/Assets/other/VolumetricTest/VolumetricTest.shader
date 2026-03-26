Shader "_BB/VolumetricTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex, _CameraDepthTexture;
            float _Half_FOV_Tan;

            float _SphereRadius;
            float _DensityFalloff;
            float _StepSize, _LightStepSize;

            float3 _ContainerPosition, _ContainerLocalScale;

            float4 _ExtinctionColor, _ScateringColor, _LightColor0;
            float3 _AbsorptionCoef, _ScatteringCoef;
            float _VolumeDensity, _AsymmetryFactor;
            
            Texture3D<float> _VolumeTex;
            SamplerState sampler_VolumeTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float3 ConstructRay(v2f i)
            {
                float3 ray;

                float2 ndc = i.uv * 2 - 1;
                float screenAspect = _ScreenParams.x / _ScreenParams.y;

                ray.x = ndc.x * _Half_FOV_Tan;
                ray.x *= screenAspect;
                ray.y = ndc.y * _Half_FOV_Tan;
                ray.z = 1;

                return ray;
            }
            float4 GetViewPos(v2f i)
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                depth = Linear01Depth(depth) * _ProjectionParams.z;

                float3 ray = ConstructRay(i);
                return float4(ray * depth, 1); 
            }
            float3 GetWorldPos(v2f i)
            {
                float4 viewPos = GetViewPos(i);
                return mul(unity_CameraToWorld, viewPos);
            }

            // Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
            float2 RayBoxDst(float3 _position, float3 _localScale, float3 _rayOrigin, float3 _rayDir) 
            {
                // Adapted from: http://jcgt.org/published/0007/03/04/

                float3 halfLocalScale = _localScale / 2;
                float3 boundsMin = _position - halfLocalScale;
                float3 boundsMax = _position + halfLocalScale;

                float3 oneOverRayDir = 1 / _rayDir;

                float3 t0 = (boundsMin - _rayOrigin) * oneOverRayDir;
                float3 t1 = (boundsMax - _rayOrigin) * oneOverRayDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(tmax.x, min(tmax.y, tmax.z));

                // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
                // dstA is dst to nearest intersection, dstB dst to far intersection

                // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
                // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

                // CASE 3: ray misses box (dstA > dstB)

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }     
            float2 SolveQuadratic(float a, float b, float c)
            {
                float D = (b * b) - (4 * a * c);
                
                if(D <= 0)
                    return 0;

                float t1 = (-b + sqrt(D)) / (2 * a);
                float t2 = (-b - sqrt(D)) / (2 * a);

                return float2(t1, t2);
            }
            float2 RaySphereDst(float3 _position, float _radius, float3 _rayOrigin, float3 _rayDir)
            {
                float3 center = _position;
                float radius = _radius;


                float3 vecToOrigin = _rayOrigin - center;
                
                // normally dot(_rayDir, _rayDir), but rayDir is normalized
                float a = 1;
                float b = 2 * (dot(_rayDir, vecToOrigin));
                float c = dot(vecToOrigin, vecToOrigin) - (radius * radius);

                float2 solutions = SolveQuadratic(a,b,c);

                float dstA = min(solutions.x, solutions.y);
                float dstB = max(solutions.x, solutions.y);

                // CASE 1: ray intersects sphere from outside (0 <= dstA <= dstB)
                // dstA is dst to nearest intersection, dstB dst to far intersection

                // CASE 2: ray intersects sphere from inside (dstA < 0 < dstB)
                // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

                // CASE 3: ray misses sphere (dstA == dstB == 0)

                float dstToSphere = max(0, dstA);
                float dstInsideSphere = max(0, dstB - dstToSphere);
                return float2(dstToSphere, dstInsideSphere);
            }

            float GetLightTravelledDist(float3 _samplePoint, float3 _dirToLight) 
            {
                return RaySphereDst(_ContainerPosition, _SphereRadius, _samplePoint, _dirToLight).y;
            }
            
            float Isotropic_Scatering()
            {
                return 1.0 / (4.0 * UNITY_PI);
            }
            float Henyey_Greenstein(float g, float _cosTheta)
            {
                float numen = 1.0 - (g * g);
                float denom = 1.0 + (g * g) - (2.0 * g * _cosTheta);
                denom = denom * sqrt(denom);

                //return Isotropic_Scatering() * (numen / denom);
                return (numen / denom);
            }

            float GetDensity(float3 _origin, float3 _samplePoint)
            {       
                float noiseValue = _VolumeTex.Sample(sampler_VolumeTex, (_samplePoint + 2) * 0.25);

                noiseValue = saturate(noiseValue);

                //noiseValue = 0.5;
                //return noiseValue;

                float3 vp = _samplePoint - _origin;
                float dist = min(1.0f, length(vp / _SphereRadius));
                float falloff = smoothstep(_DensityFalloff, 1, dist);

                return noiseValue * (1 - falloff) * _VolumeDensity;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                //src = _ScateringColor;

                // early terminations
                if(_VolumeDensity == 0)
                {
                    return src;
                }
                float3 extinctionCoef = _AbsorptionCoef + _ScatteringCoef;
                if(max(max(extinctionCoef.x, extinctionCoef.y), extinctionCoef.z) == 0)
                {
                    return src;
                }

                // checking volume intersection
                float3 worldPos = GetWorldPos(i);
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 worldSpaceRay = worldPos - rayOrigin;
                float3 rayDir = normalize(worldSpaceRay);

                float2 rayVolumeInfo = RaySphereDst(_ContainerPosition, _SphereRadius, rayOrigin, rayDir);
                float dstToVolume = rayVolumeInfo.x;
                float dstInsideVolume = rayVolumeInfo.y;

                bool volumeHit = dstInsideVolume > 0 && dstToVolume < length(worldSpaceRay);
                if(!volumeHit)
                {
                    return src;
                }

                // raymarching
                float stepSize = _StepSize;
                float lightStepSize = _LightStepSize;
                float3 dirToLight = _WorldSpaceLightPos0.xyz;

                int steps = ceil(dstInsideVolume / stepSize);
                float3 volumeEntryPos = rayOrigin + rayDir * dstToVolume;

                float3 transmittance = 1;
                float3 output = 0;
                [loop]
                for(int s = 0; s < steps; s++)
                {
                    float distTravelled = stepSize * (s + 0.5);
                    float3 samplePoint = volumeEntryPos + rayDir * distTravelled;

                    float sampleDensity = GetDensity(_ContainerPosition, samplePoint);
                    transmittance *= exp(-stepSize * extinctionCoef * sampleDensity);

                    // light ray raymarching
                    float lightTravelledDist = GetLightTravelledDist(samplePoint, dirToLight); 
                    if(sampleDensity > 0 && lightTravelledDist > 0)
                    {
                        // we need direction to Camera, which is -rayDir
                        float cosAngle = dot(-rayDir, dirToLight);
                        float phase = Henyey_Greenstein(_AsymmetryFactor, cosAngle);

                        int lightSteps = ceil(lightTravelledDist / lightStepSize);
                        float tau = 0;
                        [loop]
                        for(int l = 0; l < lightSteps; l++)
                        {                            
                            float lightDistTravelled = lightStepSize * (l + 0.5f);
                            float3 lightSamplePoint = samplePoint + dirToLight * lightDistTravelled;
                            tau += GetDensity(_ContainerPosition, lightSamplePoint);
                        }

                        float3 lightAttenuation = exp(-tau * lightStepSize * extinctionCoef);         
                        float3 scateredLight = phase * lightAttenuation * _LightColor0.rgb;
                        
                        output += transmittance
                                * scateredLight
                                * _ScatteringCoef
                                * stepSize
                                * sampleDensity;
                    }
                    
                    if(min(min(transmittance.x, transmittance.y), transmittance.z) < 0.001)
                        break;
                }

                float3 finalOutput = src * transmittance + output;
                return float4(finalOutput, 1);
            }
            ENDCG
        }
    }
}
