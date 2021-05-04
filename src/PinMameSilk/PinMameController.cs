using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LibDmd;
using NLog;
using Silk.NET.Input;
using Silk.NET.OpenAL;

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

        private static int BUFFERS = 2;

        private PinMame.PinMameAudioInfo _audioInfo;

        private AL _al;
        private uint[] _audioBuffers;
        private uint _audioSource;
        private short[] _audioBuffer;

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
                { Key.Number0, PinMame.PinMameKeycode.Number0 },
                { Key.Number1, PinMame.PinMameKeycode.Number1 },
                { Key.Number2, PinMame.PinMameKeycode.Number2 },
                { Key.Number3, PinMame.PinMameKeycode.Number3 },
                { Key.Number4, PinMame.PinMameKeycode.Number4 },
                { Key.Number5, PinMame.PinMameKeycode.Number5 },
                { Key.Number6, PinMame.PinMameKeycode.Number6 },
                { Key.Number7, PinMame.PinMameKeycode.Number7 },
                { Key.Number8, PinMame.PinMameKeycode.Number8 },
                { Key.Number9, PinMame.PinMameKeycode.Number9 },
                { Key.Keypad0, PinMame.PinMameKeycode.Keypad0 },
                { Key.Keypad1, PinMame.PinMameKeycode.Keypad1 },
                { Key.Keypad2, PinMame.PinMameKeycode.Keypad2 },
                { Key.Keypad3, PinMame.PinMameKeycode.Keypad3 },
                { Key.Keypad4, PinMame.PinMameKeycode.Keypad4 },
                { Key.Keypad5, PinMame.PinMameKeycode.Keypad5 },
                { Key.Keypad6, PinMame.PinMameKeycode.Keypad6 },
                { Key.Keypad7, PinMame.PinMameKeycode.Keypad7 },
                { Key.Keypad8, PinMame.PinMameKeycode.Keypad8 },
                { Key.Keypad9, PinMame.PinMameKeycode.Keypad9 },
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
                { Key.GraveAccent, PinMame.PinMameKeycode.GraveAccent },
                { Key.Minus, PinMame.PinMameKeycode.Minus },
                { Key.Equal, PinMame.PinMameKeycode.Equals },
                { Key.Backspace, PinMame.PinMameKeycode.Backspace },
                { Key.Tab, PinMame.PinMameKeycode.Tab },
                { Key.LeftBracket, PinMame.PinMameKeycode.LeftBracket },
                { Key.RightBracket, PinMame.PinMameKeycode.RightBracket },
                { Key.Enter, PinMame.PinMameKeycode.Enter },
                { Key.Semicolon, PinMame.PinMameKeycode.Semicolon },
                { Key.Apostrophe, PinMame.PinMameKeycode.Quote },
                { Key.BackSlash, PinMame.PinMameKeycode.Backslash },
                { Key.Comma, PinMame.PinMameKeycode.Comma },
                { Key.Period, PinMame.PinMameKeycode.Period },
                { Key.Slash, PinMame.PinMameKeycode.Slash },
                { Key.Space, PinMame.PinMameKeycode.Space },
                { Key.Insert, PinMame.PinMameKeycode.Insert },
                { Key.Delete, PinMame.PinMameKeycode.Delete },
                { Key.Home, PinMame.PinMameKeycode.Home },
                { Key.End, PinMame.PinMameKeycode.End },
                { Key.PageUp, PinMame.PinMameKeycode.PageUp },
                { Key.PageDown, PinMame.PinMameKeycode.PageDown },
                { Key.Left, PinMame.PinMameKeycode.Left },
                { Key.Right, PinMame.PinMameKeycode.Right },
                { Key.Up, PinMame.PinMameKeycode.Up },
                { Key.Down, PinMame.PinMameKeycode.Down },
                { Key.KeypadDivide, PinMame.PinMameKeycode.KeypadDivide },
                { Key.KeypadMultiply, PinMame.PinMameKeycode.KeypadMultiply },
                { Key.KeypadSubtract, PinMame.PinMameKeycode.KeypadSubtract },
                { Key.KeypadAdd, PinMame.PinMameKeycode.KeypadAdd },
                { Key.KeypadEnter, PinMame.PinMameKeycode.KeypadEnter },
                { Key.PrintScreen, PinMame.PinMameKeycode.PrintScreen },
                { Key.Pause, PinMame.PinMameKeycode.Pause },
                { Key.ShiftLeft, PinMame.PinMameKeycode.LeftShift },
                { Key.ShiftRight, PinMame.PinMameKeycode.RightShift },
                { Key.ControlLeft, PinMame.PinMameKeycode.LeftControl },
                { Key.ControlRight, PinMame.PinMameKeycode.RightControl },
                { Key.AltLeft, PinMame.PinMameKeycode.LeftAlt },
                { Key.AltRight, PinMame.PinMameKeycode.RightAlt },
                { Key.ScrollLock, PinMame.PinMameKeycode.ScrollLock },
                { Key.NumLock, PinMame.PinMameKeycode.NumLock },
                { Key.CapsLock, PinMame.PinMameKeycode.CapsLock },
                { Key.SuperLeft, PinMame.PinMameKeycode.LeftSuper },
                { Key.SuperRight, PinMame.PinMameKeycode.RightSuper },
                { Key.Menu, PinMame.PinMameKeycode.Menu }
        };

        private int[] _keypress = new int[128];

        public static PinMameController Instance(IInputContext input = null) =>
         _instance ?? (_instance = new PinMameController(input));

        private unsafe PinMameController(IInputContext input)
        {
            _pinMame = PinMame.PinMame.Instance();

            _pinMame.OnGameStarted += OnGameStarted;
            _pinMame.OnDisplayAvailable += OnDisplayAvailable;
            _pinMame.OnDisplayUpdated += OnDisplayUpdated;
            _pinMame.OnAudioAvailable += OnAudioAvailable;
            _pinMame.OnAudioUpdated += OnAudioUpdated;
            _pinMame.OnGameEnded += OnGameEnded;
            _pinMame.IsKeyPressed += IsKeyPressed;

            _dmdController = DmdController.Instance();

            _input = input;

            foreach (var keyboard in _input.Keyboards)
            {
                keyboard.KeyDown += (arg1, arg2, arg3) =>
                {

                    if (_keycodeMap.TryGetValue(arg2, out var keycode))
                    {
                        Logger.Trace($"KeyDown() {keycode} ({(int)keycode})");

                        _keypress[(int)keycode] = 1;
                    }
                };

                keyboard.KeyUp += (arg1, arg2, arg3) =>
                {
                    if (_keycodeMap.TryGetValue(arg2, out var keycode))
                    {
                        Logger.Trace($"KeyUp() {keycode} ({(int)keycode})");

                        _keypress[(int)keycode] = 0;
                    }
                };
            }

            _al = AL.GetApi();

            ALContext alContext = ALContext.GetApi();
            var device = alContext.OpenDevice("");

            if (device == null)
            {
                Console.WriteLine("Could not create device");
                return;
            }

            var context = alContext.CreateContext(device, null);
            alContext.MakeContextCurrent(context);

            _audioSource = _al.GenSource();
            _audioBuffers = _al.GenBuffers(BUFFERS);
        }

        public List<PinMame.PinMameGame> GetGames(bool forceRefresh = false)
        {
            if (_games == null || forceRefresh)
            {
                _games = _pinMame.GetFoundGames().OrderBy(g => g.Name).ToList();
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
            return _keypress[(int)keycode];
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

        private int OnAudioAvailable(PinMame.PinMameAudioInfo audioInfo)
        {
            Logger.Info($"OnAudioAvailable: audioInfo={audioInfo}");

            _audioInfo = audioInfo;
            _audioBuffer = new short[audioInfo.BufferSize];

            unsafe
            {
                fixed (void* ptr = _audioBuffer)
                {
                    for (int index = 0; index < BUFFERS; index++)
                    {
                        _al.BufferData(_audioBuffers[index],
                          _audioInfo.Channels == 2 ? BufferFormat.Stereo16 : BufferFormat.Mono16,
                          ptr, _audioInfo.SamplesPerFrame, (int)_audioInfo.SampleRate);
                    }

                    _al.SourceQueueBuffers(_audioSource, _audioBuffers);
                }
            }

            return audioInfo.SamplesPerFrame;
        }

        private int OnAudioUpdated(IntPtr framePtr, int samples)
        {
            Logger.Trace($"OnAudioUpdated");

            unsafe
            {

                var data = (short*)framePtr;

                for (int loop = 0; loop < samples * 2; loop++)
                {
                    _audioBuffer[loop] = data[loop];
                }
            }
            return samples;
        }

        private void OnGameEnded()
        {
            Logger.Info($"OnGameEnded");
        }

        public unsafe void UpdateSound()
        {
            _al.GetSourceProperty(_audioSource, GetSourceInteger.BuffersProcessed, out int buffersProcessed);

            while (buffersProcessed > 0)
            {
                uint buffer = 0;
                _al.SourceUnqueueBuffers(_audioSource, 1, &buffer);

                fixed (void* ptr = _audioBuffer)
                {
                    _al.BufferData(buffer,
                        _audioInfo.Channels == 2 ? BufferFormat.Stereo16 : BufferFormat.Mono16, ptr, _audioInfo.SamplesPerFrame, (int)_audioInfo.SampleRate);
                }

                _al.SourceQueueBuffers(_audioSource, 1, &buffer);

                buffersProcessed--;
            }

            _al.GetSourceProperty(_audioSource, GetSourceInteger.SourceState, out int state);

            if ((SourceState)state != SourceState.Playing)
            {
                _al.SourcePlay(_audioSource);
            }
        }
    }
}
