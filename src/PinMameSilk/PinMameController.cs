using System;
using System.Collections.Generic;
using System.Drawing;
using LibDmd;
using NLog;
using Silk.NET.Input;

namespace PinMameSilk
{
    class PinMameController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static PinMameController _instance;
        private IInputContext _input;

        public Color DotColor { get; set; } = Color.FromArgb(255, 255, 88, 32);
        public DmdStyle DmdStyle { get; } = new DmdStyle();
        public PinMame.PinMameGame CurrentGame { get; set; } = null;

        private PinMame.PinMame _pinMame;
        private List<PinMame.PinMameGame> _games = null;
        private DmdController _dmdController;

        private int[] _keys = new int[128];

        public static PinMameController Instance(IInputContext input = null) =>
            _instance ?? (_instance = new PinMameController(input));

        public static readonly Dictionary<Key, PinMame.PinMameKeycode> _keycodeMap = new Dictionary<Key, PinMame.PinMameKeycode>() {
                { Key.A, PinMame.PinMameKeycode.A },
                { Key.B, PinMame.PinMameKeycode.B },
                { Key.C, PinMame.PinMameKeycode.C },
                { Key.D, PinMame.PinMameKeycode.D },
                { Key.E, PinMame.PinMameKeycode.E },
                { Key.F, PinMame.PinMameKeycode.F },
                { Key.G, PinMame.PinMameKeycode.G },
                { Key.H, PinMame.PinMameKeycode.H },
                { Key.I, PinMame.PinMameKeycode.I },
                { Key.J, PinMame.PinMameKeycode.J },
                { Key.K, PinMame.PinMameKeycode.K },
                { Key.L, PinMame.PinMameKeycode.L },
                { Key.M, PinMame.PinMameKeycode.M },
                { Key.N, PinMame.PinMameKeycode.N },
                { Key.O, PinMame.PinMameKeycode.O },
                { Key.P, PinMame.PinMameKeycode.P },
                { Key.Q, PinMame.PinMameKeycode.Q },
                { Key.R, PinMame.PinMameKeycode.R },
                { Key.S, PinMame.PinMameKeycode.S },
                { Key.T, PinMame.PinMameKeycode.T },
                { Key.U, PinMame.PinMameKeycode.U },
                { Key.V, PinMame.PinMameKeycode.V },
                { Key.W, PinMame.PinMameKeycode.W },
                { Key.X, PinMame.PinMameKeycode.X },
                { Key.Y, PinMame.PinMameKeycode.Y },
                { Key.Z, PinMame.PinMameKeycode.Z },
                { Key.Number0, PinMame.PinMameKeycode.Num0 },
                { Key.Number1, PinMame.PinMameKeycode.Num1 },
                { Key.Number2, PinMame.PinMameKeycode.Num2 },
                { Key.Number3, PinMame.PinMameKeycode.Num3 },
                { Key.Number4, PinMame.PinMameKeycode.Num4 },
                { Key.Number5, PinMame.PinMameKeycode.Num5 },
                { Key.Number6, PinMame.PinMameKeycode.Num6 },
                { Key.Number7, PinMame.PinMameKeycode.Num7 },
                { Key.Number8, PinMame.PinMameKeycode.Num8 },
                { Key.Number9, PinMame.PinMameKeycode.Num9 },
                { Key.Keypad0, PinMame.PinMameKeycode.Num0Pad },
                { Key.Keypad1, PinMame.PinMameKeycode.Num1Pad },
                { Key.Keypad2, PinMame.PinMameKeycode.Num2Pad },
                { Key.Keypad3, PinMame.PinMameKeycode.Num3Pad },
                { Key.Keypad4, PinMame.PinMameKeycode.Num4Pad },
                { Key.Keypad5, PinMame.PinMameKeycode.Num5Pad },
                { Key.Keypad6, PinMame.PinMameKeycode.Num6Pad },
                { Key.Keypad7, PinMame.PinMameKeycode.Num7Pad },
                { Key.Keypad8, PinMame.PinMameKeycode.Num8Pad },
                { Key.Keypad9, PinMame.PinMameKeycode.Num9Pad },
                { Key.F1, PinMame.PinMameKeycode.F1 },
                { Key.F2, PinMame.PinMameKeycode.F2 },
                { Key.F3, PinMame.PinMameKeycode.F3 },
                { Key.F4, PinMame.PinMameKeycode.F4 },
                { Key.F5, PinMame.PinMameKeycode.F5 },
                { Key.F6, PinMame.PinMameKeycode.F6 },
                { Key.F7, PinMame.PinMameKeycode.F7 },
                { Key.F8, PinMame.PinMameKeycode.F8 },
                { Key.F9, PinMame.PinMameKeycode.F9 },
                { Key.F10, PinMame.PinMameKeycode.F10 },
                { Key.F11, PinMame.PinMameKeycode.F11 },
                { Key.F12, PinMame.PinMameKeycode.F12 },
                { Key.Escape, PinMame.PinMameKeycode.Escape },
                { Key.GraveAccent, PinMame.PinMameKeycode.Tilde },
                { Key.Minus, PinMame.PinMameKeycode.Minus },
                { Key.Equal, PinMame.PinMameKeycode.Equals },
                { Key.Backspace, PinMame.PinMameKeycode.Backspace },
                { Key.Tab, PinMame.PinMameKeycode.Tab },
                { Key.LeftBracket, PinMame.PinMameKeycode.OpenBrace },
                { Key.RightBracket, PinMame.PinMameKeycode.CloseBrace },
                { Key.Enter, PinMame.PinMameKeycode.Enter },
                { Key.Semicolon, PinMame.PinMameKeycode.Colon },
                { Key.Apostrophe, PinMame.PinMameKeycode.Quote },
                { Key.BackSlash, PinMame.PinMameKeycode.Backslash },
                //{ Key.BACKSLASH2, PinMame.PinMameKeycode.BACKSLASH2 },
                { Key.Comma, PinMame.PinMameKeycode.Comma },
                //{ Key.STOP, PinMame.PinMameKeycode.STOP },
                { Key.Slash, PinMame.PinMameKeycode.Slash },
                { Key.Space, PinMame.PinMameKeycode.Space },
                { Key.Insert, PinMame.PinMameKeycode.Insert },
                { Key.Delete, PinMame.PinMameKeycode.Del },
                { Key.Home, PinMame.PinMameKeycode.Home },
                { Key.End, PinMame.PinMameKeycode.End },
                { Key.PageUp, PinMame.PinMameKeycode.PageUp },
                { Key.PageDown, PinMame.PinMameKeycode.PageDown },
                { Key.Left, PinMame.PinMameKeycode.Left },
                { Key.Right, PinMame.PinMameKeycode.Right },
                { Key.Up, PinMame.PinMameKeycode.Up },
                { Key.Down, PinMame.PinMameKeycode.Down },
                { Key.KeypadDivide, PinMame.PinMameKeycode.SlashPad },
                { Key.KeypadMultiply, PinMame.PinMameKeycode.Asterisk },
                { Key.KeypadSubtract, PinMame.PinMameKeycode.MinusPad },
                { Key.KeypadAdd, PinMame.PinMameKeycode.PlusPad },
                //{ Key.DEL_PAD, PinMame.PinMameKeycode.DEL_PAD },
                { Key.KeypadEnter, PinMame.PinMameKeycode.EnterPad },
                { Key.PrintScreen, PinMame.PinMameKeycode.PrintScreen },
                { Key.Pause, PinMame.PinMameKeycode.Pause },
                { Key.ShiftLeft, PinMame.PinMameKeycode.LeftShift },
                { Key.ShiftRight, PinMame.PinMameKeycode.RightShift },
                { Key.ControlLeft, PinMame.PinMameKeycode.LeftControl },
                { Key.ControlRight, PinMame.PinMameKeycode.RightControl },
                { Key.AltLeft, PinMame.PinMameKeycode.LeftAlt },
                { Key.AltRight, PinMame.PinMameKeycode.RightAlt },
                //{ Key.ScrollLock, PinMame.PinMameKeycode.ScrollLock },
                { Key.NumLock, PinMame.PinMameKeycode.NumLock },
                { Key.CapsLock, PinMame.PinMameKeycode.CapsLOCK },
                //{ Key.LWIN, PinMame.PinMameKeycode.LWIN },
                //{ Key.RWIN, PinMame.PinMameKeycode.RWIN },
                { Key.Menu, PinMame.PinMameKeycode.Menu }
        };
       
