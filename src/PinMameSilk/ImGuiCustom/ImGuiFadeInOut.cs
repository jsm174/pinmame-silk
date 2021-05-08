//
// ImGuiFadeInOut was derived from imgui_fade_in_out.cpp and imgui_fade_in_out.hpp at:
// https://framagit.org/ericb/miniDart/-/blob/master/Sources/src/3rdparty/imgui_custom
//

using System;
using ImGuiNET;

namespace ImGuiCustom
{
    public class ImGuiFadeInOut
    {
        private ImGuiIOPtr _io;

        private bool _upHeartBeatAction;
        private float _upHeartBeatStep;
        private float _downHeartBeatStep;
        private float _range;

        private float _opacityHeartBeat;
        private float _opacity;

        public ImGuiFadeInOut()
        {
            _io = ImGui.GetIO();

            _opacityHeartBeat = 1.0f;
            _opacity = 1.0f;
        }

        public float HeartBeat(float upDuration, float downDuration, float min, float max, bool insideWindow)
        {
            SetRange(min, max);

            _upHeartBeatStep = CalculateStep(GetRange(), upDuration);
            _downHeartBeatStep = CalculateStep(GetRange(), downDuration);

            if (insideWindow)
            {
                _opacityHeartBeat = max;
            }
            else if (_upHeartBeatAction == true)
            {
                _opacityHeartBeat += _upHeartBeatStep;

                if (_opacityHeartBeat >= max)
                {
                    _opacityHeartBeat = max;
                    _upHeartBeatAction = false;
                }
            }
            else
            {
                _opacityHeartBeat -= _downHeartBeatStep;

                if (_opacityHeartBeat <= min)
                {
                    _opacityHeartBeat = min;
                    _upHeartBeatAction = true;
                }
            }

            return (float)Math.Sin(_opacityHeartBeat);
        }

        public float FadeInOut(float upDuration, float downDuration, float min, float max, bool insideWindow)
        {
            if (!insideWindow)
            {
                _opacity -= CalculateStep((max - min), downDuration);

                if (_opacity < min)
                    _opacity = min;
            }
            else
            {
                _opacity += CalculateStep((max - min), upDuration);

                if (_opacity > max)
                    _opacity = max;
            }
            return _opacity;
        }

        private void SetRange(float min, float max)
        {
            _range = max - min;
        }

        private float GetRange()
        {
            return _range;
        }

        private float CalculateStep(float range, float duration)
        {
            return (range * _io.DeltaTime) / duration;
        }
    }
}
