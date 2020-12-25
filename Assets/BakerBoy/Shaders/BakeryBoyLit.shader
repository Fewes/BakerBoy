// When creating shaders for Universal Render Pipeline you can you the ShaderGraph which is super AWESOME!
// However, if you want to author shaders in shading language you can use this teamplate as a base.
// Please note, this shader does not necessarily match perfomance of the built-in URP Lit shader.
// This shader works with URP 7.1.x and above
Shader "Universal Render Pipeline/Custom/BakeryBoy Example"
{
	Properties
	{
		// Specular vs Metallic workflow
		[HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0

		[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		[MainColor] _BaseColor("Color", Color) = (0.5,0.5,0.5,1)

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
		// _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		// _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

		[NoScaleOffset] _MetallicGlossMap("Metallic", 2D) = "white" {}
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

		// [NoScaleOffset] _SpecGlossMap("Specular", 2D) = "white" {}
		// _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)

		[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Scale", Range(0, 1)) = 1.0

		[NoScaleOffset] _BentNormalMap("Bent Normal Map", 2D) = "bump" {}
		[Toggle(_BENTNORMALMAP)] _BentNormalMapOn("Use Bent Normal", Float) = 1.0
		_SelfReflectionAmount("Self Reflection Amount", Range(0, 1)) = 0.1

		[Toggle(_SPECULAR_AA)] _SpecularAA("Specular AA", Float) = 1.0

		[NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

		[NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
		_EmissionColor("Emission Color", Color) = (0,0,0)

		// Blending state
		[HideInInspector] _Surface("__surface", Float) = 0.0
		[HideInInspector] _Blend("__blend", Float) = 0.0
		[HideInInspector] _AlphaClip("__clip", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
		[HideInInspector] _Cull("__cull", Float) = 2.0

		[ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff(_GLOSSYREFLECTIONS_OFF)] _EnvironmentReflections("Environment Reflections", Float) = 1.0
		[ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Receive Shadows", Float) = 1.0

		// Editmode props
		[HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
	}

	SubShader
	{
		// With SRP we introduce a new "RenderPipeline" tag in Subshader. This allows to create shaders
		// that can match multiple render pipelines. If a RenderPipeline tag is not set it will match
		// any render pipeline. In case you want your subshader to only run in LWRP set the tag to
		// "UniversalRenderPipeline"
		Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True"}
		LOD 300

		// ------------------------------------------------------------------
		// Forward pass. Shades GI, emission, fog and all lights in a single pass.
		// Compared to Builtin pipeline forward renderer, LWRP forward renderer will
		// render a scene with multiple lights with less drawcalls and less overdraw.
		Pass
		{
			// "Lightmode" tag must be "UniversalForward" or not be defined in order for
			// to render objects.
			Name "StandardLit"
			Tags{"LightMode" = "UniversalForward"}

			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]
			Cull[_Cull]

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard SRP library
			// All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// -------------------------------------
			// Material Keywords
			// unused shader_feature variants are stripped from build automatically
			// #pragma shader_feature _NORMALMAP
			#pragma shader_feature _ALPHATEST_ON
			#pragma shader_feature _ALPHAPREMULTIPLY_ON
			// #pragma shader_feature _EMISSION
			// #pragma shader_feature _METALLICSPECGLOSSMAP
			// #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			// #pragma shader_feature _OCCLUSIONMAP

			#define _NORMALMAP 1
			#define _EMISSION 1
			#define _METALLICSPECGLOSSMAP 1
			#define _OCCLUSIONMAP 1

			#pragma multi_compile _ _BENTNORMALMAP
			#pragma multi_compile _ _SPECULAR_AA

			#pragma shader_feature _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature _GLOSSYREFLECTIONS_OFF
			// #pragma shader_feature _SPECULAR_SETUP
			#pragma shader_feature _RECEIVE_SHADOWS_OFF

			// -------------------------------------
			// Universal Render Pipeline keywords
			// When doing custom shaders you most often want to copy and past these #pragmas
			// These multi_compile variants are stripped from the build depending on:
			// 1) Settings in the LWRP Asset assigned in the GraphicsSettings at build time
			// e.g If you disable AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
			// will be stripped from build
			// 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
			// but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// Including the following two function is enought for shading with Universal Pipeline. Everything is included in them.
			// Core.hlsl will include SRP shader library, all constant buffers not related to materials (perobject, percamera, perframe).
			// It also includes matrix/space conversion functions and fog.
			// Lighting.hlsl will include the light functions/data to abstract light constants. You should use GetMainLight and GetLight functions
			// that initialize Light struct. Lighting.hlsl also include GI, Light BDRF functions. It also includes Shadows.

			// Required by all Universal Render Pipeline shaders.
			// It will include Unity built-in shader variables (except the lighting variables)
			// (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
			// It will also include many utilitary functions. 
			// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Lib/BakerBoyCore.hlsl"

			// Include this if you are doing a lit shader. This includes lighting shader variables,
			// lighting and shadow functions
			// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Lib/BakerBoyLighting.hlsl"

			// Material shader variables are not defined in SRP or LWRP shader library.
			// This means _BaseColor, _BaseMap, _BaseMap_ST, and all variables in the Properties section of a shader
			// must be defined by the shader itself. If you define all those properties in CBUFFER named
			// UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost
			// of each drawcall.
			// In this case, for sinmplicity LitInput.hlsl is included. This contains the CBUFFER for the material
			// properties defined above. As one can see this is not part of the ShaderLibrary, it specific to the
			// LWRP Lit shader.
			// #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Lib/BakerBoyLitInput.hlsl"

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float4 tangentOS    : TANGENT;
				float2 uv           : TEXCOORD0;
				float2 uvLM         : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float2 uv                       : TEXCOORD0;
				float2 uvLM                     : TEXCOORD1;
				float4 positionWSAndFogFactor   : TEXCOORD2; // xyz: positionWS, w: vertex fog factor
				half3  normalWS                 : TEXCOORD3;

				#if _NORMALMAP || _BENTNORMALMAP
				half3 tangentWS                 : TEXCOORD4;
				half3 bitangentWS               : TEXCOORD5;
				#endif

				#ifdef _MAIN_LIGHT_SHADOWS
				float4 shadowCoord              : TEXCOORD6; // compute shadow coord per-vertex for the main light
				#endif
				float4 positionCS               : SV_POSITION;
			};

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output;

				// VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space)
				// Our compiler will strip all unused references (say you don't use view space).
				// Therefore there is more flexibility at no additional cost with this struct.
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

				// Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
				// in world space. If not used it will be stripped.
				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

				// Computes fog factor per-vertex.
				float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

				// TRANSFORM_TEX is the same as the old shader library.
				output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
				output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

				output.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);
				output.normalWS = vertexNormalInput.normalWS;

				// Here comes the flexibility of the input structs.
				// In the variants that don't have normal map defined
				// tangentWS and bitangentWS will not be referenced and
				// GetVertexNormalInputs is only converting normal
				// from object to world space
				#if _NORMALMAP || _BENTNORMALMAP
				output.tangentWS = vertexNormalInput.tangentWS;
				output.bitangentWS = vertexNormalInput.bitangentWS;
				#endif

				#ifdef _MAIN_LIGHT_SHADOWS
				// shadow coord for the main light is computed in vertex.
				// If cascades are enabled, LWRP will resolve shadows in screen space
				// and this coord will be the uv coord of the screen space shadow texture.
				// Otherwise LWRP will resolve shadows in light space (no depth pre-pass and shadow collect pass)
				// In this case shadowCoord will be the position in light space.
				// output.shadowCoord = GetShadowCoord(vertexInput);
				output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
				#endif
				// We just use the homogeneous clip position from the vertex input
				output.positionCS = vertexInput.positionCS;
				return output;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				// Surface data contains albedo, metallic, specular, smoothness, occlusion, emission and alpha
				// InitializeStandarLitSurfaceData initializes based on the rules for standard shader.
				// You can write your own function to initialize the surface data of your shader.
				SurfaceData surfaceData;
				InitializeStandardLitSurfaceData(input.uv, surfaceData);

				#if _SPECULAR_AA
				// Modulate smoothness by geometry normal (alleviates some specular aliasing)
				ModulateSmoothnessByNormal(surfaceData.smoothness, input.normalWS.xyz);
				#endif

				#if _NORMALMAP
				half3 normalWS = TransformTangentToWorld(surfaceData.normalTS,
					half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
				#else
				half3 normalWS = input.normalWS;
				#endif
				normalWS = normalize(normalWS);

				half3 ambientNormal = normalWS;

				#if _BENTNORMALMAP
				half3 bentNormalWS = TransformTangentToWorld(surfaceData.bentNormalTS,
					half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
				ambientNormal = bentNormalWS;
				#else
				half3 bentNormalWS = normalWS;
				#endif


				#ifdef LIGHTMAP_ON
				// Normal is required in case Directional lightmaps are baked
				half3 bakedGI = SampleLightmap(input.uvLM, ambientNormal);
				#else
				// Samples SH fully per-pixel. SampleSHVertex and SampleSHPixel functions
				// are also defined in case you want to sample some terms per-vertex.
				half3 bakedGI = SampleSH(ambientNormal);
				#endif

				float3 positionWS = input.positionWSAndFogFactor.xyz;
				half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);

				// BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
				// It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
				// below with your own.
				BRDFData brdfData;
				InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

				// Light struct is provide by LWRP to abstract light shader variables.
				// It contains light direction, color, distanceAttenuation and shadowAttenuation.
				// LWRP take different shading approaches depending on light and platform.
				// You should never reference light shader variables in your shader, instead use the GetLight
				// funcitons to fill this Light struct.
				#ifdef _MAIN_LIGHT_SHADOWS
				// Main light is the brightest directional light.
				// It is shaded outside the light loop and it has a specific set of variables and shading path
				// so we can be as fast as possible in the case when there's only a single directional light
				// You can pass optionally a shadowCoord (computed per-vertex). If so, shadowAttenuation will be
				// computed.
				Light mainLight = GetMainLight(input.shadowCoord);
				#else
				Light mainLight = GetMainLight();
				#endif

				// Mix diffuse GI with environment reflections.
				half3 color = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, bentNormalWS, viewDirectionWS);

				// LightingPhysicallyBased computes direct light contribution.
				// Use the bent normal here instead to get some nice light occlusion
				color += LightingPhysicallyBased(brdfData, mainLight, bentNormalWS, bentNormalWS, viewDirectionWS);

				// Additional lights loop
				#ifdef _ADDITIONAL_LIGHTS

				// Returns the amount of lights affecting the object being renderer.
				// These lights are culled per-object in the forward renderer
				int additionalLightsCount = GetAdditionalLightsCount();
				for (int i = 0; i < additionalLightsCount; ++i)
				{
					// Similar to GetMainLight, but it takes a for-loop index. This figures out the
					// per-object light index and samples the light buffer accordingly to initialized the
					// Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
					Light light = GetAdditionalLight(i, positionWS);

					// Same functions used to shade the main light.
					// Use the bent normal here instead to get some nice light occlusion
					color += LightingPhysicallyBased(brdfData, light, bentNormalWS, bentNormalWS, viewDirectionWS);
				}
				#endif
				// Emission
				color += surfaceData.emission;

				// half reflOcclusion = GetReflectionOcclusion(reflect(-viewDirectionWS, normalWS), bentNormalWS, surfaceData.occlusion, 1-_Smoothness);
				// color = float3(reflOcclusion, 0, 0);

				float fogFactor = input.positionWSAndFogFactor.w;

				// Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
				// with a custom one.
				color = MixFog(color, fogFactor);
				return half4(color, surfaceData.alpha);
			}
			ENDHLSL
		}

		// Used for rendering shadowmaps
		UsePass "Universal Render Pipeline/Lit/ShadowCaster"

		// Used for depth prepass
		// If shadows cascade are enabled we need to perform a depth prepass. 
		// We also need to use a depth prepass in some cases camera require depth texture
		// (e.g, MSAA is enabled and we can't resolve with Texture2DMS
		UsePass "Universal Render Pipeline/Lit/DepthOnly"

		// Used for Baking GI. This pass is stripped from build.
		UsePass "Universal Render Pipeline/Lit/Meta"
	}
}