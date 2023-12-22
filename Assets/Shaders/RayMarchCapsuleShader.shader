Shader "Hidden/RayMarchCapsuleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "DistanceFunctions.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4x4 _CamFrustrum, _CamToWorld;
            float _maxDistance;
            float4 _sphere1, _box1, _sphere2;
            float3 _LightDir, _LightCol;
            fixed4 _mainColor;
            float3 _modInterval;
            float _box1Round;
            float _boxSphereSmooth;
            float _sphereInteresectSmooth;
            float _LightIntensity;
            float2 _ShadowDistance;
            float _ShadowIntensity, _ShadowPenumbra;
            
            float _AmbientOcclusionStepSize;
            int _AmbientOcclusionIterations;
            float _AmbientOcclusionIntensity;

            int _MaxIterations;
            float _Accuracy;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ray : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                half index = v.vertex.z;
                v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
    
                o.ray = _CamFrustrum[(int)index].xyz;
                o.ray /= abs(o.ray.z);
                o.ray = mul(_CamToWorld, o.ray);
                return o;
            }
            float BoxSphere(float3 p) {
                float Sphere1 = sdSphere(p - _sphere1.xyz, _sphere1.w);
                float Box1 = sdRoundBox(p - _box1.xyz, _box1.www, _box1Round);
                float combine1 = opSS(Sphere1, Box1, _boxSphereSmooth);
    
                float Sphere2 = sdSphere(p - _sphere2.xyz, _sphere2.w);
                float combine2 = opIS(Sphere2, combine1, _sphereInteresectSmooth);
                return combine2;
            }

            float distanceField(float3 p) {
                float ground = sdPlane(p, float4(0, 1, 0, 0));
                float boxSphere1 = BoxSphere(p);
    
                return opU(ground, boxSphere1);
            }
            
            float3 getNormal(float3 p) {
                const float2 offset = float2(0.001, 0.0);
                float3 n = float3(
                    distanceField(p + offset.xyy) - distanceField(p - offset.xyy),
                    distanceField(p + offset.yxy) - distanceField(p - offset.yxy),
                    distanceField(p + offset.yyx) - distanceField(p - offset.yyx)
                    );
                return normalize(n);
            }

            float hardShadow(float3 ro, float3 rd, float mint, float maxt) {
                for (float t = mint; t < maxt;) {
                    float h = distanceField(ro + rd * t);
                    if (h < 0.001) {
                        return 0.0;
                    }
                    t += h;
                }
                return 1.0;
            }

            float softShadow(float3 ro, float3 rd, float mint, float maxt, float k) {
                float result = 1.0;
    
                for (float t = mint; t < maxt;) {
                    float h = distanceField(ro + rd * t);
                    if (h < 0.001) {
                        return 0.0;
                    }
                    result = min(result, k*h/t);
                    t += h;
                }
                return result;
            }
            float AmbientOcclusion(float3 p, float3 n) {
                float step = _AmbientOcclusionStepSize;
                float ao = 0.0;
                float intensity = _AmbientOcclusionIntensity;
                float dist;
                int max_iterations = _AmbientOcclusionIterations;
                for(int i = 1; i <= max_iterations; i++) {
                    dist = step * i;
                    ao += max(0.0, (dist - distanceField(p + n * dist)) / dist);
                }
                return (1 - ao * intensity);
            }

            float3 Shading(float3 p, float3 n) {
                float3 result;
                // Diffuse Color
                float3 color = _mainColor.rgb;
    
                // Directional light
                float3 light = (_LightCol * dot(-_LightDir, n) * 0.5 + 0.5) * _LightIntensity;
    
                // Shadows
                float shadow = softShadow(p, -_LightDir, _ShadowDistance.x, _ShadowDistance.y, _ShadowPenumbra) * 0.5 + 0.5;
                shadow = max(0.0, pow(shadow, _ShadowIntensity));
                // Ambient Occlusion
                float ao = AmbientOcclusion(p, n);
                result = color * light * shadow * ao;
                return result;
            }

            fixed4 raymarching(float3 ro, float3 rd, float depth) {
                fixed4 result = fixed4(rd, 0);
                const int max_iteration = _MaxIterations;
                float t = 0.0;
    
                for(int i = 0; i < max_iteration; i++) {
                    if(t > _maxDistance || t >= depth) {
                        // Environment
                        result = fixed4(rd, 0);
                        break;
                    }
                    float3 p = ro + rd * t;
                    float d = distanceField(p);
                    if (d < _Accuracy) {
                        // Hit!
                        float3 n = getNormal(p);
                        float3 s = Shading(p, n);
                        result = fixed4(s, 1);      
                        break;
                    }
                    t += d;
                }
                return result;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 col = tex2D(_MainTex, i.uv);
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);
                depth *= length(i.ray);
    
                float3 rayDirection = normalize(i.ray.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos;
                fixed4 result = raymarching(rayOrigin, rayDirection, depth);
                return fixed4(col * (1.0 - result.w) + result.xyz * result.w, 1.0);
            }
            ENDCG
        }
    }
}
