using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Windows;

namespace MakeSpline
{
    // Обёртка над шейдером
    public class Shader
    {
        public int Handle { get; }

        public Shader(string vertexPath, string fragmentPath)
        {
            string VertexShaderSource = "";
            string FragmentShaderSource = "";
            try
            {
                string workingDirectory = Environment.CurrentDirectory;
                string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
                string VertexPath = projectDirectory + vertexPath;
                string FragmentPath = projectDirectory + fragmentPath;
                VertexShaderSource = File.ReadAllText(VertexPath);
                FragmentShaderSource = File.ReadAllText(FragmentPath);
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException || e is FileNotFoundException)
                {
                    MessageBox.Show("Не удалось найти шейдеры");
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("Не удалось прочитать файлы шейдеров");
                    Application.Current.Shutdown();
                }
            }

            int VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShader, VertexShaderSource);

            int FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShader, FragmentShaderSource);

            // Компиляция шейдеров-----------------------------------------------------
            GL.CompileShader(VertexShader);

            GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int success_v);
            if (success_v == 0)
            {
                string infoLog = "Ошибка компиляции vertex шейдера\n" + GL.GetShaderInfoLog(VertexShader);
                MessageBox.Show(infoLog);
                Application.Current.Shutdown();
            }

            GL.CompileShader(FragmentShader);

            GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out int success_f);
            if (success_f == 0)
            {
                string infoLog = "Ошибка компиляции fragment шейдера\n" + GL.GetShaderInfoLog(FragmentShader);
                MessageBox.Show(infoLog);
                Application.Current.Shutdown();
            }

            // Связывание шейдеров-----------------------------------------------------
            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = "Ошибка связывания шейдеров\n" + GL.GetProgramInfoLog(Handle);
                MessageBox.Show(infoLog);
                Application.Current.Shutdown();
            }

            // Clenup
            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);
            GL.DeleteShader(FragmentShader);
            GL.DeleteShader(VertexShader);
        }

        // Привязка шейдера
        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public void SetFloat(string name, float f)
        {
            Use();
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform1(location, f);
        }

        public void SetInt(string name, int i)
        {
            Use();
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform1(location, i);
        }

        public void SetMatrix4(string name, ref Matrix4 matrix)
        {
            Use();
            int location = GL.GetUniformLocation(Handle, name);
            GL.UniformMatrix4(location, true, ref matrix);
        }

        public void SetColor4(string name, Color4 vector)
        {
            Use();
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform4(location, vector.R, vector.G, vector.B, vector.A);
        }


        public void DashedLines(bool dash, float linesSize = 0f, float dash_size = 0.2f, float gap_size = 0.5f)
        {
            if (dash)
            {
                SetFloat("u_dashSize", dash_size);
                SetFloat("u_gapSize", gap_size);
            }
            else
            {
                SetFloat("u_dashSize", linesSize);
                SetFloat("u_gapSize", 0f);
            }
            Use();
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~Shader()
        {
            GL.DeleteProgram(Handle);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}