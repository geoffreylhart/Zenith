float4x4 WVP;

texture Texture;
sampler2D textureSampler = sampler_state {
	Texture = (Texture);
	MinFilter = Point;
	MagFilter = Point;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 DepthW : TEXCOORD0;
	float2 TextureCoordinate : TEXCOORD1;
};

struct PixelShaderOutput
{
	float4 PNA : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input, float4x4 instanceTransform : TEXCOORD1)
{
	VertexShaderOutput output;
	output.Position = mul(mul(input.Position, transpose(instanceTransform)), WVP);
	output.DepthW = float2(output.Position.z, output.Position.w);
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 albedo = tex2D(textureSampler, input.TextureCoordinate);
	output.PNA = float4(albedo.rgb, input.DepthW.x / input.DepthW.y);
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