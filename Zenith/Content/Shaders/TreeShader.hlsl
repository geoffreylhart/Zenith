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
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	output.Color = float4(0, 0, 0, 0);
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
		
		// I bet our issue before was not dealing with homogenous coordinates properly, duh-doy
		// TODO: obviously optimize all of this if it works
		float4 scPixel = mul(mul(mul(float4(input.TextureCoordinate, 0, 1), World), View), Projection);
		scPixel /= scPixel.w;
		float4 scCenter = mul(mul(mul(float4(treePosAbs, 0, 1), World), View), Projection);
		scCenter /= scCenter.w;
		float3 scRight = scCenter.xyz + float3(0.1, 0, 0);
		float3 scUp = scCenter.xyz + float3(0, 0.1, 0);
		float4 locRight = mul(float4(scRight,1), Inverse);
		float4 locUp = mul(float4(scUp,1), Inverse);
		locRight /= locRight.w;
		locUp /= locUp.w;
		float3 unitRight = locRight.xyz - float3(treePosAbs, 0);
		float3 unitUp = locUp.xyz - float3(treePosAbs, 0);
		// normalize to the size of a tree
		unitRight *= TreeSize / Resolution / length(unitRight);
		unitUp *= TreeSize / Resolution / length(unitUp);
		float3 topLeft = float3(treePosAbs, 0) - unitRight * TreeCenter.x + unitUp * TreeCenter.y;
		float3 bottomRight = float3(treePosAbs, 0) + unitRight * (1 - TreeCenter.x) - unitUp * (1 - TreeCenter.y);
		float4 scTopLeft = mul(mul(mul(float4(topLeft, 1), World), View), Projection);
		float4 scBottomRight = mul(mul(mul(float4(bottomRight, 1), World), View), Projection);
		scTopLeft /= scTopLeft.w;
		scBottomRight /= scBottomRight.w;
		relTreeCoord = (scPixel.xy - scTopLeft.xy);
		relTreeCoord.x /= (scBottomRight.x - scTopLeft.x);
		relTreeCoord.y /= (scBottomRight.y - scTopLeft.y);
		
		float2 treePosRelTex = (treePosAbs - Min) / (Max - Min); // position of tree relative to the zoomed-in density texture
		if (tex2D(textureSampler, treePosRelTex).r > seed3 % 83 / 83.0) {
			if (relTreeCoord.x >= 0 && relTreeCoord.x <= 1 && relTreeCoord.y >=0 && relTreeCoord.y <= 1) {
				int seed4 = seed3 % 1273 * 43 + 17;
				relTreeCoord.x = (relTreeCoord.x + seed4 % TextureCount) / TextureCount;
				// ddx is probably the amount the texture changes per pixel
				float2 derivX = float2(1, 0) * (Max.x - Min.x); // this logic should still suffice even after we switch to 3d trees
				float2 derivY = float2(0, 1) * (Max.y - Min.y);
				float4 temp = tex2Dgrad(treeTextureSampler, relTreeCoord, derivX, derivY);
				//float newa = (1 - temp.a) * output.Color.a + temp.a;
				output.Color = temp * temp.a + output.Color * (1 - temp.a);
				output.Color = temp * temp.a + output.Color * (1 - temp.a);
			}
		}
	}
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