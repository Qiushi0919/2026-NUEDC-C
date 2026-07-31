#include "bsp_tof.h"

#include "board.h"
#include "bsp_oled.h"
#include <limits.h>
#include <stdio.h>

#define TOF_FRAME_HEADER_0       (0x57U)
#define TOF_FRAME_HEADER_1       (0x00U)
#define TOF_FRAME_HEADER_2       (0xFFU)
#define TOF_READ_FRAME_MARK      (0x10U)
#define TOF_MAX_DISTANCE_MM      (5000)
#define TOF_DATA_TIMEOUT_MS      (250U)
#define TOF_QUERY_INTERVAL_MS    (100U)
#define TOF_OLED_LINE            (7U)

volatile int32_t g_tofDistanceMm = 0;
volatile uint16_t g_tofSignalStrength = 0U;
volatile uint32_t g_tofSensorTimestamp = 0U;
volatile uint32_t g_tofFrameCount = 0U;
volatile uint32_t g_tofChecksumErrorCount = 0U;
volatile uint8_t g_tofDistanceStatus = 0U;
volatile uint8_t g_tofSensorId = 0U;
volatile bool g_tofDistanceValid = false;

static uint8_t g_tofFrame[TOF_FRAME_LENGTH] = {0U};
static uint8_t g_tofFrameIndex = 0U;
static volatile uint32_t g_tofDataAgeMs = UINT32_MAX;
static volatile bool g_tofDisplayPending = true;

extern uint64_t HAL_GetTick(void);

static void TOF_ResetParser(uint8_t currentByte)
{
    if (currentByte == TOF_FRAME_HEADER_0)
    {
        g_tofFrame[0] = currentByte;
        g_tofFrameIndex = 1U;
    }
    else
    {
        g_tofFrameIndex = 0U;
    }
}

static int32_t TOF_DecodeSigned24(const uint8_t *data)
{
    int32_t value = (int32_t) data[0] |
                    ((int32_t) data[1] << 8) |
                    ((int32_t) data[2] << 16);

    if ((value & 0x00800000L) != 0)
    {
        value |= (int32_t) 0xFF000000L;
    }

    return value;
}

static void TOF_ProcessFrame(void)
{
    uint8_t checksum = 0U;
    uint8_t index;
    int32_t distanceMm;
    uint16_t signalStrength;

    for (index = 0U; index < (TOF_FRAME_LENGTH - 1U); index++)
    {
        checksum = (uint8_t) (checksum + g_tofFrame[index]);
    }

    if (checksum != g_tofFrame[TOF_FRAME_LENGTH - 1U])
    {
        g_tofChecksumErrorCount++;
        return;
    }

    distanceMm = TOF_DecodeSigned24(&g_tofFrame[8]);
    signalStrength = (uint16_t) g_tofFrame[12] |
                     ((uint16_t) g_tofFrame[13] << 8);

    g_tofSensorId = g_tofFrame[3];
    g_tofSensorTimestamp = (uint32_t) g_tofFrame[4] |
                           ((uint32_t) g_tofFrame[5] << 8) |
                           ((uint32_t) g_tofFrame[6] << 16) |
                           ((uint32_t) g_tofFrame[7] << 24);
    g_tofDistanceStatus = g_tofFrame[11];
    g_tofSignalStrength = signalStrength;
    g_tofDistanceMm = distanceMm;
    g_tofFrameCount++;
    g_tofDataAgeMs = 0U;

    /*
     * The original drone implementation treats a non-zero signal strength
     * and an in-range distance as valid; distance_status is exposed to callers
     * but is not used as an undocumented rejection criterion.
     */
    g_tofDistanceValid =
        ((distanceMm >= 0) &&
         (distanceMm <= TOF_MAX_DISTANCE_MM) &&
         (signalStrength != 0U));
    g_tofDisplayPending = true;
}

