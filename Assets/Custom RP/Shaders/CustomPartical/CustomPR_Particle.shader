Shader "Custom RP/CustomPR_Particle"
{
    Properties
    {
        [Header(Base Setting)]
        [Space(10)]
        _MainTex ("Main Texture", 2D) = "white" {}

        _TessDegree("Particle Count", Range(0, 100)) = 10
        [HDR] _ParticleColor("Particle Color", Color) = (1,1,1,1)
        
        _ParticleLength("Particle Length", Range(0, 3)) = 0.5
        _RowCount ("Row count", INT) = 1
        _ColumnCount ("Column count", INT) = 1

        [Header(Distortion Setting)]
        [Space(10)]
        [Toggle(_DISTORTION)] _UseDistorion("Use Distorion", float) = 0
        [NoScaleOffset] _DistortionTex("Distortion Texture", 2D) = "Bump"{}
        _DistortionSize ("Distortion Size", Range(0, 1)) = 1
        _DistortionBlend ("Distortion Blend", Range(0, 1)) = 1

        [Header(Particles Setting)]
        [Space(10)]
        _NearFadeDistance("Near Fade Distance", Range(0, 5)) = 1
        _NearFadeRange ("Near Fade Range", Range(0, 5)) = 1
        _SoftParticlesDistance("Soft Particle Distance", Range(0.0, 10.0)) = 0.01
        _SoftParticlesRange ("Soft Particle Range", Range(0.01, 10.0)) = 1

        [Header(Other Setting)]
        [Space(10)]
        //VFX的移动根据是用两个方向存储xyz的波动范围，然后随机选值，这里面的值即表示方向，也表示速度
        _VerticalStart("Ramdom Velocity Begin", VECTOR) = (0,0,0)
        _VerticalEnd("Ramdom Velocity End", VECTOR) = (0,0,0)

        _LifeTime("Particle Life Time", FLOAT) = 1

        [Toggle(_ANIMATE_UV)] _UseAnimateUV("Use Animate UV", float) = 0


    }
    SubShader
    {
        Tags {"LightMode" = "CustomLit"}
        Pass
        {

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.6

            #pragma vertex tessvert
            #pragma fragment SimpleFrag
            #pragma geometry geom
            //细分控制
            #pragma hull hull
            //细分计算
            #pragma domain domain
            
            #pragma shader_feature  _CURVE_MOVE
            #pragma shader_feature  _MOVE_HIGHT
            #pragma shader_feature  _MOVE_WIDTH
            #pragma shader_feature  _CURVE_SIZE
            #pragma shader_feature  _CURVE_ALPHA
            #pragma shader_feature  _DISTORTION
            #pragma shader_feature  _ANIMATE_UV

            #include "HLSL/CRP_ParticlePass.hlsl"

            float3 _VerticalStart;
            float3 _VerticalEnd;
            float _LifeTime;

            void LoadOnePoint(TessOutPutSimple IN, inout TriangleStream<FragInput> tristream){
                //随机xy值
                float4 ramdom = 0;
                ramdom.x = frac( pow( cos( (IN.vertex.x + IN.vertex.z) * 100 ), 2) );
                ramdom.y = frac( pow( cos( (IN.vertex.y - IN.vertex.z) * 100 ), 2) );
                ramdom.z = frac( abs( sin( (ramdom.x + ramdom.y ) ) ) );

                ramdom.w = frac( abs( sin( (ramdom.x + ramdom.y + ramdom.z) * PI * 100000 ) ) * 1000 );

                float3 dir0 = lerp(_VerticalStart, _VerticalEnd, ramdom.xyz);

                float addTime = _LifeTime * ramdom.w;

                //归一化
                float time = fmod( (_Time.y + addTime), _LifeTime) / _LifeTime;

                dir0 *= time;

                dir0 = mul((float3x3)unity_ObjectToWorld, dir0);

                // //确定要移动曲线控制移动
                #ifdef _CURVE_MOVE
                    float moveVal = LoadCurveTime(time, _MovePointCount, _MovePointArray);
                    //控制y轴移动
                    #ifdef _MOVE_HIGHT
                        dir0.y = dir0.y * moveVal;
                    //控制水平移动
                    #elif _MOVE_WIDTH
                        dir0.xz = dir0.xz * moveVal;
                    #endif
                #endif
                
                IN.vertex = mul(unity_ObjectToWorld, float4(dir0,1));

                outOnePoint(tristream, IN, time);
            }
            ENDHLSL
        }
    }
}
