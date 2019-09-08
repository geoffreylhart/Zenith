float4x4 World;
float4x4 View;
float4x4 Projection;

#define TREE_COUNT 1
float Resolution;
float TreeSize;
float2 TextureOffsets[9];
//float4 KeyColor;
// frustrum bounds
float MinX;
float MaxX;
float MinY;
float MaxY;
// sector bounds
float sMinX;
float sMaxX;
float sMinY;
float sMaxY;

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
	float4 KeyColor = tex2D(treeTextureSampler, float2(0, 0));
	float4 treeColor = float4(0, 0, 0, 0);
	for (int j = 0; j < 9; j++) {
		float4 textureColor = tex2D(textureSampler, input.TextureCoordinate + TextureOffsets[j]);
		for (int i = 0; i < TREE_COUNT; i++) {
			float2 tileCoord = input.TextureCoordinate * Resolution % 1; // coordinate within tile, from 0-1
			float2 tile = (input.TextureCoordinate + TextureOffsets[j]) * Resolution; // coordinate of tile from 0-REZ
			tile.x = int(tile.x);
			tile.y = int(tile.y);
			// TODO: somehow integer overflow breaks this?
			int seed1 = ((tile.x * 217 + tile.y) * 453 + i) % 1024 * 711 + 319;
			int seed2 = seed1 * 97 + 11;
			float2 randPos = float2(seed1 % 83 / 166.0 - TreeSize / 2, seed2 % 83 / 166.0 - TreeSize / 2); // random variation from 0 to 0.5
			float2 treeCoord = (tileCoord - randPos - TextureOffsets[j] * Resolution) / TreeSize; // coordinate relative to tree texture
			float2 treePosAbs = tile / Resolution + randPos / Resolution; // absolute sector coordinate of tree
			float2 treePosTex = treePosAbs * float2(sMaxX - sMinX, sMaxY - sMinY) + float2(sMinX, sMinY);
			treePosTex -= float2(MinX, MinY);
			treePosTex.x /= MaxX - MinX;
			treePosTex.y /= MaxY - MinY;
			if (tex2D(textureSampler, treePosTex).r != 0) {
				if (treeCoord.x >= 0 && treeCoord.x <= 1 && treeCoord.y >=0 && treeCoord.y <= 1) {
					float4 temp = tex2D(treeTextureSampler, treeCoord);
					if (!all(temp == KeyColor)) treeColor = temp;
				}
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