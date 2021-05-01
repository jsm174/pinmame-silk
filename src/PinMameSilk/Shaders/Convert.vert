#version 330 core
layout (location = 0) in vec3 Position;
layout (location = 1) in vec2 TexCoord;

out vec2 uv;

void main()
{
	uv = vec2(TexCoord.x, 1.0 - TexCoord.y);
    gl_Position = vec4(Position, 1.0);
}