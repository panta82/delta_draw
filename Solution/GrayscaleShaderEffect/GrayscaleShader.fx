// Static input variables

// Default sampler; the 2d surface/texture/picture on which shader is applied
sampler2D implicitInput : register(s0);

//////////////////
// Main function
//////////////////

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(implicitInput, uv);
	
	float avg = (color.r + color.g + color.b) / 3;
	
	color.r = avg;
	color.g = avg;
	color.b = avg;

	return color;
}