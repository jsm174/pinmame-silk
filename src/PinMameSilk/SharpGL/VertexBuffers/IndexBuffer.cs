using Silk.NET.OpenGL;

namespace SharpGL.VertexBuffers
{
    public class IndexBuffer
    {
        public void Create(GL gl)
        {
            //  Generate the vertex array.
            uint[] ids = new uint[1];
            gl.GenBuffers(1, ids);
            bufferObject = ids[0];
        }

        public unsafe void SetData(GL gl, ushort[] rawData)
        {
            // gl.BufferData(GLEnum.ElementArrayBuffer, rawData, GLEnum.StaticDraw);
            fixed (void* d = rawData)
            {
                gl.BufferData(GLEnum.ArrayBuffer, (nuint)(rawData.Length * sizeof(ushort)), d, GLEnum.StaticDraw);
            }
        }

        public void Bind(GL gl)
        {
            gl.BindBuffer(GLEnum.ElementArrayBuffer, bufferObject);
        }

        public void Unbind(GL gl)
        {
            gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
        }

        public bool IsCreated() { return bufferObject != 0; }

        /// <summary>
        /// Gets the index buffer object.
        /// </summary>
        public uint IndexBufferObject
        {
            get { return bufferObject; }
        }

        private uint bufferObject;
    }
}