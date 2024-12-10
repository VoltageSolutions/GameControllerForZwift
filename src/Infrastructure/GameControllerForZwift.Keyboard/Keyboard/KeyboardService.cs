using GameControllerForZwift.Core;
using InputSimulatorEx;
using InputSimulatorEx.Native;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GameControllerForZwift.Keyboard
{
    public class KeyboardService : IOutputService
    {
        #region Fields
        private readonly InputSimulator _simulator = new InputSimulator();
        private readonly ILogger<KeyboardService> _logger;
        private readonly ConcurrentDictionary<ZwiftFunction, DateTime> _lastExecutionTimes = new ConcurrentDictionary<ZwiftFunction, DateTime>();
        private readonly TimeSpan _actionTimeout = TimeSpan.FromMilliseconds(250); // 500 ms timeout window
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1); // To handle async thread safety
        #endregion

        #region Constructor

        public KeyboardService(ILogger<KeyboardService> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Methods

        //ActionResult IOutputService.PerformAction(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView, ZwiftRiderAction riderAction)
        //{
        //    throw new NotImplementedException();
        //}

        // rename this to be async
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

                    // Add other ZwiftFunction cases as needed...

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
