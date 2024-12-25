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

float4 Palette[32]; //adjust size according to palette
int PaletteSize;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord: TEXCOORD0;
};

//vertex shader: transforms vertex positions based on world view projection
VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.Color = input.Color;
	output.TexCoord = input.TexCoord;

	return output;
}

//pixel shader: shifts each pixel color to the closest in the palette
float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 original_color = tex2D(TextureSampler, input.TexCoord);

	float4 closest_color = Palette[0];
	float min_distance = distance(original_color.rgb, closest_color.rgb);

	//loop through palette to find closest color
	for (int i = 0; i < PaletteSize; i++) {
		float dist = distance(original_color.rgb, Palette[i].rgb);
		closest_color = (dist < min_distance) ? Palette[i] : closest_color;
		min_distance = min(dist, min_distance);
	}
	
	return float4(closest_color.rgb, original_color.a);
}

technique PaletteShiftEffect
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};