Shader "Hidden/RayTracingShader"
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

            sampler2D _MainTex;
            
            float4x4 _CamToWorld;
            float4 _CamFrustrum[4];
            fixed4 _mainColor;
            float _maxDistance;
            float4 _sphere1;

            float _LightIntensity;
            float3 _LightDir;
            float4 _LightCol;

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

            float3 rayTraceSphere(float3 ro, float3 rd) {
                float r = _sphere1.w;
                float3 so = _sphere1.xyz - ro;
    
                // equation
                float a = dot(rd, rd);
                float b = -2.0 * dot(rd, so);
                float c = dot(so, so) - r * r;
    
                //discriminant
                float disc = b * b - 4.0 * a * c;
                
                float3 res = 0.0;
                if (disc < 0) {
                    return res;
                }
    
                res.x = (-b - sqrt(disc)) / (2.0 * a);
                res.y = (-b + sqrt(disc)) / (2.0 * a);
                res.z = disc;
                return res;
            }

            float3 Lighting(float3 p, float3 n) {
                // Diffuse Color
                float3 color = _mainColor.rgb;
    
                // Directional light
                float3 light = (_LightCol * dot(-_LightDir, n) * 0.5 + 0.5) * _LightIntensity;
    
                float3 result = color * light;
                return result;
            }

            bool hasSphere(float3 sphereIntersection) {
                return !(sphereIntersection.x == sphereIntersection.y 
                        && sphereIntersection.y == sphereIntersection.z 
                        && sphereIntersection.x == 0.0);
            }
            
            float3 getSphereNormal(float3 p) {
                return normalize(p - _sphere1.xyz);
            }

            fixed4 raytrace(float3 ro, float3 rd) {
                
                float r = _sphere1.w;
                
                float3 result = rayTraceSphere(ro, rd);
            
                fixed4 col = fixed4(rd, 0);
            
                if (hasSphere(result)) {
                    float3 p = ro + rd * result.x;
                    float3 n = getSphereNormal(p);
                    col = fixed4(Lighting(p, n), 1);
                }
    
                return col;
            }

            v2f vert(appdata v) {
                v2f o;
                half index = v.vertex.z;
                v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
    
                o.ray = _CamFrustrum[(int) index].xyz;
                o.ray = mul(_CamToWorld, o.ray);
                return o;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 rayDirection = normalize(i.ray.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos;
                
                fixed4 result = raytrace(rayOrigin, i.ray.xyz);
                
                return fixed4(col * (1.0 - result.w) + result.xyz * result.w, 1.0);
                
}
            ENDCG
        }
    }
}
