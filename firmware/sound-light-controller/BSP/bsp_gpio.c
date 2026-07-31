#include "bsp_gpio.h"

#define DIP_OLED_LINE              5U
#define DIP_DISPLAY_INTERVAL_MS    100U

volatile uint8_t g_dipSwitchHexValue = 0U;

//#define BEEP_PORT
//#define BEEP_PIN

uint8_t DIP_ReadValue(void)
{
    uint8_t value = 0U;

    /* The switches are active-low: ON = 1, OFF = 0. */
    if ((DL_GPIO_readPins(DIP_SW1_PORT, DIP_SW1_PIN) & DIP_SW1_PIN) == 0U)
    {
        value |= 0x08U;
    }
    if ((DL_GPIO_readPins(DIP_SW2_PORT, DIP_SW2_PIN) & DIP_SW2_PIN) == 0U)
    {
        value |= 0x04U;
    }
    if ((DL_GPIO_readPins(DIP_SW3_PORT, DIP_SW3_PIN) & DIP_SW3_PIN) == 0U)
    {
        value |= 0x02U;
    }
    if ((DL_GPIO_readPins(DIP_SW4_PORT, DIP_SW4_PIN) & DIP_SW4_PIN) == 0U)
    {
        value |= 0x01U;
    }

    return value;
}

void DIP_Output_Init(void)
{
    /*
     * PA27 was ADC0_CH0 in the original board configuration. Disable the ADC
     * function before repurposing the pin. PA18, PB19 and PA8 were track
     * inputs.
     */
    DL_ADC12_disableInterrupt(ADC0_VOLTAGE_INST,
        DL_ADC12_INTERRUPT_MEM0_RESULT_LOADED);
    DL_ADC12_disableConversions(ADC0_VOLTAGE_INST);
    NVIC_DisableIRQ(ADC0_VOLTAGE_INST_INT_IRQN);

    DL_GPIO_initDigitalOutputFeatures(DIP_LED1_IOMUX,
        DL_GPIO_INVERSION_DISABLE, DL_GPIO_RESISTOR_NONE,
        DL_GPIO_DRIVE_STRENGTH_LOW, DL_GPIO_HIZ_DISABLE);
    DL_GPIO_initDigitalOutputFeatures(DIP_LED2_IOMUX,
        DL_GPIO_INVERSION_DISABLE, DL_GPIO_RESISTOR_NONE,
        DL_GPIO_DRIVE_STRENGTH_LOW, DL_GPIO_HIZ_DISABLE);
    DL_GPIO_initDigitalOutputFeatures(DIP_LED3_IOMUX,
        DL_GPIO_INVERSION_DISABLE, DL_GPIO_RESISTOR_NONE,
        DL_GPIO_DRIVE_STRENGTH_LOW, DL_GPIO_HIZ_DISABLE);
    DL_GPIO_initDigitalOutputFeatures(DIP_BEEP_IOMUX,
        DL_GPIO_INVERSION_DISABLE, DL_GPIO_RESISTOR_NONE,
        DL_GPIO_DRIVE_STRENGTH_LOW, DL_GPIO_HIZ_DISABLE);

    /* All loads are active-high, so clear before enabling outputs. */
    DL_GPIO_clearPins(GPIOA, DIP_LED1_PIN | DIP_LED3_PIN | DIP_BEEP_PIN);
    DL_GPIO_clearPins(GPIOB, DIP_LED2_PIN);
    DL_GPIO_enableOutput(GPIOA, DIP_LED1_PIN | DIP_LED3_PIN | DIP_BEEP_PIN);
    DL_GPIO_enableOutput(GPIOB, DIP_LED2_PIN);
}

void DeviceOutputs_Set(uint8_t outputFlags)
{
    if ((outputFlags & DEVICE_OUTPUT_LED1) != 0U)
    {
        DL_GPIO_setPins(DIP_LED1_PORT, DIP_LED1_PIN);
    }
    else
    {
        DL_GPIO_clearPins(DIP_LED1_PORT, DIP_LED1_PIN);
    }

    if ((outputFlags & DEVICE_OUTPUT_LED2) != 0U)
    {
        DL_GPIO_setPins(DIP_LED2_PORT, DIP_LED2_PIN);
    }
    else
    {
        DL_GPIO_clearPins(DIP_LED2_PORT, DIP_LED2_PIN);
    }

    if ((outputFlags & DEVICE_OUTPUT_LED3) != 0U)
    {
        DL_GPIO_setPins(DIP_LED3_PORT, DIP_LED3_PIN);
    }
    else
    {
        DL_GPIO_clearPins(DIP_LED3_PORT, DIP_LED3_PIN);
    }

    if ((outputFlags & DEVICE_OUTPUT_BEEP) != 0U)
    {
        DL_GPIO_setPins(DIP_BEEP_PORT, DIP_BEEP_PIN);
    }
    else
    {
        DL_GPIO_clearPins(DIP_BEEP_PORT, DIP_BEEP_PIN);
    }
}

void DIP_Task(void)
{
    static uint64_t lastDisplayMs = 0U;
    uint64_t nowMs = HAL_GetTick();
    char displayText[17];

    g_dipSwitchHexValue = DIP_ReadValue();

    if ((nowMs - lastDisplayMs) >= DIP_DISPLAY_INTERVAL_MS)
    {
        lastDisplayMs = nowMs;
        snprintf(displayText, sizeof(displayText), "DIP:%2u          ",
            (unsigned int)g_dipSwitchHexValue);
        OLED_ShowString(0U, DIP_OLED_LINE, (uint8_t *)displayText);
    }
}

// 蜂鸣器开启
void Beep_On()
{
    DL_GPIO_setPins(DIP_BEEP_PORT, DIP_BEEP_PIN);
}

// 蜂鸣器关闭
void Beep_Off()
{
    DL_GPIO_clearPins(DIP_BEEP_PORT, DIP_BEEP_PIN);
}

// 蜂鸣器按时间响
void Beep_Time(uint32_t time)
{
    Beep_On();
    delay_ms(time);
    Beep_Off();
}

// OpenMV开启
void OpenMV_On()
{
    DL_GPIO_setPins(OPENMV_PORT, OPENMV_OPENMV_0_PIN);
}

// OpenMV关闭
void OpenMV_Off()
{
    DL_GPIO_clearPins(OPENMV_PORT, OPENMV_OPENMV_0_PIN);
}
