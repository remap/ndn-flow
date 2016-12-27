// shadertype <unity>
Shader "WahnStudio/HeightShader" {		  
    Properties{
		//LastLevel
        _LastLevelTexture("LastLevelTexture", 2D) = "white" {
            }
		_LastLevelColor ("LastLevelColor", color) = (0.8,0.9,0.9)
		_LastLevel("LastLevelLevel", Float) = 220
		_LastLevelTextureSize("LastLevelTextureSize", Float) = 50
		//Level3
		_Level3Texture("Level3Texture", 2D) = "white" {
            }
		_Level3Color("Level3Color", Color) = (0.75,0.53,0,1)
		_Level3("Level3", Float) = 190
		_Level3TextureSize("Level3TextureSize", Float) = 10
		//Level2
		_Level2Texture("Level2Texture", 2D) = "white" {
            }
		_Level2Color("Level2Color", Color) = (0.69,0.63,0.31,1)
		_Level2("Level2", Float) = 180
		_Level2TextureSize("Level2TextureSize", Float) = 10
		//Level1
		_Level1Texture("Level1Texture", 2D) = "white" {
            }
		_Level1Color("Level1Color", Color) = (0.65,0.86,0.63,1)
		_Level1("Level1", Float) = 140
		_Level1TextureSize("Level1TextureSize", Float) = 20
		//Beach		
		_BeachTexture("BeachTexture", 2D) = "white" {
            }
		_BeachColor("BeachColor", Color) = (0.9,0.86,0,1)
		_Beach("BeachLevel", Float) = 135
		_BeachTextureSize("BeachTextureSize", Float) = 100
		//UnderWater
		_WaterLevel("WaterLevel", Float) = 126
		_UnderWaterTexture("UnderWaterTexture", 2D) = "white" {} 
		_UnderWaterColor("UnderWaterColor", Color) = (0.37,0.78,0.92,1)
		_UnderWaterTextureSize("UnderwaterTextureSize", Float) = 6
		_Slope("Slope Fader", Range(0,1)) = .3
		_Brightness("Brightness", Range(0,1)) = 0.5
		
	}
	SubShader{
        Tags{
            "RenderType" = "Opaque" }
		LOD 300	

		CGPROGRAM
#pragma surface surf Standard vertex:vert 
#pragma target 3.0


	struct Input {
            float3 customColor;
            float3 worldPos;
            float2 uv_LastLevelTexture;
        }


;
        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o)
			o.customColor = abs(v.normal.y);
        }

		fixed4 SumFloat4(fixed4 tex1, fixed4 tex2) {
			fixed4 x = fixed4(tex1.r + tex2.r, tex1.g + tex2.g, tex1.b + tex2.b, tex1.a + tex2.a);
			return x;
		}
	
	sampler2D _LastLevelTexture;
        sampler2D _Level3Texture;
        sampler2D _Level2Texture;
        sampler2D _Level1Texture;
		sampler2D _BeachTexture;
		sampler2D _UnderWaterTexture;
        float _LastLevel;
        float4 _LastLevelColor;
        float _Level3;
        float4 _Level3Color;
        float _Level2;
        float4 _Level2Color;
        float _Level1;
        float4 _Level1Color;
        float _Slope;
        float _WaterLevel;
        float4 _UnderWaterColor;
        float _Brightness;
        float4 _BeachColor;
        float _Beach;
        float _LastLevelTextureSize;
		float _Level3TextureSize;
		float _Level2TextureSize;
		float _Level1TextureSize;
		float _BeachTextureSize;
		float _UnderWaterTextureSize;
		
		
        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed2 cm = IN.uv_LastLevelTexture;
            fixed4 cM = lerp(_LastLevelColor*1.3, SumFloat4(tex2D(_LastLevelTexture, cm*_LastLevelTextureSize).rgba*_LastLevelColor, _LastLevelColor), (_LastLevel*1.1 - IN.worldPos.y)/(_LastLevel*1.1 - _LastLevel)*_Slope);
            fixed4 cP = lerp(SumFloat4(tex2D(_LastLevelTexture, cm*_LastLevelTextureSize).rgba*_LastLevelColor, _LastLevelColor), SumFloat4(tex2D(_Level3Texture, cm*_Level3TextureSize).rgba*_Level3Color, tex2D(_LastLevelTexture, cm*_LastLevelTextureSize)*_LastLevelColor),   (_LastLevel - IN.worldPos.y) / (_LastLevel - _Level3)*_Slope);
            fixed4 c3 = lerp(SumFloat4(tex2D(_Level3Texture, cm*_Level3TextureSize).rgba*_Level3Color, tex2D(_LastLevelTexture, cm*_LastLevelTextureSize)*_LastLevelColor), SumFloat4(tex2D(_Level2Texture, cm*_Level2TextureSize).rgba*_Level2Color, tex2D(_Level3Texture, cm*_Level3TextureSize)*_Level3Color),   (_Level3 - IN.worldPos.y) / (_Level3 - _Level2)*_Slope);
            fixed4 c2 = lerp(SumFloat4(tex2D(_Level2Texture, cm*_Level2TextureSize).rgba*_Level2Color, tex2D(_Level3Texture, cm*_Level3TextureSize)*_Level3Color), SumFloat4(tex2D(_Level1Texture, cm*_Level1TextureSize).rgba*_Level1Color, tex2D(_Level2Texture, cm*_Level2TextureSize)*_Level2Color),   (_Level2 - IN.worldPos.y) / (_Level2 - _Level1)*_Slope);
			fixed4 c1 = lerp(SumFloat4(tex2D(_Level1Texture, cm*_Level1TextureSize).rgba*_Level1Color, tex2D(_Level2Texture, cm*_Level2TextureSize).rgba*_Level2Color), SumFloat4(tex2D(_BeachTexture, cm*_BeachTextureSize).rgba*_BeachColor, tex2D(_Level1Texture, cm*_Level1TextureSize)*_Level1Color),   (_Level1 - IN.worldPos.y) / (_Level1 - _Beach)*_Slope);
            fixed4 cB = lerp(SumFloat4(tex2D(_BeachTexture, cm*_BeachTextureSize).rgba*_BeachColor, tex2D(_Level1Texture, cm*_Level1TextureSize)*_Level1Color), tex2D(_BeachTexture, cm*_BeachTextureSize).rgba*_BeachColor*1.5, (_Beach - IN.worldPos.y) / (_Beach - _WaterLevel)*_Slope);
			fixed4 cW = lerp(tex2D(_BeachTexture, cm*_BeachTextureSize).rgba*_BeachColor*1.5, tex2D(_UnderWaterTexture, cm*_UnderWaterTextureSize).rgba*_UnderWaterColor, (_Beach - IN.worldPos.y) / (_Beach - _WaterLevel));
			
			if(IN.worldPos.y > _LastLevel*1.1)
			o.Albedo = _LastLevelColor.rgb*1.3;
            if (IN.worldPos.y <= _LastLevel*1.1)
			o.Albedo = cM.rgb;
            if (IN.worldPos.y <= _LastLevel)
			o.Albedo = cP.rgb;
            if (IN.worldPos.y <= _Level3)
			o.Albedo = c3.rgb;
            if (IN.worldPos.y <= _Level2)
			o.Albedo = c2.rgb;
			if(IN.worldPos.y <= _Level1)
			o.Albedo = c1.rgb;
            if (IN.worldPos.y <= _Beach)
			o.Albedo = cB.rgb;
            if (IN.worldPos.y <= _WaterLevel)
			o.Albedo = cW.rgb;
			o.Albedo *= _Brightness * 2; 
        }



	ENDCG
	}
		Fallback "Diffuse"
}