# Button reference

This table lists every Zwift Ride input, its bit in the keypad bitmap and the app function that uses it. The labels are Zwift's own.

Remember that the bitmap on the wire is **inverted**: to press a button, clear its bit. See [Keypad frames](zwift-protocol.md#keypad-frames).

| Zwift label | Bit mask | Bit index | App function | Notes |
|---|---|---|---|---|
| Left | `0x0001` | 0 | Navigate Left | D-pad left. |
| Up | `0x0002` | 1 | Navigate Up | D-pad up. Opens the action bar in a ride. |
| Right | `0x0004` | 2 | Navigate Right | D-pad right. |
| Down | `0x0008` | 3 | Navigate Down | D-pad down. U-turn in a ride. |
| A | `0x0010` | 4 | Select (A) | |
| B | `0x0020` | 5 | Back / Menu (B) | |
| Y | `0x0040` | 6 | Power-up (Y) | Hold to skip the current workout block. |
| Z | `0x0080` | 7 | Ride On (Z) | Hold to send Ride Ons to nearby riders. |
| LS1 | `0x0100` | 8 | Shift down | Left shifter 1. Easier gear in Sequential shift style. |
| LS2 | `0x0200` | 9 | Not used | Left shifter 2. Also an easier gear. |
| LB | `0x0400` | 10 | Not used | Left side button. |
| LPower | `0x0800` | 11 | Not used | Left power button. |
| RS1 | `0x1000` | 12 | Shift up | Right shifter 1. Harder gear in Sequential shift style. |
| RS2 | `0x2000` | 13 | Not used | Right shifter 2. Also a harder gear. |
| RB | `0x4000` | 14 | Not used | Right side button. |
| RPower | `0x8000` | 15 | Not used | Right power button. |
| ZL | Analog, location 0 | | Steer left, Brake | Tilt outward (negative) steers left. Squeeze inward (positive) brakes. |
| ZR | Analog, location 1 | | Steer right, Brake | Tilt outward (positive) steers right. Squeeze inward (negative) brakes. |

The single place in the code that maps app functions to these buttons is `ZwiftRideOutputService.ButtonMap`. The masks themselves are defined in `ZwiftRideButton`.

## Quick encoder

To build field 1 of a keypad frame:

1. OR together the masks of every pressed button.
2. Invert all 32 bits.
3. Write the result as a protobuf varint after the tag byte `08`.

| Pressed | Mask | Inverted | Varint bytes |
|---|---|---|---|
| Nothing | `0x00000000` | `0xFFFFFFFF` | `FF FF FF FF 0F` |
| A | `0x00000010` | `0xFFFFFFEF` | `EF FF FF FF 0F` |
| LS1 | `0x00000100` | `0xFFFFFEFF` | `FF FD FF FF 0F` |
| RS1 | `0x00001000` | `0xFFFFEFFF` | `FF DF FF FF 0F` |
| A and RS1 | `0x00001010` | `0xFFFFEFEF` | `EF DF FF FF 0F` |

Several buttons can be pressed in one frame.
