#ifndef _BSP_TOF_H
#define _BSP_TOF_H

#include <stdbool.h>
#include <stdint.h>

#define TOF_UART_BAUD_RATE (921600U)
#define TOF_FRAME_LENGTH   (16U)

/*
 * Latest NLink TOFSense measurement. Distance is stored in millimetres.
 * Check g_tofDistanceValid before using the other measurement fields.
 */
extern volatile int32_t g_tofDistanceMm;
extern volatile uint16_t g_tofSignalStrength;
extern volatile uint32_t g_tofSensorTimestamp;
extern volatile uint32_t g_tofFrameCount;
extern volatile uint32_t g_tofChecksumErrorCount;
extern volatile uint8_t g_tofDistanceStatus;
extern volatile uint8_t g_tofSensorId;
extern volatile bool g_tofDistanceValid;

void TOF_Init(void);
void TOF_Task(void);
void TOF_Tick1ms(void);
void TOF_ParseByte(uint8_t data);
void TOF_SendReadRequest(uint8_t sensorId);

int32_t TOF_GetDistanceMm(void);
float TOF_GetDistanceCm(void);
bool TOF_IsDistanceValid(void);

#endif
