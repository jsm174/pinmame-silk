using System.Numerics;
using LibDmd.Common;
using NLog;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using ImGuiNET;
using ImGuiCustom;

namespace PinMameSilk
{
    class UIOverlayController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static UIOverlayController _instance;

        private IView _window;
        private IInputContext _input;

        private ImGuiController _imGuiController;
        private PinMameController _pinMameController;
        private DmdController _dmdController;

        public static UIOverlayController Instance(IView window, IInputContext input, GL gl) =>
            _instance ?? (_instance = new UIOverlayController(window, input, gl));

        private ImGuiFadeInOut _imGuiFadeInOut;
        private Vector2 _lastPosition;
        private double _inactivityTime;

        private bool _overlayEnabled = true;

        private UIOverlayController(IView window, IInputContext input, GL gl)
        {
            _window = window;
            _input = input;

            _imGuiController = new ImGuiController(
               gl,
               window,
               _input);

            _imGuiFadeInOut = new ImGuiFadeInOut();

            _pinMameController = PinMameController.Instance(_input);
            _dmdController = DmdController.Instance();
        }

        public void Render(double delta)
        {
            var position = _input.Mice[0].Position;
            var movement = position != _lastPosition;
            var inside = (position.X >= -5f && position.X <= _window.Size.X + 5f &&
                        position.Y >= -25f && position.Y <= _window.Size.Y + 10f);

            if (_overlayEnabled)
            {
                if (movement && inside)
                {
                    _inactivityTime = 0;
                }

                if ((_inactivityTime > 1.5f && !inside) ||
                    _inactivityTime > 6f)
                {
                    _overlayEnabled = false;
                    _inactivityTime = 0;
                }
            }
            else if (movement)
            {
               _overlayEnabled = inside;
               _inactivityTime = 0;
            }
                
            _imGuiController.Update((float)delta);

            var opacity = _imGuiFadeInOut.FadeInOut(.1f, .20f, 0, 1, _overlayEnabled);
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);

            ShowRomWindow(opacity);
            ShowColorsWindow(opacity);
            ShowStylesWindow(opacity);

            ImGui.PopStyleVar();

            _imGuiController.Render();

            _inactivityTime += delta;
            _lastPosition = position;
        }

        private void ShowRomWindow(float opacity)
        {
            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Once);
            ImGui.SetNextWindowSize(new Vector2(500, 50), ImGuiCond.Once);
            ImGui.SetNextWindowBgAlpha(opacity);

            ImGui.Begin("ROM", ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);

            var games = _pinMameController.GetGames();
            var currentGame = _pinMameController.CurrentGame;

            var text = "";

            if (currentGame != null)
            {
                text = $"{currentGame.Name} - {currentGame.Description}";
            }
            else
            {
                text = (games.Count > 0) ? "Select..." : "No Games Found";
            }

            if (ImGui.BeginCombo("", text))
            {
                foreach (var game in games)
                {
                    bool isSelected = currentGame != null && currentGame.Name == game.Name;

                    if (ImGui.Selectable($"{game.Name} - {game.Description}", isSelected))
                    {
                        if (!_pinMameController.IsRunning)
                        {
                            _pinMameController.CurrentGame = game;
                        }
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            if (!_pinMameController.IsRunning)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, .39f, 0, 1));
                if (ImGui.Button("Start"))
                {
                    _pinMameController.Start();
                }
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1));
                if (ImGui.Button("Stop"))
                {
                    _pinMameController.Stop();
                }
                ImGui.PopStyleColor();

                ImGui.SameLine();

                if (ImGui.Button("Reset"))
                {
                    _pinMameController.Reset();
                }
            }

            ImGui.End();
        }

        private void ShowColorsWindow(float opacity)
        {
            var styleChange = false;
            var paletteChange = false;

            ImGui.SetNextWindowPos(new Vector2(0, _window.Size.Y - 80), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(320, 80), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(opacity);

            ImGui.Begin("Colors", ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);

            Vector3 tmpColor3 = ColorUtil.ToVector3(_dmdController.DotColor);

            if (ImGui.ColorEdit3("Dot", ref tmpColor3))
            {
                _dmdController.DotColor = ColorUtil.FromVector3(tmpColor3);

                paletteChange = true;
            }

            var dmdStyle = _dmdController.DmdStyle;

            Vector4 tmpColor4 = ColorUtil.ToVector4(dmdStyle.Tint);

            if (ImGui.ColorEdit4("Tint", ref tmpColor4))
            {
                dmdStyle.Tint = ColorUtil.FromVector4(tmpColor4);

                paletteChange = true;
            }

            tmpColor3 = ColorUtil.ToVector3(dmdStyle.UnlitDot);

            if (ImGui.ColorEdit3("Unlit Dot", ref tmpColor3))
            {
                dmdStyle.UnlitDot = ColorUtil.FromVector3(tmpColor3);

                styleChange = true;
            }

            if (paletteChange)
            {
                _dmdController.InvalidatePalette();
            }

            if (styleChange)
            {
                _dmdController.InvalidateStyle();
            }

            ImGui.End();
        }

        private void ShowStylesWindow(float opacity)
        {
            ImGui.SetNextWindowPos(new Vector2(_window.Size.X - 310, (_window.Size.Y - 165) / 2), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(310, 165), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(opacity);

            ImGui.Begin("Styles", ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);

            var styleChange = false;
            var paletteChange = false;

            var dmdStyle = _dmdController.DmdStyle;

            float tmpFloat = (float)dmdStyle.DotSize;

            if (ImGui.SliderFloat("Dot Size", ref tmpFloat, 0, 1))
            {
                dmdStyle.DotSize = tmpFloat;

                styleChange = true;
            }

            tmpFloat = (float)dmdStyle.DotRounding;

            if (ImGui.SliderFloat("Dot Rounding", ref tmpFloat, 0, 1))
            {
                dmdStyle.DotRounding = tmpFloat;

                styleChange = true;
            }

            tmpFloat = (float)dmdStyle.DotSharpness;

            if (ImGui.SliderFloat("Dot Sharpness", ref tmpFloat, 0, 1))
            {
                dmdStyle.DotSharpness = tmpFloat;

                styleChange = true;
            }

            tmpFloat = (float)dmdStyle.Brightness;

            if (ImGui.SliderFloat("Brightness", ref tmpFloat, 0, 1))
            {
                dmdStyle.Brightness = tmpFloat;

                styleChange = true;
            }

            tmpFloat = (float)dmdStyle.DotGlow;

            if (ImGui.SliderFloat("Dot Glow", ref tmpFloat, 0, 1))
            {
                dmdStyle.DotGlow = tmpFloat;

                styleChange = true;
                paletteChange = true;
            }

            tmpFloat = (float)dmdStyle.BackGlow;

            if (ImGui.SliderFloat("Back Glow", ref tmpFloat, 0, 1))
            {
                dmdStyle.BackGlow = tmpFloat;

                styleChange = true;
                paletteChange = true;
            }

            tmpFloat = (float)dmdStyle.Gamma;

            if (ImGui.SliderFloat("Gamma", ref tmpFloat, 0, 1))
            {
                dmdStyle.Gamma = tmpFloat;

                styleChange = true;
            }

            if (paletteChange)
            {
                _dmdController.InvalidatePalette();
            }

            if (styleChange)
            {
                _dmdController.InvalidateStyle();
            }

            ImGui.End();
        }
    }
}
