float4x4 WVP;
float2 PointSize;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color: COLOR0;
	float2 TextureCoordinate: TEXCOORD0;
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct PixelShaderOutput
{
	float4 PNA : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(input.Position, WVP);
	output.Position /= output.Position.w;
	// TODO: make sure flipping makes sense, and the two times (because we're going from 0-1 to -1-1?
	output.Position += float4((input.TextureCoordinate - float2(0.5, 0.5)) * PointSize * float2(2, -2), 0, 0);
	output.Color = input.Color;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	output.PNA = input.Color;
	return output;
}

technique Ambient
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}