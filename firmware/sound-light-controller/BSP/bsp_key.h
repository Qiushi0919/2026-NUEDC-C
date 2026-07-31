#ifndef __BSP_KEY_H__
#define __BSP_KEY_H__

#include "board.h"


typedef struct
{
    GPIO_Regs* port; //GPIO
    uint32_t pin; //GPIO PIN
    void(*keyshortpress_fun)(); //短按回调函数
    void(*keylongpress_fun)(); //长按回调函数
} keyPortStructdef;

typedef struct
{
    uint32_t state; //按键IO状态
    uint8_t mask; //按键位移消抖变量
    uint8_t press; //按键按下或者释放状态
    uint32_t startTime; // 按键按下的起始时间戳
    uint8_t iscontinuousLongpress; //是否连续响应长按
    keyPortStructdef* keyPortPdu; //按键IO对象
} keystateStructdef;

enum //响应状态枚举变量(连续或者单次)
{
    SINGLE_RESPONSE,
    CONTINUOUS_RESPONSE,
};

/**
    NONE                无按键
    PRESS               按下
    RELEASE             释放
    SHORTPRESS          短按
    LONGPRESS           长按
**/
#define NONE            0
#define PRESS           1
#define RELEASE         2
#define SHORTPRESS      3
#define LONGPRESS       4

#define LONGPRESS_TIME  500 // 长按时间，单位：毫秒

extern void Key_Scan();
static void Key_SW1_ShortPress();
static void Key_SW1_LongPress();

#endif