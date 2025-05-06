// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Light Volume Samples/Light Volume Transparent PBR"
{
	Properties
	{
		_MainTex("Albedo", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		[NoScaleOffset]_BumpMap("Normal", 2D) = "bump" {}
		_NormalPower("Normal Power", Float) = 1
		[NoScaleOffset]_Glossiness("MOHS", 2D) = "white" {}
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_Metalness("Metallic", Range( 0 , 1)) = 0
		_AmbientOcclusion("AO", Range( 0 , 1)) = 1
		[Toggle(_ADDITIVEONLY_ON)] _AdditiveOnly("Additive Only", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "LightVolumes.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 5.0
		#pragma shader_feature_local _ADDITIVEONLY_ON
		#define ASE_VERSION 19801
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
		};

		uniform sampler2D _BumpMap;
		uniform float _NormalPower;
		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _Glossiness;
		uniform float _Metalness;
		uniform float _Smoothness;
		uniform float _AmbientOcclusion;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap5 = i.uv_texcoord;
			float3 tex2DNode5 = UnpackScaleNormal( tex2D( _BumpMap, uv_BumpMap5 ), _NormalPower );
			o.Normal = tex2DNode5;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode3 = tex2D( _MainTex, uv_MainTex );
			float3 temp_output_88_0 = ( _Color.rgb * tex2DNode3.rgb );
			o.Albedo = temp_output_88_0;
			float3 newWorldNormal14 = (WorldNormalVector( i , tex2DNode5 ));
			float3 normalizeResult3_g3 = normalize( newWorldNormal14 );
			float3 worldNormal2_g3 = normalizeResult3_g3;
			float localLightVolumeSH1_g3 = ( 0.0 );
			float3 ase_positionWS = i.worldPos;
			float3 temp_output_6_0_g3 = ase_positionWS;
			float3 worldPos1_g3 = temp_output_6_0_g3;
			float3 L01_g3 = float3( 0,0,0 );
			float3 L1r1_g3 = float3( 0,0,0 );
			float3 L1g1_g3 = float3( 0,0,0 );
			float3 L1b1_g3 = float3( 0,0,0 );
			LightVolumeSH( worldPos1_g3 , L01_g3 , L1r1_g3 , L1g1_g3 , L1b1_g3 );
			float3 L02_g3 = L01_g3;
			float3 L1r2_g3 = L1r1_g3;
			float3 L1g2_g3 = L1g1_g3;
			float3 L1b2_g3 = L1b1_g3;
			float3 localLightVolumeEvaluate2_g3 = LightVolumeEvaluate( worldNormal2_g3 , L02_g3 , L1r2_g3 , L1g2_g3 , L1b2_g3 );
			float2 uv_Glossiness50 = i.uv_texcoord;
			float4 tex2DNode50 = tex2D( _Glossiness, uv_Glossiness50 );
			float temp_output_54_0 = ( tex2DNode50.r * _Metalness );
			float temp_output_80_0 = ( 1.0 - temp_output_54_0 );
			float3 normalizeResult3_g2 = normalize( newWorldNormal14 );
			float3 worldNormal2_g2 = normalizeResult3_g2;
			float localLightVolumeAdditiveSH9_g2 = ( 0.0 );
			float3 temp_output_6_0_g2 = ase_positionWS;
			float3 worldPos9_g2 = temp_output_6_0_g2;
			float3 L09_g2 = float3( 0,0,0 );
			float3 L1r9_g2 = float3( 0,0,0 );
			float3 L1g9_g2 = float3( 0,0,0 );
			float3 L1b9_g2 = float3( 0,0,0 );
			LightVolumeAdditiveSH( worldPos9_g2 , L09_g2 , L1r9_g2 , L1g9_g2 , L1b9_g2 );
			float3 L02_g2 = L09_g2;
			float3 L1r2_g2 = L1r9_g2;
			float3 L1g2_g2 = L1g9_g2;
			float3 L1b2_g2 = L1b9_g2;
			float3 localLightVolumeEvaluate2_g2 = LightVolumeEvaluate( worldNormal2_g2 , L02_g2 , L1r2_g2 , L1g2_g2 , L1b2_g2 );
			#ifdef _ADDITIVEONLY_ON
				float3 staticSwitch77 = saturate( ( temp_output_80_0 * localLightVolumeEvaluate2_g2 ) );
			#else
				float3 staticSwitch77 = ( temp_output_88_0 * localLightVolumeEvaluate2_g3 * temp_output_80_0 );
			#endif
			o.Emission = staticSwitch77;
			o.Metallic = temp_output_54_0;
			o.Smoothness = ( tex2DNode50.a * _Smoothness );
			float lerpResult57 = lerp( 1.0 , tex2DNode50.g , _AmbientOcclusion);
			o.Occlusion = lerpResult57;
			o.Alpha = ( _Color.a * tex2DNode3.a );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows exclude_path:deferred noambient 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.RangedFloatNode;6;-1264,-32;Inherit;False;Property;_NormalPower;Normal Power;4;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-1072,-80;Inherit;True;Property;_BumpMap;Normal;3;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;53;-816,688;Inherit;False;Property;_Metalness;Metallic;7;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;50;-848,304;Inherit;True;Property;_Glossiness;MOHS;5;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.WorldNormalVector;14;-752,112;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-400,272;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;79;-496,160;Inherit;False;LightVolume;-1;;2;78706f2b7f33b1c44b4f381a7904a7e1;4,8,1,10,1,11,1,12,1;2;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;80;-208,112;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-512,-256;Inherit;True;Property;_MainTex;Albedo;1;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;87;-448,-464;Inherit;False;Property;_Color;Color;2;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode;78;-480,32;Inherit;False;LightVolume;-1;;3;78706f2b7f33b1c44b4f381a7904a7e1;4,8,0,10,0,11,0,12,0;2;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-816,576;Inherit;False;Property;_Smoothness;Smoothness;6;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-800,784;Inherit;False;Property;_AmbientOcclusion;AO;8;0;Create;False;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;82;-32,144;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-189.2056,-340.9681;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;57;-416,624;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-400,384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-32,16;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;86;128,144;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;83;272,272;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;84;288,304;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;85;320,368;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;77;304,16;Inherit;False;Property;_AdditiveOnly;Additive Only;9;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;89;-176,-208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;72;752,-112;Float;False;True;-1;7;AmplifyShaderEditor.MaterialInspector;0;0;Standard;Light Volume Samples/Light Volume Transparent PBR;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;False;Transparent;;Transparent;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;5;6;0
WireConnection;14;0;5;0
WireConnection;54;0;50;1
WireConnection;54;1;53;0
WireConnection;79;5;14;0
WireConnection;80;0;54;0
WireConnection;78;5;14;0
WireConnection;82;0;80;0
WireConnection;82;1;79;0
WireConnection;88;0;87;5
WireConnection;88;1;3;5
WireConnection;57;1;50;2
WireConnection;57;2;56;0
WireConnection;51;0;50;4
WireConnection;51;1;52;0
WireConnection;16;0;88;0
WireConnection;16;1;78;0
WireConnection;16;2;80;0
WireConnection;86;0;82;0
WireConnection;83;0;54;0
WireConnection;84;0;51;0
WireConnection;85;0;57;0
WireConnection;77;1;16;0
WireConnection;77;0;86;0
WireConnection;89;0;87;4
WireConnection;89;1;3;4
WireConnection;72;0;88;0
WireConnection;72;1;5;0
WireConnection;72;2;77;0
WireConnection;72;3;83;0
WireConnection;72;4;84;0
WireConnection;72;5;85;0
WireConnection;72;9;89;0
ASEEND*/
//CHKSM=AF4682D418A4B09029A8600A064B9A9A2A3AD08B