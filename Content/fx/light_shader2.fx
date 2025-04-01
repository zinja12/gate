#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler2D EntityTexture : register(s0);
sampler2D LightTexture : register(s1);

struct PixelShaderInput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(PixelShaderInput input) : COLOR
{
	float4 entity_original_color = tex2D(EntityTexture, input.TextureCoordinates) * input.Color;
	float4 light_original_color = tex2D(LightTexture, input.TextureCoordinates) * input.Color;

	//entity_original_color.rgb *= light_original_color.rgb;
	float3 final_color = entity_original_color.rgb * max(light_original_color.rgb, 0.7);

	return float4(final_color.rgb, entity_original_color.a);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};