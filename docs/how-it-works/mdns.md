# mDNS discovery

!!! tldr "TL;DR"
    Zwift finds network devices with mDNS (multicast DNS), the same "zero configuration" discovery that printers and smart speakers use. The app advertises the service type `_wahoo-fitness-tnp._tcp` with a PTR, SRV, TXT and A record set that describes a Wahoo-style device. The TXT record carries the Bluetooth service list, a MAC-style address, a serial number and Zwift manufacturer data. When the app exits, it sends "goodbye" records so Zwift drops it straight away.

## mDNS in two minutes

mDNS works like normal DNS, but without a server. Devices send DNS questions and answers as UDP packets to the multicast address `224.0.0.251` on port 5353. Every device on the local network segment hears them.

Service discovery (DNS-SD) adds a simple naming scheme on top:

| Record | Example name | What it says |
|---|---|---|
| PTR | `_wahoo-fitness-tnp._tcp.local` | "Here is an instance of this service type." Points to the instance name. |
| SRV | `Voltage Gamepad._wahoo-fitness-tnp._tcp.local` | The host name and TCP port of the instance. |
| TXT | `Voltage Gamepad._wahoo-fitness-tnp._tcp.local` | A list of `key=value` strings with extra details. |
| A | `Voltage-Gamepad.local` | The IPv4 address of the host. |

A client such as Zwift asks for the PTR records of a service type, then follows the SRV, TXT and A records to learn where to connect.

!!! note "mDNS stays on your local network"
    Routers do not forward mDNS between networks. That is why Zwift and the app must be on the same local network.

## What the app advertises

| Item | Value | Notes |
|---|---|---|
| Service type | `_wahoo-fitness-tnp._tcp` | The service type used by Wahoo Direct Connect devices such as the KICKR family. |
| Instance name | `Voltage Gamepad` | Shown in Zwift's pairing screen. Dots are replaced by spaces, because the name must be one DNS label. |
| Host name | `Voltage-Gamepad.local` | The instance name with spaces turned into hyphens. |
| Port (SRV) | `36866` | The port the TCP server actually bound. If 36866 is busy, the server tries up to 36876, and the advertisement always carries the real port. |
| TXT time to live | 4500 s | |
| A record time to live | 120 s | |

### TXT record fields

| Key | Value sent by the app | What it means |
|---|---|---|
| `ble-service-uuids` | `0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B,d273f680-d548-419d-b9d1-fa0472345229` | The Bluetooth services the device offers. This is the list a KICKR-style device announces (Zwift's FC82 service, Cycling Power 1818, Fitness Machine 1826, Zwift's 128-bit service and a Wahoo service), followed by the OpenBikeControl service UUID. |
| `mac-address` | `02-47-5A-00-00-01` | A MAC-style identifier. The first byte `02` marks it as a locally administered address, so it can never clash with real hardware. |
| `serial-number` | `612034871` | Digits only. Some apps parse it as a 64-bit number, so letters would break them. |
| `manufacturer-data` | `4A09` + type byte + two id bytes | ASCII hex text, the same layout as Bluetooth manufacturer data. See below. |

### Manufacturer data

The `manufacturer-data` value is written as hex text, not raw bytes. It follows the layout Zwift's controllers use in their Bluetooth advertisements:

| Bytes | Example | Meaning |
|---|---|---|
| 0-1 | `4A 09` | Zwift's Bluetooth company identifier, 0x094A, in little-endian order. |
| 2 | `06` | Device type byte. |
| 3-4 | `AA BB` | Two identifier bytes. |

```text
TXT  manufacturer-data=4A0906AABB
                       ^^^^        Zwift company id 0x094A (little endian)
                           ^^      device type
                             ^^^^  identifier bytes (synthetic example)
```

The app sends the exact value defined in `ZwiftRideProfile.ManufacturerData`. The example above uses made-up identifier bytes.

For reference, the type bytes known from Zwift's Bluetooth controllers are: Play right `0x02`, Play left `0x03`, Ride right `0x07`, Ride left `0x08`, Click `0x09`.

## What Zwift pays attention to

From live testing:

- **The service list matters.** Zwift lists the device when `ble-service-uuids` carries the KICKR-style list above. Adverts that only listed the Fitness Machine service, or only FC82, did not appear in the pairing screen.
- **Manufacturer data is part of the identity.** The Zwift company id plus a device type byte is part of the advertisement that Zwift pairs as a Zwift Ride.
- **The address must be reachable.** Zwift connects to the A record address and the SRV port, so both must be right.

## Choosing which address to advertise

A Windows PC often has several network adapters: Wi-Fi, Ethernet, VPNs, Hyper-V, WSL, Docker and more. The app advertises only addresses Zwift can likely reach:

1. Take every IPv4 address on adapters that are up.
2. Skip loopback and tunnel adapters, and link-local addresses (`169.254.x.x`).
3. Mark adapters whose name or description looks virtual (for example "virtual", "vmware", "hyper-v", "vethernet", "wsl", "docker", "vpn", "tailscale", "bluetooth").
4. Prefer real adapters that have a default gateway. If there are none, fall back to real adapters without a gateway, then to anything with a gateway, then to anything at all.

!!! tip "Advertise your own address"
    Each A record must hold the address of the machine running the service. An early prototype answered with the address of the device that asked, which pointed Zwift back at itself.

## Saying goodbye

When the app stops, it sends every record again (PTR, SRV, TXT and A) with a time to live of 0. In mDNS this is a "goodbye" packet. Zwift removes the device at once instead of waiting for the old records to expire.

## Example: what Zwift sees

All addresses below are documentation examples.

```text
_wahoo-fitness-tnp._tcp.local.                  PTR  Voltage Gamepad._wahoo-fitness-tnp._tcp.local.
Voltage Gamepad._wahoo-fitness-tnp._tcp.local.  SRV  0 0 36866 Voltage-Gamepad.local.
Voltage Gamepad._wahoo-fitness-tnp._tcp.local.  TXT  "ble-service-uuids=0xFC82,0x1818,..."
                                                     "mac-address=02-47-5A-00-00-01"
                                                     "serial-number=612034871"
                                                     "manufacturer-data=4A0906AABB"
Voltage-Gamepad.local.                          A    192.0.2.10
```
