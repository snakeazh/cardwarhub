// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LTY/shader/NiuQu"
{
	Properties
	{
		_NiuQu_Tex("NiuQu_Tex", 2D) = "white" {}
		_NiuQuQiangDu("NiuQuQiangDu", Range( 0 , 3)) = 1
		_NiuQu_U_Speed("NiuQu_U_Speed", Float) = 0
		_NiuQu_V_Speed("NiuQu_V_Speed", Float) = 0
		_Mask_Tex("Mask_Tex", 2D) = "white" {}
		[Header(Refraction)]
		_ChromaticAberration("Chromatic Aberration", Range( 0 , 0.3)) = 0.1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" }
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile _ALPHAPREMULTIPLY_ON
		#pragma surface surf Lambert keepalpha finalcolor:RefractionF noshadow exclude_path:deferred noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float4 screenPos;
			float3 worldPos;
		};

		uniform sampler2D _NiuQu_Tex;
		uniform half _NiuQu_U_Speed;
		uniform half _NiuQu_V_Speed;
		uniform float4 _NiuQu_Tex_ST;
		uniform half _NiuQuQiangDu;
		uniform sampler2D _GrabTexture;
		uniform float _ChromaticAberration;
		uniform sampler2D _Mask_Tex;
		uniform half4 _Mask_Tex_ST;

		inline float4 Refraction( Input i, SurfaceOutput o, float indexOfRefraction, float chomaticAberration ) {
			float3 worldNormal = o.Normal;
			float4 screenPos = i.screenPos;
			#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
			#else
				float scale = 1.0;
			#endif
			float halfPosW = screenPos.w * 0.5;
			screenPos.y = ( screenPos.y - halfPosW ) * _ProjectionParams.x * scale + halfPosW;
			#if SHADER_API_D3D9 || SHADER_API_D3D11
				screenPos.w += 0.00000000001;
			#endif
			float2 projScreenPos = ( screenPos / screenPos.w ).xy;
			float3 worldViewDir = normalize( UnityWorldSpaceViewDir( i.worldPos ) );
			float3 refractionOffset = ( indexOfRefraction - 1.0 ) * mul( UNITY_MATRIX_V, float4( worldNormal, 0.0 ) ) * ( 1.0 - dot( worldNormal, worldViewDir ) );
			float2 cameraRefraction = float2( refractionOffset.x, refractionOffset.y );
			float4 redAlpha = tex2D( _GrabTexture, ( projScreenPos + cameraRefraction ) );
			float green = tex2D( _GrabTexture, ( projScreenPos + ( cameraRefraction * ( 1.0 - chomaticAberration ) ) ) ).g;
			float blue = tex2D( _GrabTexture, ( projScreenPos + ( cameraRefraction * ( 1.0 + chomaticAberration ) ) ) ).b;
			return float4( redAlpha.r, green, blue, redAlpha.a );
		}

		void RefractionF( Input i, SurfaceOutput o, inout half4 color )
		{
			#ifdef UNITY_PASS_FORWARDBASE
			float2 uv_Mask_Tex = i.uv_texcoord * _Mask_Tex_ST.xy + _Mask_Tex_ST.zw;
			half4 tex2DNode15 = tex2D( _Mask_Tex, uv_Mask_Tex );
			half3 desaturateInitialColor16 = tex2DNode15.rgb;
			half desaturateDot16 = dot( desaturateInitialColor16, float3( 0.299, 0.587, 0.114 ));
			half3 desaturateVar16 = lerp( desaturateInitialColor16, desaturateDot16.xxx, 1.0 );
			half temp_output_8_0 = ( i.vertexColor.a * _NiuQuQiangDu );
			half lerpResult10 = lerp( 1.0 , 1.4 , ( desaturateVar16 * tex2DNode15.a * temp_output_8_0 ).x);
			color.rgb = color.rgb + Refraction( i, o, lerpResult10, _ChromaticAberration ) * ( 1 - color.a );
			color.a = 1;
			#endif
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			o.Normal = float3(0,0,1);
			half2 appendResult20 = (half2(_NiuQu_U_Speed , _NiuQu_V_Speed));
			float2 uv_NiuQu_Tex = i.uv_texcoord * _NiuQu_Tex_ST.xy + _NiuQu_Tex_ST.zw;
			half2 panner22 = ( 1.0 * _Time.y * appendResult20 + uv_NiuQu_Tex);
			half temp_output_8_0 = ( i.vertexColor.a * _NiuQuQiangDu );
			half4 lerpResult4 = lerp( half4( half3(0,0,1) , 0.0 ) , (tex2D( _NiuQu_Tex, panner22 )).rgba , temp_output_8_0);
			o.Normal = lerpResult4.rgb;
			o.Alpha = 0.0;
			o.Normal = o.Normal + 0.00001 * i.screenPos * i.worldPos;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
2037;124;1461;824;1463.987;12.12207;1;True;True
Node;AmplifyShaderEditor.RangedFloatNode;18;-2047.17,156.4461;Inherit;False;Property;_NiuQu_V_Speed;NiuQu_V_Speed;4;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-2054.644,77.38411;Inherit;False;Property;_NiuQu_U_Speed;NiuQu_U_Speed;3;0;Create;True;0;0;0;False;0;False;0;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;21;-2044.789,-91.54771;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;20;-1569.712,77.70556;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;15;-1197.118,537.0566;Inherit;True;Property;_Mask_Tex;Mask_Tex;5;0;Create;True;0;0;0;False;0;False;-1;5daac5a2070c70441a0a53429658c844;988b2d0a8a806d748b6cb3dfe96754ed;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;7;-999.1617,410.851;Inherit;False;Property;_NiuQuQiangDu;NiuQuQiangDu;2;0;Create;True;0;0;0;False;0;False;1;1.5;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;22;-1404.048,54.60316;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;6;-884.8552,236.5932;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DesaturateOpNode;16;-774.1181,536.7443;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-593.6942,332.5932;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-1219.444,26.63928;Inherit;True;Property;_NiuQu_Tex;NiuQu_Tex;1;0;Create;True;0;0;0;False;0;False;-1;4adc7030b11dcc049aa8db00cd906155;640296d74b41ea44182ba02b949edf52;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-393.0546,538.7648;Inherit;True;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-404.019,353.8244;Inherit;False;Constant;_Float2;Float 2;3;0;Create;True;0;0;0;False;0;False;1.4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-400.6119,272.8222;Inherit;False;Constant;_Float3;Float 3;3;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;3;-663.9684,-124.9718;Inherit;False;Constant;_Vector0;Vector 0;2;0;Create;True;0;0;0;False;0;False;0,0,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;5;-746.3124,24.49524;Inherit;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;4;-432.8652,3.3004;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-167.6163,491.877;Inherit;False;Constant;_Float1;Float 1;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;10;-227.0172,249.4417;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;125.3913,-18.03691;Half;False;True;-1;2;ASEMaterialInspector;0;0;Lambert;LTY/shader/NiuQu;False;False;False;False;True;True;True;True;True;True;True;True;False;False;False;False;False;False;False;False;False;Off;2;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;ForwardOnly;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;6;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;20;0;19;0
WireConnection;20;1;18;0
WireConnection;22;0;21;0
WireConnection;22;2;20;0
WireConnection;16;0;15;0
WireConnection;8;0;6;4
WireConnection;8;1;7;0
WireConnection;1;1;22;0
WireConnection;13;0;16;0
WireConnection;13;1;15;4
WireConnection;13;2;8;0
WireConnection;5;0;1;0
WireConnection;4;0;3;0
WireConnection;4;1;5;0
WireConnection;4;2;8;0
WireConnection;10;0;12;0
WireConnection;10;1;11;0
WireConnection;10;2;13;0
WireConnection;0;1;4;0
WireConnection;0;8;10;0
WireConnection;0;9;9;0
ASEEND*/
//CHKSM=5907F1315299A33A5D0BA1CB100C44E0D96D5562