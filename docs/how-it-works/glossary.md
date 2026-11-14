# Glossary

## Bluetooth LE and GATT

### GATT

The Generic Attribute Profile. It is how Bluetooth Low Energy devices organise their data: a device offers **services**, and each service holds **characteristics**. Clients discover them, read and write them, and subscribe to changes.

### Service

A group of related characteristics with a UUID. For example `0x180A` is the standard Device Information service, and `0xFC82` is Zwift's controls service.

### Characteristic

A single value inside a service, such as a manufacturer name or a stream of button frames. Each characteristic has a UUID and **properties** that say what a client may do with it: read, write, notify or indicate.

### UUID

A 128-bit identifier, written like `00000002-19CA-4651-86E5-FA29DCDD09D1`. Standard Bluetooth UUIDs have a short 16-bit form, such as `0x1826`, which expands to `00001826-0000-1000-8000-00805F9B34FB`. On the Direct Connect wire, every UUID is sent as 16 bytes in written order.

### Notification and subscription

A **notification** is a value the device pushes to the client without being asked. The client first **subscribes** (enables notifications) on a characteristic. An **indication** is the same idea with an acknowledgement from the client. Direct Connect has a single notification message for both.

## Networking

### mDNS

Multicast DNS. Devices send DNS questions and answers to the multicast address `224.0.0.251` on UDP port 5353, so names and services can be found on a local network without a DNS server. See [mDNS discovery](mdns.md).

### TXT record

A DNS record that holds a list of `key=value` text strings. In service discovery it carries extra details about a service, such as the `ble-service-uuids` list.

### DIRCON

Short for Wahoo **Direct Connect**. It carries Bluetooth GATT operations over a TCP connection so that a trainer app can use a network device as if it were a Bluetooth one. See [Wahoo Direct Connect](direct-connect.md).

## Encoding

### Protobuf

Protocol Buffers, a compact binary format from Google. A message is a list of fields. Each field starts with a **tag** byte that combines the field number and a wire type: `(field_number << 3) | wire_type`. Wire type 0 is a varint and wire type 2 is a length-prefixed block. For example `08` means "field 1, varint" and `1A` means "field 3, length-prefixed".

### Varint

A variable-length integer. Each byte holds 7 bits of the number, lowest bits first. The top bit of each byte is 1 if more bytes follow. For example 300 is `AC 02`, and 1024 is `80 08`.

### Zigzag encoding

A way to store signed numbers in a varint so that small negative numbers stay small. It maps 0, -1, 1, -2, 2 to 0, 1, 2, 3, 4. The formula is `(n << 1) ^ (n >> 31)` for a 32-bit value. For example -50 becomes 99 (`63`), 100 becomes 200 (`C8 01`) and -100 becomes 199 (`C7 01`).

### Little-endian and big-endian

The order of bytes in a number. **Big-endian** puts the most significant byte first (the Direct Connect length field). **Little-endian** puts the least significant byte first (Zwift's company id `0x094A` is written `4A 09`).
