#include "uart.h"


static uint8_t  g_u8UartRevBuf[BUFF_LEN_1K] = {0},
                g_u8UartSendRevBuf[BUFF_LEN_1K*8],

                g_u8Uart_MS901M_RevBuf[BUFF_LEN_1K*4] = {0},
                g_u8Uart_MS901M_SendRevBuf[BUFF_LEN_1K/2];

struct ringbuffer g_rb_UartRevBuf={
    .read_index		= 0,
    .read_mirror	= 0,
    .write_index	= 0,
    .write_mirror	= 0,
    .buffer_ptr		= g_u8UartRevBuf,
    .buffer_size	= sizeof(g_u8UartRevBuf),
};
struct ringbuffer g_rb_UartSendBuf={
    .read_index		= 0,
    .read_mirror	= 0,
    .write_index	= 0,
    .write_mirror	= 0,
    .buffer_ptr		= g_u8UartSendRevBuf,
    .buffer_size	= sizeof(g_u8UartSendRevBuf),
};

struct ringbuffer g_rb_Uart_MS901M_RevBuf={
    .read_index		= 0,
    .read_mirror	= 0,
    .write_index	= 0,
    .write_mirror	= 0,
    .buffer_ptr		= g_u8Uart_MS901M_RevBuf,
    .buffer_size	= sizeof(g_u8Uart_MS901M_RevBuf),
};
struct ringbuffer g_rb_Uart_MS901M_SendBuf={
    .read_index		= 0,
    .read_mirror	= 0,
    .write_index	= 0,
    .write_mirror	= 0,
    .buffer_ptr		= g_u8Uart_MS901M_SendRevBuf,
    .buffer_size	= sizeof(g_u8Uart_MS901M_SendRevBuf),
};

volatile uint8_t g_digitalKeyRemoteId = 0U;
volatile uint8_t g_digitalKeyAuthResult = DIGITAL_KEY_AUTH_UNKNOWN;
volatile uint8_t g_digitalKeyZone = DIGITAL_KEY_ZONE_NONE;
volatile uint8_t g_digitalKeyEvent = 0U;
volatile uint16_t g_digitalKeyDistanceMm = 0U;
volatile int8_t g_digitalKeyAngleDeg = 0;
volatile uint8_t g_digitalKeyStateFlags = 0U;
volatile uint8_t g_digitalKeySignalQuality = 0U;
volatile uint32_t g_digitalKeyStatusFrameCount = 0U;
volatile uint32_t g_digitalKeyCrcErrorCount = 0U;
volatile bool g_digitalKeyStatusValid = false;
volatile bool g_digitalKeyWelcomeConfirmed = false;
volatile uint8_t g_digitalKeyWelcomeConfirmCount = 0U;
volatile uint8_t g_digitalKeyWelcomeAlertType = DIGITAL_KEY_ALERT_NONE;
volatile bool g_digitalKeyUnlockConfirmed = false;
volatile uint8_t g_digitalKeyUnlockConfirmCount = 0U;

volatile uint64_t SystemTickCNT = 0;
uint64_t HAL_GetTick(void)
{
    return SystemTickCNT;
}

// 自定义输出函数带时间戳
int uart_printf(const char *format, ...)
{
#if UART0_BLUETOOTH_MODE
    /*
     * PA10/PA11 now carry framed Bluetooth data. Suppress legacy debug text
     * so the peer never mistakes printf output for protocol traffic.
     */
    (void) format;
    return 0;
#else
    va_list args;
    char buffer[1024]; // 根据需要调整缓冲区大小
    int len;

    // 获取当前时间戳（以毫秒为单位）
    uint32_t timestamp = (uint32_t)HAL_GetTick();

    // 计算秒数和毫秒数
    uint32_t seconds = timestamp / 1000;
    uint32_t milliseconds = timestamp % 1000;

    // 确定秒数的最大宽度（根据 uint32_t 的最大值）
    // uint32_t 最大值为 4294967295 ms，即 4294967 秒
    // 因此秒数最多占 7 位
    char timestamp_str[16];
    snprintf(timestamp_str, sizeof(timestamp_str), "[%07u.%03u] ", seconds, milliseconds);

    // 将时间戳和用户提供的格式化字符串合并到缓冲区
    va_start(args, format);
    len = vsnprintf(buffer, sizeof(buffer) - strlen(timestamp_str), format, args);
    va_end(args);

    // 确保有足够的空间容纳时间戳
    if (len > 0 && len < (int)(sizeof(buffer) - strlen(timestamp_str)))
    {
        // 拼接时间戳和用户提供的字符串
        memmove(buffer + strlen(timestamp_str), buffer, len + 1); // 移动原始内容
        memcpy(buffer, timestamp_str, strlen(timestamp_str));      // 添加时间戳

        // 发送拼接后的字符串到环形缓冲区
        ringbuffer_put(&g_rb_UartSendBuf, (uint8_t *)buffer, strlen(buffer));
    }
    return 1;
#endif
}

