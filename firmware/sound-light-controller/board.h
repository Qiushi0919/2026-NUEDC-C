#ifndef	__BOARD_H__
#define __BOARD_H__

#include "ti_msp_dl_config.h"
#include <stdint.h>
#include <stdio.h>      // 添加标准IO头文件
#include <stdarg.h>     // 解决 va_list 问题
#include <string.h>     // 解决 strlen/memmove/memcpy 问题
#include "math.h"
#include "ringbuffer.h"
#include "bsp_motor.h"
#include "bsp_pid.h"
#include "bsp_gyro.h"
#include "bsp_track.h"
#include "bsp_oled.h"
#include "bsp_Steering.h"
#include "bsp_gpio.h"
#include "bsp_key.h"
#include "bsp_ms901m.h"
#include "iic.h"
#include "uart.h"
#include "bsp_adc.h"
#include "bsp_tof.h"
#include "task.h"


#define MCU_VERSION     "2024_EDC_H_AutoCar v0.6"


// ------------------- 增加的宏和变量 -------------------
#define WHEEL_DIAMETER_CM       6.5f     // 轮子直径，单位：cm
#define GEAR_RATIO              20       // 电机减速比
#define ENCODER_PPR             11       // 电机编码器每圈脉冲数
#define PI                      3.14159f

// 计算 1cm 对应的脉冲数
// #define CM_TO_PULSE (ENCODER_PPR * GEAR_RATIO / (PI * WHEEL_DIAMETER_CM))
// 根据实测校准后的值
#define CM_TO_PULSE (10.62f)

#define DIST_TARGET_CM          100.0f   // 目标前进距离
#define DIST_REACH_TOLERANCE    3.0f     // 容差,单位cm

extern float g_f32TargetDistCM;






#define TWO_WHEELS		1	//三轮车
#define AKM_WHEELS		0	//阿克曼车
#define FOUR_WHEELS		0	//四轮车

#define Angle_SPEED  500
#define Track_SPEED  280


extern float Turn;
extern int Velocity;
extern uint8_t Do_TASK;
extern uint8_t key_Confirm,key_num,Set_Num;
extern int Key_Set_Yaw;
extern uint8_t YAW_adjust;
extern uint8_t YAW_adjust_ok;
extern volatile float g_f32Current_YawDeg;
extern float s_f32Yaw_Init;
extern float s_f32Current_yaw;
extern float s_f32Base_Speed;
extern bool pass_point_flag,line_flag;

void board_init(void);
void delay_us(unsigned long __us);
void delay_ms(unsigned long ms);

#endif
