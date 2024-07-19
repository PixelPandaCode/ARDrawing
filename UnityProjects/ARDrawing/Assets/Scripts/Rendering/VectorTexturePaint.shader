// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/VectorTexturePaint"
{
	SubShader
	{
		// =====================================================================================================================
		// TAGS AND SETUP ------------------------------------------
		Tags { "RenderType"="Opaque" }
		LOD	   100
		ZTest  Off
		ZWrite Off
		Cull   Off

		Pass
		{
			CGPROGRAM
			// =====================================================================================================================
			// DEFINE AND INCLUDE ----------------------------------
			#pragma vertex   vert
			#pragma fragment frag

			
			#include "UnityCG.cginc"

			// =====================================================================================================================
			// DECLERANTIONS ----------------------------------
			struct appdata
			{
				float4 vertex   : POSITION;
				float2 uv	    : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float2 uv       : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
			};

			float4    _Cursor;
			float3 _SprayDirection;
			float4x4  mesh_Object2World;
			sampler2D _MainTex;
			float4	  _BrushColor;
			float	  _BrushOpacity;
			float	  _BrushHardness;
			float	  _BrushSize;
			sampler2D _CursorDataTex;
			sampler2D _SprayDirectionDataTex;
			int _CursorCount;

			// =====================================================================================================================
			// VERTEX FRAGMENT ----------------------------------

			v2f vert (appdata v)
			{
				v2f o;

				float2 uvRemapped   = v.uv.xy;
				       uvRemapped.y = 1. - uvRemapped.y;
					   uvRemapped   = uvRemapped *2. - 1.;

					   o.vertex     = float4(uvRemapped.xy, 0., 1.);
				       o.worldPos   = mul(mesh_Object2World, v.vertex);
				       o.uv         = v.uv;
					   o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
				return o;
			}

			float planeDistance(float3 p, float3 pointA, float3 pointB)
            {
				float3 lineVec = pointB - pointA;
                float3 normal = cross(lineVec,  _SprayDirection);
                float d = -dot(normal, pointA);
				float dist = (dot(normal, p) + d) / length(normal);
				return abs(dist);  // Return the absolute value to get the distance
            }

			bool areOnTheSameSide(float3 A, float3 B, float3 C, float3 D) {
                float3 AB = B - A;
                float3 AC = C - A;
                float3 AD = D - A;

                float3 crossC = cross(AB, AC);
                float3 crossD = cross(AB, AD);

                return dot(crossC, crossD) >= 0;
            }
			
            bool isPointWithinRays(float3 A, float3 B, float3 C) {
				// Calculate vectors from points A and B to point C
				float3 AC = C - A;
				float3 AB = B - A;
				// Calculate cross products with spray direction
				float3 crossAB = cross(_SprayDirection, AB);
				float3 crossAC = cross(_SprayDirection, AC);
				// Determine if C is on the same side of spray direction from A as B
				bool BCsameSide = dot(crossAB, crossAC) >= 0;

				// Calculate vectors from point B to points A and C
				float3 BC = C - B;
				float3 BA = A - B;
				// Calculate cross products with spray direction
				float3 crossBA = cross(_SprayDirection, BA);
				float3 crossBC = cross(_SprayDirection, BC);
				// Determine if C is on the same side of spray direction from B as A
				bool ACsameSide = dot(crossBA, crossBC) >= 0;

				return BCsameSide && ACsameSide;
            }
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 col  = tex2D(_MainTex, i.uv);
				float  size = _BrushSize;
				float  soft = _BrushHardness;
				for (int idx = 0; idx < _CursorCount; ++idx)
				{
                    int x = idx % 512;
                    int y = idx / 512;
                    float4 _Cursor = tex2D(_CursorDataTex, float2(x / 512.0, y / 512.0));
					// if _Cursor.w is 0 then it is the start of new stroke
					float  f	= distance(_Cursor.xyz, i.worldPos);
					if (idx != 0 && f > size && _Cursor.w != 0.5) {
						int lx = (idx - 1) % 512;
						int ly = (idx - 1) / 512;
						float4 _LastCursor = tex2D(_CursorDataTex, float2(lx / 512.0, ly / 512.0));
						if (isPointWithinRays( _Cursor.xyz, _LastCursor.xyz, i.worldPos))
						{
							f = min(f, planeDistance(i.worldPos, _Cursor.xyz, _LastCursor.xyz));
						}
					}
					f = 1.-smoothstep(size*soft, size, f);
					col  = lerp(col, _BrushColor, f * _BrushOpacity);
					col  = saturate(col);
				}
				return col;
			}
			ENDCG
		}
	}
}
