#ifndef CUSTOM_PARTICLE_PASS
#define CUSTOM_PARTICLE_PASS

#include "CRP_ParticleInput.hlsl"
#include "../../../ShaderLibrary/Fragment.hlsl"

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

//细分文件，进行细分计算
#include "../../ShaderLibrary/Tesseilation.hlsl"


//封装点生成面，朝向摄像机的面，disTime是长度距离，用来控制大小，trueTime是实际的时间
void outOnePoint(inout TriangleStream<FragInput> tristream, TessOutPutSimple IN, float time){
    FragInput o[4] = (FragInput[4])0;

    float3 worldVer = IN.vertex.xyz;
    float paritcleLen = _ParticleLength;

    float sumTime = _RowCount * _ColumnCount;
    float midTime = time * sumTime;

    #ifdef _ANIMATE_UV

        float bottomTime = floor(midTime);
        float topTime = bottomTime + 1.0;
        float interpolation = (midTime - bottomTime) / (topTime - bottomTime);

        float bottomRow = floor(bottomTime / _RowCount);
        float bottomColumn = floor(bottomTime - bottomRow * _ColumnCount);

        float topRow = floor(topTime / _RowCount);
        float topColumn = floor(topTime - topRow * _ColumnCount);
    #else
        float bottomTime = floor(midTime);
        float interpolation = 0;

        float bottomRow = floor(bottomTime / _RowCount);
        float bottomColumn = floor(bottomTime - bottomRow * _ColumnCount);
    #endif

    #ifdef _CURVE_SIZE
        paritcleLen *= LoadCurveTime(time, _SizePointCount, _SizePointArray);
    #endif

    float3 worldPos = worldVer + -unity_MatrixV[0].xyz * paritcleLen + -unity_MatrixV[1].xyz * paritcleLen;
    o[0].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));

    #ifdef _ANIMATE_UV
        SetUV(o[0], float2(0, 0), float4(topColumn, -topRow, bottomColumn, -bottomRow), interpolation, time);
    #else
        SetUV(o[0], float2(0, 0), float4(0, 0, bottomColumn, -bottomRow), interpolation, time);
    #endif
    o[0].time = time;

    worldPos = worldVer + UNITY_MATRIX_V[0].xyz * -paritcleLen 
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[1].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    #ifdef _ANIMATE_UV
        SetUV(o[1], float2(1, 0), float4(topColumn, -topRow, bottomColumn, -bottomRow), interpolation, time);
    #else
        SetUV(o[1], float2(1, 0), float4(0, 0, bottomColumn, -bottomRow), interpolation, time);
    #endif
    o[1].time = time;

    worldPos = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen 
        + UNITY_MATRIX_V[1].xyz * -paritcleLen;
    o[2].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    #ifdef _ANIMATE_UV
        SetUV(o[2], float2(0, 1), float4(topColumn, -topRow, bottomColumn, -bottomRow), interpolation, time);
    #else
        SetUV(o[2], float2(0, 1), float4(0, 0, bottomColumn, -bottomRow), interpolation, time);
    #endif
    o[2].time = time;

    worldPos = worldVer + UNITY_MATRIX_V[0].xyz * paritcleLen 
        + UNITY_MATRIX_V[1].xyz * paritcleLen;
    o[3].pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
    #ifdef _ANIMATE_UV
        SetUV(o[3], float2(1, 1), float4(topColumn, -topRow, bottomColumn, -bottomRow), interpolation, time);
    #else
        SetUV(o[3], float2(1, 1), float4(0, 0, bottomColumn, -bottomRow), interpolation, time);
    #endif
    o[3].time = time;

    tristream.Append(o[1]);
    tristream.Append(o[2]);
    tristream.Append(o[0]);
    tristream.RestartStrip();

    tristream.Append(o[1]);
    tristream.Append(o[3]);
    tristream.Append(o[2]);
    tristream.RestartStrip();
}

//定义位移方法
void LoadOnePoint(TessOutPutSimple IN, inout TriangleStream<FragInput> tristream);

//几何着色器的处理方法
[maxvertexcount(50)]
void geom(triangle TessOutPutSimple IN[3], inout TriangleStream<FragInput> tristream)
{
    LoadOnePoint(IN[0], tristream);
    LoadOnePoint(IN[1], tristream);
    LoadOnePoint(IN[2], tristream);
}

float4 SimpleFrag (FragInput i) : SV_Target
{
    Fragment fragment = GetFragment(i.pos);

    float4 col = GetBaseColor(i, fragment);


    #ifdef _CURVE_ALPHA
        col.a *= saturate( LoadCurveTime( i.time.x, _AlphaPointCount, _AlphaPointArray ) );
    #endif

    #ifdef _DISTORTION
        float4 bufferColor = GetBufferColor(fragment, GetDistortion(i, col.a));
        col.rgb = lerp( bufferColor.rgb, 
            col.rgb, saturate(col.a - GetDistortionBlend()));
    #endif

    return col;
}


#endif