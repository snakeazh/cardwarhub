Shader "Custom/UI/Mobile_Particles_AlphaBlended"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        // --- 新增：UV 滚动速度控制 ---
        _Main_u_speed("Main_u_speed", Float) = 0
        _Main_v_speed("Main_v_speed", Float) = 0
        // --- UI Mask (Stencil) 支持所需属性 ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15 // 15 = RGBA

        // --- RectMask2D 支持所需属性 ---
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

        // --- Stencil 块：让标准 Mask 组件生效 ---
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
        ZTest [unity_GUIZTestMode] // 允许UI层级控制深度测试
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc" // 引入UI辅助函数库

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
                float4 worldPosition : TEXCOORD1; // 用于计算裁剪位置
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClipRect; // 由 RectMask2D 传入的裁剪区域
            // --- 声明变量 ---
            float _Main_u_speed;
            float _Main_v_speed;
            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // --- UV 滚动逻辑 ---
                // 1. 获取基础 UV 并应用 Tiling/Offset
                float2 baseUV = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // 2. 计算基于时间的偏移 ( _Time.y 是游戏运行的秒数 )
                float2 uvOffset = float2(_Main_u_speed, _Main_v_speed) * _Time.y;
                
                // 3. 应用偏移
                o.texcoord = baseUV + uvOffset;
                
                o.color = v.color;
                
                // --- RectMask2D 坐标计算 ---
                // RectMask2D 坐标计算
                // 强制转换为世界坐标，不再依赖宏判断
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 采样纹理并乘以顶点颜色 (粒子颜色)
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // --- RectMask2D 核心逻辑 ---
                // 判断当前像素的世界坐标是否在 ClipRect 矩形内
                // 如果在外部，alpha 会被乘以 0
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                
                // 如果 Alpha 极小，丢弃像素 (优化)
                clip(col.a - 0.001);

                return col;
            }
            ENDCG
        }
    }
}