        private PinMameController(IInputContext input)
        {
            _input = input;

            foreach (var keyboard in _input.Keyboards)
            {
                keyboard.KeyDown += (arg1, arg2, arg3) =>
                {
                    
                    if (_keycodeMap.TryGetValue(arg2, out var keycode))
                    {
                        int keycodeInt = (int)keycode;

                        Logger.Info($"KeyDown() {keycode} ({keycodeInt})");

                        _keys[(int)keycode] = 1;
                    }
                };

                keyboard.KeyUp += (arg1, arg2, arg3) =>
                {
                    if (_keycodeMap.TryGetValue(arg2, out var keycode))
                    {
                        int keycodeInt = (int)keycode;

                        Logger.Info($"KeyUp() {keycode} ({keycodeInt})");

                        _keys[(int)keycode] = 0;
                    }
                };
            }

            _pinMame = PinMame.PinMame.Instance();

            _pinMame.OnGameStarted += OnGameStarted;
            _pinMame.OnDisplayAvailable += OnDisplayAvailable;
            _pinMame.OnDisplayUpdated += OnDisplayUpdated;
            _pinMame.OnGameEnded += OnGameEnded;
            _pinMame.IsKeyPressed += IsKeyPressed;

            _dmdController = DmdController.Instance();
        }

