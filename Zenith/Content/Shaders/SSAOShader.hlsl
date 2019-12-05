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
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
};

struct PixelShaderOutput
{
	float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(input.Position, WVP);
	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 bufferPosition = tex2D(PositionSampler, input.Position.xy / ScreenSize);
	float4 originalPos = mul(bufferPosition, InverseProjection); // gets the position relative to the camera
	originalPos /= originalPos.w;
	float occludeCount = 0;
	float4 colorAverage = float4(0, 0, 0, 0);
	for(int i = 0; i < KERNEL_SIZE; i++) {
		float4 samplePos = originalPos + SphereRadius * offsets[i];
		float4 projectedSamplePos = mul(samplePos, Projection);
		projectedSamplePos /= projectedSamplePos.w;
		float4 sampleBufferPos = tex2D(PositionSampler, projectedSamplePos.xy*float2(0.5,-0.5)+float2(0.5,0.5));
		float4 sampleColor = tex2D(AlbedoSampler, projectedSamplePos.xy*float2(0.5,-0.5)+float2(0.5,0.5));
		if (sampleBufferPos.z > projectedSamplePos.z) {
			occludeCount++;
		}
		colorAverage += sampleColor / KERNEL_SIZE;
	}
	float occlude = occludeCount / KERNEL_SIZE;
	//return float4(occlude, occlude, occlude, 1);
	//output.Color = float4(tex2D(PositionSampler, input.Position.xy/ScreenSize).xyz%1,1);
	//output.Color = colorAverage;
	output.Color = float4(occlude*tex2D(AlbedoSampler, input.Position.xy / ScreenSize).xyz, 1);
	return output;
}

technique Ambient
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 VertexShaderFunction();
		PixelShader = compile ps_4_0 PixelShaderFunction();
	}
}