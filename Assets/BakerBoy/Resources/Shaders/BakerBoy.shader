Shader "Hidden/BakerBoy"
{
	SubShader
	{
		CGINCLUDE
		#include "UnityCG.cginc"

		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f
		{
			float4 vertex 	: SV_POSITION;
			float2 texcoord	: TEXCOORD0;
			float3 worldPos	: TEXCOORD1;
			float3x3 TBN	: TEXCOORD2;
		};

		float _UseUV2;

		v2f vertWorld (appdata v)
		{
			v2f o = (v2f)0;

			o.vertex 	= UnityObjectToClipPos(v.vertex);
			o.texcoord 	= v.texcoord;
			o.worldPos	= mul(UNITY_MATRIX_M, v.vertex).xyz;

			float3 worldNormal = UnityObjectToWorldNormal(v.normal);
			float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
			float  tangentSign = v.tangent.w * unity_WorldTransformParams.w;
			float3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;

			o.TBN = transpose(float3x3(worldTangent, worldBinormal, worldNormal));

			return o;
		}

		v2f vertUV (appdata v)
		{
			v2f o = (v2f)0;

			#if _USE_UV2
			o.vertex 	= float4((v.texcoord1 * 2 - 1) * float2(1, -1), 0.5, 1);
			#else
			o.vertex 	= float4((v.texcoord * 2 - 1) * float2(1, -1), 0.5, 1);
			#endif
			o.texcoord 	= v.texcoord;
			o.worldPos	= mul(UNITY_MATRIX_M, v.vertex).xyz;

			float3 worldNormal = UnityObjectToWorldNormal(v.normal);
			float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
			float  tangentSign = v.tangent.w * unity_WorldTransformParams.w;
			float3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;

			o.TBN = transpose(float3x3(worldTangent, worldBinormal, worldNormal));

			return o;
		}

		float3 _LightDir;
		float _ShadowDepthBias;

		UNITY_DECLARE_SHADOWMAP(_ShadowMap);
		float4x4 _WorldToShadow;

		float GetAttenuation (float3 worldPos)
		{
			// float3 lightPos = mul(_WorldToShadow, float4(worldPos, 1.0)).xyz;
			float4 lightPos = mul(_WorldToShadow, float4(worldPos - _LightDir * _ShadowDepthBias, 1.0));
			return UNITY_SAMPLE_SHADOW_PROJ(_ShadowMap, lightPos);
		}

		sampler2D _PositionMap;
		sampler2D _WorldNormalMap;
		ENDCG

		Pass
		{
			Name "PositionNormal"
			Cull Off
			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vertUV
			#pragma fragment frag
			#pragma multi_compile _ _USE_UV2

			struct FragOut
			{
				float4 worldPosition : SV_Target0;
				float4 worldNormal : SV_Target1;
			};

			sampler2D _NormalMap;
			float _HasNormalMap;

			FragOut frag (v2f i)
			{
				FragOut o;

				float3 tNormal = float3(0, 0, 1);
				if (_HasNormalMap == 1.0)
					tNormal = UnpackNormal(tex2D(_NormalMap, i.texcoord));

				o.worldPosition = float4(i.worldPos, 1);
				o.worldNormal = float4(normalize(mul(i.TBN, tNormal)), 1);

				return o;
			}
			ENDCG
		}

		Pass
		{
			Name "Gather"
			Cull Off
			ZWrite On
			ZTest LEqual
			
			Blend One One

			CGPROGRAM
			#pragma vertex vertUV
			#pragma fragment frag
			#pragma multi_compile _ _USE_UV2

			struct FragOut
			{
				float4 occlusion : SV_Target0;
				float4 bentNormal : SV_Target1;
			};

			float _GatherAmount;

			FragOut frag (v2f i) : SV_Target
			{
				FragOut o;

				float3 worldPos = tex2D(_PositionMap, i.texcoord);
				float3 worldNormal = tex2D(_WorldNormalMap, i.texcoord);

				float  attenuation = GetAttenuation(worldPos);

				// float nDotL = dot(worldNormal, -_LightDir);
				// if (nDotL > 0)
				// 	attenuation *= nDotL;
					// attenuation *= 1;
				// else
				// 	attenuation = 1;

				attenuation *= max(0, dot(worldNormal, -_LightDir));
				// return float4(worldNormal, 1) * _GatherAmount;

				o.occlusion = float4(attenuation.xxx, 1) * _GatherAmount;
				o.bentNormal = float4(-_LightDir * lerp(1, attenuation, 0.9), 1) * _GatherAmount;

				return o;
			}
			ENDCG
		}

		Pass
		{
			Name "PackNormal"
			Cull Off

			CGPROGRAM
			#pragma vertex vertUV
			#pragma fragment frag
			#pragma multi_compile _ _USE_UV2

			float4 frag (v2f i) : SV_Target
			{
				float4 normal = tex2D(_WorldNormalMap, i.texcoord);
				normal.xyz = normalize(mul(transpose(i.TBN), normal.xyz))*0.5+0.5;

				return normal;
			}
			ENDCG
		}
	}
}