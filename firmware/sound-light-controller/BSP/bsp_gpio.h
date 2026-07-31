#ifndef __BSP_GPIO_H__
#define __BSP_GPIO_H__

#include "board.h"

#define DIP_SW1_PORT       TRACK_S1_PORT
#define DIP_SW1_PIN        TRACK_S1_PIN
#define DIP_SW2_PORT       TRACK_S2_PORT
#define DIP_SW2_PIN        TRACK_S2_PIN
#define DIP_SW3_PORT       TRACK_S3_PORT
#define DIP_SW3_PIN        TRACK_S3_PIN
#define DIP_SW4_PORT       TRACK_S4_PORT
#define DIP_SW4_PIN        TRACK_S4_PIN

#define DIP_LED1_PORT      GPIOA
#define DIP_LED1_PIN       DL_GPIO_PIN_18
#define DIP_LED1_IOMUX     IOMUX_PINCM40
#define DIP_LED2_PORT      GPIOB
#define DIP_LED2_PIN       DL_GPIO_PIN_19
#define DIP_LED2_IOMUX     IOMUX_PINCM45
#define DIP_LED3_PORT      GPIOA
#define DIP_LED3_PIN       DL_GPIO_PIN_8
#define DIP_LED3_IOMUX     IOMUX_PINCM19
#define DIP_BEEP_PORT      GPIOA
#define DIP_BEEP_PIN       DL_GPIO_PIN_27
#define DIP_BEEP_IOMUX     IOMUX_PINCM60

#define DEVICE_OUTPUT_LED1    0x01U
#define DEVICE_OUTPUT_LED2    0x02U
#define DEVICE_OUTPUT_BEEP    0x04U
#define DEVICE_OUTPUT_LED3    0x08U

extern volatile uint8_t g_dipSwitchHexValue;

void Beep_On();
void Beep_Off();
void Beep_Time(uint32_t time);

void OpenMV_On();
void OpenMV_Off();

uint8_t DIP_ReadValue(void);
void DIP_Output_Init(void);
void DeviceOutputs_Set(uint8_t outputFlags);
void DIP_Task(void);

#endif
