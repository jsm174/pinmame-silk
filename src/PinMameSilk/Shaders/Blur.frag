#version 330 core
in vec2 uv;

uniform sampler2D tex;
uniform vec2 direction;

out vec4 FragColor;

// The Kernels are derived from https://rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
// They are designed to be used with a texture with linear sampling (see reference for explanation)

vec4 blur_level_2(sampler2D image, vec2 uv, vec2 direction) {
	vec4 color = vec4(0.0);
	vec2 off1 = direction;
	color += texture(image, uv) * 0.5;
	color += texture(image, uv + off1) * 0.25;
	color += texture(image, uv - off1) * 0.25;
	return color; 
}

vec4 blur_level_12(sampler2D image, vec2 uv, vec2 direction) {
	vec4 color = vec4(0.0);
	vec2 off1 = vec2(1.3846153846) * direction;
	vec2 off2 = vec2(3.2307692308) * direction;
	color += texture(image, uv) * 0.2270270270;
	color += texture(image, uv + off1) * 0.3162162162;
	color += texture(image, uv - off1) * 0.3162162162;
	color += texture(image, uv + off2) * 0.0702702703;
	color += texture(image, uv - off2) * 0.0702702703;
	return color;
}
