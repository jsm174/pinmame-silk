using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using LibDmd;
using LibDmd.Common;
using NLog;
using SharpGL.Shaders;
using SharpGL.VertexBuffers;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace PinMameSilk
{
    class DmdController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static DmdController _instance;

        private IView _window;
        private GL _gl;
       
        public Color DotColor { get; set; } = Color.FromArgb(255, 255, 88, 32);
        public DmdStyle DmdStyle { get; } = new DmdStyle();

        private VertexBufferArray _quadVbo;
        private const uint PositionAttribute = 0; // Fixed index of position attribute in the quad VBO
        private const uint TexCoordAttribute = 1; // Fixed index of texture attribute in the quad VBO
        private readonly Dictionary<uint, string> _attributeLocations = new Dictionary<uint, string> { { PositionAttribute, "Position" }, { TexCoordAttribute, "TexCoord" }, };
        private ShaderProgram _blurShader1;
        private int _bs1Texture;
        private int _bs1Direction;
        private ShaderProgram _blurShader2;
        private int _bs2Texture;
        private int _bs2Direction;
        private ShaderProgram _convertShader;
        private int _csTexture;
        private int _csPalette;
        private ShaderProgram _dmdShader;
        private bool _dmdShaderInvalid = true;
        private int _dsDmdTexture;
        private int _dsDmdDotGlow;
        private int _dsDmdBackGlow;
        private int _dsDmdSize;
        private int _dsUnlitDot;
        private int _dsGlassTexture;
        private int _dsGlassTexOffset;
        private int _dsGlassTexScale;
        private int _dsGlassColor;
        private bool _lutInvalid = true;
        private bool _fboInvalid = true;
        private readonly uint[] _textures = new uint[8]; // The 8 textures are: glass, palette LUT, input data, dmd, dot glow, intermediate blur, back blur, temp
        private readonly uint[] _fbos = new uint[5]; // The 5 FBO used to write to the 5 last textures (dmd, dot glow, intermediate blur, back blur, temp)
        private bool _hasFrame = false;

        private Dictionary<byte, byte> _levels;
        private int _dmdWidth;
        private int _dmdHeight;
        private byte[] _frame;

        public static DmdController Instance(IView window = null) =>
            _instance ?? (_instance = new DmdController(window));

        private DmdController(IView window)
        {
            _window = window;

            _window.FramebufferResize += (size) =>
            {
                _fboInvalid = true;
                _hasFrame = true;
            };

            _gl = GL.GetApi(window);
            _gl.GenTextures(3, _textures);

            _fbos[0] = 0;
            _fbos[1] = 0;
            _fbos[2] = 0;
            _fbos[3] = 0;
            _fbos[4] = 0;

            _textures[3] = 0;
            _textures[4] = 0;
            _textures[5] = 0;
            _textures[6] = 0;
            _textures[7] = 0;

            try
            {
                _blurShader1 = new ShaderProgram();
                var frag = ReadResource(@"PinMameSilk.Shaders.Blur.frag") + "void main() { FragColor = vec4(blur_level_2(tex, uv, direction).rgb, 1.0); }";
                _blurShader1.Create(_gl, ReadResource(@"PinMameSilk.Shaders.Blur.vert"), frag, _attributeLocations);
                _bs1Texture = _blurShader1.GetUniformLocation(_gl, "tex");
                _bs1Direction = _blurShader1.GetUniformLocation(_gl, "direction");
            }

            catch (ShaderCompilationException e)
            {
                Logger.Fatal($"Blur Shader 1 compilation failed: output={e.CompilerOutput}");
            }

            try
            {
                _blurShader2 = new ShaderProgram();
                var frag = ReadResource(@"PinMameSilk.Shaders.Blur.frag") + "void main() { FragColor = vec4(blur_level_12(tex, uv, direction).rgb, 1.0); }";
                _blurShader2.Create(_gl, ReadResource(@"PinMameSilk.Shaders.Blur.vert"), frag, _attributeLocations);
                _bs2Texture = _blurShader2.GetUniformLocation(_gl, "tex");
                _bs2Direction = _blurShader2.GetUniformLocation(_gl, "direction");
            }

            catch (ShaderCompilationException e)
            {
                Logger.Fatal($"Blur Shader 2 compilation failed: output={e.CompilerOutput}");
            }

            _quadVbo = new VertexBufferArray();
            _quadVbo.Create(_gl);
            _quadVbo.Bind(_gl);

            var posVBO = new VertexBuffer();
            posVBO.Create(_gl);
            posVBO.Bind(_gl);
            posVBO.SetData(_gl, PositionAttribute, new float[] { -1f, -1f, -1f, 1f, 1f, 1f, 1f, -1f }, false, 2);

            var texVBO = new VertexBuffer();
            texVBO.Create(_gl);
            texVBO.Bind(_gl);
            texVBO.SetData(_gl, TexCoordAttribute, new float[] { 0f, 1f, 0f, 0f, 1f, 0f, 1f, 1f }, false, 2);
            _quadVbo.Unbind(_gl);
        }

        public void SetLayout(Dictionary<byte, byte> levels, int dmdWidth, int dmdHeight)
        {
            _levels = levels;
            _dmdWidth = dmdWidth;
            _dmdHeight = dmdHeight;
            _frame = new byte[dmdWidth * dmdHeight];

            _fboInvalid = true;
            _lutInvalid = true;
        }

        public unsafe void SetFrame(IntPtr framePtr)
        {
            _hasFrame = true;

            var ptr = (byte*)framePtr;

            for (var y = 0; y < _dmdHeight; y++)
            {
                for (var x = 0; x < _dmdWidth; x++)
                {
                    var pos = y * _dmdWidth + x;
                    _frame[pos] = _levels[ptr[pos]];
                }
            }
        }

        public unsafe void Render()
        {
            var createTexture = false;

            if (_dmdShaderInvalid)
            {
                // Create a dedicated DMD shader based on the selected style settings
                createTexture = true;

                _dmdShaderInvalid = false;
                _dmdShader?.Delete(_gl);

                try
                {
                    _dmdShader = new ShaderProgram();

                    var code = new StringBuilder();
                    code.Append("#version 330\n");

                    if (DmdStyle.HasBackGlow)
                    {
                        code.Append("#define BACKGLOW\n");
                    }
                    if (DmdStyle.HasDotGlow)
                    {
                        code.Append("#define DOTGLOW\n");
                    }
                    if (DmdStyle.HasBrightness)
                    {
                        code.Append("#define BRIGHTNESS\n");
                    }
                    if (DmdStyle.HasUnlitDot)
                    {
                        code.Append("#define UNLIT\n");
                    }
                    if (DmdStyle.HasGlass)
                    {
                        code.Append("#define GLASS\n");
                    }
                    if (DmdStyle.HasGamma)
                    {
                        code.Append("#define GAMMA\n");
                    }
                    if (DmdStyle.DotSize > 0.5)
                    {
                        code.Append("#define DOT_OVERLAP\n");
                    }

                    var nfi = System.Globalization.NumberFormatInfo.InvariantInfo;
                    code.AppendFormat(nfi, "const float dotSize = {0:0.00000};\n", DmdStyle.DotSize);
                    code.AppendFormat(nfi, "const float dotRounding = {0:0.00000};\n", DmdStyle.DotRounding);
                    code.AppendFormat(nfi, "const float sharpMax = {0:0.00000};\n", 0.01 + DmdStyle.DotSize * (1.0 - DmdStyle.DotSharpness));
                    code.AppendFormat(nfi, "const float sharpMin = {0:0.00000};\n", -0.01 - DmdStyle.DotSize * (1.0 - DmdStyle.DotSharpness));
                    code.AppendFormat(nfi, "const float brightness = {0:0.00000};\n", DmdStyle.Brightness);
                    code.AppendFormat(nfi, "const float backGlow = {0:0.00000};\n", DmdStyle.BackGlow);
                    code.AppendFormat(nfi, "const float dotGlow = {0:0.00000};\n", DmdStyle.DotGlow);
                    code.AppendFormat(nfi, "const float gamma = {0:0.00000};\n", DmdStyle.Gamma);
                    code.Append(ReadResource(@"PinMameSilk.Shaders.Dmd.frag"));
                    _dmdShader.Create(_gl, ReadResource(@"PinMameSilk.Shaders.Dmd.vert"), code.ToString(), _attributeLocations);
                    _dsDmdTexture = _dmdShader.GetUniformLocation(_gl, "dmdTexture");
                    _dsDmdDotGlow = _dmdShader.GetUniformLocation(_gl, "dmdDotGlow");
                    _dsDmdBackGlow = _dmdShader.GetUniformLocation(_gl, "dmdBackGlow");
                    _dsDmdSize = _dmdShader.GetUniformLocation(_gl, "dmdSize");
                    _dsUnlitDot = _dmdShader.GetUniformLocation(_gl, "unlitDot");
                    _dsGlassTexture = _dmdShader.GetUniformLocation(_gl, "glassTexture");
                    _dsGlassTexOffset = _dmdShader.GetUniformLocation(_gl, "glassTexOffset");
                    _dsGlassTexScale = _dmdShader.GetUniformLocation(_gl, "glassTexScale");
                    _dsGlassColor = _dmdShader.GetUniformLocation(_gl, "glassColor");
                }

                catch (ShaderCompilationException e)
                {
                    Logger.Fatal($"DMD Shader compilation failed: output={e.CompilerOutput}");
                }
            }

            if (_fboInvalid)
            {
                _fboInvalid = false;
                createTexture = true;

                uint[] texs = new uint[5]
                {
                    _textures[3],
                    _textures[4],
                    _textures[5],
                    _textures[6],
                    _textures[7]
                };

                _gl.DeleteTextures(5, texs);
                _gl.DeleteFramebuffers(5, _fbos);

                _gl.GenTextures(5, texs);
                _gl.GenFramebuffers(5, _fbos);
                _textures[3] = texs[0];
                _textures[4] = texs[1];
                _textures[5] = texs[2];
                _textures[6] = texs[3];
                _textures[7] = texs[4];

                for (int i = 0; i < _fbos.Length; i++)
                {
                    _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[i]);
                    _gl.BindTexture(TextureTarget.Texture2D, _textures[i + 3]);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 0f });
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgb, (uint)_window.FramebufferSize.X, (uint)_window.FramebufferSize.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
                    _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, TextureTarget.Texture2D, _textures[i + 3], 0);

                    GLEnum status = _gl.CheckFramebufferStatus(GLEnum.Framebuffer);
                    switch (status)
                    {
                        case GLEnum.FramebufferComplete:
                            break;
                        default:
                            Logger.Fatal($"Failed to build FBO for virtual DMD: [error: {status}]");
                            break;
                    }
                }
            }

            _quadVbo.Bind(_gl);

            for (int i = 0; i < _textures.Length; i++)
            {
                _gl.ActiveTexture(GLEnum.Texture0 + i);
                _gl.BindTexture(GLEnum.Texture2D, _textures[i]);
            }

            // Glass To Render

            if (_hasFrame)
            {
                _hasFrame = false;

                if (_lutInvalid && _levels != null)
                {
                    _lutInvalid = false;

                    byte[] data = new byte[3 * _levels.Count];

                    foreach (var level in _levels)
                    {
                        var alpha = 1.0f - DmdStyle.Tint.A / 255.0;
                        var beta = DmdStyle.Tint.A / 255.0;

                        int levelR = (int)(level.Key / 100f * DotColor.R);
                        int levelG = (int)(level.Key / 100f * DotColor.G);
                        int levelB = (int)(level.Key / 100f * DotColor.B);

                        ColorUtil.RgbToHsl((byte)levelR, (byte)levelG, (byte)levelB, out var dotHue, out var dotSat, out var dotLum);
                        ColorUtil.RgbToHsl(DmdStyle.Tint.R, DmdStyle.Tint.G, DmdStyle.Tint.B, out var tintHue, out var tintSat, out var tintLum);

                        ColorUtil.HslToRgb(dotHue, dotSat, dotLum, out var dotRed, out var dotGreen, out var dotBlue);
                        ColorUtil.HslToRgb(tintHue, tintSat, tintLum, out var tintRed, out var tintGreen, out var tintBlue);

                        var red = (byte)(dotRed * alpha + tintRed * beta);
                        var green = (byte)(dotGreen * alpha + tintGreen * beta);
                        var blue = (byte)(dotBlue * alpha + tintBlue * beta);

                        data[level.Value * 3] = red;
                        data[level.Value * 3 + 1] = green;
                        data[level.Value * 3 + 2] = blue;
                    }

                    _gl.ActiveTexture(GLEnum.Texture1);

                    fixed (void* d = data)
                    {
                        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgb, 16, 1, 0, PixelFormat.Rgb, PixelType.UnsignedByte, d);
                    }

                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
                }

                if (_convertShader == null)
                {
                    _convertShader?.Delete(_gl);
                    createTexture = true;
                    try
                    {
                        _convertShader = new ShaderProgram();
                        var code = new StringBuilder();
                        code.Append("#version 330 core\n");
                        if (DmdStyle.HasGamma) code.Append("#define GAMMA\n");
                        var nfi = System.Globalization.NumberFormatInfo.InvariantInfo;
                        code.AppendFormat(nfi, "const float gamma = {0:0.00000};\n", DmdStyle.Gamma);
                        code.AppendFormat(nfi, "const int dmdWidth = {0};\n", _dmdWidth);
                        code.Append(ReadResource(@"PinMameSilk.Shaders.Convert.frag"));
                        _convertShader.Create(_gl, ReadResource(@"PinMameSilk.Shaders.Convert.vert"), code.ToString(), _attributeLocations);
                        _csTexture = _convertShader.GetUniformLocation(_gl, "dmdData");
                        _csPalette = _convertShader.GetUniformLocation(_gl, "palette");
                    }
                    catch (ShaderCompilationException e)
                    {
                        Logger.Fatal($"Convert compilation failed: output={e.CompilerOutput}");
                    }
                }

                // Update DMD texture with latest frame

                _gl.ActiveTexture(GLEnum.Texture2);

                fixed (void* data = _frame)
                {
                    if (createTexture)
                    {
                        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Red,
                            (uint)_dmdWidth, (uint)_dmdHeight, 0, PixelFormat.Red, PixelType.UnsignedByte, data);
                    }
                    else
                    {
                        _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0,
                            (uint)_dmdWidth, (uint)_dmdHeight, GLEnum.Red, PixelType.UnsignedByte, data);
                    }
                }

                if (createTexture)
                {
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 0f });

                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
                }

                _convertShader.Bind(_gl);
                _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[0]);
                _gl.Uniform1(_csPalette, 1); // Color palette
                _gl.Uniform1(_csTexture, 2); // DMD texture
                _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                _convertShader.Unbind(_gl);

                if (DmdStyle.HasGlass || DmdStyle.HasDotGlow || DmdStyle.HasBackGlow)
                {
                    _blurShader1.Bind(_gl);
                    _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[4]);
                    _gl.Uniform1(_bs1Texture, 3); // DMD texture
                    _gl.Uniform2(_bs1Direction, 1.0f / _dmdWidth, 0.0f);
                    _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                    _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[1]);
                    _gl.Uniform1(_bs1Texture, 7); // DMD texture
                    _gl.Uniform2(_bs1Direction, 0.0f, 1.0f / _dmdHeight);
                    _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                    _blurShader1.Unbind(_gl);

                    _blurShader2.Bind(_gl);
                    _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[4]);
                    _gl.Uniform1(_bs2Texture, 4); // Previous Blur
                    _gl.Uniform2(_bs2Direction, 1.0f / _dmdWidth, 0.0f);
                    _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                    _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[2]);
                    _gl.Uniform1(_bs2Texture, 7); // DMD texture
                    _gl.Uniform2(_bs2Direction, 0.0f, 1.0f / _dmdHeight);
                    _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

                    _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[4]);
                    _gl.Uniform1(_bs2Texture, 5); // Previous Blur
                    _gl.Uniform2(_bs2Direction, 1.0f / _dmdWidth, 0.0f);
                    _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                    _gl.BindFramebuffer(GLEnum.Framebuffer, _fbos[3]);
                    _gl.Uniform1(_bs2Texture, 7); // DMD texture
                    _gl.Uniform2(_bs2Direction, 0.0f, 1.0f / _dmdHeight);
                    _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                    _blurShader2.Unbind(_gl);
                }
            }

            _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            _dmdShader.Bind(_gl);

            if (_dsGlassTexture != -1)
            {
                _gl.Uniform1(_dsGlassTexture, 0);
            }

            if (_dsDmdTexture != -1)
            {
                _gl.Uniform1(_dsDmdTexture, 3);
            }

            if (_dsDmdDotGlow != -1)
            {
                _gl.Uniform1(_dsDmdDotGlow, 4);
            }

            if (_dsDmdBackGlow != -1)
            {
                _gl.Uniform1(_dsDmdBackGlow, 6);
            }

            if (_dsDmdSize != -1)
            {
                _gl.Uniform2(_dsDmdSize, (float)_dmdWidth, (float)_dmdHeight);
            }

            if (_dsUnlitDot != -1)
            {
                _gl.Uniform3(_dsUnlitDot,
                    (float)(DmdStyle.UnlitDot.R / 255.0 / DmdStyle.Brightness),
                    (float)(DmdStyle.UnlitDot.G / 255.0 / DmdStyle.Brightness),
                    (float)(DmdStyle.UnlitDot.B / 255.0 / DmdStyle.Brightness));
            }

            //if (_dsGlassTexOffset != -1) gl.Uniform2(_dsGlassTexOffset, (float)(_style.GlassPadding.Left / DmdWidth), (float)(_style.GlassPadding.Top / DmdHeight));
            //if (_dsGlassTexScale != -1) gl.Uniform2(_dsGlassTexScale, (float)(1f + (_style.GlassPadding.Left + _style.GlassPadding.Right) / DmdWidth), (float)(1f + (_style.GlassPadding.Top + _style.GlassPadding.Bottom) / DmdHeight));
            //if (_dsGlassColor != -1) gl.Uniform4(_dsGlassColor, _style.GlassColor.ScR, _style.GlassColor.ScG, _style.GlassColor.ScB, (float)_style.GlassLighting);

            _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            _dmdShader.Unbind(_gl);

            _quadVbo.Unbind(_gl);
        }

        public void InvalidateStyle()
        {
            _dmdShaderInvalid = true;
        }

        public void InvalidatePalette()
        {
            _lutInvalid = true;
            _hasFrame = true;
        }

        private string ReadResource(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
