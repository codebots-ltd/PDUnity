Shader "PDUnityShader" {
    Properties {
		[HideInInspector]_MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}

		[HideInInspector]
		_BlendSrcMode ("Src Mode", Float) = 1
	    
	    [HideInInspector]
	    _BlendDstMode ("Dest Mode", Float) = 10
		
		[HideInInspector]
		_OpacityModifyRGB ("Opacity Modify RGB", Int) = 0
    }
    SubShader {
    
    	Tags { 
    		"QUEUE"="Transparent" 
    		"IGNOREPROJECTOR"="true"
    	}

		ZWrite Off
		Cull Off
		Blend [_BlendSrcMode] [_BlendDstMode]
		AlphaTest Greater 0.01
		
        Pass {
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			
            uniform sampler2D _MainTex;
            uniform int _OpacityModifyRGB;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 texcoord : TEXCOORD0;
                fixed4 color : COLOR0;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = i.color;
				
				if (_OpacityModifyRGB == 1) {
					color = fixed4 (color.r * color.a,
									color.g * color.a,
									color.b * color.a,
									color.a);

				}

				return fixed4 (color * tex2D(_MainTex, i.texcoord));				
            }

			ENDCG
        }
    }
}