        public List<PinMame.PinMameGame> GetGames(bool forceRefresh = false)
        {
            if (_games == null || forceRefresh)
            {
                _games = (List<PinMame.PinMameGame>)_pinMame.GetFoundGames();
            }

            return _games;
        }

        public void Start()
        {
            try
            {
                _pinMame.StartGame(CurrentGame.Name);
            }

            catch (Exception e)
            {
                Logger.Fatal(e);
            }
        }

        public bool IsRunning => PinMame.PinMame.IsRunning;

        public void SetSwitch(int slot, bool state)
        {
            _pinMame.SetSwitch(slot, state);
        }

        public void Stop()
        {
            Logger.Info("Stop");

            _pinMame.StopGame();
        }

        public void Reset()
        {
            Logger.Info("Reset");

            _pinMame.ResetGame();
        }

        private void OnGameStarted()
        {
            Logger.Info("OnGameStarted");
        }


        private int IsKeyPressed(PinMame.PinMameKeycode keycode)
        { 
            return _keys[(int)keycode];
        }

        private void OnDisplayAvailable(int index, int displayCount, PinMame.PinMameDisplayLayout displayLayout)
        {
            Logger.Info($"OnDisplayAvailable: index={index}, displayCount={displayCount}, displayLayout={displayLayout}");

            if (displayLayout.IsDmd)
            {
                _dmdController.SetLayout(displayLayout.Levels, displayLayout.Width, displayLayout.Height);
            }
        }

        private void OnDisplayUpdated(int index, IntPtr framePtr, PinMame.PinMameDisplayLayout displayLayout)
        {
            Logger.Debug($"OnDisplayUpdated: index={index}");

            if (displayLayout.IsDmd)
            {
                _dmdController.SetFrame(framePtr);
            }
        }

        private void OnGameEnded()
        {
            Logger.Info($"OnGameEnded");
        }
    }
}
