#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix WorldViewProjection;
sampler2D TextureSampler : register(s0);

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.TexCoord = input.TexCoord;
    return output;
}

// The intensity of the scanline effect
float ScanlineIntensity = 0.05; // Adjust for subtlety
//screen height parameter
float screen_height;

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // Sample the base color from the texture
    float4 color = tex2D(TextureSampler, input.TexCoord);

    // Determine the screen position from texture coordinates (adjust to match resolution)
    int y = (int)(input.TexCoord.y * screen_height); // Adjust 600 for screen height

    // Create a scanline effect by modifying brightness for every other line
    float lineFactor = (y % 2 == 0) ? 1.0 - ScanlineIntensity : 1.0;

    // Adjust color based on scanline factor
    color.rgb *= lineFactor;

    // Subtle color shift every few lines for a retro look
    if (y % 4 == 0)
    {
        color.g *= 0.95; // Slightly reduce green on every 4th line
    }
    else if (y % 4 == 2)
    {
        color.b *= 0.95; // Slightly reduce blue on every 2nd line
    }

    return color;
}

technique ScanlineEffect
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}