void uart_init(void)
{
    //清除串口中断标志
    NVIC_ClearPendingIRQ(UART_MS901M_INST_INT_IRQN);
    //使能串口中断
    NVIC_EnableIRQ(UART_MS901M_INST_INT_IRQN);

    //清除串口中断标志
    NVIC_ClearPendingIRQ(UART_DEBUG_INST_INT_IRQN);
    //使能串口中断
    NVIC_EnableIRQ(UART_DEBUG_INST_INT_IRQN);
	
    //printf("UART Init\r\n");
}

//***uart0***//
void uart0_send_char(char ch)
{
    while (DL_UART_isBusy(UART_DEBUG_INST) == true)
    {
        ;
    }
    DL_UART_Main_transmitData(UART_DEBUG_INST, ch);
}

void uart0_send_data(char* str, uint16_t length)
{
    for (uint16_t index = 0; index < length; index++)
    {
        uart0_send_char(str[index]);
    }
}
void Print_Data_To_Uart0(void)
{
#if UART0_BLUETOOTH_MODE
    /* UART0 is reserved for the Bluetooth protocol in this build. */
    return;
#else
    if (ringbuffer_data_len(&g_rb_UartSendBuf))
    {
        uint8_t print_buff[BUFF_LEN_1K] = {0};
        uint16_t l_u8SendDataLen = ringbuffer_get(&g_rb_UartSendBuf, &print_buff[0], BUFF_LEN_1K);
        uart0_send_data((char*)&print_buff[0], l_u8SendDataLen);
    }
#endif
}
//串口的中断服务函数
void UART_DEBUG_INST_IRQHandler(void)
{
    uint8_t rx_buf;
    //如果产生了串口中断
    switch( DL_UART_getPendingInterrupt(UART_DEBUG_INST) )
    {
        case DL_UART_IIDX_RX://如果是接收中断
            while (!DL_UART_Main_isRXFIFOEmpty(UART_DEBUG_INST))
            {
                rx_buf = DL_UART_Main_receiveData(UART_DEBUG_INST);
                ringbuffer_put(&g_rb_UartRevBuf, &rx_buf, 1);
            }
        break;
        default://其他的串口中断
            break;
    }
}

typedef enum
{
    BLUETOOTH_PARSE_WAIT_HEAD = 0,
    BLUETOOTH_PARSE_WAIT_TYPE,
    BLUETOOTH_PARSE_WAIT_LENGTH,
    BLUETOOTH_PARSE_WAIT_PAYLOAD,
    BLUETOOTH_PARSE_WAIT_CHECKSUM,
    BLUETOOTH_PARSE_WAIT_TAIL
} Bluetooth_Parse_State;

static Bluetooth_Parse_State g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_HEAD;
static uint8_t g_bluetoothPayload[BLUETOOTH_MAX_PAYLOAD_LEN + 1U] = {0};
static uint8_t g_bluetoothLatestPayload[BLUETOOTH_MAX_PAYLOAD_LEN + 1U] = {0};
static uint8_t g_bluetoothPayloadLength = 0U;
static uint8_t g_bluetoothPayloadIndex = 0U;
static uint8_t g_bluetoothCalculatedChecksum = 0U;
static uint8_t g_bluetoothReceivedChecksum = 0U;
static bool g_bluetoothPayloadIsPrintable = true;
static bool g_bluetoothDisplayPending = false;

