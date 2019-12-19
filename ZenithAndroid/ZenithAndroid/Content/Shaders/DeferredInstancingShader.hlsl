float4x4 WV;
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
	float3 Normal : TEXCOORD0;
	float2 TextureCoordinate : TEXCOORD1;
	float4 TexPosition : TEXCOORD2; // TODO: why does using this work but using position not??
};

struct PixelShaderOutput
{
	float4 Position : COLOR0;
	float4 Normal : COLOR1;
	float4 Albedo : COLOR2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input, float4x4 instanceTransform : TEXCOORD1)
{
	VertexShaderOutput output;
	output.Position = mul(mul(input.Position, transpose(instanceTransform)), WVP);
	output.TexPosition = mul(mul(input.Position, transpose(instanceTransform)), WVP);
	float4 pos = mul(mul(input.Position, transpose(instanceTransform)), WV);
	pos /= pos.w;
	float4 normalpos = mul(mul(input.Position + float4(input.Normal, 0), transpose(instanceTransform)), WV);
	normalpos /= normalpos.w;
	output.Normal = -(normalpos.xyz - pos.xyz);
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	output.Position = float4(input.TexPosition.xyz / input.TexPosition.w, 1);
	output.Normal = float4(normalize(input.Normal), 1);
	output.Albedo = tex2D(textureSampler, input.TextureCoordinate);
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