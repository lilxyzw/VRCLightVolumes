// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Light Volume ASE Transparent"
{
	Properties
	{
		[NoScaleOffset]_BumpMap("Normal", 2D) = "bump" {}
		_NormalPower("Normal Power", Float) = 1
		_Opacity("Opacity", Range( 0 , 1)) = 0.06
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#include "LightVolumes.cginc"
		#include "UnityStandardUtils.cginc"
		#pragma target 5.0
		#define ASE_VERSION 19801
		#pragma surface surf Standard keepalpha noshadow exclude_path:deferred noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		struct Input
		{
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv_texcoord;
			float3 worldPos;
		};

		uniform sampler2D _BumpMap;
		uniform float _NormalPower;
		uniform float _Opacity;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float2 uv_BumpMap5 = i.uv_texcoord;
			float3 normalizeResult73 = normalize( (WorldNormalVector( i , UnpackScaleNormal( tex2D( _BumpMap, uv_BumpMap5 ), _NormalPower ) )) );
			float3 worldNormal74 = normalizeResult73;
			float localLightVolumeSH13 = ( 0.0 );
			float3 ase_positionWS = i.worldPos;
			float3 worldPos13 = ase_positionWS;
			float3 L013 = float3( 0,0,0 );
			float3 L1r13 = float3( 0,0,0 );
			float3 L1g13 = float3( 0,0,0 );
			float3 L1b13 = float3( 0,0,0 );
			LightVolumeSH( worldPos13 , L013 , L1r13 , L1g13 , L1b13 );
			float3 L074 = L013;
			float3 L1r74 = L1r13;
			float3 L1g74 = L1g13;
			float3 L1b74 = L1b13;
			float3 localLightVolumeEvaluate74 = LightVolumeEvaluate( worldNormal74 , L074 , L1r74 , L1g74 , L1b74 );
			o.Emission = localLightVolumeEvaluate74;
			o.Alpha = _Opacity;
		}

		ENDCG
	}
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.RangedFloatNode;6;-1232,-32;Inherit;False;Property;_NormalPower;Normal Power;3;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-1040,-80;Inherit;True;Property;_BumpMap;Normal;2;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.WorldPosInputsNode;15;-864,368;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;14;-752,112;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;73;-560,160;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomExpressionNode;13;-640,352;Inherit;False; ;7;File;5;True;worldPos;FLOAT3;0,0,0;In;;Inherit;False;True;L0;FLOAT3;0,0,0;Out;;Inherit;False;True;L1r;FLOAT3;0,0,0;Out;;Inherit;False;True;L1g;FLOAT3;0,0,0;Out;;Inherit;False;True;L1b;FLOAT3;0,0,0;Out;;Inherit;False;LightVolumeSH;False;True;0;4ae0b01e695ad7545a078e1e04d2e609;False;6;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;5;FLOAT;0;FLOAT3;3;FLOAT3;4;FLOAT3;5;FLOAT3;6
Node;AmplifyShaderEditor.RangedFloatNode;77;19.88708,-229.8751;Inherit;False;Property;_Brightness;Brightness;9;0;Create;True;0;0;0;False;0;False;1;0.867;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;74;-384,352;Inherit;False; ;3;File;5;True;worldNormal;FLOAT3;0,0,0;In;;Inherit;False;True;L0;FLOAT3;0,0,0;In;;Inherit;False;True;L1r;FLOAT3;0,0,0;In;;Inherit;False;True;L1g;FLOAT3;0,0,0;In;;Inherit;False;True;L1b;FLOAT3;0,0,0;In;;Inherit;False;LightVolumeEvaluate;False;True;0;4ae0b01e695ad7545a078e1e04d2e609;False;5;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;3;-512,-256;Inherit;True;Property;_MainTex;Albedo;1;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-80,64;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;53;48,704;Inherit;False;Property;_Metalness;Metallic;6;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;48,592;Inherit;False;Property;_Smoothness;Smoothness;5;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;64,800;Inherit;False;Property;_AmbientOcclusion;AO;7;0;Create;False;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;50;16,320;Inherit;True;Property;_Glossiness;MOHS;4;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;57;448,640;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;464,400;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;464,288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;507.8871,-113.8751;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;75;496,208;Inherit;False;Property;_Opacity;Opacity;8;0;Create;True;0;0;0;False;0;False;0.06;0.02;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;79;896,-96;Float;False;True;-1;7;AmplifyShaderEditor.MaterialInspector;0;0;Standard;Light Volume ASE Transparent;False;False;False;False;True;True;True;True;True;True;True;True;False;False;False;False;False;False;False;False;False;Off;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Transparent;;Transparent;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;5;6;0
WireConnection;14;0;5;0
WireConnection;73;0;14;0
WireConnection;13;1;15;0
WireConnection;74;0;73;0
WireConnection;74;1;13;3
WireConnection;74;2;13;4
WireConnection;74;3;13;5
WireConnection;74;4;13;6
WireConnection;16;0;3;5
WireConnection;57;1;50;2
WireConnection;57;2;56;0
WireConnection;51;0;50;4
WireConnection;51;1;52;0
WireConnection;54;0;50;1
WireConnection;54;1;53;0
WireConnection;76;0;77;0
WireConnection;79;2;74;0
WireConnection;79;9;75;0
ASEEND*/
//CHKSM=E525B8CDB0FA432C5C0A5F31F264FE0A4B22FF50