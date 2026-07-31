/*
 * Copyright (c) 2023, Texas Instruments Incorporated - http://www.ti.com
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * *  Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * *  Neither the name of Texas Instruments Incorporated nor the names of
 *    its contributors may be used to endorse or promote products derived
 *    from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/*
 *  ============ ti_msp_dl_config.h =============
 *  Configured MSPM0 DriverLib module declarations
 *
 *  DO NOT EDIT - This file is generated for the MSPM0G350X
 *  by the SysConfig tool.
 */
#ifndef ti_msp_dl_config_h
#define ti_msp_dl_config_h

#define CONFIG_MSPM0G350X
#define CONFIG_MSPM0G3505

#if defined(__ti_version__) || defined(__TI_COMPILER_VERSION__)
#define SYSCONFIG_WEAK __attribute__((weak))
#elif defined(__IAR_SYSTEMS_ICC__)
#define SYSCONFIG_WEAK __weak
#elif defined(__GNUC__)
#define SYSCONFIG_WEAK __attribute__((weak))
#endif

#include <ti/devices/msp/msp.h>
#include <ti/driverlib/driverlib.h>
#include <ti/driverlib/m0p/dl_core.h>

#ifdef __cplusplus
extern "C" {
#endif

/*
 *  ======== SYSCFG_DL_init ========
 *  Perform all required MSP DL initialization
 *
 *  This function should be called once at a point before any use of
 *  MSP DL.
 */


/* clang-format off */

#define POWER_STARTUP_DELAY                                                (16)


#define GPIO_HFXT_PORT                                                     GPIOA
#define GPIO_HFXIN_PIN                                             DL_GPIO_PIN_5
#define GPIO_HFXIN_IOMUX                                         (IOMUX_PINCM10)
#define GPIO_HFXOUT_PIN                                            DL_GPIO_PIN_6
#define GPIO_HFXOUT_IOMUX                                        (IOMUX_PINCM11)
#define CPUCLK_FREQ                                                     80000000



/* Defines for PWM_MOTOR_RearSide */
#define PWM_MOTOR_RearSide_INST                                            TIMG7
#define PWM_MOTOR_RearSide_INST_IRQHandler                        TIMG7_IRQHandler
#define PWM_MOTOR_RearSide_INST_INT_IRQN                        (TIMG7_INT_IRQn)
#define PWM_MOTOR_RearSide_INST_CLK_FREQ                                20000000
/* GPIO defines for channel 0 */
#define GPIO_PWM_MOTOR_RearSide_C0_PORT                                    GPIOB
#define GPIO_PWM_MOTOR_RearSide_C0_PIN                            DL_GPIO_PIN_15
#define GPIO_PWM_MOTOR_RearSide_C0_IOMUX                         (IOMUX_PINCM32)
#define GPIO_PWM_MOTOR_RearSide_C0_IOMUX_FUNC             IOMUX_PINCM32_PF_TIMG7_CCP0
#define GPIO_PWM_MOTOR_RearSide_C0_IDX                       DL_TIMER_CC_0_INDEX
/* GPIO defines for channel 1 */
#define GPIO_PWM_MOTOR_RearSide_C1_PORT                                    GPIOB
#define GPIO_PWM_MOTOR_RearSide_C1_PIN                            DL_GPIO_PIN_16
#define GPIO_PWM_MOTOR_RearSide_C1_IOMUX                         (IOMUX_PINCM33)
#define GPIO_PWM_MOTOR_RearSide_C1_IOMUX_FUNC             IOMUX_PINCM33_PF_TIMG7_CCP1
#define GPIO_PWM_MOTOR_RearSide_C1_IDX                       DL_TIMER_CC_1_INDEX

/* Defines for PWM_MOTOR_FrontSide */
#define PWM_MOTOR_FrontSide_INST                                          TIMG12
#define PWM_MOTOR_FrontSide_INST_IRQHandler                       TIMG12_IRQHandler
#define PWM_MOTOR_FrontSide_INST_INT_IRQN                       (TIMG12_INT_IRQn)
#define PWM_MOTOR_FrontSide_INST_CLK_FREQ                                20000000
/* GPIO defines for channel 0 */
#define GPIO_PWM_MOTOR_FrontSide_C0_PORT                                   GPIOB
#define GPIO_PWM_MOTOR_FrontSide_C0_PIN                           DL_GPIO_PIN_20
#define GPIO_PWM_MOTOR_FrontSide_C0_IOMUX                         (IOMUX_PINCM48)
#define GPIO_PWM_MOTOR_FrontSide_C0_IOMUX_FUNC            IOMUX_PINCM48_PF_TIMG12_CCP0
#define GPIO_PWM_MOTOR_FrontSide_C0_IDX                      DL_TIMER_CC_0_INDEX
/* GPIO defines for channel 1 */
#define GPIO_PWM_MOTOR_FrontSide_C1_PORT                                   GPIOB
#define GPIO_PWM_MOTOR_FrontSide_C1_PIN                           DL_GPIO_PIN_24
#define GPIO_PWM_MOTOR_FrontSide_C1_IOMUX                         (IOMUX_PINCM52)
#define GPIO_PWM_MOTOR_FrontSide_C1_IOMUX_FUNC            IOMUX_PINCM52_PF_TIMG12_CCP1
#define GPIO_PWM_MOTOR_FrontSide_C1_IDX                      DL_TIMER_CC_1_INDEX

/* Defines for PWM_Steering */
#define PWM_Steering_INST                                                  TIMG6
#define PWM_Steering_INST_IRQHandler                            TIMG6_IRQHandler
#define PWM_Steering_INST_INT_IRQN                              (TIMG6_INT_IRQn)
#define PWM_Steering_INST_CLK_FREQ                                         20000
/* GPIO defines for channel 1 */
#define GPIO_PWM_Steering_C1_PORT                                          GPIOA
#define GPIO_PWM_Steering_C1_PIN                                  DL_GPIO_PIN_30
#define GPIO_PWM_Steering_C1_IOMUX                                (IOMUX_PINCM5)
#define GPIO_PWM_Steering_C1_IOMUX_FUNC               IOMUX_PINCM5_PF_TIMG6_CCP1
#define GPIO_PWM_Steering_C1_IDX                             DL_TIMER_CC_1_INDEX



/* Defines for TIMER_0 */
#define TIMER_0_INST                                                     (TIMA0)
#define TIMER_0_INST_IRQHandler                                 TIMA0_IRQHandler
#define TIMER_0_INST_INT_IRQN                                   (TIMA0_INT_IRQn)
#define TIMER_0_INST_LOAD_VALUE                                            (99U)




/* Defines for I2C_0 */
#define I2C_0_INST                                                          I2C0
#define I2C_0_INST_IRQHandler                                    I2C0_IRQHandler
#define I2C_0_INST_INT_IRQN                                        I2C0_INT_IRQn
#define I2C_0_BUS_SPEED_HZ                                                400000
#define GPIO_I2C_0_SDA_PORT                                                GPIOA
#define GPIO_I2C_0_SDA_PIN                                         DL_GPIO_PIN_0
#define GPIO_I2C_0_IOMUX_SDA                                      (IOMUX_PINCM1)
#define GPIO_I2C_0_IOMUX_SDA_FUNC                       IOMUX_PINCM1_PF_I2C0_SDA
#define GPIO_I2C_0_SCL_PORT                                                GPIOA
#define GPIO_I2C_0_SCL_PIN                                         DL_GPIO_PIN_1
#define GPIO_I2C_0_IOMUX_SCL                                      (IOMUX_PINCM2)
#define GPIO_I2C_0_IOMUX_SCL_FUNC                       IOMUX_PINCM2_PF_I2C0_SCL


/* Defines for UART_DEBUG */
#define UART_DEBUG_INST                                                    UART0
#define UART_DEBUG_INST_FREQUENCY                                        4000000
#define UART_DEBUG_INST_IRQHandler                              UART0_IRQHandler
#define UART_DEBUG_INST_INT_IRQN                                  UART0_INT_IRQn
#define GPIO_UART_DEBUG_RX_PORT                                            GPIOA
#define GPIO_UART_DEBUG_TX_PORT                                            GPIOA
#define GPIO_UART_DEBUG_RX_PIN                                    DL_GPIO_PIN_11
#define GPIO_UART_DEBUG_TX_PIN                                    DL_GPIO_PIN_10
#define GPIO_UART_DEBUG_IOMUX_RX                                 (IOMUX_PINCM22)
#define GPIO_UART_DEBUG_IOMUX_TX                                 (IOMUX_PINCM21)
#define GPIO_UART_DEBUG_IOMUX_RX_FUNC                  IOMUX_PINCM22_PF_UART0_RX
#define GPIO_UART_DEBUG_IOMUX_TX_FUNC                  IOMUX_PINCM21_PF_UART0_TX
#define UART_DEBUG_BAUD_RATE                                            (115200)
#define UART_DEBUG_IBRD_4_MHZ_115200_BAUD                                    (2)
#define UART_DEBUG_FBRD_4_MHZ_115200_BAUD                                   (11)
/* Defines for UART_MS901M */
#define UART_MS901M_INST                                                   UART2
#define UART_MS901M_INST_FREQUENCY                                       4000000
#define UART_MS901M_INST_IRQHandler                             UART2_IRQHandler
#define UART_MS901M_INST_INT_IRQN                                 UART2_INT_IRQn
#define GPIO_UART_MS901M_RX_PORT                                           GPIOB
#define GPIO_UART_MS901M_TX_PORT                                           GPIOB
#define GPIO_UART_MS901M_RX_PIN                                   DL_GPIO_PIN_18
#define GPIO_UART_MS901M_TX_PIN                                   DL_GPIO_PIN_17
#define GPIO_UART_MS901M_IOMUX_RX                                (IOMUX_PINCM44)
#define GPIO_UART_MS901M_IOMUX_TX                                (IOMUX_PINCM43)
#define GPIO_UART_MS901M_IOMUX_RX_FUNC                 IOMUX_PINCM44_PF_UART2_RX
#define GPIO_UART_MS901M_IOMUX_TX_FUNC                 IOMUX_PINCM43_PF_UART2_TX
#define UART_MS901M_BAUD_RATE                                           (115200)
#define UART_MS901M_IBRD_4_MHZ_115200_BAUD                                   (2)
#define UART_MS901M_FBRD_4_MHZ_115200_BAUD                                  (11)
/* Defines for UART_TOF */
#define UART_TOF_INST                                                      UART1
#define UART_TOF_INST_FREQUENCY                                         40000000
#define UART_TOF_INST_IRQHandler                                UART1_IRQHandler
#define UART_TOF_INST_INT_IRQN                                    UART1_INT_IRQn
#define GPIO_UART_TOF_RX_PORT                                              GPIOA
#define GPIO_UART_TOF_TX_PORT                                              GPIOA
#define GPIO_UART_TOF_RX_PIN                                       DL_GPIO_PIN_9
#define GPIO_UART_TOF_TX_PIN                                      DL_GPIO_PIN_17
#define GPIO_UART_TOF_IOMUX_RX                                   (IOMUX_PINCM20)
#define GPIO_UART_TOF_IOMUX_TX                                   (IOMUX_PINCM39)
#define GPIO_UART_TOF_IOMUX_RX_FUNC                    IOMUX_PINCM20_PF_UART1_RX
#define GPIO_UART_TOF_IOMUX_TX_FUNC                    IOMUX_PINCM39_PF_UART1_TX
#define UART_TOF_BAUD_RATE                                              (921600)
#define UART_TOF_IBRD_40_MHZ_921600_BAUD                                     (2)
#define UART_TOF_FBRD_40_MHZ_921600_BAUD                                    (46)





/* Defines for ADC0_VOLTAGE */
#define ADC0_VOLTAGE_INST                                                   ADC0
#define ADC0_VOLTAGE_INST_IRQHandler                             ADC0_IRQHandler
#define ADC0_VOLTAGE_INST_INT_IRQN                               (ADC0_INT_IRQn)
#define ADC0_VOLTAGE_ADCMEM_ADC0_CH0                          DL_ADC12_MEM_IDX_0
#define ADC0_VOLTAGE_ADCMEM_ADC0_CH0_REF         DL_ADC12_REFERENCE_VOLTAGE_VDDA
#define ADC0_VOLTAGE_ADCMEM_ADC0_CH0_REF_VOLTAGE_V                                     3.3
#define GPIO_ADC0_VOLTAGE_C0_PORT                                          GPIOA
#define GPIO_ADC0_VOLTAGE_C0_PIN                                  DL_GPIO_PIN_27



/* Port definition for Pin Group LED1 */
#define LED1_PORT                                                        (GPIOA)

/* Defines for PIN_14: GPIOA.14 with pinCMx 36 on package pin 7 */
#define LED1_PIN_14_PIN                                         (DL_GPIO_PIN_14)
#define LED1_PIN_14_IOMUX                                        (IOMUX_PINCM36)
/* Port definition for Pin Group BEEP */
#define BEEP_PORT                                                        (GPIOB)

/* Defines for BEEP1: GPIOB.12 with pinCMx 29 on package pin 64 */
#define BEEP_BEEP1_PIN                                          (DL_GPIO_PIN_12)
#define BEEP_BEEP1_IOMUX                                         (IOMUX_PINCM29)
/* Port definition for Pin Group KEY */
#define KEY_PORT                                                         (GPIOA)

/* Defines for KEY1: GPIOA.28 with pinCMx 3 on package pin 35 */
#define KEY_KEY1_PIN                                            (DL_GPIO_PIN_28)
#define KEY_KEY1_IOMUX                                            (IOMUX_PINCM3)
/* Port definition for Pin Group OPENMV */
#define OPENMV_PORT                                                      (GPIOA)

/* Defines for OPENMV_0: GPIOA.24 with pinCMx 54 on package pin 25 */
#define OPENMV_OPENMV_0_PIN                                     (DL_GPIO_PIN_24)
#define OPENMV_OPENMV_0_IOMUX                                    (IOMUX_PINCM54)
/* Port definition for Pin Group LED0 */
#define LED0_PORT                                                        (GPIOB)

/* Defines for PIN_0: GPIOB.27 with pinCMx 58 on package pin 29 */
#define LED0_PIN_0_PIN                                          (DL_GPIO_PIN_27)
#define LED0_PIN_0_IOMUX                                         (IOMUX_PINCM58)
/* Defines for OA3: GPIOA.31 with pinCMx 6 on package pin 39 */
#define ENCODER_OA3_PORT                                                 (GPIOA)
// pins affected by this interrupt request:["OA3","OA4","OA1","OB1"]
#define ENCODER_GPIOA_INT_IRQN                                  (GPIOA_INT_IRQn)
#define ENCODER_GPIOA_INT_IIDX                  (DL_INTERRUPT_GROUP1_IIDX_GPIOA)
#define ENCODER_OA3_IIDX                                    (DL_GPIO_IIDX_DIO31)
#define ENCODER_OA3_PIN                                         (DL_GPIO_PIN_31)
#define ENCODER_OA3_IOMUX                                         (IOMUX_PINCM6)
/* Defines for OB3: GPIOB.13 with pinCMx 30 on package pin 1 */
#define ENCODER_OB3_PORT                                                 (GPIOB)
// pins affected by this interrupt request:["OB3","OB4","OA2","OB2"]
#define ENCODER_GPIOB_INT_IRQN                                  (GPIOB_INT_IRQn)
#define ENCODER_GPIOB_INT_IIDX                  (DL_INTERRUPT_GROUP1_IIDX_GPIOB)
#define ENCODER_OB3_IIDX                                    (DL_GPIO_IIDX_DIO13)
#define ENCODER_OB3_PIN                                         (DL_GPIO_PIN_13)
#define ENCODER_OB3_IOMUX                                        (IOMUX_PINCM30)
/* Defines for OA4: GPIOA.29 with pinCMx 4 on package pin 36 */
#define ENCODER_OA4_PORT                                                 (GPIOA)
#define ENCODER_OA4_IIDX                                    (DL_GPIO_IIDX_DIO29)
#define ENCODER_OA4_PIN                                         (DL_GPIO_PIN_29)
#define ENCODER_OA4_IOMUX                                         (IOMUX_PINCM4)
/* Defines for OB4: GPIOB.3 with pinCMx 16 on package pin 51 */
#define ENCODER_OB4_PORT                                                 (GPIOB)
#define ENCODER_OB4_IIDX                                     (DL_GPIO_IIDX_DIO3)
#define ENCODER_OB4_PIN                                          (DL_GPIO_PIN_3)
#define ENCODER_OB4_IOMUX                                        (IOMUX_PINCM16)
/* Defines for OA1: GPIOA.15 with pinCMx 37 on package pin 8 */
#define ENCODER_OA1_PORT                                                 (GPIOA)
#define ENCODER_OA1_IIDX                                    (DL_GPIO_IIDX_DIO15)
#define ENCODER_OA1_PIN                                         (DL_GPIO_PIN_15)
#define ENCODER_OA1_IOMUX                                        (IOMUX_PINCM37)
/* Defines for OB1: GPIOA.16 with pinCMx 38 on package pin 9 */
#define ENCODER_OB1_PORT                                                 (GPIOA)
#define ENCODER_OB1_IIDX                                    (DL_GPIO_IIDX_DIO16)
#define ENCODER_OB1_PIN                                         (DL_GPIO_PIN_16)
#define ENCODER_OB1_IOMUX                                        (IOMUX_PINCM38)
/* Defines for OA2: GPIOB.10 with pinCMx 27 on package pin 62 */
#define ENCODER_OA2_PORT                                                 (GPIOB)
#define ENCODER_OA2_IIDX                                    (DL_GPIO_IIDX_DIO10)
#define ENCODER_OA2_PIN                                         (DL_GPIO_PIN_10)
#define ENCODER_OA2_IOMUX                                        (IOMUX_PINCM27)
/* Defines for OB2: GPIOB.11 with pinCMx 28 on package pin 63 */
#define ENCODER_OB2_PORT                                                 (GPIOB)
#define ENCODER_OB2_IIDX                                    (DL_GPIO_IIDX_DIO11)
#define ENCODER_OB2_PIN                                         (DL_GPIO_PIN_11)
#define ENCODER_OB2_IOMUX                                        (IOMUX_PINCM28)
/* Port definition for Pin Group MOTOR */
#define MOTOR_PORT                                                       (GPIOB)

/* Defines for AIN3: GPIOB.22 with pinCMx 50 on package pin 21 */
#define MOTOR_AIN3_PIN                                          (DL_GPIO_PIN_22)
#define MOTOR_AIN3_IOMUX                                         (IOMUX_PINCM50)
/* Defines for AIN4: GPIOB.23 with pinCMx 51 on package pin 22 */
#define MOTOR_AIN4_PIN                                          (DL_GPIO_PIN_23)
#define MOTOR_AIN4_IOMUX                                         (IOMUX_PINCM51)
/* Defines for BIN3: GPIOB.25 with pinCMx 56 on package pin 27 */
#define MOTOR_BIN3_PIN                                          (DL_GPIO_PIN_25)
#define MOTOR_BIN3_IOMUX                                         (IOMUX_PINCM56)
/* Defines for BIN4: GPIOB.26 with pinCMx 57 on package pin 28 */
#define MOTOR_BIN4_PIN                                          (DL_GPIO_PIN_26)
#define MOTOR_BIN4_IOMUX                                         (IOMUX_PINCM57)
/* Defines for AIN1: GPIOB.6 with pinCMx 23 on package pin 58 */
#define MOTOR_AIN1_PIN                                           (DL_GPIO_PIN_6)
#define MOTOR_AIN1_IOMUX                                         (IOMUX_PINCM23)
/* Defines for AIN2: GPIOB.7 with pinCMx 24 on package pin 59 */
#define MOTOR_AIN2_PIN                                           (DL_GPIO_PIN_7)
#define MOTOR_AIN2_IOMUX                                         (IOMUX_PINCM24)
/* Defines for BIN1: GPIOB.8 with pinCMx 25 on package pin 60 */
#define MOTOR_BIN1_PIN                                           (DL_GPIO_PIN_8)
#define MOTOR_BIN1_IOMUX                                         (IOMUX_PINCM25)
/* Defines for BIN2: GPIOB.9 with pinCMx 26 on package pin 61 */
#define MOTOR_BIN2_PIN                                           (DL_GPIO_PIN_9)
#define MOTOR_BIN2_IOMUX                                         (IOMUX_PINCM26)
/* Port definition for Pin Group GYRO */
#define GYRO_PORT                                                        (GPIOA)

/* Defines for SDA: GPIOA.23 with pinCMx 53 on package pin 24 */
#define GYRO_SDA_PIN                                            (DL_GPIO_PIN_23)
#define GYRO_SDA_IOMUX                                           (IOMUX_PINCM53)
/* Defines for SCL: GPIOA.21 with pinCMx 46 on package pin 17 */
#define GYRO_SCL_PIN                                            (DL_GPIO_PIN_21)
#define GYRO_SCL_IOMUX                                           (IOMUX_PINCM46)
/* Defines for S1: GPIOA.22 with pinCMx 47 on package pin 18 */
#define TRACK_S1_PORT                                                    (GPIOA)
#define TRACK_S1_PIN                                            (DL_GPIO_PIN_22)
#define TRACK_S1_IOMUX                                           (IOMUX_PINCM47)
/* Defines for S2: GPIOA.12 with pinCMx 34 on package pin 5 */
#define TRACK_S2_PORT                                                    (GPIOA)
#define TRACK_S2_PIN                                            (DL_GPIO_PIN_12)
#define TRACK_S2_IOMUX                                           (IOMUX_PINCM34)
/* Defines for S3: GPIOA.13 with pinCMx 35 on package pin 6 */
#define TRACK_S3_PORT                                                    (GPIOA)
#define TRACK_S3_PIN                                            (DL_GPIO_PIN_13)
#define TRACK_S3_IOMUX                                           (IOMUX_PINCM35)
/* Defines for S4: GPIOB.5 with pinCMx 18 on package pin 53 */
#define TRACK_S4_PORT                                                    (GPIOB)
#define TRACK_S4_PIN                                             (DL_GPIO_PIN_5)
#define TRACK_S4_IOMUX                                           (IOMUX_PINCM18)
/* Defines for S5: GPIOA.18 with pinCMx 40 on package pin 11 */
#define TRACK_S5_PORT                                                    (GPIOA)
#define TRACK_S5_PIN                                            (DL_GPIO_PIN_18)
#define TRACK_S5_IOMUX                                           (IOMUX_PINCM40)
/* Defines for S6: GPIOB.19 with pinCMx 45 on package pin 16 */
#define TRACK_S6_PORT                                                    (GPIOB)
#define TRACK_S6_PIN                                            (DL_GPIO_PIN_19)
#define TRACK_S6_IOMUX                                           (IOMUX_PINCM45)
/* Defines for S7: GPIOA.8 with pinCMx 19 on package pin 54 */
#define TRACK_S7_PORT                                                    (GPIOA)
#define TRACK_S7_PIN                                             (DL_GPIO_PIN_8)
#define TRACK_S7_IOMUX                                           (IOMUX_PINCM19)
/* Defines for S8: GPIOA.7 with pinCMx 14 on package pin 49 */
#define TRACK_S8_PORT                                                    (GPIOA)
#define TRACK_S8_PIN                                             (DL_GPIO_PIN_7)
#define TRACK_S8_IOMUX                                           (IOMUX_PINCM14)



/* clang-format on */

void SYSCFG_DL_init(void);
void SYSCFG_DL_initPower(void);
void SYSCFG_DL_GPIO_init(void);
void SYSCFG_DL_SYSCTL_init(void);
void SYSCFG_DL_PWM_MOTOR_RearSide_init(void);
void SYSCFG_DL_PWM_MOTOR_FrontSide_init(void);
void SYSCFG_DL_PWM_Steering_init(void);
void SYSCFG_DL_TIMER_0_init(void);
void SYSCFG_DL_I2C_0_init(void);
void SYSCFG_DL_UART_DEBUG_init(void);
void SYSCFG_DL_UART_MS901M_init(void);
void SYSCFG_DL_UART_TOF_init(void);
void SYSCFG_DL_ADC0_VOLTAGE_init(void);

void SYSCFG_DL_SYSTICK_init(void);

bool SYSCFG_DL_saveConfiguration(void);
bool SYSCFG_DL_restoreConfiguration(void);

#ifdef __cplusplus
}
#endif

#endif /* ti_msp_dl_config_h */