void TOF_ParseByte(uint8_t data)
{
    switch (g_tofFrameIndex)
    {
        case 0U:
            if (data == TOF_FRAME_HEADER_0)
            {
                g_tofFrame[0] = data;
                g_tofFrameIndex = 1U;
            }
            break;

        case 1U:
            if (data == TOF_FRAME_HEADER_1)
            {
                g_tofFrame[1] = data;
                g_tofFrameIndex = 2U;
            }
            else
            {
                TOF_ResetParser(data);
            }
            break;

        case 2U:
            if (data == TOF_FRAME_HEADER_2)
            {
                g_tofFrame[2] = data;
                g_tofFrameIndex = 3U;
            }
            else
            {
                TOF_ResetParser(data);
            }
            break;

        default:
            g_tofFrame[g_tofFrameIndex++] = data;
            if (g_tofFrameIndex >= TOF_FRAME_LENGTH)
            {
                TOF_ProcessFrame();
                g_tofFrameIndex = 0U;
            }
            break;
    }
}

void TOF_SendReadRequest(uint8_t sensorId)
{
    uint8_t request[8] = {
        TOF_FRAME_HEADER_0,
        TOF_READ_FRAME_MARK,
        0xFFU,
        0xFFU,
        sensorId,
        0xFFU,
        0xFFU,
        0U
    };
    uint8_t index;
    uint8_t checksum = 0U;

    for (index = 0U; index < 7U; index++)
    {
        checksum = (uint8_t) (checksum + request[index]);
    }
    request[7] = checksum;

    for (index = 0U; index < sizeof(request); index++)
    {
        DL_UART_Main_transmitDataBlocking(UART_TOF_INST, request[index]);
    }
}

void TOF_Init(void)
{
    g_tofFrameIndex = 0U;
    g_tofDataAgeMs = UINT32_MAX;
    g_tofDistanceValid = false;
    g_tofDisplayPending = true;

    NVIC_ClearPendingIRQ(UART_TOF_INST_INT_IRQN);
    NVIC_EnableIRQ(UART_TOF_INST_INT_IRQN);
}

void UART_TOF_INST_IRQHandler(void)
{
    switch (DL_UART_getPendingInterrupt(UART_TOF_INST))
    {
        case DL_UART_IIDX_RX:
            while (!DL_UART_Main_isRXFIFOEmpty(UART_TOF_INST))
            {
                TOF_ParseByte(DL_UART_Main_receiveData(UART_TOF_INST));
            }
            break;

        default:
            break;
    }
}

void TOF_Tick1ms(void)
{
    if (g_tofDataAgeMs != UINT32_MAX)
    {
        g_tofDataAgeMs++;
    }
}

void TOF_Task(void)
{
    static uint64_t lastQueryMs = 0U;
    static bool timeoutDisplayed = false;
    uint64_t nowMs = HAL_GetTick();
    char displayText[17];

    if ((nowMs - lastQueryMs) >= TOF_QUERY_INTERVAL_MS)
    {
        lastQueryMs = nowMs;
        TOF_SendReadRequest(0U);
    }

    if (g_tofDataAgeMs > TOF_DATA_TIMEOUT_MS)
    {
        g_tofDistanceValid = false;
        if (!timeoutDisplayed)
        {
            g_tofDisplayPending = true;
            timeoutDisplayed = true;
        }
    }
    else
    {
        timeoutDisplayed = false;
    }

    if (g_tofDisplayPending)
    {
        if (g_tofDistanceValid)
        {
            snprintf(displayText, sizeof(displayText), "TOF:%4ldmm %4u",
                (long) g_tofDistanceMm,
                (unsigned int) g_tofSignalStrength);
        }
        else
        {
            snprintf(displayText, sizeof(displayText), "TOF:----mm      ");
        }

        OLED_ShowString(0U, TOF_OLED_LINE, (uint8_t *) displayText);
        g_tofDisplayPending = false;
    }
}

int32_t TOF_GetDistanceMm(void)
{
    return g_tofDistanceMm;
}

float TOF_GetDistanceCm(void)
{
    return (float) g_tofDistanceMm / 10.0f;
}

bool TOF_IsDistanceValid(void)
{
    return g_tofDistanceValid;
}
