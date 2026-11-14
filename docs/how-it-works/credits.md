# Credits

This project stands on the shoulders of people who shared what they learned. Thank you.

- **Makinolo**, [Zwift Ride protocol write-up](https://www.makinolo.com/blog/2024/07/26/zwift-ride-protocol/). This careful look at the Zwift Ride's Bluetooth messages helped us understand the keypad frame, the button bitmap and the analog paddles.
- **BikeControl** by Jonas Bark, [github.com/OpenBikeControl/bikecontrol](https://github.com/OpenBikeControl/bikecontrol) (formerly SwiftControl). Its open code showed us the Zwift Ride button masks, the 5 second keepalive and a presentation that Zwift pairs as a Zwift Ride over the network.
- **OpenBikeControl protocol**, [github.com/OpenBikeControl/openbikecontrol-protocol](https://github.com/OpenBikeControl/openbikecontrol-protocol). This open specification for bike controllers helped us understand how controller services are announced on the network, and its service UUID is part of our advertisement.
- **qdomyos-zwift** by Roberto Viola (cagnulein), [github.com/cagnulein/qdomyos-zwift](https://github.com/cagnulein/qdomyos-zwift). Its open Direct Connect server confirmed the 6-byte header, the message payloads, the response codes and the sequence number rules.
- **Wahoo Direct Connect**, the protocol by Wahoo Fitness that carries Bluetooth GATT over a network connection. It is the foundation that lets a Windows app look like a native device to Zwift.
- **MattW2**, who shared a [Wireshark capture](https://github.com/MattW2/KickrShiftKey/tree/main/KickrBikeWireShark) of a Wahoo KICKR BIKE SHIFT talking to Zwift. It was an early window into how Zwift talks to a smart bike.

Game Controller for Zwift is not affiliated with Zwift, Wahoo Fitness or any of the projects above. Zwift, Zwift Ride, Zwift Click, Zwift Play, Wahoo and KICKR are trademarks of their respective owners.
