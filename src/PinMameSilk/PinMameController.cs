using System;
using System.Collections.Generic;
using System.Drawing;
using LibDmd;
using NLog;

namespace PinMameSilk
{
    class PinMameController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static PinMameController _instance;

        public Color DotColor { get; set; } = Color.FromArgb(255, 255, 88, 32);
        public DmdStyle DmdStyle { get; } = new DmdStyle();
        public PinMame.PinMameGame CurrentGame { get; set; } = null;

        private PinMame.PinMame _pinMame;
        private List<PinMame.PinMameGame> _games = null;
        private DmdController _dmdController;

        public static PinMameController Instance() =>
            _instance ?? (_instance = new PinMameController());

        private PinMameController()
        {
            _pinMame = PinMame.PinMame.Instance();

            _pinMame.OnGameStarted += OnGameStarted;
            _pinMame.OnDisplayAvailable += OnDisplayAvailable;
            _pinMame.OnDisplayUpdated += OnDisplayUpdated;
            _pinMame.OnGameEnded += OnGameEnded;

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
