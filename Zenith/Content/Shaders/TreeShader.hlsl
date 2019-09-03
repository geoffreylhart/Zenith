float4x4 World;
float4x4 View;
float4x4 Projection;

#define TREE_COUNT 11
float Resolution;
float TreeSize;
float2 TextureOffsets[9];
//float4 KeyColor;

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
		bool allow = true;
		for (int i = 0; i < 9; i++) {
			float4 textureColor = tex2D(textureSampler, input.TextureCoordinate + TextureOffsets[i] + TextureOffsets[j]);
			if (textureColor.r == 0) {
				allow = false;
			}
		}
		if (allow) {
			float4 textureColor = tex2D(textureSampler, input.TextureCoordinate + TextureOffsets[j]);
			for (int i = 0; i < TREE_COUNT; i++) {
				float2 tileCoord = input.TextureCoordinate * Resolution % 1;
				float2 tile = (input.TextureCoordinate + TextureOffsets[j]) * Resolution;
				// TODO: somehow integer overflow breaks this?
				int seed1 = ((int(tile.x) * 217 + int(tile.y)) * 453 + i) % 1024 * 711 + 319;
				int seed2 = seed1 * 97 + 11;
				float2 randPos = float2(seed1 % 83 / 83.0 - TreeSize / 2, seed2 % 83 / 83.0 - TreeSize / 2);
				float2 treeCoord = (tileCoord - randPos - TextureOffsets[j] * Resolution) / TreeSize;
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