#include "board.h"
#include <stdio.h>      // 添加标准IO头文件
#include <stdarg.h>     // 解决 va_list 问题
#include <string.h>     // 解决 strlen/memmove/memcpy 问题
#include <uart.h> 

void board_init(void)
{
    //printf("%s\r\n",MCU_VERSION);
    SYSCFG_DL_init();
    DIP_Output_Init();       // PA18 LED1, PB19 LED2, PA8 LED3, PA27 BEEP
    // 定时器中断
    NVIC_ClearPendingIRQ(TIMER_0_INST_INT_IRQN);
    NVIC_EnableIRQ(TIMER_0_INST_INT_IRQN);
    //printf("SYSCFG_DL_init\r\n");

		OpenMV_Off();
	
    uart_init();            //带有陀螺仪的串口需要最先初始化，否则可能会导致后面收数据和解析数据出问题吧！！！
    TOF_Init();             //PA17 TX、PA9 RX，921600 8N1
		motor_pid_init();				//电机控制模式初始化
//    adc_init();
    delay_ms(1000);          //这里要加上延时,不然会导致oled不显示
    OLED_Init();
    motor_init();           //开启电机编码器引脚外部中断
    track_init();           //开启循迹模块初始化，该函数没有作用，只是提醒工程师有配置循迹模块
//    delay_ms(500);
}

//搭配滴答定时器实现的精确us延时
void delay_us(unsigned long __us)
{
    uint32_t ticks;
    uint32_t told, tnow, tcnt = 38;

    // 计算需要的时钟数 = 延迟微秒数 * 每微秒的时钟数
    ticks = __us * (80000000 / 1000000);

    // 获取当前的SysTick值
    told = SysTick->VAL;

    while (1)
    {
        // 重复刷新获取当前的SysTick值
        tnow = SysTick->VAL;

        if (tnow != told)
        {
            if (tnow < told)
            {
                tcnt += told - tnow;
            }
            else
            {
                tcnt += SysTick->LOAD - tnow + told;
            }

            told = tnow;

            // 如果达到了需要的时钟数，就退出循环
            if (tcnt >= ticks)
            {
                break;
            }
        }
    }
}

//搭配滴答定时器实现的精确ms延时
void delay_ms(unsigned long ms)
{
    delay_us(ms * 1000);
}
