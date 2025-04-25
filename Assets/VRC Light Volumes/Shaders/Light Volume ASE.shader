// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Light Volume ASE"
{
	Properties
	{
		_MainTex("Albedo", 2D) = "white" {}
		[NoScaleOffset]_BumpMap("Normal", 2D) = "bump" {}
		_NormalPower("Normal Power", Float) = 1
		[NoScaleOffset]_Glossiness("MOHS", 2D) = "white" {}
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_Metalness("Metallic", Range( 0 , 1)) = 0
		_AmbientOcclusion("AO", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "LightVolumes.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 5.0
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
			float3 temp_output_3_5 = tex2DNode3.rgb;
			o.Albedo = temp_output_3_5;
			float3 normalizeResult73 = normalize( (WorldNormalVector( i , tex2DNode5 )) );
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
			o.Emission = ( tex2DNode3.rgb * localLightVolumeEvaluate74 );
			float2 uv_Glossiness50 = i.uv_texcoord;
			float4 tex2DNode50 = tex2D( _Glossiness, uv_Glossiness50 );
			o.Metallic = ( tex2DNode50.r * _Metalness );
			o.Smoothness = ( tex2DNode50.a * _Smoothness );
			float lerpResult57 = lerp( 1.0 , tex2DNode50.g , _AmbientOcclusion);
			o.Occlusion = lerpResult57;
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
Node;AmplifyShaderEditor.RangedFloatNode;6;-1232,-32;Inherit;False;Property;_NormalPower;Normal Power;2;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-1040,-80;Inherit;True;Property;_BumpMap;Normal;1;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.WorldNormalVector;14;-752,112;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;15;-864,368;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;73;-560,160;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomExpressionNode;13;-640,352;Inherit;False; ;7;File;5;True;worldPos;FLOAT3;0,0,0;In;;Inherit;False;True;L0;FLOAT3;0,0,0;Out;;Inherit;False;True;L1r;FLOAT3;0,0,0;Out;;Inherit;False;True;L1g;FLOAT3;0,0,0;Out;;Inherit;False;True;L1b;FLOAT3;0,0,0;Out;;Inherit;False;LightVolumeSH;False;True;0;4ae0b01e695ad7545a078e1e04d2e609;False;6;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;5;FLOAT;0;FLOAT3;3;FLOAT3;4;FLOAT3;5;FLOAT3;6
Node;AmplifyShaderEditor.RangedFloatNode;53;48,704;Inherit;False;Property;_Metalness;Metallic;5;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;48,592;Inherit;False;Property;_Smoothness;Smoothness;4;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;64,800;Inherit;False;Property;_AmbientOcclusion;AO;6;0;Create;False;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;50;16,320;Inherit;True;Property;_Glossiness;MOHS;3;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;3;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;3;-512,-256;Inherit;True;Property;_MainTex;Albedo;0;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CustomExpressionNode;74;-384,352;Inherit;False; ;3;File;5;True;worldNormal;FLOAT3;0,0,0;In;;Inherit;False;True;L0;FLOAT3;0,0,0;In;;Inherit;False;True;L1r;FLOAT3;0,0,0;In;;Inherit;False;True;L1g;FLOAT3;0,0,0;In;;Inherit;False;True;L1b;FLOAT3;0,0,0;In;;Inherit;False;LightVolumeEvaluate;False;True;0;4ae0b01e695ad7545a078e1e04d2e609;False;5;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;57;448,640;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;464,400;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;464,288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-80,64;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;72;896,-96;Float;False;True;-1;7;AmplifyShaderEditor.MaterialInspector;0;0;Standard;Light Volume ASE;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;5;6;0
WireConnection;14;0;5;0
WireConnection;73;0;14;0
WireConnection;13;1;15;0
WireConnection;74;0;73;0
WireConnection;74;1;13;3
WireConnection;74;2;13;4
WireConnection;74;3;13;5
WireConnection;74;4;13;6
WireConnection;57;1;50;2
WireConnection;57;2;56;0
WireConnection;51;0;50;4
WireConnection;51;1;52;0
WireConnection;54;0;50;1
WireConnection;54;1;53;0
WireConnection;16;0;3;5
WireConnection;16;1;74;0
WireConnection;72;0;3;5
WireConnection;72;1;5;0
WireConnection;72;2;16;0
WireConnection;72;3;54;0
WireConnection;72;4;51;0
WireConnection;72;5;57;0
ASEEND*/
//CHKSM=C13B21D2F8C2CD19A1B43527FD91656A5C4D4E65