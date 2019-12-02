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
	float3 Normal : NORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
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
	float4 normal = mul(mul(input.Position + float4(input.Normal, 0), transpose(instanceTransform)), WVP) - output.Position;
	output.Normal = normal.xyz / normal.w;
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float depth = input.Position.z / input.Position.w;
	output.Position = float4(depth * 256 * 256 % 1, depth * 256 % 1  - depth % 0.00390625, depth - depth % 0.0000152587890625, 1);
	output.Normal = float4(normalize(input.Normal), 1);
	output.Albedo = tex2D(textureSampler, input.TextureCoordinate);
	return output;
}

technique Ambient
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 VertexShaderFunction();
		PixelShader = compile ps_4_0 PixelShaderFunction();
	}
}