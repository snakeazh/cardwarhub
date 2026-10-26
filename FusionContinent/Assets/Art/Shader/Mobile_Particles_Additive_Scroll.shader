Shader "Custom/UI/Mobile_Particles_Additive_Scroll"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        
        // UV 滚动速度
        _Main_u_speed("Main_u_speed", Float) = 0
        _Main_v_speed("Main_v_speed", Float) = 0

        // UI Mask (Stencil) 支持
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15 

        // RectMask2D 支持
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
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
        
        // --- 核心修改：改为叠加模式 ---
        // SrcAlpha: 使用源颜色的Alpha值作为因子
        // One: 目标颜色因子为1 (直接叠加，不减去背景色)
        Blend SrcAlpha One 
        
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClipRect;
            float _Main_u_speed;
            float _Main_v_speed;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // UV 滚动计算
                float2 baseUV = TRANSFORM_TEX(v.texcoord, _MainTex);
                float2 uvOffset = float2(_Main_u_speed, _Main_v_speed) * _Time.y;
                o.texcoord = baseUV + uvOffset;
                
                o.color = v.color;
                
                // RectMask2D 坐标计算
                // 强制转换为世界坐标，不再依赖宏判断
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                
                // RectMask2D 裁剪逻辑
                // 在叠加模式下，如果 alpha 变为 0，SrcAlpha * Color 就会变成 0，
                // 也就不会叠加任何颜色上去，从而实现"消失"的效果。
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                
                // 只有完全透明时才丢弃，避免叠加模式下的边缘黑边问题
                clip(col.a - 0.001);

                return col;
            }
            ENDCG
        }
    }
}