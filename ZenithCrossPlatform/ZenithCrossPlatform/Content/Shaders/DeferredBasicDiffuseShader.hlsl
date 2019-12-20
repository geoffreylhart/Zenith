float4x4 WV;
float4x4 WVP;
float4 DiffuseColor;

struct VertexShaderInput
{
	float4 Position : POSITION0;
};
 
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float3 Normal : TEXCOORD0;
	float4 TexPosition : TEXCOORD1; // TODO: why does using this work but using position not??
};

struct PixelShaderOutput
{
	float4 PNA : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(input.Position, WVP);
	output.TexPosition = mul(input.Position, WVP);
	// assumes the normal if not provided
	float4 pos = mul(input.Position, WV);
	pos /= pos.w;
	float4 normalpos = mul(input.Position + float4(0, 0, 1, 0), WV);
	normalpos /= normalpos.w;
	output.Normal = normalpos.xyz - pos.xyz;
	return output;
}

//#define Pack(c) (dot(round((c) * 255), float3(65536, 256, 1)))

inline float Pack(float3 enc)
{
    float3 kDecodeDot = float3(1.0, 1/255.0, 1/65025.0);
    return dot(enc, kDecodeDot);
}

inline float PackNormal(float3 e3)
{
	float2 enc = e3.xy / 3 + 0.5;
	enc = round(enc * 255) / 255;
    float2 kDecodeDot = float2(1.0, 1 / 255.0);
    return dot(enc, kDecodeDot);
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;
	float4 position = float4(input.TexPosition.xyz / input.TexPosition.w, 1);
	float4 normal = float4(normalize(input.Normal), 1);
	float4 albedo = DiffuseColor;
	output.PNA = float4(position.z, PackNormal(normal.rgb), Pack(albedo.rgb), 1);
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