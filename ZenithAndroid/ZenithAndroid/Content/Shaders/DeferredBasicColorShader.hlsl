float4x4 WV;
float4x4 WVP;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 Color : Color0;
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float3 Color : Color0;
	float4 TexPosition : TEXCOORD1; // TODO: why does using this work but using position not??
};

struct PixelShaderOutput
{
	float4 Position : COLOR0;
	float4 Normal : COLOR1;
	float4 Albedo : COLOR2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(input.Position, WVP);
	output.TexPosition = mul(input.Position, WVP);
	// assumes the normal if not provided
	float4 pos = mul(input.Position, WV);
	pos /= pos.w;
	float4 normalpos = mul(input.Position + float4(0, 0, 1, 0), WV);
	normalpos /= normalpos.w;
	output.Normal = normalpos.xyz - pos.xyz;
	output.Color = input.Color;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	output.Position = float4(input.TexPosition.xyz / input.TexPosition.w, 1);
	output.Normal = float4(normalize(input.Normal), 1);
	output.Albedo = float4(input.Color, 1);
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