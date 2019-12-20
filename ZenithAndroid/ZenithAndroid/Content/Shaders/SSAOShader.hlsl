float4x4 Projection;
float4x4 InverseProjection;
float4x4 WVP;
#define KERNEL_SIZE 16
float4 offsets[KERNEL_SIZE];
float SphereRadius;

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

//#define Unpack(f) (frac((f) / float3(16777216, 65536, 256)))

inline float3 Unpack(float v)
{
    float3 kEncodeMul = float3(1.0, 255.0, 65025.0);
    float kEncodeBit = 1.0 / 255.0;
    float3 enc = kEncodeMul * v;
    enc = frac(enc);
    //enc -= enc.yzz * kEncodeBit;
    return enc;
}

inline float3 UnpackNormal(float v)
{
    float3 kEncodeMul = float3(1.0, 255.0, 65025.0);
    float kEncodeBit = 1.0 / 255.0;
    float3 enc = kEncodeMul * v;
    enc = (frac(enc)-0.5)*3;
    //enc -= enc.yzz * kEncodeBit;
    return float3(enc.x,enc.y,sqrt(1-enc.x*enc.x-enc.y*enc.y));
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 bufferPNA = tex2D(PNASampler, input.TextureCoordinate.xy);
	float4 bufferPosition = float4(input.TextureCoordinate.x * 2 - 1, 1 - input.TextureCoordinate.y * 2, bufferPNA.x, 1);
	float4 bufferNormal = float4(UnpackNormal(bufferPNA.y), 1);
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
		float4 sampleBufferPos = tex2D(PNASampler, projectedSamplePos.xy * float2(0.5, -0.5) + float2(0.5, 0.5));
		if (sampleBufferPos.x > projectedSamplePos.z) {
			occludeCount++;
		}
	}
	float occlude = occludeCount / KERNEL_SIZE * occludeCount / KERNEL_SIZE;
	output.Color = float4(occlude * Unpack(bufferPNA.z), 1);
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