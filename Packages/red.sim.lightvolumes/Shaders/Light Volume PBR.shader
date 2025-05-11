// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Light Volume Samples/Light Volume PBR"
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
		[Toggle(_SPECULARS_ON)] _Speculars("Speculars", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "Packages/red.sim.lightvolumes/Shaders/LightVolumes.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 5.0
		#pragma shader_feature_local _SPECULARS_ON
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
			float3 Albedo337 = ( _Color.rgb * tex2D( _MainTex, uv_MainTex ).rgb );
			o.Albedo = Albedo337;
			float3 normalizeResult349 = normalize( (WorldNormalVector( i , tex2DNode5 )) );
			float3 World_Normal112 = normalizeResult349;
			float3 worldNormal2_g213 = World_Normal112;
			float localLightVolumeSH1_g3 = ( 0.0 );
			float3 ase_positionWS = i.worldPos;
			float3 temp_output_6_0_g3 = ase_positionWS;
			float3 worldPos1_g3 = temp_output_6_0_g3;
			float3 L01_g3 = float3( 0,0,0 );
			float3 L1r1_g3 = float3( 0,0,0 );
			float3 L1g1_g3 = float3( 0,0,0 );
			float3 L1b1_g3 = float3( 0,0,0 );
			LightVolumeSH( worldPos1_g3 , L01_g3 , L1r1_g3 , L1g1_g3 , L1b1_g3 );
			float localLightVolumeAdditiveSH9_g4 = ( 0.0 );
			float3 temp_output_6_0_g4 = ase_positionWS;
			float3 worldPos9_g4 = temp_output_6_0_g4;
			float3 L09_g4 = float3( 0,0,0 );
			float3 L1r9_g4 = float3( 0,0,0 );
			float3 L1g9_g4 = float3( 0,0,0 );
			float3 L1b9_g4 = float3( 0,0,0 );
			LightVolumeAdditiveSH( worldPos9_g4 , L09_g4 , L1r9_g4 , L1g9_g4 , L1b9_g4 );
			#ifdef _ADDITIVEONLY_ON
				float3 staticSwitch92 = L09_g4;
			#else
				float3 staticSwitch92 = L01_g3;
			#endif
			float3 L098 = staticSwitch92;
			float3 L02_g213 = L098;
			#ifdef _ADDITIVEONLY_ON
				float3 staticSwitch93 = L1r9_g4;
			#else
				float3 staticSwitch93 = L1r1_g3;
			#endif
			float3 L1r99 = staticSwitch93;
			float3 L1r2_g213 = L1r99;
			#ifdef _ADDITIVEONLY_ON
				float3 staticSwitch94 = L1g9_g4;
			#else
				float3 staticSwitch94 = L1g1_g3;
			#endif
			float3 L1g100 = staticSwitch94;
			float3 L1g2_g213 = L1g100;
			#ifdef _ADDITIVEONLY_ON
				float3 staticSwitch95 = L1b9_g4;
			#else
				float3 staticSwitch95 = L1b1_g3;
			#endif
			float3 L1b101 = staticSwitch95;
			float3 L1b2_g213 = L1b101;
			float3 localLightVolumeEvaluate2_g213 = LightVolumeEvaluate( worldNormal2_g213 , L02_g213 , L1r2_g213 , L1g2_g213 , L1b2_g213 );
			float3 temp_output_350_0 = localLightVolumeEvaluate2_g213;
			float2 uv_Glossiness50 = i.uv_texcoord;
			float4 tex2DNode50 = tex2D( _Glossiness, uv_Glossiness50 );
			float Metallic334 = ( tex2DNode50.r * _Metalness );
			float temp_output_80_0 = ( 1.0 - Metallic334 );
			#ifdef _ADDITIVEONLY_ON
				float3 staticSwitch77 = saturate( ( temp_output_80_0 * temp_output_350_0 ) );
			#else
				float3 staticSwitch77 = ( Albedo337 * temp_output_350_0 * temp_output_80_0 );
			#endif
			float3 albedo157_g220 = Albedo337;
			float Smoothness109 = ( tex2DNode50.a * _Smoothness );
			float smoothness157_g220 = Smoothness109;
			float metallic157_g220 = Metallic334;
			float3 worldNormal157_g220 = World_Normal112;
			float3 worldPosition157_g220 = ase_positionWS;
			float3 L0157_g220 = L098;
			float3 L1r157_g220 = L1r99;
			float3 L1g157_g220 = L1g100;
			float3 L1b157_g220 = L1b101;
			float3 localLightVolumeSpecular157_g220 = LightVolumeSpecular( albedo157_g220 , smoothness157_g220 , metallic157_g220 , worldNormal157_g220 , worldPosition157_g220 , L0157_g220 , L1r157_g220 , L1g157_g220 , L1b157_g220 );
			float lerpResult57 = lerp( 1.0 , tex2DNode50.g , _AmbientOcclusion);
			float AO357 = lerpResult57;
			#ifdef _SPECULARS_ON
				float3 staticSwitch361 = ( staticSwitch77 + ( localLightVolumeSpecular157_g220 * AO357 ) );
			#else
				float3 staticSwitch361 = staticSwitch77;
			#endif
			o.Emission = staticSwitch361;
			o.Metallic = Metallic334;
			o.Smoothness = Smoothness109;
			o.Occlusion = AO357;
			o.Alpha = 1;
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
Node;AmplifyShaderEditor.RangedFloatNode;6;-1440,-128;Inherit;False;Property;_NormalPower;Normal Power;3;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-1248,-176;Inherit;True;Property;_BumpMap;Normal;2;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;50;-832,592;Inherit;True;Property;_Glossiness;MOHS;4;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;53;-832,864;Inherit;False;Property;_Metalness;Metallic;6;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;14;-928,-176;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;78;-1456,112;Inherit;False;LightVolume;-1;;3;78706f2b7f33b1c44b4f381a7904a7e1;4,8,0,10,0,11,0,12,0;1;6;FLOAT3;0,0,0;False;4;FLOAT3;13;FLOAT3;14;FLOAT3;15;FLOAT3;16
Node;AmplifyShaderEditor.FunctionNode;79;-1456,272;Inherit;False;LightVolume;-1;;4;78706f2b7f33b1c44b4f381a7904a7e1;4,8,1,10,1,11,1,12,1;1;6;FLOAT3;0,0,0;False;4;FLOAT3;13;FLOAT3;14;FLOAT3;15;FLOAT3;16
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-512,640;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;349;-688,-96;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;93;-1200,176;Inherit;False;Property;_AdditiveOnly;Additive Only;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;77;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;95;-1200,368;Inherit;False;Property;_AdditiveOnly;Additive Only;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;77;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;94;-1200,272;Inherit;False;Property;_AdditiveOnly;Additive Only;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;77;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;92;-1200,80;Inherit;False;Property;_AdditiveOnly;Additive Only;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;77;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;3;-560,-352;Inherit;True;Property;_MainTex;Albedo;0;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;87;-496,-544;Inherit;False;Property;_Color;Color;1;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;334;-352,640;Inherit;False;Metallic;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;112;-512,-96;Inherit;False;World Normal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-832,784;Inherit;False;Property;_Smoothness;Smoothness;5;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;99;-960,176;Inherit;False;L1r;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;100;-960,272;Inherit;False;L1g;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;101;-960,368;Inherit;False;L1b;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;98;-960,80;Inherit;False;L0;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;113;-960,-16;Inherit;False;112;World Normal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-224,-352;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;336;-416,272;Inherit;False;334;Metallic;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-512,736;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-832,944;Inherit;False;Property;_AmbientOcclusion;AO;7;0;Create;False;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;80;-224,272;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;337;-32,-352;Inherit;False;Albedo;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;350;-608,64;Inherit;False;LightVolumeEvaluate;-1;;213;4919cc1d83093f24f802ce655e9f3303;0;5;5;FLOAT3;0,0,0;False;13;FLOAT3;1,1,1;False;14;FLOAT3;0,0,0;False;15;FLOAT3;0,0,0;False;16;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;109;-352,736;Inherit;False;Smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;57;-512,832;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;82;-32,144;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;338;-240,16;Inherit;False;337;Albedo;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;357;-352,832;Inherit;False;AO;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;340;-480,1168;Inherit;False;334;Metallic;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;108;-480,1232;Inherit;False;99;L1r;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;122;-480,1296;Inherit;False;100;L1g;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;123;-480,1360;Inherit;False;101;L1b;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;115;-480,1424;Inherit;False;98;L0;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;114;-512,1488;Inherit;False;112;World Normal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;111;-512,1104;Inherit;False;109;Smoothness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;339;-480,1040;Inherit;False;337;Albedo;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-32,16;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;86;128,144;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;359;-144,1440;Inherit;False;357;AO;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;362;-208,1168;Inherit;False;LightVolumeSpecular;-1;;220;a5ec4a1f240e00f47a5deb132f113431;0;9;138;FLOAT3;1,1,1;False;3;FLOAT;0;False;137;FLOAT;0;False;1;FLOAT3;0,0,0;False;36;FLOAT3;0,0,0;False;37;FLOAT3;0,0,0;False;65;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;9;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;360;80,1296;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;77;304,16;Inherit;False;Property;_AdditiveOnly;Additive Only;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;124;576,128;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;110;416,592;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;335;416,496;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;358;416,688;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;361;704,16;Inherit;False;Property;_Speculars;Speculars;9;0;Create;True;0;0;0;False;0;False;0;1;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;72;1136,-208;Float;False;True;-1;7;AmplifyShaderEditor.MaterialInspector;0;0;Standard;Light Volume Samples/Light Volume PBR;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;5;6;0
WireConnection;14;0;5;0
WireConnection;54;0;50;1
WireConnection;54;1;53;0
WireConnection;349;0;14;0
WireConnection;93;1;78;14
WireConnection;93;0;79;14
WireConnection;95;1;78;16
WireConnection;95;0;79;16
WireConnection;94;1;78;15
WireConnection;94;0;79;15
WireConnection;92;1;78;13
WireConnection;92;0;79;13
WireConnection;334;0;54;0
WireConnection;112;0;349;0
WireConnection;99;0;93;0
WireConnection;100;0;94;0
WireConnection;101;0;95;0
WireConnection;98;0;92;0
WireConnection;88;0;87;5
WireConnection;88;1;3;5
WireConnection;51;0;50;4
WireConnection;51;1;52;0
WireConnection;80;0;336;0
WireConnection;337;0;88;0
WireConnection;350;5;113;0
WireConnection;350;13;98;0
WireConnection;350;14;99;0
WireConnection;350;15;100;0
WireConnection;350;16;101;0
WireConnection;109;0;51;0
WireConnection;57;1;50;2
WireConnection;57;2;56;0
WireConnection;82;0;80;0
WireConnection;82;1;350;0
WireConnection;357;0;57;0
WireConnection;16;0;338;0
WireConnection;16;1;350;0
WireConnection;16;2;80;0
WireConnection;86;0;82;0
WireConnection;362;138;339;0
WireConnection;362;3;111;0
WireConnection;362;137;340;0
WireConnection;362;1;108;0
WireConnection;362;36;122;0
WireConnection;362;37;123;0
WireConnection;362;65;115;0
WireConnection;362;2;114;0
WireConnection;360;0;362;0
WireConnection;360;1;359;0
WireConnection;77;1;16;0
WireConnection;77;0;86;0
WireConnection;124;0;77;0
WireConnection;124;1;360;0
WireConnection;110;0;109;0
WireConnection;335;0;334;0
WireConnection;358;0;357;0
WireConnection;361;1;77;0
WireConnection;361;0;124;0
WireConnection;72;0;337;0
WireConnection;72;1;5;0
WireConnection;72;2;361;0
WireConnection;72;3;335;0
WireConnection;72;4;110;0
WireConnection;72;5;358;0
ASEEND*/
//CHKSM=11435A52A0E2AD4B75366293931D4A47667CBBA9