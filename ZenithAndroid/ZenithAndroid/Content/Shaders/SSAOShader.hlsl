float2 ScreenSize;
float4x4 Projection;
float4x4 InverseProjection;
float4x4 WVP;
#define KERNEL_SIZE 128
float4 offsets[KERNEL_SIZE];
float SphereRadius;

texture PositionTexture;
sampler PositionSampler = sampler_state {
	Texture = (PositionTexture);
	MinFilter = Linear;
	MagFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

texture NormalTexture;
sampler2D NormalSampler = sampler_state {
	Texture = (NormalTexture);
	MinFilter = Linear;
	MagFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

texture AlbedoTexture;
sampler2D AlbedoSampler = sampler_state {
	Texture = (AlbedoTexture);
	MinFilter = Linear;
	MagFilter = Linear;
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
	output.TextureCoordinate = mul(input.Position, WVP).xy;
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 bufferPosition = tex2D(PositionSampler, input.TextureCoordinate.xy / ScreenSize);
	float4 bufferNormal = tex2D(NormalSampler, input.TextureCoordinate.xy / ScreenSize);
	float3 normal = -bufferNormal.xyz;
	float4 originalPos = mul(bufferPosition, InverseProjection); // gets the position relative to the camera
	originalPos /= originalPos.w;
	float occludeCount = 0;
	for(int i = 0; i < KERNEL_SIZE; i++) {
		float3 randomVec = normalize(float3(1, 1, 1));
		float3 tangent = normalize(cross(normal, randomVec));
		float3 bitangent = cross(normal, tangent);
		float3x3 mat = float3x3(tangent, bitangent, normal);
		float3 offset = offsets[i].z * normal + offsets[i].x * tangent + offsets[i].y * bitangent;
		float4 samplePos = originalPos + float4(SphereRadius * offset, 0);
		float4 projectedSamplePos = mul(samplePos, Projection);
		projectedSamplePos /= projectedSamplePos.w;
		float4 sampleBufferPos = tex2D(PositionSampler, projectedSamplePos.xy * float2(0.5, -0.5) + float2(0.5, 0.5));
		if (sampleBufferPos.z > projectedSamplePos.z) {
			occludeCount++;
		}
	}
	float occlude = occludeCount / KERNEL_SIZE * occludeCount / KERNEL_SIZE;
	output.Color = float4(occlude * tex2D(AlbedoSampler, input.TextureCoordinate.xy / ScreenSize).xyz, 1);
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