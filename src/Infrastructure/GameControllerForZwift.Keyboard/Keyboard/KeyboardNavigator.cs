using GameControllerForZwift.Core;
using InputSimulatorEx;
using InputSimulatorEx.Native;

namespace GameControllerForZwift.Keyboard
{
    public class KeyboardNavigator : IUINavigator
    {
        private readonly InputSimulator _simulator = new InputSimulator();

        // todo - accept a logger interface
        public KeyboardNavigator()
        {

        }

        public ActionResult PerformAction(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView = ZwiftPlayerView.Default, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
        {
            try
            {
                switch (zwiftFunction)
                {
                    case ZwiftFunction.ShowMenu:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.UP); // Show actions/options menu (use left/right arrows to select)
                        break;

                    case ZwiftFunction.NavigateLeft:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.LEFT); // Left arrow to navigate left
                        break;

                    case ZwiftFunction.NavigateRight:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.RIGHT); // Right arrow to navigate right
                        break;

                    case ZwiftFunction.Uturn:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.DOWN); // Down arrow to perform a U-Turn
                        break;

                    case ZwiftFunction.Powerup:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.SPACE); // Use power-up
                        break;

                    case ZwiftFunction.Select:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN); // Select
                        break;

                    case ZwiftFunction.GoBack:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE); // Go back
                        break;

                    case ZwiftFunction.SkipWorkoutBlock:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.TAB); // Skip workout block
                        break;

                    case ZwiftFunction.FTPBiasUp:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.PRIOR); // Increase FTP Bias (adjust workout intensity)
                        break;

                    case ZwiftFunction.FTPBiasDown:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.NEXT); // Decrease FTP Bias (adjust workout intensity)
                        break;

                    case ZwiftFunction.HideHUD:
                        _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_H); // Show or Hide the HUD
                        break;

                    case ZwiftFunction.AdjustCameraAngle:
                        switch (playerView)
                        {
                            case ZwiftPlayerView.Default:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_1); // Default 6 o’clock view
                                break;
                            case ZwiftPlayerView.ThirdPerson:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_2); // Third person view
                                break;
                            case ZwiftPlayerView.FPS:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3); // FPS view
                                break;
                            case ZwiftPlayerView.FrontLeftSide:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4); // Front-left side of rider
                                break;
                            case ZwiftPlayerView.RearRightSide:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_5); // Rear-right side of rider
                                break;
                            case ZwiftPlayerView.FacingRider:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_6); // Facing rider
                                break;
                            case ZwiftPlayerView.Spectator:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_7); // Spectator view
                                break;
                            case ZwiftPlayerView.Helicopter:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_8); // Helicopter view
                                break;
                            case ZwiftPlayerView.BirdsEye:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_9); // Bird’s eye view
                                break;
                            case ZwiftPlayerView.Drone:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.VK_0); // Drone view
                                break;
                            default:
                                return new ActionResult { Success = false, ErrorMessage = "Unhandled Camera Angle selection." };
                        }
                        break;

                    case ZwiftFunction.RiderAction:
                        switch (riderAction)
                        {
                            case ZwiftRiderAction.Elbow:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F1); // Stick out elbow
                                break;
                            case ZwiftRiderAction.WaveHand:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F2); // Wave hand
                                break;
                            case ZwiftRiderAction.RideOn:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F3); // Ride On!
                                break;
                            case ZwiftRiderAction.HammerTime:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F4); // Hammer Time!
                                break;
                            case ZwiftRiderAction.Nice:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F5); // Nice!
                                break;
                            case ZwiftRiderAction.BringIt:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F6); // Bring It!
                                break;
                            case ZwiftRiderAction.ImToast:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F7); // I’m toast
                                break;
                            case ZwiftRiderAction.BikeBell:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F8); // Bike bell
                                break;
                            case ZwiftRiderAction.ScreenShot:
                                _simulator.Keyboard.KeyPress(VirtualKeyCode.F10); // Take screenshot
                                break;
                            default:
                                return new ActionResult { Success = false, ErrorMessage = "Unhandled Rider Action selection." };
                        }
                        break;
                    default:
                        return new ActionResult { Success = false, ErrorMessage = "Unhandled Zwift Function selection." };
                }

                return new ActionResult { Success = true };
            }
            catch (Exception ex)
            {
                // log a message here!
                return new ActionResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
