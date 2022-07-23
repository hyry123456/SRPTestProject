#ifndef CS_PARTICLE_NOISE_PASS
#define CS_PARTICLE_NOISE_PASS


#include "CS_ParticleInput.hlsl"
#include "../../../ShaderLibrary/Fragment.hlsl"

struct ToGeom {
    float3 worldPos : VAR_POSITION;
    bool isUse : CHECK;
    float4 transferData : UV_TRANSFER;
    float interplation : UV_INTERPELATION;
    float4 color : COLOR;
    float size : SIZE;
    float3 speed : SPEED;
};

ToGeom vert(uint id : SV_InstanceID)
{
    ToGeom o = (ToGeom)0;
    o.worldPos = _ParticleNoiseBuffer[id].worldPos;
    if (_ParticleNoiseBuffer[id].index.y < 0)
        o.isUse = false;
    else o.isUse = true;
    if(id < 100)
        o.isUse = false;
    o.interplation = _ParticleNoiseBuffer[id].interpolation;
    o.transferData = _ParticleNoiseBuffer[id].uvTransData;
    o.color = _ParticleNoiseBuffer[id].color;
    o.size = _ParticleNoiseBuffer[id].size;
    o.speed = _ParticleNoiseBuffer[id].nowSpeed;
    return o;
}

void LoadOnePoint(ToGeom IN, inout TriangleStream<FragInput> tristream) {
    PointToQuad o;
    o.worldPos = IN.worldPos;
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