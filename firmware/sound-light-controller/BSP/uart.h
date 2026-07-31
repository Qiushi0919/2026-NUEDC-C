#ifndef _UART_H
#define _UART_H
#include "board.h"
#include <stdio.h>      // 添加标准IO头文件
#include <stdarg.h>     // 解决 va_list 问题
#include <string.h>     // 解决 strlen/memmove/memcpy 问题


#define BUFF_LEN_1K	1024

/*
 * UART0 on PA10 (TX) / PA11 (RX) is dedicated to the HC-05 link.
 * Disable legacy printf forwarding so debug text cannot pollute protocol frames.
 */
#define UART0_BLUETOOTH_MODE                1

#define BLUETOOTH_FRAME_HEAD                0xFFU
#define BLUETOOTH_FRAME_TYPE_DISPLAY        0x2FU
#define BLUETOOTH_FRAME_TAIL                0xCCU
#define BLUETOOTH_MAX_PAYLOAD_LEN           16U
#define BLUETOOTH_OLED_LINE                 3U

#define DIGITAL_KEY_FRAME_HEAD_0            0xAAU
#define DIGITAL_KEY_FRAME_HEAD_1            0x55U
#define DIGITAL_KEY_PROTOCOL_VERSION        0x01U
#define DIGITAL_KEY_MAX_PAYLOAD_LEN         16U
#define DIGITAL_KEY_MSG_ID_REPORT           0x10U
#define DIGITAL_KEY_MSG_LOCK_STATUS         0x20U
#define DIGITAL_KEY_ID_REPORT_INTERVAL_MS   100U
#define DIGITAL_KEY_STATUS_TIMEOUT_MS       500U
#define DIGITAL_KEY_ZONE_CONFIRM_FRAMES     10U
#define DIGITAL_KEY_ALERT_DURATION_MS       3000U
#define DIGITAL_KEY_LED_BLINK_INTERVAL_MS   250U

#define DIGITAL_KEY_AUTH_UNKNOWN            0U
#define DIGITAL_KEY_AUTH_FAILED             1U
#define DIGITAL_KEY_AUTH_PASSED             2U

#define DIGITAL_KEY_ZONE_NONE               0U
#define DIGITAL_KEY_ZONE_SENSING            1U
#define DIGITAL_KEY_ZONE_WELCOME            2U
#define DIGITAL_KEY_ZONE_UNLOCK             3U

#define DIGITAL_KEY_EVENT_STEADY             0U
#define DIGITAL_KEY_EVENT_DISCOVERED         1U
#define DIGITAL_KEY_EVENT_ENTER_WELCOME      2U
#define DIGITAL_KEY_EVENT_ENTER_UNLOCK       3U
#define DIGITAL_KEY_EVENT_LEAVE_UNLOCK       4U
#define DIGITAL_KEY_EVENT_LEAVE_WELCOME      5U
#define DIGITAL_KEY_EVENT_LOST               6U

#define DIGITAL_KEY_STATE_KEY_PRESENT       0x01U
#define DIGITAL_KEY_STATE_ID_MATCH          0x02U
#define DIGITAL_KEY_STATE_WELCOME_ACTIVE    0x04U
#define DIGITAL_KEY_STATE_UNLOCKED          0x08U
#define DIGITAL_KEY_STATE_LOCKED            0x10U

#define DIGITAL_KEY_ALERT_NONE              0U
#define DIGITAL_KEY_ALERT_ENTER_WELCOME     1U
#define DIGITAL_KEY_ALERT_LEAVE_WELCOME     2U

extern struct ringbuffer g_rb_Uart_MS901M_RevBuf;
extern volatile uint8_t g_digitalKeyRemoteId;
extern volatile uint8_t g_digitalKeyAuthResult;
extern volatile uint8_t g_digitalKeyZone;
extern volatile uint8_t g_digitalKeyEvent;
extern volatile uint16_t g_digitalKeyDistanceMm;
extern volatile int8_t g_digitalKeyAngleDeg;
extern volatile uint8_t g_digitalKeyStateFlags;
extern volatile uint8_t g_digitalKeySignalQuality;
extern volatile uint32_t g_digitalKeyStatusFrameCount;
extern volatile uint32_t g_digitalKeyCrcErrorCount;
extern volatile bool g_digitalKeyStatusValid;
extern volatile bool g_digitalKeyWelcomeConfirmed;
extern volatile uint8_t g_digitalKeyWelcomeConfirmCount;
extern volatile uint8_t g_digitalKeyWelcomeAlertType;
extern volatile bool g_digitalKeyUnlockConfirmed;
extern volatile uint8_t g_digitalKeyUnlockConfirmCount;

void uart_init(void);

void MS901M_uart1_send_data(unsigned char* str, uint16_t length);
void uart0_send_data(char* str, uint16_t length);
void uart1_send_data(char* str, uint16_t length);

void Print_Data_To_Uart0(void);
void Print_Data_To_MS901M_Uart(void);
void Send_Data_To_OpenMV_Uart(const uint8_t *data, uint16_t length);
void Print_Data_To_Uart1(void);
void Uart1_Receive_Data(uint8_t uart1_data);

/*
 * Frame: FF 2F LEN PAYLOAD XOR CC
 * XOR covers 2F, LEN and every PAYLOAD byte.
 * Call Bluetooth_Task() regularly from the non-interrupt context.
 */
void Bluetooth_Task(void);
void DigitalKey_SendIdReport(uint8_t keyId);
uint64_t HAL_GetTick(void);

//宏替换 printf
//#define printf(...) uart_printf(__VA_ARGS__)
extern int uart_printf(const char *format, ...);


#endif
