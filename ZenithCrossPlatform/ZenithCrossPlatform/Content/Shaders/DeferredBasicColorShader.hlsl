float4x4 WVP;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 Color : Color0;
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float Depth : TEXCOORD0;
	float3 Color : TEXCOORD1;
};

struct PixelShaderOutput
{
	float4 PNA : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(input.Position, WVP);
	output.Depth = output.Position.z / output.Position.w;
	output.Color = input.Color;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 albedo = float4(input.Color, 1);
	output.PNA = float4(albedo.rgb, input.Depth);
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