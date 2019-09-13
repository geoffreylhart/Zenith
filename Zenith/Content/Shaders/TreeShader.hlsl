float4x4 World;
float4x4 View;
float4x4 Projection;

float Resolution;
float TreeSize;
float2 TextureOffsets[9];
// frustrum bounds
float2 Min;
float2 Max;
float2 TreeCenter;
float2 TreeVariance; // the "width" of the variance of a tree off its normal center of 0.5, 0.5

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

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float4 treeColor = float4(0, 0, 0, 0);
	for (int j = 0; j < 9; j++) {
		float2 tileCoord = input.TextureCoordinate * Resolution % 1; // coordinate within tile, from 0-1
		float2 tile = floor(input.TextureCoordinate * Resolution + TextureOffsets[j]); // coordinate of tile from 0-REZ
		// TODO: somehow integer overflow breaks this?
		int seed1 = ((tile.x * 217 + tile.y) * 453) % 1024 * 711 + 319;
		int seed2 = seed1 * 97 + 11;
		int seed3 = seed2 % 742 * 97 + 11;
		float2 randPos = float2(seed1 % 83 / 83.0 - 0.5, seed2 % 83 / 83.0 - 0.5) * TreeVariance; // random variation off of center
		float2 treeCornerPos = randPos + float2(0.5 - TreeSize / 2, 0.5 - TreeSize / 2); // position of the tree texture coordinate's top left corner
		float2 treeCenterPos = treeCornerPos + TreeCenter * TreeSize; // position of the center-base of the tree
		float2 relTreeCoord = (tileCoord - treeCornerPos - TextureOffsets[j]) / TreeSize; // coordinate relative to tree texture
		float2 treePosAbs = tile / Resolution + treeCenterPos / Resolution; // absolute coordinate of tree within sector
		float2 treePosRelTex = (treePosAbs - Min) / (Max - Min);
		if (tex2D(textureSampler, treePosRelTex).r > seed3 % 83 / 83.0) {
			if (relTreeCoord.x >= 0 && relTreeCoord.x <= 1 && relTreeCoord.y >=0 && relTreeCoord.y <= 1) {
				// ddx is probably the amount the texture changes per pixel
				float2 derivX = float2(1, 0) * (Max.x - Min.x); 
				float2 derivY = float2(0, 1) * (Max.y - Min.y);
				float4 temp = tex2Dgrad(treeTextureSampler, relTreeCoord, derivX, derivY);
				//float newa = (1 - temp.a) * treeColor.a + temp.a;
				treeColor = temp * temp.a + treeColor * (1 - temp.a);
				//treeColor.a = newa;
			}
		}
	}
	return treeColor;
}

technique Ambient
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}