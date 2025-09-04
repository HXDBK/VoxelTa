Shader "UI/MetaballEffect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Threshold ("Threshold", Range(0.1, 1.0)) = 0.5
        _Smoothness ("Smoothness", Range(0.01, 0.5)) = 0.1
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 1.2
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Threshold;
            float _Smoothness;
            float _GlowIntensity;
            fixed4 _GlowColor;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            // 高斯模糊采样函数
            fixed4 GaussianBlur(sampler2D tex, float2 uv, float2 texelSize)
            {
                fixed4 color = fixed4(0, 0, 0, 0);
                
                // 简化的5x5高斯核
                float kernel[9] = {
                    0.0625, 0.125, 0.0625,
                    0.125, 0.25, 0.125,
                    0.0625, 0.125, 0.0625
                };
                
                int index = 0;
                for(int y = -1; y <= 1; y++)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y) * texelSize * 2.0;
                        color += tex2D(tex, uv + offset) * kernel[index];
                        index++;
                    }
                }
                
                return color;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // 获取纹理尺寸
                float2 texelSize = float2(1.0 / 512.0, 1.0 / 512.0); // 可以根据实际纹理尺寸调整
                
                // 原始采样
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                // 模糊采样用于融合效果
                half4 blurredColor = GaussianBlur(_MainTex, IN.texcoord, texelSize) * IN.color;
                
                // 计算alpha阈值
                float alpha = blurredColor.a;
                
                // 平滑阈值处理，创建融合边缘
                float smoothMin = _Threshold - _Smoothness;
                float smoothMax = _Threshold + _Smoothness;
                alpha = smoothstep(smoothMin, smoothMax, alpha);
                
                // 添加发光效果
                if(alpha > 0.01)
                {
                    float glowFactor = 1.0 - saturate((alpha - _Threshold) / _Smoothness);
                    color.rgb = lerp(color.rgb, _GlowColor.rgb * _GlowIntensity, glowFactor * 0.5);
                }
                
                // 应用最终alpha
                color.a = alpha;
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
    
    // 第二个Pass：用于增强融合效果
    SubShader
    {
        Tags
        {
            "Queue"="Transparent+1"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        
        Pass
        {
            Name "MetaballBlend"
            Blend SrcAlpha One
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert_blend
            #pragma fragment frag_blend
            
            #include "UnityCG.cginc"
            
            struct appdata_blend
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };
            
            struct v2f_blend
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Threshold;
            float _Smoothness;
            
            v2f_blend vert_blend(appdata_blend v)
            {
                v2f_blend o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag_blend(v2f_blend i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                
                // 增强边缘融合
                float edge = 1.0 - saturate(abs(col.a - _Threshold) / _Smoothness);
                col.rgb *= edge * 0.5;
                col.a *= edge * 0.3;
                
                return col;
            }
            ENDCG
        }
    }
}