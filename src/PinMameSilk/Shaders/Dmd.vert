#version 330 core
layout (location = 0) in vec3 Position;
layout (location = 1) in vec2 TexCoord;

out vec2 dmdUv;
out vec2 glassUv;

uniform vec2 glassTexOffset; // Offset and scale of DMD inside Glass
uniform vec2 glassTexScale; // Scale for margin glass

void main()
{
	dmdUv = TexCoord; // * glassTexScale - glassTexOffset;
	glassUv = TexCoord;
    gl_Position = vec4(Position, 1.0);
}