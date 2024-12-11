// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "NeoFPS/Standard/Blink Panner"
{
	Properties
	{
		_Colour("Colour", Color) = (0.4438857,0.9736407,0.990566,1)
		_Image("Image", 2D) = "white" {}
		_FixedAlpha("FixedAlpha", 2D) = "white" {}
		_PanningSpeed("PanningSpeed", Range( -100 , 100)) = 1
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Overlay+0" }
		Cull Off
		Blend One One
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha noshadow 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Image;
		uniform float4 _Image_ST;
		uniform half _PanningSpeed;
		uniform half4 _Colour;
		uniform sampler2D _FixedAlpha;
		uniform half4 _FixedAlpha_ST;
		uniform float _Cutoff = 0.5;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Image = i.uv_texcoord * _Image_ST.xy + _Image_ST.zw;
			half mulTime19 = _Time.y * _PanningSpeed;
			half2 appendResult17 = (half2(uv_Image.x , ( uv_Image.y + mulTime19 )));
			half4 tex2DNode1 = tex2D( _Image, appendResult17 );
			float2 uv_FixedAlpha = i.uv_texcoord * _FixedAlpha_ST.xy + _FixedAlpha_ST.zw;
			half grayscale24 = Luminance(tex2DNode1.rgb);
			half temp_output_9_0 = ( tex2DNode1.a * tex2D( _FixedAlpha, uv_FixedAlpha ).r * grayscale24 );
			o.Albedo = ( (( tex2DNode1 * _Colour )).rgb * tex2DNode1.a * _Colour.a * temp_output_9_0 );
			o.Alpha = temp_output_9_0;
			clip( temp_output_9_0 - _Cutoff );
		}

		ENDCG
	}
}
/*ASEBEGIN
Version=18909
2582;730;1010;994;2303.578;430.5962;1.932251;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;15;-1936.291,-381.1813;Inherit;True;Property;_Image;Image;1;0;Create;True;0;0;0;True;0;False;23ba756402165e54d902903d853f10a4;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;4;-2021.021,86.4485;Inherit;False;Property;_PanningSpeed;PanningSpeed;3;0;Create;True;0;0;0;False;0;False;1;1;-100;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;19;-1646.669,130.9919;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;6;-1630.496,-195.4556;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;18;-1418.021,47.15408;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;17;-1358.808,-168.9991;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;1;-1180.3,-196.3679;Inherit;True;Property;_PannerTexture;PannerTexture;3;0;Create;True;0;0;0;True;0;False;15;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;20;-922.9274,71.32944;Inherit;False;Property;_Colour;Colour;0;0;Create;True;0;0;0;True;0;False;0.4438857,0.9736407,0.990566,1;0.4438857,0.9736407,0.990566,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-1159.824,405.3182;Inherit;True;Property;_FixedAlpha;FixedAlpha;2;0;Create;True;0;0;0;True;0;False;-1;32c2db27cac028548839d9cbd4a2d30d;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCGrayscale;24;-1084.328,317.2852;Inherit;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-587.6061,-187.0807;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-725.8625,409.258;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;12;-405.3727,-185.7211;Inherit;False;FLOAT3;0;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-391.3352,50.31241;Inherit;True;4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-49.52575,269.4202;Half;False;True;-1;2;;0;0;Standard;NeoFPS/Standard/Blink Panner;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;False;Transparent;;Overlay;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;4;1;False;-1;1;False;-1;0;1;False;-1;1;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;4;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;19;0;4;0
WireConnection;6;2;15;0
WireConnection;18;0;6;2
WireConnection;18;1;19;0
WireConnection;17;0;6;1
WireConnection;17;1;18;0
WireConnection;1;0;15;0
WireConnection;1;1;17;0
WireConnection;24;0;1;0
WireConnection;21;0;1;0
WireConnection;21;1;20;0
WireConnection;9;0;1;4
WireConnection;9;1;2;1
WireConnection;9;2;24;0
WireConnection;12;0;21;0
WireConnection;13;0;12;0
WireConnection;13;1;1;4
WireConnection;13;2;20;4
WireConnection;13;3;9;0
WireConnection;0;0;13;0
WireConnection;0;9;9;0
WireConnection;0;10;9;0
ASEEND*/
//CHKSM=86C3AAD7B9C2E5AA07B163557EB6868903014912