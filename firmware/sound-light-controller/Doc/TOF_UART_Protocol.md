# NLink TOFSense UART integration

## Wiring

| TOF sensor | MSPM0G3507 |
| --- | --- |
| TX | PA9 / UART1 RX |
| RX | PA17 / UART1 TX |
| GND | GND |
| VCC | Use the voltage specified by the sensor module |

PA17 and PA9 are dedicated to the TOF UART in this build. The previous
HC-SR04 GPIO implementation has been removed. Bluetooth remains on PA10/PA11.

## UART configuration

- 921600 baud
- 8 data bits
- 1 stop bit
- no parity
- no flow control

The settings and frame decoder were ported from the drone project's UART0
implementation in `WP_Src/Usart.c` and `WP_Src/TOFSense.c`.

## 16-byte measurement frame

| Offset | Meaning |
| --- | --- |
| 0..2 | Header: `57 00 FF` |
| 3 | Sensor ID |
| 4..7 | Sensor timestamp, little-endian |
| 8..10 | Signed 24-bit distance, little-endian, millimetres |
| 11 | Distance status |
| 12..13 | Signal strength, little-endian |
| 14 | Reserved |
| 15 | Unsigned 8-bit sum of bytes 0..14 |

The car sends `57 10 FF FF 00 FF FF 63` every 100 ms to request the latest
frame from sensor ID 0. Spontaneously streamed frames are accepted as well.

## Public data

`BSP/bsp_tof.h` exports:

- `g_tofDistanceMm`
- `g_tofSignalStrength`
- `g_tofSensorTimestamp`
- `g_tofDistanceStatus`
- `g_tofSensorId`
- `g_tofDistanceValid`
- `TOF_GetDistanceMm()`
- `TOF_GetDistanceCm()`
- `TOF_IsDistanceValid()`

The OLED verification output is shown on row 7. A valid sample is displayed
as `TOF:1234mm  567`; a timeout or invalid sample is displayed as
`TOF:----mm`.
