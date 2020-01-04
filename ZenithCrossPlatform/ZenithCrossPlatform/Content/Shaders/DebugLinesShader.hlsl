float4x4 WVP;
float2 ScreenSize;

texture Texture;
sampler2D textureSampler = sampler_state {
	Texture = (Texture);
	MinFilter = Point;
	MagFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Normal : NORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TextureCoordinate : TEXCOORD1;
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
	float4 position2 = mul(input.Normal, WVP);
	position2 /= position2.w;
	float2 tempNormal = float2(output.Position.y - position2.y, position2.x - output.Position.x);
	tempNormal *= (input.TextureCoordinate.x - 0.5) * (input.TextureCoordinate.y - 0.5) * 4;
	float2 screenNormal = normalize(tempNormal * ScreenSize);
	output.Position.xy += 10 * screenNormal / ScreenSize;
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 albedo = tex2D(textureSampler, input.TextureCoordinate);
	output.PNA = float4(albedo.rgb, 0);
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