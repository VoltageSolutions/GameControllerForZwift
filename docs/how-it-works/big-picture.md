# The big picture

!!! tldr "TL;DR"
    The app pretends to be a Zwift Ride controller on your local network. It announces itself with mDNS, accepts a TCP connection from Zwift, and speaks Wahoo Direct Connect, which carries Bluetooth GATT operations over TCP. After a short "RideOn" handshake and a few device information requests, the app sends button and paddle frames whenever your controller changes, plus a keepalive every 5 seconds.

This section is for anyone who wants to understand, debug or reimplement the link between a controller and Zwift. Nothing here is specific to C#. If you can open a TCP socket and send a multicast UDP packet, you can build the same thing in any language.

## Three layers

| Layer | What it does | Page |
|---|---|---|
| mDNS | Lets Zwift find the device on the local network. | [mDNS discovery](mdns.md) |
| Wahoo Direct Connect | Carries Bluetooth GATT operations (discover, read, write, subscribe, notify) over a TCP connection. | [Direct Connect](direct-connect.md) |
| Zwift controller protocol | The bytes Zwift and a Zwift Ride exchange on Zwift's own GATT characteristics: handshake, device information, button frames. | [Zwift controller protocol](zwift-protocol.md) |

## A pairing session from start to finish

```mermaid
sequenceDiagram
    autonumber
    participant Z as Zwift
    participant A as App (Voltage Gamepad)

    Note over A: Starts TCP server on port 36866<br/>and advertises it with mDNS
    Z->>A: mDNS query for _wahoo-fitness-tnp._tcp
    A-->>Z: mDNS answer: name, port, TXT, IPv4 address
    Note over Z: Rider opens the pairing screen<br/>and picks Voltage Gamepad
    Z->>A: TCP connect
    Z->>A: Discover services (0x01)
    A-->>Z: List of service UUIDs
    Z->>A: Discover characteristics (0x02), per service
    A-->>Z: Characteristic UUIDs and properties
    Z->>A: Enable notifications (0x05) on 0002 and 0004
    A-->>Z: Subscribed
    Z->>A: Write "RideOn" to 0003
    A-->>Z: Write acknowledged
    A-->>Z: Notify on 0004: "RideOn" 02 00
    A-->>Z: Notify on 0002: status "RIDE_ON(0)"
    Z->>A: Device information requests on 0003 (opcode 0x00)
    A-->>Z: Replies on 0004 (opcode 0x3C): device info, battery, input config
    loop While connected
        A-->>Z: Button and paddle frame on 0002 (opcode 0x23) when input changes
        A-->>Z: Same frame again every 5 s (keepalive)
    end
```

Zwift does not wait for each answer before sending the next request. It sends discovery, subscription and the RideOn write in one quick burst. The [Direct Connect page](direct-connect.md#pipelining-and-queued-notifications) explains how the app copes with that.

## Why present as a Zwift Ride?

Zwift already knows how to use a Zwift Ride. It has a D-pad, A, B, Y and Z buttons, two shifters on each side and two analog paddles (ZL and ZR) that steer when tilted outward and brake when squeezed inward. That covers everything a rider needs from a game controller: menus, shifting, steering and braking.

Presenting as a Zwift Ride has two big advantages:

- **It sits next to your trainer.** A Zwift Ride is a pure controller. Zwift pairs it under **Controls** while your trainer stays the Power Source and Resistance device, and Zwift's virtual shifting then drives your trainer.
- **No new protocol.** The app uses formats Zwift already understands, so it works on every platform Zwift runs on, including tablets and Apple TV, with no plugin or companion app.

## What the app actually sends

```mermaid
flowchart LR
    pad["Gamepad state<br/>every 25 ms"] --> map["Your mapping"]
    map --> state["Pressed functions,<br/>steer -1..1, brake 0..1"]
    state --> frame["Zwift Ride frame:<br/>button bitmap +<br/>ZL and ZR paddles"]
    frame --> zwift["Zwift"]
```

The app reads the controller every 25 ms. When the resulting frame changes, it sends it at once. While anything is pressed it repeats the frame every 100 ms, and when you let go it sends one neutral frame. On top of that, the current frame goes out every 5 seconds as a keepalive. Details are on the [Zwift controller protocol page](zwift-protocol.md#timing).
