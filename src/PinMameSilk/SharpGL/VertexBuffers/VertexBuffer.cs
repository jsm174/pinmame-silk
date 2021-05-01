using Silk.NET.OpenGL;

namespace SharpGL.VertexBuffers
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Very useful reference for management of VBOs and VBAs:
    /// http://stackoverflow.com/questions/8704801/glvertexattribpointer-clarification
    /// </remarks>
    public class VertexBuffer
    {
        public void Create(GL gl)
        {
            //  Generate the vertex array.
            uint[] ids = new uint[1];
            gl.GenBuffers(1, ids);
            vertexBufferObject = ids[0];
        }

        public unsafe void SetData(GL gl, uint attributeIndex, float[] rawData, bool isNormalised, int stride)
        {
            // Set the data, specify its shape and assign it to a vertex attribute (so shaders can bind to it).
            fixed (void* d = rawData)
            {
                gl.BufferData(GLEnum.ArrayBuffer, (nuint)(rawData.Length * sizeof(float)), d, GLEnum.StaticDraw);
            }
            gl.VertexAttribPointer(attributeIndex, stride, GLEnum.Float, isNormalised, 0, null);
            gl.EnableVertexAttribArray(attributeIndex);
        }

        public void Bind(GL gl)
        {
            gl.BindBuffer(GLEnum.ArrayBuffer, vertexBufferObject);
        }

        public void Unbind(GL gl)
        {
            gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        }

        public bool IsCreated() { return vertexBufferObject != 0; }

        /// <summary>
        /// Gets the vertex buffer object.
        /// </summary>
        public uint VertexBufferObject
        {
            get { return vertexBufferObject; }
        }

        private uint vertexBufferObject;
    }
}
