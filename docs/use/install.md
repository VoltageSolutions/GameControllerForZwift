# Install and first run

This page gets the app running on your Windows computer. It takes about five minutes.

## 1. Download the app

Go to the [Releases page on GitHub](https://github.com/VoltageSolutions/GameControllerForZwift/releases) and open the newest release. You will see two downloads. Pick one:

| Download | Size | What you need |
|---|---|---|
| `GameControllerForZwift-<version>-win-x64.zip` | Small | The .NET 10 Desktop Runtime. If it is missing, Windows tells you when you start the app and offers the download. |
| `GameControllerForZwift-<version>-win-x64-standalone.zip` | Larger | Nothing else. Everything is inside. |

!!! tip "Not sure which one?"
    Take the **standalone** download. It is bigger, but it works without installing anything else.

Each release also has a `SHA256SUMS.txt` file. You can use it to check that your download is complete and unchanged.

## 2. Unpack and start

1. Right-click the zip file and choose **Extract All**.
2. Open the extracted folder and run `GameControllerForZwift.exe`.

### If Windows SmartScreen stops the app

The app is not digitally signed yet, so Microsoft Defender SmartScreen may show a warning the first time. Click **More info**, then **Run anyway**. The app starts.

### Allow the app through Windows Firewall

The first time the app runs, Windows Firewall asks whether the app may use the network. Allow it on **private networks** (your home network). Zwift finds the app over your local network, so this step matters.

!!! note "Why the app needs the network"
    Zwift discovers controllers on your network and then talks to them over a normal network connection. The app listens on TCP port 36866 (or the next free port up to 36876) and announces itself with mDNS on UDP port 5353. You do not need to open anything on your router.

## 3. Connect your game controller

1. Connect your controller to Windows with a USB cable or Bluetooth.
2. In the app, choose your controller from the list. If you do not see it, click **Refresh List**.
3. Press some buttons. The matching row in the app lights up while you press it.

The app uses one controller at a time. A pair of Joy-Cons counts as one controller.

## Next step

Your app is ready. Now [pair it with Zwift](pair-with-zwift.md).
