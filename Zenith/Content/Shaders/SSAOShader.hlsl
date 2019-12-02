float2 ScreenSize;
texture ScreenTexture;

sampler TextureSampler = sampler_state
{
	Texture = <ScreenTexture>;
};


float4 PixelShaderFunction(float4 Position : POSITION0) : COLOR0
{
	float4 depthLeftColor = tex2D(TextureSampler, (Position.xy + float2(-1, 0)) / ScreenSize);
	float4 depthRightColor = tex2D(TextureSampler, (Position.xy + float2(1, 0)) / ScreenSize);
	float4 depthTopColor = tex2D(TextureSampler, (Position.xy + float2(0, 1)) / ScreenSize);
	float4 depthBottomColor = tex2D(TextureSampler, (Position.xy + float2(0, -1)) / ScreenSize);
	float depthLeftValue = depthLeftColor.r;
	float depthRightValue = depthRightColor.r;
	float depthTopValue = depthTopColor.r;
	float depthBottomValue = depthBottomColor.r;
	float diff = sqrt((depthRightValue - depthLeftValue) * (depthRightValue - depthLeftValue) + (depthTopValue - depthBottomValue) * (depthTopValue - depthBottomValue)) * 0.01;
	return float4(diff, diff, diff, 1);
}

technique Ambient
{
	pass Pass1
	{
		PixelShader = compile ps_4_0 PixelShaderFunction();
	}
}