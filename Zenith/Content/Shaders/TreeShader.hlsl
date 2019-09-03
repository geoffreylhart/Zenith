float4x4 World;
float4x4 View;
float4x4 Projection;

float4 AmbientColor = float4(1, 1, 1, 1);
float Resolution;

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
    float4 textureColor = tex2D(textureSampler, input.TextureCoordinate);
    float4 textureColor1 = tex2D(textureSampler, input.TextureCoordinate + float2(0, 1 / Resolution));
    float4 textureColor2 = tex2D(textureSampler, input.TextureCoordinate + float2(0, -1 / Resolution));
    float4 textureColor3 = tex2D(textureSampler, input.TextureCoordinate + float2(1 / Resolution, 0));
    float4 textureColor4 = tex2D(textureSampler, input.TextureCoordinate + float2(-1 / Resolution, 0));
	if (textureColor.r == 0 || textureColor1.r == 0 || textureColor2.r == 0 || textureColor3.r == 0 || textureColor4.r == 0) {
		return float4(0, 0, 0, 0);
	} else {
		return AmbientColor;
	}
}

technique Ambient
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}