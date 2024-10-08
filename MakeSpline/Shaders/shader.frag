#version 330 core
uniform vec4 current_color;
out vec4 FragColor;

void main()
{
    FragColor = current_color;
}