float2 ScreenSize;
texture ScreenTexture;

sampler TextureSampler = sampler_state
{
	Texture = <ScreenTexture>;
};


float4 PixelShaderFunction(float4 Position : POSITION0) : COLOR0
{
	return tex2D(TextureSampler, Position.xy / ScreenSize);
}

technique Ambient
{
	pass Pass1
	{
		PixelShader = compile ps_4_0 PixelShaderFunction();
	}
}