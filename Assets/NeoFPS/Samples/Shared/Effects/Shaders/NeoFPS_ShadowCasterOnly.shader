Shader "NeoFPS/Standard/ShadowCasterOnly"
{
    SubShader
    {
        Pass {
        Name "ShadowCaster"
        Tags { "LightMode" = "ShadowCaster" "IgnoreProjector" = "True" }
     
        Fog {Mode Off}
        ZWrite On ZTest LEqual Cull Back
 
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_shadowcaster noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd
        #include "UnityCG.cginc" 

        struct v2f {
            V2F_SHADOW_CASTER;
        };
 
        v2f vert( appdata_base v )
        {
            v2f o;
            TRANSFER_SHADOW_CASTER(o)
            return o;
        }
 
        float4 frag( v2f i ) : SV_Target
        {
            SHADOW_CASTER_FRAGMENT(i)
        }
        ENDCG
 
        }
    }
}