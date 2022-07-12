#ifndef CUSTOM_PARTICLE_WATER_INPUT
#define CUSTOM_PARTICLE_WATER_INPUT

#include "../../../ShaderLibrary/Common.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)

    float _OutTime;
    float _OffsetTime;
    float3 _VerticalStart;
    float3 _VerticalEnd;

    //移动大小控制
    float4 _MoveSizePointArray[10];
    int _MoveSizePointCount;

    //移动透明控制
    float4 _MoveAlphaPointArray[10];
    int _MoveAlphaPointCount;

    //偏移大小控制
    float4 _OffsetSizePointArray[10];
    int _OffsetSizePointCount;

    //移动透明控制
    float4 _OffsetAlphaPointArray[10];
    int _OffsetAlphaPointCount;



    float _ParticleLength;

	float _RowCount;
	float _ColumnCount;
	float4 _MainTex_ST;
    float4 _Color;

CBUFFER_END

//时间控制函数，用来读取Curve中的值
float LoadCurveTime(float nowTime, int _Count, float4 _PointArray[10]){
    //有数据才循环
    for(int i=0; i<_Count; i++){
        //找到在范围中的
        if(_PointArray[i].x < nowTime && _PointArray[i+1].x > nowTime){
            //Unity的Curve的曲线本质上是一个三次多项式插值，公式为：y = ax^3 + bx^2 + cx +d
            float a = ( _PointArray[i].w + _PointArray[i+1].z ) * ( _PointArray[i + 1].x - _PointArray[i].x ) - 2 * ( _PointArray[i + 1].y - _PointArray[i].y );
            float b = ( -2 * _PointArray[i].w - _PointArray[i + 1].z ) * ( _PointArray[i + 1].x - _PointArray[i].x ) + 3 * ( _PointArray[i + 1].y - _PointArray[i].y );
            float c = _PointArray[i].w * ( _PointArray[i + 1].x - _PointArray[i].x );
            float d = _PointArray[i].y;

            float trueTime = (nowTime - _PointArray[i].x) / ( _PointArray[i+1].x  - _PointArray[i].x);
            return a * pow( trueTime, 3 ) + b * pow( trueTime, 2 ) + c * trueTime + d;
            
        }

    }
    //返回1，表示没有变化
    return 1;
}

//封装贝塞尔曲线方法，用来对三点的贝塞尔曲线进行采样，直接定义点数量，加快运行速度
float3 Get3PointBezier(float3 begin, float3 center, float3 end, float time){
    time = saturate(time);
    //第一条线的位置
    float3 point1 = begin + (center - begin) * time;
    //第二条线的位置
    float3 point2 = center + (end - center) * time;

    return point1 + (point2 - point1) * time;
}

//封装创建一个传入到片源着色器的函数
FragInput GetFragInputValue(float2 uv, float time, float3 worldPos, float4 otherDate, float4 otherDate2 = 0){
    FragInput o = (FragInput)0;
    o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    o.uv = uv;
    o.time = time;
    o.worldPos = worldPos;
    o.otherDate = otherDate;
    o.otherDate2 = otherDate2;
    return o;
}

//移动部分的封装点到面，因为要制作一些定义操作
void Move_outOnePoint(inout TriangleStream<FragInput> tristream, TessOutput_All IN, float moveTime, float3 end = 0){
    FragInput o[4] = (FragInput[4])0;

    float3 worldVer = IN.vertex.xyz;
    float paritcleLen = _ParticleLength;
    float3 sphereCenter = worldVer - UNITY_MATRIX_V[2].xyz * paritcleLen;
    
    #ifdef _MOVE_SIZE
        paritcleLen *= LoadCurveTime(moveTime, _MoveSizePointCount, _MoveSizePointArray);
    #endif

    float3 vertex = worldVer + UNITY_MATRIX_V[0].xyz * -paritcleLen 
        + UNITY_MATRIX_V[1].xyz * -paritcleLen;
    //otherData的x用来确定目前位置，0表示为移动阶段
    o[0] = GetFragInputValue(float2(0, 0), moveTime, vertex, float4(0, sphereCenter), float4(end, 0));

    vertex = worldVer + UNITY_MATRIX_V[0].xyz * -paritcleLen 
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[1] = GetFragInputValue(float2(1, 0), moveTime, vertex, float4(0, sphereCenter), float4(end, 0));

    vertex = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen 
        + UNITY_MATRIX_V[1].xyz * -paritcleLen;
    o[2] = GetFragInputValue(float2(0, 1), moveTime, vertex, float4(0, sphereCenter), float4(end, 0));

    vertex = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen 
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[3] = GetFragInputValue(float2(1, 1), moveTime, vertex, float4(0, sphereCenter), float4(end, 0));

    tristream.Append(o[1]);
    tristream.Append(o[2]);
    tristream.Append(o[0]);
    tristream.RestartStrip();

    tristream.Append(o[1]);
    tristream.Append(o[3]);
    tristream.Append(o[2]);
    tristream.RestartStrip();
}

//偏移部分的封装点到面，因为要制作一些定义操作
void Offset_outOnePoint(inout TriangleStream<FragInput> tristream, TessOutput_All IN, float offsetTime){
    FragInput o[4] = (FragInput[4])0;

    float3 worldVer = IN.vertex.xyz;
    float paritcleLen = _ParticleLength;
    float3 sphereCenter = worldVer - UNITY_MATRIX_V[2].xyz * paritcleLen;
    
    #ifdef _OFFSET_SIZE
        paritcleLen *= LoadCurveTime(offsetTime, _OffsetSizePointCount, _OffsetSizePointArray);
    #endif

    float3 vertex = worldVer + UNITY_MATRIX_V[0].xyz * -paritcleLen 
        + UNITY_MATRIX_V[1].xyz * -paritcleLen;
    //1表示为移动阶段
    o[0] = GetFragInputValue(float2(0, 0), offsetTime, vertex, float4(1, sphereCenter));

    vertex = worldVer + UNITY_MATRIX_V[0].xyz * -paritcleLen 
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[1] = GetFragInputValue(float2(1, 0), offsetTime, vertex, float4(1, sphereCenter));

    vertex = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen 
        + UNITY_MATRIX_V[1].xyz * -paritcleLen;
    o[2] = GetFragInputValue(float2(0, 1), offsetTime, vertex, float4(1, sphereCenter));

    vertex = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen 
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[3] = GetFragInputValue(float2(1, 1), offsetTime, vertex, float4(1, sphereCenter));

    tristream.Append(o[1]);
    tristream.Append(o[2]);
    tristream.Append(o[0]);
    tristream.RestartStrip();

    tristream.Append(o[1]);
    tristream.Append(o[3]);
    tristream.Append(o[2]);
    tristream.RestartStrip();
}


#endif