static void Bluetooth_ResetParser(uint8_t currentByte)
{
    /*
     * If the byte that caused the reset is another frame head, retain it as
     * the beginning of a possible next frame. This makes resynchronization
     * fast after dropped or corrupted bytes.
     */
    if (currentByte == BLUETOOTH_FRAME_HEAD)
    {
        g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_TYPE;
    }
    else
    {
        g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_HEAD;
    }

    g_bluetoothPayloadLength = 0U;
    g_bluetoothPayloadIndex = 0U;
    g_bluetoothCalculatedChecksum = 0U;
    g_bluetoothReceivedChecksum = 0U;
    g_bluetoothPayloadIsPrintable = true;
}

static void Bluetooth_ParseByte(uint8_t data)
{
    uint8_t index;

    switch (g_bluetoothParseState)
    {
        case BLUETOOTH_PARSE_WAIT_HEAD:
            if (data == BLUETOOTH_FRAME_HEAD)
            {
                g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_TYPE;
            }
            break;

        case BLUETOOTH_PARSE_WAIT_TYPE:
            if (data == BLUETOOTH_FRAME_TYPE_DISPLAY)
            {
                g_bluetoothCalculatedChecksum = data;
                g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_LENGTH;
            }
            else
            {
                Bluetooth_ResetParser(data);
            }
            break;

        case BLUETOOTH_PARSE_WAIT_LENGTH:
            if ((data > 0U) && (data <= BLUETOOTH_MAX_PAYLOAD_LEN))
            {
                g_bluetoothPayloadLength = data;
                g_bluetoothPayloadIndex = 0U;
                g_bluetoothPayloadIsPrintable = true;
                g_bluetoothCalculatedChecksum ^= data;
                g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_PAYLOAD;
            }
            else
            {
                Bluetooth_ResetParser(data);
            }
            break;

        case BLUETOOTH_PARSE_WAIT_PAYLOAD:
            g_bluetoothPayload[g_bluetoothPayloadIndex++] = data;
            g_bluetoothCalculatedChecksum ^= data;

            /* OLED font table is indexed by printable ASCII characters. */
            if ((data < 0x20U) || (data > 0x7EU))
            {
                g_bluetoothPayloadIsPrintable = false;
            }

            if (g_bluetoothPayloadIndex >= g_bluetoothPayloadLength)
            {
                g_bluetoothPayload[g_bluetoothPayloadLength] = '\0';
                g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_CHECKSUM;
            }
            break;

        case BLUETOOTH_PARSE_WAIT_CHECKSUM:
            g_bluetoothReceivedChecksum = data;
            g_bluetoothParseState = BLUETOOTH_PARSE_WAIT_TAIL;
            break;

        case BLUETOOTH_PARSE_WAIT_TAIL:
            if ((data == BLUETOOTH_FRAME_TAIL) &&
                (g_bluetoothReceivedChecksum == g_bluetoothCalculatedChecksum) &&
                g_bluetoothPayloadIsPrintable)
            {
                for (index = 0U; index < g_bluetoothPayloadLength; index++)
                {
                    g_bluetoothLatestPayload[index] = g_bluetoothPayload[index];
                }
                g_bluetoothLatestPayload[g_bluetoothPayloadLength] = '\0';
                g_bluetoothDisplayPending = true;
            }
            Bluetooth_ResetParser(data);
            break;

        default:
            Bluetooth_ResetParser(data);
            break;
    }
}

typedef enum
{
    DIGITAL_KEY_PARSE_WAIT_HEAD_0 = 0,
    DIGITAL_KEY_PARSE_WAIT_HEAD_1,
    DIGITAL_KEY_PARSE_WAIT_VERSION,
    DIGITAL_KEY_PARSE_WAIT_TYPE,
    DIGITAL_KEY_PARSE_WAIT_SEQUENCE,
    DIGITAL_KEY_PARSE_WAIT_LENGTH,
    DIGITAL_KEY_PARSE_WAIT_PAYLOAD,
    DIGITAL_KEY_PARSE_WAIT_CRC_LOW,
    DIGITAL_KEY_PARSE_WAIT_CRC_HIGH
} DigitalKey_Parse_State;

static DigitalKey_Parse_State g_digitalKeyParseState =
    DIGITAL_KEY_PARSE_WAIT_HEAD_0;
