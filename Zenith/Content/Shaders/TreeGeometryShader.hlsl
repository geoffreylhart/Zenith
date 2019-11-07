float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 Inverse;

float Resolution;
float TreeSize; // the scaling of the tree image, I've usually set this to 2 for healthy amounts of overlap (and because my images have lots of whitespace)
float2 TextureOffsets[9];
// frustrum bounds
float2 Min; // vector that represents the coordinate of the topleft within the sector? used to know how to reference the density texture
float2 Max;
float2 TreeCenter;
float2 TreeVariance; // the "width" of the variance of a tree off its normal center of 0.5, 0.5
int TextureCount;

texture Texture;
sampler2D textureSampler = sampler_state {
	Texture = (Texture);
	MinFilter = Point;
	MagFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

texture TreeTexture;
sampler2D treeTextureSampler = sampler_state {
	Texture = (TreeTexture);
	MinFilter = Point;
	MagFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TextureCoordinate : TEXCOORD0;
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TextureCoordinate : TEXCOORD0;
};

struct PixelShaderOutput
{
	float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	float xAmount = (input.TextureCoordinate.x - 0.5);
	float zAmount = (1 - input.TextureCoordinate.y);
	float4 scCenter = mul(mul(mul(input.Position, World), View), Projection);
	scCenter /= scCenter.w;
	float3 scRight = scCenter.xyz + float3(0.1, 0, 0);
	float3 scUp = scCenter.xyz + float3(0, 0.1, 0);
	float4 locRight = mul(float4(scRight,1), Inverse);
	float4 locUp = mul(float4(scUp,1), Inverse);
	locRight /= locRight.w;
	locUp /= locUp.w;
	float3 unitRight = locRight.xyz - input.Position.xyz;
	float3 unitUp = locUp.xyz - input.Position.xyz;
	// normalize to the size of a tree
	unitRight *= 1.0 / 256 / length(unitRight);
	unitUp *= 1.0 / 256 / length(unitUp);
	input.Position.xyz += xAmount * unitRight;
	input.Position.xyz += zAmount * unitUp;
	
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	output.Color = tex2D(treeTextureSampler, input.TextureCoordinate);
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