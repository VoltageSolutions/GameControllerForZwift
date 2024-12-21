using GameControllerForZwift.Core;
using InputSimulatorEx;
using InputSimulatorEx.Native;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameControllerForZwift.Keyboard
{
    public class KeyboardService : IOutputService
    {
        #region Fields
        private readonly InputSimulator _simulator = new InputSimulator();
        private readonly ILogger<KeyboardService> _logger;
        private readonly ConcurrentDictionary<ZwiftFunction, DateTime> _lastExecutionTimes = new ConcurrentDictionary<ZwiftFunction, DateTime>();
        private readonly TimeSpan _actionTimeout = TimeSpan.FromMilliseconds(250);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1); // To handle async thread safety
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
            // Check if the action should be skipped due to timeout
            if (IsActionWithinTimeout(zwiftFunction))
            {
                return new ActionResult { Success = false, ErrorMessage = "Action ignored due to recent execution." };
            }

            await _lock.WaitAsync();
            try
            {
                UpdateLastExecutionTime(zwiftFunction);

                switch (zwiftFunction)
                {
                    case ZwiftFunction.ShowMenu:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.UP);
                        break;

                    case ZwiftFunction.NavigateLeft:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.LEFT);
                        break;

                    case ZwiftFunction.NavigateRight:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                        break;

                    case ZwiftFunction.Uturn:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                        break;

                    case ZwiftFunction.Powerup:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                        break;

                    case ZwiftFunction.Select:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                        break;

                    case ZwiftFunction.GoBack:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                        break;

                    case ZwiftFunction.ShowPairedDevices:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_A);
                        break;

                    case ZwiftFunction.ShowGarage:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_T);
                        break;

                    case ZwiftFunction.ToggleGraphs:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_G);
                        break;

                    case ZwiftFunction.SendGroupText:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_M);
                        break;

                    case ZwiftFunction.HideHUD:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_H);
                        break;

                    case ZwiftFunction.PromoCode:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_P);
                        break;

                    case ZwiftFunction.ShowTrainingMenu:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_E);
                        break;

                    case ZwiftFunction.SkipWorkoutBlock:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                        break;

                    case ZwiftFunction.FTPBiasUp:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.PRIOR); // Page Up
                        break;

                    case ZwiftFunction.FTPBiasDown:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.NEXT); // Page Down
                        break;

                    case ZwiftFunction.AdjustCameraAngle:
                        switch (playerView)
                        {
                            case ZwiftPlayerView.Default:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_1);
                                break;

                            case ZwiftPlayerView.ThirdPerson:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_2);
                                break;

                            case ZwiftPlayerView.FPS:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                                break;

                            case ZwiftPlayerView.FrontLeftSide:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
                                break;

                            case ZwiftPlayerView.RearRightSide:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_5);
                                break;

                            case ZwiftPlayerView.FacingRider:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_6);
                                break;

                            case ZwiftPlayerView.Spectator:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_7);
                                break;

                            case ZwiftPlayerView.Helicopter:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_8);
                                break;

                            case ZwiftPlayerView.BirdsEye:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_9);
                                break;

                            case ZwiftPlayerView.Drone:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_0);
                                break;

                            default:
                                return new ActionResult { Success = false, ErrorMessage = "Unhandled ZwiftPlayerView selection." };
                        }
                        break;

                    case ZwiftFunction.RiderAction:
                        switch (riderAction)
                        {
                            case ZwiftRiderAction.Elbow:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F1);
                                break;

                            case ZwiftRiderAction.WaveHand:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F2);
                                break;

                            case ZwiftRiderAction.RideOn:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F3);
                                break;

                            case ZwiftRiderAction.HammerTime:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F4);
                                break;

                            case ZwiftRiderAction.Nice:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F5);
                                break;

                            case ZwiftRiderAction.BringIt:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F6);
                                break;

                            case ZwiftRiderAction.ImToast:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F7);
                                break;

                            case ZwiftRiderAction.BikeBell:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F8);
                                break;

                            case ZwiftRiderAction.ScreenShot:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F10);
                                break;

                            default:
                                return new ActionResult { Success = false, ErrorMessage = "Unhandled ZwiftRiderAction selection." };
                        }
                        break;

                    default:
                        return new ActionResult { Success = false, ErrorMessage = "Unhandled Zwift Function selection." };
                }

                //await Task.Delay(100); // Short delay to simulate key press duration if needed
                return new ActionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to keyboard.");
                return new ActionResult { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Checks if the action was performed recently within the timeout window.
        /// </summary>
        private bool IsActionWithinTimeout(ZwiftFunction zwiftFunction)
        {
            if (_lastExecutionTimes.TryGetValue(zwiftFunction, out var lastTime))
            {
                if (DateTime.UtcNow - lastTime < _actionTimeout)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the last execution time for the given function.
        /// </summary>
        private void UpdateLastExecutionTime(ZwiftFunction zwiftFunction)
        {
            _lastExecutionTimes[zwiftFunction] = DateTime.UtcNow;
        }

        #endregion
    }
}