static uint8_t g_digitalKeyMessageType = 0U;
static uint8_t g_digitalKeySequence = 0U;
static uint8_t g_digitalKeyPayloadLength = 0U;
static uint8_t g_digitalKeyPayloadIndex = 0U;
static uint8_t g_digitalKeyPayload[DIGITAL_KEY_MAX_PAYLOAD_LEN] = {0U};
static uint16_t g_digitalKeyCalculatedCrc = 0xFFFFU;
static uint16_t g_digitalKeyReceivedCrc = 0U;
static uint64_t g_digitalKeyLastStatusMs = 0U;
static uint64_t g_digitalKeyAlertStartMs = 0U;
static uint8_t g_digitalKeyTxSequence = 0U;

static uint16_t DigitalKey_Crc16Update(uint16_t crc, uint8_t data)
{
    uint8_t bit;

    crc ^= (uint16_t)data << 8;
    for (bit = 0U; bit < 8U; bit++)
    {
        if ((crc & 0x8000U) != 0U)
        {
            crc = (uint16_t)((crc << 1) ^ 0x1021U);
        }
        else
        {
            crc <<= 1;
        }
    }

    return crc;
}

static void DigitalKey_ResetParser(uint8_t currentByte)
{
    g_digitalKeyPayloadLength = 0U;
    g_digitalKeyPayloadIndex = 0U;
    g_digitalKeyCalculatedCrc = 0xFFFFU;
    g_digitalKeyReceivedCrc = 0U;

    if (currentByte == DIGITAL_KEY_FRAME_HEAD_0)
    {
        g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_HEAD_1;
    }
    else
    {
        g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_HEAD_0;
    }
}

static void DigitalKey_StartWelcomeAlert(uint8_t alertType)
{
    g_digitalKeyWelcomeAlertType = alertType;
    g_digitalKeyAlertStartMs = HAL_GetTick();
}

static void DigitalKey_UpdateWelcomeDecision(void)
{
    bool isAuthenticatedWelcome =
        (g_digitalKeyAuthResult == DIGITAL_KEY_AUTH_PASSED) &&
        (g_digitalKeyZone == DIGITAL_KEY_ZONE_WELCOME);

    if (!g_digitalKeyWelcomeConfirmed)
    {
        if (isAuthenticatedWelcome)
        {
            if (g_digitalKeyWelcomeConfirmCount <
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyWelcomeConfirmCount++;
            }

            if (g_digitalKeyWelcomeConfirmCount >=
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyWelcomeConfirmed = true;
                g_digitalKeyWelcomeConfirmCount = 0U;
                DigitalKey_StartWelcomeAlert(
                    DIGITAL_KEY_ALERT_ENTER_WELCOME);
            }
        }
        else
        {
            g_digitalKeyWelcomeConfirmCount = 0U;
        }
    }
    else
    {
        if (!isAuthenticatedWelcome)
        {
            if (g_digitalKeyWelcomeConfirmCount <
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyWelcomeConfirmCount++;
            }

            if (g_digitalKeyWelcomeConfirmCount >=
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyWelcomeConfirmed = false;
                g_digitalKeyWelcomeConfirmCount = 0U;
                DigitalKey_StartWelcomeAlert(
                    DIGITAL_KEY_ALERT_LEAVE_WELCOME);
            }
        }
        else
        {
            g_digitalKeyWelcomeConfirmCount = 0U;
        }
    }
}

static void DigitalKey_UpdateUnlockDecision(void)
{
    bool isAuthenticatedUnlock =
        (g_digitalKeyAuthResult == DIGITAL_KEY_AUTH_PASSED) &&
        (g_digitalKeyZone == DIGITAL_KEY_ZONE_UNLOCK);

    if (!g_digitalKeyUnlockConfirmed)
    {
        if (isAuthenticatedUnlock)
        {
            if (g_digitalKeyUnlockConfirmCount <
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyUnlockConfirmCount++;
            }

            if (g_digitalKeyUnlockConfirmCount >=
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyUnlockConfirmed = true;
                g_digitalKeyUnlockConfirmCount = 0U;
            }
        }
        else
        {
            g_digitalKeyUnlockConfirmCount = 0U;
        }
    }
    else
    {
        if (!isAuthenticatedUnlock)
        {
            if (g_digitalKeyUnlockConfirmCount <
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyUnlockConfirmCount++;
            }

            if (g_digitalKeyUnlockConfirmCount >=
                DIGITAL_KEY_ZONE_CONFIRM_FRAMES)
            {
                g_digitalKeyUnlockConfirmed = false;
                g_digitalKeyUnlockConfirmCount = 0U;
            }
        }
        else
        {
            g_digitalKeyUnlockConfirmCount = 0U;
        }
    }
}

