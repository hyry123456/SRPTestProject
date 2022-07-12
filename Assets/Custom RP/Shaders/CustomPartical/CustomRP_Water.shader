Shader "Custom RP/Particle/CustomRP_Water"
{
    Properties
    {
        [Header(Base Setting)]
        [Space(10)]
        _MainTex ("Texture", 2D) = "white" {}
        _TessDegree("Particle Count", Range(0, 100)) = 10
        [HDR] _Color("Color", Color) = (1,1,1,1)
        
        _ParticleLength("Particle Length", Range(0, 3)) = 0.5
        _RowCount ("Row count", INT) = 1
        _ColumnCount ("Column count", INT) = 1

        [Header(Other Setting)]
        [Space(10)]
        
        _OutTime("Particle Out Time", float) = 0.1
        _OffsetTime ("Offset time", float) = 1
        //用来确定最后水花的效果
        _VerticalStart("Ramdom Velocity Begin", VECTOR) = (0,0,0)
        _VerticalEnd("Ramdom Velocity End", VECTOR) = (0,0,0)

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    }
    SubShader
    {

        HLSLINCLUDE
            #include "../../ShaderLibrary/Common.hlsl"
		ENDHLSL
        Pass
        {
            Tags {"LightMode" = "WaterWidth"}
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma target 4.6

            #pragma vertex tessVertAll
            #pragma geometry geom
            #pragma fragment WidthFrag
            //细分控制
            #pragma hull hull_All
            //细分计算
            #pragma domain domain_All

            #pragma shader_feature  _MOVE_SIZE
            #pragma shader_feature  _OFFSET_SIZE
            #pragma shader_feature _CURVE_ALPHA

            #include "HLSL/CRP_ParticleWaterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags {"LightMode" = "WaterDepthAndNormal"}
            ZWrite On
            HLSLPROGRAM
            #pragma target 4.6

            #pragma vertex tessVertAll
            #pragma geometry geom
            #pragma fragment NormalAndDepthFrag
            // #pragma fragment WidthFrag
            //细分控制
            #pragma hull hull_All
            //细分计算
            #pragma domain domain_All

            #pragma shader_feature  _MOVE_SIZE
            #pragma shader_feature  _OFFSET_SIZE
            #pragma shader_feature  _ALPHA

            #include "HLSL/CRP_ParticleWaterPass.hlsl"
            ENDHLSL
        }

    }
}
