#ifndef CS_PARTICLE_PASS
#define CS_PARTICLE_PASS

#include "CS_ParticleInput.hlsl"
#include "../../../ShaderLibrary/Fragment.hlsl"

struct ToGeom {
    // float4 random : RANDOM;
    float3 worldPos : VAR_POSITION;
    // float time : VAR_TIME;
    bool isUse : CHECK;
    float4 transferData : UV_TRANSFER;
    float interplation : UV_INTERPELATION;
    float4 color : COLOR;
    float size : SIZE;
};

ToGeom vert(uint id : SV_InstanceID)
{
    ToGeom o = (ToGeom)0;
    o.worldPos = _ParticleBuffer[id].worldPos;
    // o.time = _ParticleBuffer[id].random.w;
    // o.random = _ParticleBuffer[id].random;
    if (_ParticleBuffer[id].index.y < 0)
        o.isUse = false;
    else o.isUse = true;
    o.interplation = _ParticleBuffer[id].interpolation;
    o.transferData = _ParticleBuffer[id].uvTransData;
    o.color = _ParticleBuffer[id].color;
    o.size = _ParticleBuffer[id].size;
    return o;
}

void LoadOnePoint(ToGeom IN, inout TriangleStream<FragInput> tristream) {
    PointToQuad o;
    o.worldPos = IN.worldPos;
    // o.time = IN.time;
    o.uvTransfer = IN.transferData;
    o.uvInterplation = IN.interplation;
    o.color = IN.color;
    o.size = IN.size;
    outOnePoint(tristream, o);
}

[maxvertexcount(15)]
void geom(point ToGeom IN[1], inout TriangleStream<FragInput> tristream)
{
    if (!IN[0].isUse) return;
    LoadOnePoint(IN[0], tristream);
}

float4 frag(FragInput i) : SV_Target
{
    Fragment fragment = GetFragment(i.pos);
    float4 color = GetBaseColor(i);

    color.a *= ChangeAlpha(fragment);

    #ifdef _DISTORTION
        float4 bufferColor = GetBufferColor(fragment, GetDistortion(i, color.a));
        color.rgb = lerp( bufferColor.rgb, 
            color.rgb, saturate(color.a - GetDistortionBlend()));
    #endif

    return color;
}
#endif