// Grayscale.fx
// Simple grayscale pixel shader for MonoGame/XNA

sampler2D TextureSampler : register(s0);

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TextureSampler, texCoord);
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}

technique Grayscale
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}
