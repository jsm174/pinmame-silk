
in vec2 uv;

uniform sampler2D palette;
uniform sampler2D dmdData;

out vec4 FragColor;

vec3 lutPalette(float luminance)
{
	return texture(palette, vec2(luminance, 0)).rgb;
}

vec3 decode()
{
	return lutPalette(texture(dmdData, uv).r * 255.0 / 15.0);
}

void main()
{
	vec3 color = decode();

#ifdef GAMMA
	color = pow(color, vec3(gamma));
#endif

	FragColor = vec4(color, 1.0);
}