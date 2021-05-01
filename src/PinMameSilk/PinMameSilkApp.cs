﻿using NLog;
using NLog.Config;
using NLog.Targets;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace PinMameSilk
{
    class PinMameSilkApp
    {
        static void Main(string[] args)
        {
            LogManager.Configuration = new LoggingConfiguration();

            var target = new ConsoleTarget("PinMameSilk");

            LogManager.Configuration.AddTarget(target);
            LogManager.Configuration.AddRule(LogLevel.Info, LogLevel.Fatal, target);

            LogManager.ReconfigExistingLoggers();

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(128 * 6, 32 * 6);
            options.Title = "PinMAME .NET Silk";

            var window = Window.Create(options);
            var gl = GL.GetApi(window);

            DmdController dmdController = null;
            UIOverlayController uiOverlayController = null;

            window.Load += () =>
            {
                dmdController = DmdController.Instance(window);
                uiOverlayController = UIOverlayController.Instance(window, gl);

                // (ImGui needs resize message first)

                window.Resize += (size) =>
                {
                    size.Y = size.X * 32 / 128;

                    window.Size = size;
                };
            };

            window.FramebufferResize += (size) =>
            {
                gl.Viewport(size);
            };

            window.Render += (delta) =>
            {
                gl.Clear((uint)ClearBufferMask.ColorBufferBit);
                gl.ClearColor(1.0f, 1.0f, 0.0f, 1.0f);

                dmdController.Render();
                uiOverlayController.Render(delta);
            };

            window.Run();
        }
    }
}