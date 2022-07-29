#ifndef GPUPIPELINE_PARTICLE_WATER_PASS
#define GPUPIPELINE_PARTICLE_WATER_PASS

#include "CS_WaterInput.hlsl"
#include "../../../ShaderLibrary/Fragment.hlsl"

struct ToGeom {
    float3 worldPos : VAR_POSITION;
    bool isUse : CHECK;
    float alpha : COLOR;
    float size : SIZE;
};

ToGeom vert(uint id : SV_InstanceID)
{
    ToGeom o = (ToGeom)0;
    o.worldPos = _WaterParticleBuffer[id].worldPos;
    if (_WaterParticleBuffer[id].index < 0)
        o.isUse = false;
    else o.isUse = true;
    o.alpha = _WaterParticleBuffer[id].alpha;
    o.size = _WaterParticleBuffer[id].size;
    return o;
}

void LoadOnePoint(ToGeom IN, inout TriangleStream<FragInput> tristream) {
    PointToQuad o;
    o.worldPos = IN.worldPos;
    o.alpha = IN.alpha;
    o.size = IN.size;
    outOnePoint(tristream, o);
}

[maxvertexcount(15)]
void geom(point ToGeom IN[1], inout TriangleStream<FragInput> tristream)
{
    if (!IN[0].isUse) return;
    LoadOnePoint(IN[0], tristream);
}

float4 widthFrag(FragInput i) : SV_Target
{
    Fragment fragment = GetFragment(i.pos);
    float4 color = GetBaseColor(i);

    color.a *= ChangeAlpha(fragment);

    return color;
}

float4 NormalAndDepthFrag (FragInput i) : SV_Target
{
    Fragment fragment = GetFragment(i.pos);
    float4 color = GetBaseColor(i);

    color.a *= ChangeAlpha(fragment);

    clip(color.a - 0.01);

    float3 normal = normalize(i.worldPos - i.center);
    return float4(normal * 0.5 + 0.5, 1);
}

#endif