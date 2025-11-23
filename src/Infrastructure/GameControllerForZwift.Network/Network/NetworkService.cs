using GameControllerForZwift.Core;
using GameControllerForZwift.Network.Network;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameControllerForZwift.Network
{
    public class NetworkService : IOutputService
    {
        #region Fields
        private readonly ILogger<NetworkService> _logger;

        private readonly MulticastDNSHelper _mdnsHelper;
        private readonly TCPMessagingHelper _tcpHelper;
        #endregion

        #region Constructor

        public NetworkService(ILogger<NetworkService> logger)
        {
            System.Diagnostics.Debug.WriteLine("NetworkService instantiated.");
            _logger = logger;
            _mdnsHelper = new MulticastDNSHelper();
            _tcpHelper = new TCPMessagingHelper();

            var tokenSource = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await _tcpHelper.AcceptTCPConnectionLoopAsync(tokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // Timeout reset; key release will be handled later
                }
            }, tokenSource.Token);
        }

        #endregion

        #region Methods
        public async Task<ActionResult> PerformActionAsync(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView = ZwiftPlayerView.Default, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
        {
            try
            {
                // Map the zwiftFunction to a mask and send it over the network
                if (ZwiftButtonMaskMap.TryGetValue(zwiftFunction, out var mask))
                {
                    System.Diagnostics.Debug.WriteLine("Attempting to write: " + zwiftFunction.ToString() + " with mask " + mask.ToString());

                    var sendResult = await _tcpHelper.SendActionAsync(mask);
                    // Optional: log or inspect sendResult for errors
                    _logger.LogDebug("SendActionAsync result: {Result}", sendResult);
                }
                else
                {
                    // No mapping - fallback or log
                    _logger.LogInformation("No network button mapping for function: {Function}", zwiftFunction);
                }

                var tokenSource = new CancellationTokenSource();
                
                return new ActionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to network.");
                return new ActionResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public IReadOnlyDictionary<ZwiftFunction, uint> ZwiftButtonMaskMap = new Dictionary<ZwiftFunction, uint>
        {
            [ZwiftFunction.NavigateLeft] = 0x00001u,   // LEFT_BTN
            [ZwiftFunction.ShowMenu] = 0x00002u,       // UP_BTN (toggle UI)
            [ZwiftFunction.NavigateRight] = 0x00004u,  // RIGHT_BTN
            [ZwiftFunction.Uturn] = 0x00008u,          // DOWN_BTN
                                                       // Common action -> controller buttons:
                                                       // Map meaningful ZwiftFunction enum values to the controller masks you need:
                                                       // Example mappings:
            [ZwiftFunction.Select] = 0x00010u,         // A_BTN
            [ZwiftFunction.GoBack] = 0x00020u,         // B_BTN
                                                       // Add more as required:
            [ZwiftFunction.Powerup] = 0x00040u,         // Y_BTN
            [ZwiftFunction.RiderAction] = 0x00100u,         // Z_BTN
                                                            // Powerup -> Y, RideOn -> Z etc.
                                                            // (Adjust mapping to your app's semantics)
                                                            //[ZwiftFunction.Powerup] = 0x00040u,      // Y_BTN
                                                            //[ZwiftFunction.RiderAction] = 0x00080u,  // Z_BTN

            /*
             * SHFT_UP_L_BTN = 0x200;
    SHFT_DN_L_BTN = 0x400;
    POWERUP_L_BTN = 0x800;
    ONOFF_L_BTN = 0x1000;
    SHFT_UP_R_BTN = 0x2000;
    SHFT_DN_R_BTN = 0x4000;
    POWERUP_R_BTN = 0x10000;
    ONOFF_R_BTN = 0x20000;
             */
        };

        #endregion
    }
}
