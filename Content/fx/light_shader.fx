#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates: TEXCOORD0;
};

//pixel shader
float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 original_color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
	if (original_color.r != 0 || original_color.g != 0 || original_color.b != 0) {
		original_color.rgba = 0;
	}

	return original_color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};