using GameControllerForZwift.Core;
using InputSimulatorEx.Native;
using InputSimulatorEx;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameControllerForZwift.Keyboard
{
    public class KeyboardService : IOutputService
    {
        #region Fields
        private readonly InputSimulator _simulator = new InputSimulator();
        private readonly ILogger<KeyboardService> _logger;
        private readonly ConcurrentDictionary<ZwiftFunction, CancellationTokenSource> _activeKeyPresses = new();
        private readonly TimeSpan _actionTimeout = TimeSpan.FromMilliseconds(100);
        //private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1); // Thread safety around which ZwiftFunction is acted upon
        #endregion

        #region Constructor
        public KeyboardService(ILogger<KeyboardService> logger)
        {
            _logger = logger;
        }
        #endregion

        #region Methods

        public async Task<ActionResult> PerformActionAsync(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView = ZwiftPlayerView.Default, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
        {
            //await _lock.WaitAsync();
            try
            {
                VirtualKeyCode keyCode = GetKeyCode(zwiftFunction, playerView, riderAction);

                if (keyCode == default)
                {
                    return new ActionResult { Success = false, ErrorMessage = "Unhandled Zwift function." };
                }

                // Cancel any existing key press task for this function
                if (_activeKeyPresses.TryGetValue(zwiftFunction, out var existingTokenSource))
                {
                    existingTokenSource.Cancel();
                }

                // Press the key down and start a task to release it
                _simulator.Keyboard.KeyDown(keyCode);
                var tokenSource = new CancellationTokenSource();
                _activeKeyPresses[zwiftFunction] = tokenSource;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(_actionTimeout, tokenSource.Token);
                        _simulator.Keyboard.KeyUp(keyCode);
                        _activeKeyPresses.TryRemove(zwiftFunction, out _);
                    }
                    catch (TaskCanceledException)
                    {
                        // Timeout reset; key release will be handled later
                    }
                }, tokenSource.Token);

                return new ActionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to keyboard.");
                return new ActionResult { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                //_lock.Release();
            }
        }

        private VirtualKeyCode GetKeyCode(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView, ZwiftRiderAction riderAction)
        {
            return zwiftFunction switch
            {
                ZwiftFunction.ShowMenu => VirtualKeyCode.UP,
                ZwiftFunction.NavigateLeft => VirtualKeyCode.LEFT,
                ZwiftFunction.NavigateRight => VirtualKeyCode.RIGHT,
                ZwiftFunction.Uturn => VirtualKeyCode.DOWN,
                ZwiftFunction.Powerup => VirtualKeyCode.SPACE,
                ZwiftFunction.Select => VirtualKeyCode.RETURN,
                ZwiftFunction.GoBack => VirtualKeyCode.ESCAPE,
                ZwiftFunction.ShowPairedDevices => VirtualKeyCode.VK_A,
                ZwiftFunction.ShowGarage => VirtualKeyCode.VK_T,
                ZwiftFunction.ToggleGraphs => VirtualKeyCode.VK_G,
                ZwiftFunction.SendGroupText => VirtualKeyCode.VK_M,
                ZwiftFunction.HideHUD => VirtualKeyCode.VK_H,
                ZwiftFunction.PromoCode => VirtualKeyCode.VK_P,
                ZwiftFunction.ShowTrainingMenu => VirtualKeyCode.VK_E,
                ZwiftFunction.SkipWorkoutBlock => VirtualKeyCode.TAB,
                ZwiftFunction.FTPBiasUp => VirtualKeyCode.PRIOR,
                ZwiftFunction.FTPBiasDown => VirtualKeyCode.NEXT,
                ZwiftFunction.AdjustCameraAngle => playerView switch
                {
                    ZwiftPlayerView.Default => VirtualKeyCode.VK_1,
                    ZwiftPlayerView.ThirdPerson => VirtualKeyCode.VK_2,
                    ZwiftPlayerView.FPS => VirtualKeyCode.VK_3,
                    ZwiftPlayerView.FrontLeftSide => VirtualKeyCode.VK_4,
                    ZwiftPlayerView.RearRightSide => VirtualKeyCode.VK_5,
                    ZwiftPlayerView.FacingRider => VirtualKeyCode.VK_6,
                    ZwiftPlayerView.Spectator => VirtualKeyCode.VK_7,
                    ZwiftPlayerView.Helicopter => VirtualKeyCode.VK_8,
                    ZwiftPlayerView.BirdsEye => VirtualKeyCode.VK_9,
                    ZwiftPlayerView.Drone => VirtualKeyCode.VK_0,
                    _ => default
                },
                ZwiftFunction.RiderAction => riderAction switch
                {
                    ZwiftRiderAction.Elbow => VirtualKeyCode.F1,
                    ZwiftRiderAction.WaveHand => VirtualKeyCode.F2,
                    ZwiftRiderAction.RideOn => VirtualKeyCode.F3,
                    ZwiftRiderAction.HammerTime => VirtualKeyCode.F4,
                    ZwiftRiderAction.Nice => VirtualKeyCode.F5,
                    ZwiftRiderAction.BringIt => VirtualKeyCode.F6,
                    ZwiftRiderAction.ImToast => VirtualKeyCode.F7,
                    ZwiftRiderAction.BikeBell => VirtualKeyCode.F8,
                    ZwiftRiderAction.ScreenShot => VirtualKeyCode.F10,
                    _ => default
                },
                _ => default
            };
        }

        #endregion
    }
}
