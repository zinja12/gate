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

float4 Palette[8]; //adjust size according to palette
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

	float min_distance = 1e5;
	float4 closest_color = float4(1, 1, 1, 1);

	//loop through palette to find closest color
	for (int i = 0; i < PaletteSize; i++) {
		float dist = distance(original_color.rgb, Palette[i].rgb);
		if (dist < min_distance) {
			min_distance = dist;
			closest_color = Palette[i];
		}
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