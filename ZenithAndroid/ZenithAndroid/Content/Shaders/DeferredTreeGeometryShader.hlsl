float4x4 WVP;
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
	float3 SamplePositionThreshold : TEXCOORD0;
	float2 DepthW : TEXCOORD1;
	float2 TextureCoordinate : TEXCOORD2;
};

struct PixelShaderOutput
{
	float4 PNA : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	float2 tile = floor(input.Position.xy * Resolution); // coordinate of tile from 0-REZ
	// TODO: somehow integer overflow breaks this?
	int seed1 = ((tile.x * 217 + tile.y) * 453) % 1024 * 711 + 319;
	int seed2 = seed1 * 97 + 11;
	int seed3 = seed2 % 742 * 97 + 11;
	int seed4 = seed3 % 1273 * 43 + 17;
	float2 randPos = float2(seed1 % 83 / 83.0 * 0 - 0.5, seed2 % 83 / 83.0 * 0 - 0.5) * TreeVariance; // random variation off of center
	input.Position.xy += randPos / Resolution;
	float4 originProjected = mul(input.Position, WVP);
	originProjected /= originProjected.w;
	
	VertexShaderOutput output;
	float xAmount = (input.TextureCoordinate.x - TreeCenter.x);
	float zAmount = (TreeCenter.y - input.TextureCoordinate.y);
	float4 scCenter = mul(input.Position, WVP);
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
	unitRight *= 1.0 / Resolution / length(unitRight);
	unitUp *= 1.0 / Resolution / length(unitUp);
	input.Position.xyz += xAmount * unitRight * TreeSize;
	input.Position.xyz += zAmount * unitUp * TreeSize;
	
	output.Position = mul(input.Position, WVP);
	
	float2 texPos = (originProjected.xy * float2(1, -1) + float2(1, 1)) / 2;
	float threshold = seed3 % 83 / 83.0;
	output.SamplePositionThreshold = float3(texPos, threshold);
	output.DepthW = float2(output.Position.z, output.Position.w);
	
	output.TextureCoordinate = input.TextureCoordinate;
	output.TextureCoordinate.x = (output.TextureCoordinate.x + seed4 % TextureCount) / TextureCount;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 color = tex2D(treeTextureSampler, input.TextureCoordinate);
	float4 blah = tex2Dlod(textureSampler, float4(input.SamplePositionThreshold.xy, 2, 2));
	if (blah.r <= input.SamplePositionThreshold.z) {
		discard;
	}
	float4 normal = float4(0, 0, 1, 1);
	float4 albedo = color;
	clip(color.a - 1);
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