static void DigitalKey_UpdateStatusOutputs(uint64_t nowMs)
{
    uint8_t outputFlags = 0U;
    uint64_t alertElapsedMs;

    if (g_digitalKeyUnlockConfirmed)
    {
        outputFlags |= DEVICE_OUTPUT_LED2;
    }
    else
    {
        outputFlags |= DEVICE_OUTPUT_LED3;
    }

    if (g_digitalKeyWelcomeAlertType != DIGITAL_KEY_ALERT_NONE)
    {
        alertElapsedMs = nowMs - g_digitalKeyAlertStartMs;
        if (alertElapsedMs >= DIGITAL_KEY_ALERT_DURATION_MS)
        {
            g_digitalKeyWelcomeAlertType = DIGITAL_KEY_ALERT_NONE;
        }
        else
        {
            /* Both entering and leaving use a continuous three-second beep. */
            outputFlags |= DEVICE_OUTPUT_BEEP;

            if (g_digitalKeyWelcomeAlertType ==
                DIGITAL_KEY_ALERT_ENTER_WELCOME)
            {
                /* 250 ms on/off gives a clearly visible 2 Hz blink. */
                if (((alertElapsedMs / DIGITAL_KEY_LED_BLINK_INTERVAL_MS) &
                    1U) == 0U)
                {
                    outputFlags |= DEVICE_OUTPUT_LED1;
                }
            }
            else
            {
                /* Leaving the welcome zone keeps LED1 steadily on. */
                outputFlags |= DEVICE_OUTPUT_LED1;
            }
        }
    }

    DeviceOutputs_Set(outputFlags);
}

static void DigitalKey_ProcessFrame(void)
{
    int8_t angleDeg;

    if ((g_digitalKeyMessageType != DIGITAL_KEY_MSG_LOCK_STATUS) ||
        (g_digitalKeyPayloadLength != 9U))
    {
        return;
    }

    angleDeg = (int8_t)g_digitalKeyPayload[6];
    if ((g_digitalKeyPayload[0] > 0x0FU) ||
        (g_digitalKeyPayload[1] > DIGITAL_KEY_AUTH_PASSED) ||
        (g_digitalKeyPayload[2] > DIGITAL_KEY_ZONE_UNLOCK) ||
        (angleDeg < -90) || (angleDeg > 90))
    {
        return;
    }

    g_digitalKeyRemoteId = g_digitalKeyPayload[0];
    g_digitalKeyAuthResult = g_digitalKeyPayload[1];
    g_digitalKeyZone = g_digitalKeyPayload[2];
    g_digitalKeyEvent = g_digitalKeyPayload[3];
    g_digitalKeyDistanceMm = (uint16_t)g_digitalKeyPayload[4] |
        ((uint16_t)g_digitalKeyPayload[5] << 8);
    g_digitalKeyAngleDeg = angleDeg;
    g_digitalKeyStateFlags = g_digitalKeyPayload[7];
    g_digitalKeySignalQuality = g_digitalKeyPayload[8];
    g_digitalKeyStatusFrameCount++;
    g_digitalKeyStatusValid = true;
    g_digitalKeyLastStatusMs = HAL_GetTick();

    DigitalKey_UpdateWelcomeDecision();
    DigitalKey_UpdateUnlockDecision();
}

