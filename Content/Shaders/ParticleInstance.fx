// Basic GPU Particle Effect for MonoGame

float4x4 WorldViewProjection;
float4 ParticleColor;

texture ParticleTexture;
sampler2D ParticleSampler = sampler_state
{
    Texture = <ParticleTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderInput
{
    float3 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
    float  Scale    : TEXCOORD1;
    float  Rotation : TEXCOORD2;
    float2 Corner   : TEXCOORD3;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Scale and rotate the corner offset
    float2 offset = input.Corner * input.Scale;
    float cosR = cos(input.Rotation);
    float sinR = sin(input.Rotation);
    float2 rotatedOffset = float2(
        offset.x * cosR - offset.y * sinR,
        offset.x * sinR + offset.y * cosR
    );

    float3 worldPos = input.Position.xyz + float3(rotatedOffset, 0);
    output.Position = mul(float4(worldPos, 1.0), WorldViewProjection);
    output.TexCoord = input.TexCoord;
    output.Color = input.Color * ParticleColor;
    return output;
}

float4 PS(VertexShaderOutput input) : COLOR0
{
    float4 texColor = tex2D(ParticleSampler, input.TexCoord);
    return texColor * input.Color;
}

technique ParticleTechnique
{
    pass Pass0
    {
        ZEnable = true;
        ZWriteEnable = true;
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 PS();
    }
}