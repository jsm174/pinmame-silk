using Silk.NET.OpenGL;

namespace SharpGL.Shaders
{
    /// <summary>
    /// This is the base class for all shaders (vertex and fragment). It offers functionality
    /// which is core to all shaders, such as file loading and binding.
    /// </summary>
    public class Shader
    {
        public void Create(GL gl, uint shaderType, string source)
        {
            //  Create the OpenGL shader object.
            shaderObject = gl.CreateShader((GLEnum)shaderType);

            //  Set the shader source.
            gl.ShaderSource(shaderObject, source);

            //  Compile the shader object.
            gl.CompileShader(shaderObject);

            //  Now that we've compiled the shader, check it's compilation status. If it's not compiled properly, we're
            //  going to throw an exception.
            if (GetCompileStatus(gl) == false)
            {
                throw new ShaderCompilationException(string.Format("Failed to compile shader with ID {0}.", shaderObject), GetInfoLog(gl));
            }
        }

        public void Delete(GL gl)
        {
            gl.DeleteShader(shaderObject);
            shaderObject = 0;
        }

        public bool GetCompileStatus(GL gl)
        {
            int[] parameters = new int[] { 0 };
            gl.GetShader(shaderObject, GLEnum.CompileStatus, parameters);
            return parameters[0] == (int)GLEnum.True;
        }

        public string GetInfoLog(GL gl)
        {
            return gl.GetShaderInfoLog(shaderObject);
        }

        /// <summary>
        /// The OpenGL shader object.
        /// </summary>
        private uint shaderObject;

        /// <summary>
        /// Gets the shader object.
        /// </summary>
        public uint ShaderObject
        {
            get { return shaderObject; }
        }
    }
}
