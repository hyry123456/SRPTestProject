#ifndef CUSTOM_PARTICLE_WATER_PASS
#define CUSTOM_PARTICLE_WATER_PASS

struct FragInput{
    float2 uv : TEXCOORD0;
    float4 pos : SV_POSITION;
    //当前循环到的比例，开头为0，终点为1，数据设定[纹理时间，距离时间](texTime, DisTime)
    float time : TEXCOORD2;
    //预留位置，存储一些需要自定义的数据
    float4 otherDate : TEXCOORD3;
    //存储世界空间位置
    float3 worldPos : TEXCOORD4;
    float4 otherDate2 : TEXCOORD5;
};


//细分文件，进行细分计算
#include "../../ShaderLibrary/Tesseilation.hlsl"

#include "CRP_ParticleWaterInput.hlsl"

//当第一条射线就碰到物体时执行的方法
void OnePointEnd(TessOutput_All IN, float moveTime, inout TriangleStream<FragInput> tristream){
    float3 begin = float3(IN.uv0.xy, IN.uv1.x);
    float3 center = float3(IN.uv2.xy, IN.uv1.y);
    float3 end = float3(IN.uv3.xy, IN.uv4.x);
    IN.vertex.xyz = Get3PointBezier(begin, center, end, moveTime);
    Move_outOnePoint(tristream, IN, moveTime, end);
}

//第二条射线碰撞到的情况
void TwoPointEnd(TessOutput_All IN, float moveTime, inout TriangleStream<FragInput> tristream){
    float3 begin = float3(IN.uv0.xy, IN.uv1.x);
    float3 center = float3(IN.uv2.xy, IN.uv1.y);
    float3 end = float3(IN.uv3.xy, IN.uv4.x);

    float3 target1 = Get3PointBezier(begin, center, end, moveTime);

    begin = end;
    center = float3(IN.uv5.xy, IN.uv4.y);
    end = IN.tangent.xyz;
    float3 target2 = Get3PointBezier(begin, center, end, moveTime);

    IN.vertex.xyz = target1 + (target2 - target1) * moveTime;
    Move_outOnePoint(tristream, IN, moveTime, end);
}

//偏移阶段
void Offset(TessOutput_All IN, float offsetTime, inout TriangleStream<FragInput> tristream, float4 random, float3 begin){

    if(offsetTime > 1 || offsetTime < 0) return;

    float3 dir0 = lerp(_VerticalStart, _VerticalEnd, random.xyz);
    float3 normal = IN.normal;
    //确定旋转矩阵
    float cosVal = dot(normalize( normal ), float3(0, 1, 0));
    float sinVal = sqrt(1 - cosVal * cosVal);

    float3x3 xyMatrix = float3x3(-cosVal, sinVal, 0,
                                -sinVal, -cosVal, 0,
                                0, 0, 1);

    float3x3 yzMatrix = float3x3(1, 0, 0,
                                0, -cosVal, sinVal,
                                0, -sinVal, -cosVal);
    

    float3x3 xzMatrix = float3x3(-cosVal, 0, -sinVal,
                                0, 1, 0,
                                sinVal, 0, -cosVal);

    float3 targetDir = mul(xzMatrix, mul( yzMatrix, mul(xyMatrix, dir0) ));

    IN.vertex.xyz = begin + targetDir * offsetTime;
    Offset_outOnePoint(tristream, IN, offsetTime);
}

//加载一个顶点            
void LoadWater(TessOutput_All IN, inout TriangleStream<FragInput> tristream){
    //不在运行周期中
    if(_Time.y > IN.tangent.w) return;

    //随机xyzw值，这么处理主要是保证数据能够足够乱序，如果有更好的可以直接替换
    float4 ramdom = 0;
    ramdom.x = frac( abs( cos( (IN.vertex.x / IN.vertex.z + IN.vertex.z / IN.vertex.x + sin(IN.tangent.w) )  * 100000 ) ) * 1000 );
    ramdom.y = frac( abs( sin( (IN.vertex.y / IN.vertex.z - IN.vertex.z + cos(IN.tangent.w) )  * 100000 ) ) * 1000 );
    ramdom.z = frac( abs( sin( (ramdom.x + ramdom.y )  * 100000 ) ) * 1000 );
    ramdom.w = frac( abs( sin( (ramdom.x + ramdom.y + ramdom.z) * PI * 100000 ) ) * 1000 );

    //这批粒子的启动时间，IN.uv6.x是移动时间
    float beginTime = IN.tangent.w - _OutTime - _OffsetTime - IN.uv6.x;
    //这个顶点的发出时间
    float outTime = _OutTime * ramdom.w + beginTime;
    //不在顶点发出时间，不进行射出
    if(_Time.y < outTime  ) return;

    //确定这个粒子在发出时间的哪个阶段，归为0-1
    float partiTime = (_Time.y - outTime) / IN.uv6.x;

    //偏移阶段，也就是碰撞后的一段时间
    if(partiTime >= 1){
        float3 end = (float3)0;
        if(step(0.5, IN.color.x))
            end = float3(IN.uv3.xy, IN.uv4.x);
        else
            end = IN.tangent.xyz;

        Offset(IN, (_Time.y - outTime - IN.uv6.x) / _OffsetTime, tristream, ramdom, end );
        return;
    }

    // float ramdomClip = abs( sin(ramdom.x + ramdom.y + ramdom.z + ramdom.w) );

    
    if(step(0.5, IN.color.x)){    //第一条射线射中
        OnePointEnd(IN, partiTime, tristream);
    }
    else{    //第二条射线射中
        TwoPointEnd(IN, partiTime, tristream);
    }
}


[maxvertexcount(30)]
void geom(triangle TessOutput_All IN[3], inout TriangleStream<FragInput> tristream)
{
    LoadWater(IN[0], tristream);
    LoadWater(IN[1], tristream);
    LoadWater(IN[2], tristream);
}

float4 WidthFrag (FragInput i) : SV_Target
{
    float2 uv = i.uv;
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;

    #ifdef _CURVE_ALPHA
        if(i.otherDate.x < 0.1){
            col *= saturate( LoadCurveTime( i.time.x, _MoveAlphaPointCount, _MoveAlphaPointArray ) );
        }
        else {
            col *= saturate( LoadCurveTime( i.time.x, _OffsetAlphaPointCount, _OffsetAlphaPointArray ) );
        }
    #endif
    return col;
}

float4 NormalAndDepthFrag (FragInput i) : SV_Target
{
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    #ifdef _CURVE_ALPHA
        if(i.otherDate.x < 0.1){
            col *= saturate( LoadCurveTime( i.time.x, _MoveAlphaPointCount, _MoveAlphaPointArray ) );
        }
        else 
            col *= saturate( LoadCurveTime( i.time.x, _OffsetAlphaPointCount, _OffsetAlphaPointArray ) );
    #endif
    clip(col.a - 0.5);

    float3 normal = normalize(i.worldPos - i.otherDate.yzw);
    return float4(normal * 0.5 + 0.5, 1);
}

#endif