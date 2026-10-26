// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LTY/shader/Light_dissovle_man"
{
	Properties
	{
		[Header(Main)]_Main("Main", 2D) = "white" {}
		_Main_U_Speed("Main_U_Speed", Float) = 0
		_Main_V_Speed("Main_V_Speed", Float) = 0
		[Toggle(_IS_TWO_OR_ONE_ON)] _IS_Two_or_one("IS_Two_or_one", Float) = 0
		[HDR]_FrontColor("FrontColor", Color) = (1,1,1,1)
		[HDR]_BackColor("BackColor", Color) = (1,1,1,1)
		_Mask("Mask", 2D) = "white" {}
		[Header(Dissiovle)]_Dissiovle("Dissiovle", 2D) = "white" {}
		[Toggle(_IS_DISSIOVLE_ON)] _Is_Dissiovle("Is_Dissiovle", Float) = 0
		[Toggle(_USE_WORLDLOCAL_ON)] _Use_WorldLocal("Use_WorldLocal", Float) = 1
		[Toggle(_LOCAL_UP_DOWN_ON)] _Local_Up_Down("Local_Up_Down", Float) = 0
		_Diss_U_Speed("Diss_U_Speed", Float) = 0
		_Diss_V_Speed("Diss_V_Speed", Float) = 0
		[HDR]_EdgeColor("EdgeColor", Color) = (1,1,1,1)
		_diss_Power("diss_Power", Range( 0 , 1.5)) = 0
		_EdgeWidt("EdgeWidt", Range( 0 , 0.5)) = 0.05
		_X("X", Range( -0.5 , 0.5)) = 0
		_Y("Y", Range( -0.5 , 0.5)) = 0
		_Z("Z", Range( -0.5 , 0.5)) = -0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord2( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull Off
		ZWrite Off
		ZTest LEqual
		Offset  0 , 0
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma shader_feature _USE_WORLDLOCAL_ON
		#pragma shader_feature _LOCAL_UP_DOWN_ON
		#pragma shader_feature _IS_DISSIOVLE_ON
		#pragma shader_feature_local _IS_TWO_OR_ONE_ON
		#pragma surface surf Standard keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			float3 worldPos;
			float4 uv2_tex4coord2;
			half ASEVFace : VFACE;
		};

		uniform sampler2D _Main;
		uniform half _Main_U_Speed;
		uniform half _Main_V_Speed;
		uniform float4 _Main_ST;
		uniform sampler2D _Dissiovle;
		uniform half _Diss_U_Speed;
		uniform half _Diss_V_Speed;
		uniform float4 _Dissiovle_ST;
		uniform half _X;
		uniform half _Y;
		uniform half _Z;
		uniform half _diss_Power;
		uniform half _EdgeWidt;
		uniform half4 _EdgeColor;
		uniform half4 _FrontColor;
		uniform half4 _BackColor;
		uniform sampler2D _Mask;
		uniform half4 _Mask_ST;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			half2 appendResult50 = (half2(_Main_U_Speed , _Main_V_Speed));
			float2 uv_Main = i.uv_texcoord * _Main_ST.xy + _Main_ST.zw;
			half2 panner51 = ( 1.0 * _Time.y * appendResult50 + uv_Main);
			half4 tex2DNode1 = tex2D( _Main, panner51 );
			half2 appendResult44 = (half2(_Diss_U_Speed , _Diss_V_Speed));
			float2 uv_Dissiovle = i.uv_texcoord * _Dissiovle_ST.xy + _Dissiovle_ST.zw;
			half2 panner43 = ( 1.0 * _Time.y * appendResult44 + uv_Dissiovle);
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			half temp_output_53_0 = ( ( ase_vertex3Pos.x * _X ) + ( ase_vertex3Pos.y * _Y ) + ( ase_vertex3Pos.z * _Z ) );
			#ifdef _LOCAL_UP_DOWN_ON
				half staticSwitch65 = temp_output_53_0;
			#else
				half staticSwitch65 = ( 1.0 - temp_output_53_0 );
			#endif
			#ifdef _USE_WORLDLOCAL_ON
				half staticSwitch59 = staticSwitch65;
			#else
				half staticSwitch59 = tex2D( _Dissiovle, panner43 ).r;
			#endif
			#ifdef _IS_DISSIOVLE_ON
				half staticSwitch42 = i.uv2_tex4coord2.x;
			#else
				half staticSwitch42 = _diss_Power;
			#endif
			half temp_output_36_0 = saturate( ( ( staticSwitch59 + 1.0 ) - ( staticSwitch42 * 2.0 ) ) );
			half ifLocalVar18 = 0;
			if( temp_output_36_0 >= staticSwitch42 )
				ifLocalVar18 = 1.0;
			else
				ifLocalVar18 = 0.0;
			half ifLocalVar19 = 0;
			if( temp_output_36_0 >= ( staticSwitch42 + _EdgeWidt ) )
				ifLocalVar19 = 1.0;
			else
				ifLocalVar19 = 0.0;
			half4 temp_output_33_0 = ( ifLocalVar18 + ( ( ifLocalVar18 - ifLocalVar19 ) * _EdgeColor ) );
			half4 switchResult37 = (((i.ASEVFace>0)?(_FrontColor):(_BackColor)));
			#ifdef _IS_TWO_OR_ONE_ON
				half4 staticSwitch80 = switchResult37;
			#else
				half4 staticSwitch80 = _FrontColor;
			#endif
			o.Emission = ( ( i.vertexColor * tex2DNode1 ) * temp_output_33_0 * staticSwitch80 ).rgb;
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			o.Alpha = ( i.vertexColor.a * tex2DNode1.a * _FrontColor.a * _BackColor.a * saturate( temp_output_33_0 ) * tex2D( _Mask, uv_Mask ).r ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
2099;201;1461;818;3566.079;513.0273;2.454824;True;True
Node;AmplifyShaderEditor.CommentaryNode;66;-5036.951,615.4871;Inherit;False;1027.058;459.2881;世界坐标溶解;10;65;64;62;61;60;54;56;58;57;53;;1,1,1,1;0;0
Node;AmplifyShaderEditor.PosVertexDataNode;54;-4922.795,649.0041;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;35;-5038.142,144.6472;Inherit;False;2016.194;456.7421;+1-2;14;36;7;41;43;47;44;45;46;2;3;4;5;42;59;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-4988.875,945.3525;Inherit;False;Property;_Z;Z;23;0;Create;True;0;0;0;False;0;False;-0.5;-0.5;-0.5;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;60;-4992.494,793.4872;Inherit;False;Property;_X;X;18;0;Create;True;0;0;0;False;0;False;0;0;-0.5;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;61;-4991.222,866.8203;Inherit;False;Property;_Y;Y;21;0;Create;True;0;0;0;False;0;False;0;0;-0.5;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-5030.146,353.9691;Inherit;False;Property;_Diss_U_Speed;Diss_U_Speed;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;-4648.053,676.3575;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;57;-4650.659,783.1657;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;58;-4649.355,878.251;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-5029.139,436.5667;Inherit;False;Property;_Diss_V_Speed;Diss_V_Speed;13;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;47;-5024.189,229.8979;Inherit;False;0;2;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;44;-4842.79,368.071;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;53;-4492.552,762.8856;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;43;-4668.813,220.4792;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;64;-4360.513,666.1675;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;65;-4226.389,742.1951;Inherit;True;Property;_Local_Up_Down;Local_Up_Down;11;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;41;-4638.046,428.868;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-4433.688,181.7293;Inherit;True;Property;_Dissiovle;Dissiovle;8;1;[Header];Create;True;1;Dissiovle;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;7;-4414.047,371.484;Inherit;False;Property;_diss_Power;diss_Power;16;0;Create;True;0;0;0;False;0;False;0;0;0;1.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;59;-4028.315,185.5414;Inherit;True;Property;_Use_WorldLocal;Use_WorldLocal;10;0;Create;True;0;0;0;False;0;False;0;1;0;True;;Toggle;2;Key0;Key1;Create;False;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;42;-3787.834,424.9038;Inherit;False;Property;_Is_Dissiovle;Is_Dissiovle;9;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;67;-3454.979,612.0077;Inherit;False;2258.524;556.1662;溶解亮边;11;40;33;18;31;32;27;19;22;23;28;30;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;3;-3736.198,184.4945;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-3528.629,365.021;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;4;-3384.712,179.1159;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-3311.686,974.8458;Inherit;False;Property;_EdgeWidt;EdgeWidt;17;0;Create;True;0;0;0;False;0;False;0.05;0.05;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-2995.987,836.0154;Inherit;False;Constant;_Float1;Float 1;6;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;28;-2978.37,936.6693;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;68;-3005.132,-315.767;Inherit;False;1799.734;918.7822;主贴图流动和颜色;13;1;14;34;17;16;48;49;50;52;51;69;79;80;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-2996.664,725.6478;Inherit;False;Constant;_Float0;Float 0;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;36;-3186.602,181.1934;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;19;-2758.17,904.5689;Inherit;True;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-2955.132,-44.9421;Inherit;False;Property;_Main_U_Speed;Main_U_Speed;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;18;-2761.983,668.4801;Inherit;True;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-2954.125,37.6555;Inherit;False;Property;_Main_V_Speed;Main_V_Speed;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;27;-2394.611,757.9839;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;32;-2396.987,1006.044;Inherit;False;Property;_EdgeColor;EdgeColor;14;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;50;-2767.775,-30.8401;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;52;-2850.374,-193.0133;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;69;-2866.667,162.2323;Inherit;False;412.9908;365.8174;双面颜色;3;38;15;37;;1,1,1,1;0;0
Node;AmplifyShaderEditor.PannerNode;51;-2580.998,-45.63189;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;-2116.157,756.5789;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;15;-2851.037,196.6936;Inherit;False;Property;_FrontColor;FrontColor;5;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;4.237095,4.237095,4.237095,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;38;-2854.623,361.1203;Inherit;False;Property;_BackColor;BackColor;6;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;4.237095,4.237095,4.237095,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;33;-1897.072,670.686;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;1;-2379.42,-77.01111;Inherit;True;Property;_Main;Main;1;1;[Header];Create;True;1;Main;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;14;-2248.598,-265.767;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SwitchByFaceNode;37;-2616.031,256.8479;Inherit;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;39;-6854.025,-313.9429;Inherit;False;840.9077;303;smooth无用;3;12;11;10;;0.9622642,0.4493592,0.4493592,1;0;0
Node;AmplifyShaderEditor.SaturateNode;40;-1637.813,672.0404;Inherit;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;80;-2280.928,127.5663;Inherit;False;Property;_IS_Two_or_one;IS_Two_or_one;4;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;79;-2027.785,211.1823;Inherit;True;Property;_Mask;Mask;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-1646.29,-161.5586;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-1374.397,316.9862;Inherit;False;6;6;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;5;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;12;-6525.877,-216.2247;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-5117.934,1426.984;Inherit;False;Property;_Float2;Float 2;22;0;Create;True;0;0;0;False;0;False;0;0.5;-0.5;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;70;-5051.854,1130.635;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;-4778.414,1359.882;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-1385.621,-8.566475;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;-4779.718,1264.797;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;77;-4621.61,1244.517;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-4777.112,1157.989;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;73;-5121.553,1275.118;Inherit;False;Property;_Float3;Float 3;19;0;Create;True;0;0;0;False;0;False;0;0;-0.5;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-6768.908,-93.40763;Inherit;False;Property;_smooth;smooth;15;0;Create;True;0;0;0;False;0;False;0.7342514;0;0.5;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;74;-5120.281,1348.452;Inherit;False;Property;_Float4;Float 4;20;0;Create;True;0;0;0;False;0;False;0;0;-0.5;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;10;-6277.113,-257.9454;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-1081.09,-8.861391;Half;False;True;-1;2;ASEMaterialInspector;0;0;Standard;LTY/shader/Light_dissovle_man;False;False;False;False;True;True;True;True;True;True;True;True;False;False;False;False;False;False;False;False;False;Off;2;False;-1;3;False;-1;True;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;56;0;54;1
WireConnection;56;1;60;0
WireConnection;57;0;54;2
WireConnection;57;1;61;0
WireConnection;58;0;54;3
WireConnection;58;1;62;0
WireConnection;44;0;45;0
WireConnection;44;1;46;0
WireConnection;53;0;56;0
WireConnection;53;1;57;0
WireConnection;53;2;58;0
WireConnection;43;0;47;0
WireConnection;43;2;44;0
WireConnection;64;0;53;0
WireConnection;65;1;64;0
WireConnection;65;0;53;0
WireConnection;2;1;43;0
WireConnection;59;1;2;1
WireConnection;59;0;65;0
WireConnection;42;1;7;0
WireConnection;42;0;41;1
WireConnection;3;0;59;0
WireConnection;5;0;42;0
WireConnection;4;0;3;0
WireConnection;4;1;5;0
WireConnection;28;0;42;0
WireConnection;28;1;30;0
WireConnection;36;0;4;0
WireConnection;19;0;36;0
WireConnection;19;1;28;0
WireConnection;19;2;22;0
WireConnection;19;3;22;0
WireConnection;19;4;23;0
WireConnection;18;0;36;0
WireConnection;18;1;42;0
WireConnection;18;2;22;0
WireConnection;18;3;22;0
WireConnection;18;4;23;0
WireConnection;27;0;18;0
WireConnection;27;1;19;0
WireConnection;50;0;49;0
WireConnection;50;1;48;0
WireConnection;51;0;52;0
WireConnection;51;2;50;0
WireConnection;31;0;27;0
WireConnection;31;1;32;0
WireConnection;33;0;18;0
WireConnection;33;1;31;0
WireConnection;1;1;51;0
WireConnection;37;0;15;0
WireConnection;37;1;38;0
WireConnection;40;0;33;0
WireConnection;80;1;15;0
WireConnection;80;0;37;0
WireConnection;34;0;14;0
WireConnection;34;1;1;0
WireConnection;17;0;14;4
WireConnection;17;1;1;4
WireConnection;17;2;15;4
WireConnection;17;3;38;4
WireConnection;17;4;40;0
WireConnection;17;5;79;1
WireConnection;12;0;11;0
WireConnection;76;0;70;3
WireConnection;76;1;72;0
WireConnection;16;0;34;0
WireConnection;16;1;33;0
WireConnection;16;2;80;0
WireConnection;75;0;70;2
WireConnection;75;1;74;0
WireConnection;77;0;71;0
WireConnection;77;1;75;0
WireConnection;77;2;76;0
WireConnection;71;0;70;1
WireConnection;71;1;73;0
WireConnection;10;1;12;0
WireConnection;10;2;11;0
WireConnection;0;2;16;0
WireConnection;0;9;17;0
ASEEND*/
//CHKSM=150BB2F34364B16C8D3E0AC9AB424300CEF7AB79