static void DigitalKey_ParseByte(uint8_t data)
{
    switch (g_digitalKeyParseState)
    {
        case DIGITAL_KEY_PARSE_WAIT_HEAD_0:
            if (data == DIGITAL_KEY_FRAME_HEAD_0)
            {
                g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_HEAD_1;
            }
            break;

        case DIGITAL_KEY_PARSE_WAIT_HEAD_1:
            if (data == DIGITAL_KEY_FRAME_HEAD_1)
            {
                g_digitalKeyCalculatedCrc = 0xFFFFU;
                g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_VERSION;
            }
            else
            {
                DigitalKey_ResetParser(data);
            }
            break;

        case DIGITAL_KEY_PARSE_WAIT_VERSION:
            if (data == DIGITAL_KEY_PROTOCOL_VERSION)
            {
                g_digitalKeyCalculatedCrc =
                    DigitalKey_Crc16Update(g_digitalKeyCalculatedCrc, data);
                g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_TYPE;
            }
            else
            {
                DigitalKey_ResetParser(data);
            }
            break;

        case DIGITAL_KEY_PARSE_WAIT_TYPE:
            g_digitalKeyMessageType = data;
            g_digitalKeyCalculatedCrc =
                DigitalKey_Crc16Update(g_digitalKeyCalculatedCrc, data);
            g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_SEQUENCE;
            break;

        case DIGITAL_KEY_PARSE_WAIT_SEQUENCE:
            g_digitalKeySequence = data;
            g_digitalKeyCalculatedCrc =
                DigitalKey_Crc16Update(g_digitalKeyCalculatedCrc, data);
            g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_LENGTH;
            break;

        case DIGITAL_KEY_PARSE_WAIT_LENGTH:
            if (data <= DIGITAL_KEY_MAX_PAYLOAD_LEN)
            {
                g_digitalKeyPayloadLength = data;
                g_digitalKeyPayloadIndex = 0U;
                g_digitalKeyCalculatedCrc =
                    DigitalKey_Crc16Update(g_digitalKeyCalculatedCrc, data);
                g_digitalKeyParseState = (data == 0U) ?
                    DIGITAL_KEY_PARSE_WAIT_CRC_LOW :
                    DIGITAL_KEY_PARSE_WAIT_PAYLOAD;
            }
            else
            {
                DigitalKey_ResetParser(data);
            }
            break;

        case DIGITAL_KEY_PARSE_WAIT_PAYLOAD:
            g_digitalKeyPayload[g_digitalKeyPayloadIndex++] = data;
            g_digitalKeyCalculatedCrc =
                DigitalKey_Crc16Update(g_digitalKeyCalculatedCrc, data);
            if (g_digitalKeyPayloadIndex >= g_digitalKeyPayloadLength)
            {
                g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_CRC_LOW;
            }
            break;

        case DIGITAL_KEY_PARSE_WAIT_CRC_LOW:
            g_digitalKeyReceivedCrc = data;
            g_digitalKeyParseState = DIGITAL_KEY_PARSE_WAIT_CRC_HIGH;
            break;

        case DIGITAL_KEY_PARSE_WAIT_CRC_HIGH:
            g_digitalKeyReceivedCrc |= (uint16_t)data << 8;
            if (g_digitalKeyReceivedCrc == g_digitalKeyCalculatedCrc)
            {
                DigitalKey_ProcessFrame();
            }
            else
            {
                g_digitalKeyCrcErrorCount++;
            }
            DigitalKey_ResetParser(data);
            break;

        default:
            DigitalKey_ResetParser(data);
            break;
    }
}

static void DigitalKey_SendFrame(
    uint8_t messageType, const uint8_t *payload, uint8_t payloadLength)
{
    uint16_t crc = 0xFFFFU;
    uint8_t index;
    uint8_t sequence = g_digitalKeyTxSequence++;
    uint8_t header[6] = {
        DIGITAL_KEY_FRAME_HEAD_0,
        DIGITAL_KEY_FRAME_HEAD_1,
        DIGITAL_KEY_PROTOCOL_VERSION,
        messageType,
        sequence,
        payloadLength
    };

    for (index = 2U; index < sizeof(header); index++)
    {
        crc = DigitalKey_Crc16Update(crc, header[index]);
    }
    for (index = 0U; index < payloadLength; index++)
    {
        crc = DigitalKey_Crc16Update(crc, payload[index]);
    }

    for (index = 0U; index < sizeof(header); index++)
    {
        DL_UART_Main_transmitDataBlocking(UART_DEBUG_INST, header[index]);
    }
    for (index = 0U; index < payloadLength; index++)
    {
        DL_UART_Main_transmitDataBlocking(UART_DEBUG_INST, payload[index]);
    }
    DL_UART_Main_transmitDataBlocking(UART_DEBUG_INST, (uint8_t)(crc & 0xFFU));
    DL_UART_Main_transmitDataBlocking(UART_DEBUG_INST, (uint8_t)(crc >> 8));
}

