Shader "DrashVR/BlackSmearCorrection"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Brightness ("Brightness", Range(0.0, 2.0)) = 1.0
		_Contrast ("Contrast", Range(0.0, 2.0)) = 1.0
	}

	SubShader
	{
		Pass
		{
			Fog { Mode off }
			Cull Off 
			Lighting Off
			ZWrite Off
			ZTest Always 
			
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest 
				#include "UnityCG.cginc"

				sampler2D _MainTex;
				float _Brightness;
				float _Contrast;

				fixed4 frag(v2f_img i) : COLOR
				{
					fixed4 col = tex2D(_MainTex, i.uv);
					fixed4 factor = fixed4(1.0, 1.0, 1.0, col.a);
					return saturate((col * _Brightness - factor) * _Contrast + factor);
				}
			ENDCG
		}
	}

	FallBack off
}
