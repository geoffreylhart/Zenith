float2 PixelSize;
float4x4 Projection;
float4x4 InverseProjection;
float4x4 WVP;
#define KERNEL_SIZE 16
float4 offsets[KERNEL_SIZE];
float SphereRadius;
float iTime;

texture PNATexture;
sampler PNASampler = sampler_state {
	Texture = (PNATexture);
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
	output.Position = mul(input.Position, WVP);
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 bufferPNA = tex2D(PNASampler, input.TextureCoordinate.xy);
	float4 bufferPosition = float4(input.TextureCoordinate.x * 2 - 1, 1 - input.TextureCoordinate.y * 2, bufferPNA.w, 1);
	//float4 bufferNormal = float4(UnpackNormal(bufferPNA.y), 1);
	float x1 = tex2D(PNASampler, input.TextureCoordinate.xy-PixelSize.x).w;
	float x2 = tex2D(PNASampler, input.TextureCoordinate.xy+PixelSize.x).w;
	float y1 = tex2D(PNASampler, input.TextureCoordinate.xy-PixelSize.y).w;
	float y2 = tex2D(PNASampler, input.TextureCoordinate.xy+PixelSize.y).w;
	float3 dx = float3(2*PixelSize.x,0,x2-x1);
	float3 dy = float3(0,2*PixelSize.y,y2-y1);
	
	float3 normal = normalize(cross(dx,dy)); // TODO: not accurate but lets roll with it
	float occludeCount = 0;
	for(int i = 0; i < KERNEL_SIZE; i++) {
		float2 offset = offsets[i].xy*20*PixelSize;
		float predictedZ = bufferPNA.w+dot(offsets[i].xy*20,float2(x2-x1,y2-y1)/2);
		if (tex2D(PNASampler, input.TextureCoordinate.xy+offset).w > predictedZ/1.01) {
			occludeCount+=1.0;
		}
	}
	float occlude = occludeCount / KERNEL_SIZE;
	output.Color = float4(occlude * bufferPNA.rgb, 1);
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