void DigitalKey_SendIdReport(uint8_t keyId)
{
    uint8_t payload = keyId & 0x0FU;

    DigitalKey_SendFrame(DIGITAL_KEY_MSG_ID_REPORT, &payload, 1U);
}

void Bluetooth_Task(void)
{
    uint8_t data;
    uint64_t nowMs;
    static uint64_t lastIdReportMs = 0U;

    while (ringbuffer_getchar(&g_rb_UartRevBuf, &data) != 0U)
    {
        Bluetooth_ParseByte(data);
        DigitalKey_ParseByte(data);
    }

    nowMs = HAL_GetTick();

    if ((nowMs - lastIdReportMs) >= DIGITAL_KEY_ID_REPORT_INTERVAL_MS)
    {
        lastIdReportMs = nowMs;
        DigitalKey_SendIdReport(g_dipSwitchHexValue);
    }

    if (g_digitalKeyStatusValid &&
        ((nowMs - g_digitalKeyLastStatusMs) > DIGITAL_KEY_STATUS_TIMEOUT_MS))
    {
        g_digitalKeyStatusValid = false;
        g_digitalKeyAuthResult = DIGITAL_KEY_AUTH_UNKNOWN;
        g_digitalKeyZone = DIGITAL_KEY_ZONE_NONE;
        g_digitalKeyStateFlags = DIGITAL_KEY_STATE_LOCKED;
        g_digitalKeyUnlockConfirmed = false;
        g_digitalKeyUnlockConfirmCount = 0U;
    }

    DigitalKey_UpdateStatusOutputs(nowMs);

    if (g_bluetoothDisplayPending)
    {
        /*
         * Clear exactly one 16-character OLED row, then show only the newest
         * valid payload. Invalid or incomplete frames leave the display intact.
         */
        OLED_ShowString(0U, BLUETOOTH_OLED_LINE,
                        (uint8_t *) "                ");
        OLED_ShowString(0U, BLUETOOTH_OLED_LINE,
                        g_bluetoothLatestPayload);
        g_bluetoothDisplayPending = false;
    }
}

/*
 * PA17/PA9 now belong exclusively to the TOF UART. Keep the obsolete OpenMV
 * API as a no-op so legacy task code cannot inject bytes into the TOF stream.
 */
void uart1_send_data(char* str, uint16_t length)
{
    (void) str;
    (void) length;
}

void Print_Data_To_Uart1(void)
{
    return;
}

void Send_Data_To_OpenMV_Uart(const uint8_t *data, uint16_t length)
{
    // 检查数据是否有效
    if (data == NULL || length == 0)
    {
        return; // 无效数据，直接返回
    }

//    // 发送数据到 OpenMV
//    uart1_send_data(data, length);
}

void Uart1_Receive_Data(uint8_t uart1_data)
{
    (void) uart1_data;
}

//***uart2***//
void uart2_send_char(unsigned char ch)
{
    while( DL_UART_isBusy(UART_MS901M_INST) == true )
    {
        ;
    }
    DL_UART_Main_transmitData(UART_MS901M_INST, (char)ch);
}
void MS901M_uart1_send_data(unsigned char* str,uint16_t length)
{
    for(uint16_t index=0; index < length; index++)
    {
        uart2_send_char(str[index]);
    }
}
void Print_Data_To_MS901M_Uart(void)
{
    if(ringbuffer_data_len(&g_rb_Uart_MS901M_SendBuf))
    {
        uint8_t print_buff[BUFF_LEN_1K] = {0};
        uint16_t l_u8SendDataLen = ringbuffer_get(&g_rb_Uart_MS901M_SendBuf, &print_buff[0], BUFF_LEN_1K);
        MS901M_uart1_send_data(&print_buff[0],l_u8SendDataLen);
    }
}
//_MS901M_串口的中断服务函数
void UART_MS901M_INST_IRQHandler(void)
{
    static uint8_t rx_buf;
    //如果产生了串口中断
    switch( DL_UART_getPendingInterrupt(UART_MS901M_INST) )
    {
        case DL_UART_IIDX_RX://如果是接收中断
            rx_buf = DL_UART_Main_receiveData(UART_MS901M_INST);
            ringbuffer_put(&g_rb_Uart_MS901M_RevBuf,&rx_buf, 1);
        break;
        default://其他的串口中断
            break;
    }
}
