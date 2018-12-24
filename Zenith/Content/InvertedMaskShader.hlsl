// Converts a B/W image texture to a color texture with an alpha channel

float4 maskColor;
float lo;
float hi;
texture ScreenTexture;
sampler TextureSampler = sampler_state
{
	Texture = <ScreenTexture>;
};

float4 PixelShaderFunction(float2 TextureCoordinate : TEXCOORD0) : COLOR0
{
	float4 maskSrcColor = tex2D(TextureSampler, TextureCoordinate);
	float maskAlpha = (maskSrcColor.r - lo) / (hi - lo);
	float4 color = maskColor * (1 - maskAlpha);
	return color;
}

technique BlackAndWhite
{
	pass Pass1
	{
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}