Shader "hby/GeometryGrassShader"
{
    Properties
    {
        _GrassHeight("GrassHeight", float) = 1.0
        [Header(Wind)]
        _WindAIntensity("WindAIntensity", float) = 1.77
        _WindAFrequency("WindAFrequency", float) = 4
        _WindATiling("WindATiling", Vector) = (0.1,0.1,0)
        _WindAWrap("WindAWrap", Vector) = (0.5,0.5,0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "GrassCalulate.hlsl"

            #pragma target 4.0

            struct vertextdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
                float3 color : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<uint> _VisibleIndex;
            float _GrassHeight;
            float _WindAIntensity;
            float _WindAFrequency;
            float2 _WindATiling;
            float2 _WindAWrap;
            CBUFFER_END

            float4 _CenterPosition;
            float m_MapWidth;
            float m_MapHeight;

            TEXTURE2D(_ColorMapTexture); SAMPLER(sampler_ColorMapTexture); half4 _ColorMapTexture_ST;
            TEXTURE2D(_HeightMapTexture); SAMPLER(sampler_HeightMapTexture); half4 _HeightMapTexture_ST;

            half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half positionOSY)
            {
                half3 H = normalize(light.direction + V);

                //direct diffuse 
                half directDiffuse = dot(N, light.direction) * 0.5 + 0.5; //half lambert, to fake grass SSS

                //direct specular
                float directSpecular = saturate(dot(N, H));
                //pow(directSpecular,8)
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                //directSpecular *= directSpecular; //enable this line = change to pow(directSpecular,16)

                //add direct directSpecular to result
                directSpecular *= 0.1 * positionOSY;//only apply directSpecular to grass's top area, to simulate grass AO

                half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
                half3 result = (albedo * directDiffuse + directSpecular) * lighting;
                return result;
            }

            v2f vert (vertextdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                int index = _VisibleIndex[instanceID];
                float3 grassUnitPos = _Positions[index] + _CenterPosition.xyz;
                grassUnitPos = CalulatePosition(grassUnitPos);
                half4 baseColor = SAMPLE_TEXTURE2D_LOD(_ColorMapTexture, sampler_ColorMapTexture, float2(grassUnitPos.x / m_MapWidth, grassUnitPos.z / m_MapHeight), 0);
                half4 heightBuffer = SAMPLE_TEXTURE2D_LOD(_HeightMapTexture, sampler_HeightMapTexture, float2(grassUnitPos.x / m_MapWidth, grassUnitPos.z / m_MapHeight), 0);

                grassUnitPos.y += CalulateHeight(heightBuffer);
                //rotation(make grass LookAt() camera just like a billboard)
                //=========================================
                float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;//UNITY_MATRIX_V[0].xyz == world space camera Right unit vector
                float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz;//UNITY_MATRIX_V[1].xyz == world space camera Up unit vector
                float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;//UNITY_MATRIX_V[2].xyz == -1 * world space camera Forward unit vector

                //Expand Billboard (billboard Left+right)
                float3 positionOS = v.vertex.x * cameraTransformRightWS * 0.5f * (sin(grassUnitPos.x * 95.4643 + grassUnitPos.z) * 0.45 + 0.55);

                //Expand Billboard (billboard Up)
                float offset = 0.7f + cos(tan(grassUnitPos.x * grassUnitPos.z)) * 0.3f;
                positionOS += v.vertex.y * (float3(0, 1, 0) * _GrassHeight * offset + CalulateRightWeight(cameraTransformRightWS, grassUnitPos.x, grassUnitPos.z) * offset);//(cameraTransformRightWS * cos(grassUnitPos.x * 275.0f + grassUnitPos.z * 574.0f)));//smoothstep(-4.5f, 4.5f, tan((grassUnitPos.x * 3.7f + grassUnitPos.z * 7.5f) * 128.0f))));


                // cal camera distance scale
                float3 viewWS = _WorldSpaceCameraPos - grassUnitPos;
                float ViewWSLength = length(viewWS);
                positionOS += cameraTransformRightWS * v.vertex.x * max(0, ViewWSLength * 0.0225);

                //float3 posWS = TransformObjectToWorld(positionOS);//v.vertex.xyz);
                //posWS += _Positions[_VisibleIndex[instanceID]];
                float3 posWS = positionOS + grassUnitPos;

                // Wind
                float wind = 0;
                wind += (sin(_Time.y * _WindAFrequency + grassUnitPos.x * _WindATiling.x + grassUnitPos.z * _WindATiling.y) * _WindAWrap.x + _WindAWrap.y) * _WindAIntensity;
                wind *= v.vertex.y;
                wind *= offset;
                float3 windOffset = cameraTransformRightWS * wind; //swing using billboard left right direction
                posWS.xyz += windOffset;

                o.posCS = TransformWorldToHClip(posWS);

                Light mainLight;
#if _MAIN_LIGHT_SHADOWS
                mainLight = GetMainLight(TransformWorldToShadowCoord(posWS));
#else
                mainLight = GetMainLight();
#endif
                half3 randomAddToN = (0.15f * sin(grassUnitPos.x * 82.32523 + grassUnitPos.z) + wind * -0.25f) * cameraTransformRightWS;//random normal per grass 
                //default grass's normal is pointing 100% upward in world space, it is an important but simple grass normal trick
                //-apply random to normal else lighting is too uniform
                //-apply cameraTransformForwardWS to normal because grass is billboard
                half3 N = normalize(half3(0, 1, 0) + randomAddToN - cameraTransformForwardWS * 0.5);

                half3 V = viewWS / ViewWSLength;

                //  tex2Dlod(sampler_BaseColorTexture, float4(TRANSFORM_TEX(posWS.xz, _BaseColorTexture), 0, 0))* _Color;//sample mip 0 only
                half3 albedo = lerp(baseColor.xyz * 0.25f, baseColor.xyz, v.vertex.y);//lerp(_Colors[_VisibleIndex[instanceID]].xyz * 0.5f, _Colors[_VisibleIndex[instanceID]].xyz, v.vertex.y); //lerp(_GroundColor, _Color, v.vertex.y);// lerp(_GroundColor, baseColor, v.vertex.y);

                //indirect
                half3 lightingResult = SampleSH(0) * albedo;

                //main direct light
                lightingResult += ApplySingleDirectLight(mainLight, N, V, albedo, positionOS.y);

                // Additional lights loop
#if _ADDITIONAL_LIGHTS
                // Returns the amount of lights affecting the object being renderer.
                // These lights are culled per-object in the forward renderer
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; ++i)
                {
                    // Similar to GetMainLight, but it takes a for-loop index. This figures out the
                    // per-object light index and samples the light buffer accordingly to initialized the
                    // Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
                    Light light = GetAdditionalLight(i, posWS);

                    // Same functions used to shade the main light.
                    lightingResult += ApplySingleDirectLight(light, N, V, albedo, positionOS.y);
                }
#endif


                float fogFactor = ComputeFogFactor(o.posCS.z);
                o.color = MixFog(lightingResult, fogFactor);// _Colors[instanceID];
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // sample the texture
                //half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
            // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
            // with a custom one.
                //col.xyz *= MixFog(col.xyz, i.PosWSAndFog.w);
                //return col;
                return half4(i.color, 1);
            }
            ENDHLSL
        }


        Pass
        {
            Tags { "LightMode" = "DepthOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "GrassCalulate.hlsl"

            #pragma target 4.0

            struct vertextdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<uint> _VisibleIndex;
            float _GrassHeight;
            float _WindAIntensity;
            float _WindAFrequency;
            float2 _WindATiling;
            float2 _WindAWrap;
            CBUFFER_END

            float4 _CenterPosition;
            float m_MapWidth;
            float m_MapHeight;

            TEXTURE2D(_MapTexture); SAMPLER(sampler_MapTexture); half4 _MapTexture_ST;
            TEXTURE2D(_HeightMapTexture); SAMPLER(sampler_HeightMapTexture); half4 _HeightMapTexture_ST;

            v2f vert(vertextdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                int index = _VisibleIndex[instanceID];
                float3 grassUnitPos = _Positions[index] + _CenterPosition.xyz;
                grassUnitPos = CalulatePosition(grassUnitPos);
                half4 heightBuffer = SAMPLE_TEXTURE2D_LOD(_HeightMapTexture, sampler_HeightMapTexture, float2(grassUnitPos.x / m_MapWidth, grassUnitPos.z / m_MapHeight), 0);
                grassUnitPos.y += CalulateHeight(heightBuffer);
                //rotation(make grass LookAt() camera just like a billboard)
                //=========================================
                float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;//UNITY_MATRIX_V[0].xyz == world space camera Right unit vector
                float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz;//UNITY_MATRIX_V[1].xyz == world space camera Up unit vector
                float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;//UNITY_MATRIX_V[2].xyz == -1 * world space camera Forward unit vector

                //Expand Billboard (billboard Left+right)
                float3 positionOS = v.vertex.x * cameraTransformRightWS * 0.5f * (sin(grassUnitPos.x * 95.4643 + grassUnitPos.z) * 0.45 + 0.55);

                //Expand Billboard (billboard Up)
                float offset = 0.7f + cos(tan(grassUnitPos.x * grassUnitPos.z)) * 0.3f;
                positionOS += v.vertex.y * (float3(0, 1, 0) * _GrassHeight * offset + CalulateRightWeight(cameraTransformRightWS, grassUnitPos.x, grassUnitPos.z) * offset);//(cameraTransformRightWS * cos(grassUnitPos.x * 275.0f + grassUnitPos.z * 574.0f)));//smoothstep(-4.5f, 4.5f, tan((grassUnitPos.x * 3.7f + grassUnitPos.z * 7.5f) * 128.0f))));


                // cal camera distance scale
                float3 viewWS = _WorldSpaceCameraPos - grassUnitPos;
                float ViewWSLength = length(viewWS);
                positionOS += cameraTransformRightWS * v.vertex.x * max(0, ViewWSLength * 0.0225);

                //float3 posWS = TransformObjectToWorld(positionOS);//v.vertex.xyz);
                //posWS += _Positions[_VisibleIndex[instanceID]];
                float3 posWS = positionOS + grassUnitPos;

                // Wind
                float wind = 0;
                wind += (sin(_Time.y * _WindAFrequency + grassUnitPos.x * _WindATiling.x + grassUnitPos.z * _WindATiling.y) * _WindAWrap.x + _WindAWrap.y) * _WindAIntensity;
                wind *= v.vertex.y;
                wind *= offset;
                float3 windOffset = cameraTransformRightWS * wind; //swing using billboard left right direction
                posWS.xyz += windOffset;

                o.posCS = TransformWorldToHClip(posWS);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(1,1,1,1);
            }
            ENDHLSL
        }
    }
}
