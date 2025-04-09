#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

extern Texture2D LightTexture;
extern Texture2D SceneTexture;

sampler2D SceneSampler = sampler_state
{
	Texture = <SceneTexture>;
};

sampler2D LightSampler = sampler_state
{
	Texture = <LightTexture>;
};

float smoothness = 0.5; // Smoothness of the falloff (lower value = sharper, higher = softer)

struct PixelShaderInput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(PixelShaderInput input) : COLOR
{
    // Sample scene texture and light mask
    float4 sceneColor = tex2D(SceneTexture, input.TextureCoordinates);
	float4 lightColor = tex2D(LightTexture, input.TextureCoordinates);

	float4 finalColor = sceneColor * lightColor;

    return finalColor;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};