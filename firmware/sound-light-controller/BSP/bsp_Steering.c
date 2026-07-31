#include "bsp_Steering.h"
#include "stdio.h"


unsigned int Servo_Angle = 0;//舵机角度

/******************************************************************
   配置占空比 范围 0 ~ (per-1)
   t = 0.5ms-------------------舵机会转动 0 °
   t = 1.0ms-------------------舵机会转动 45°
   t = 1.5ms-------------------舵机会转动 90°
   t = 2.0ms-------------------舵机会转动 135°
   t = 2.5ms-------------------舵机会转动 180°
******************************************************************/

/******************************************************************
 * 函 数 名 称：Steering_SetAngle
 * 函 数 说 明：设置角度
 * 函 数 形 参：angle=要设置的角度，范围0-180
 * 函 数 返 回：无
******************************************************************/
void Steering_SetAngle(unsigned int angle)
{
    uint32_t period = 400;

    if(angle > 180)
    {
        angle = 180; // 限制角度在0到180度之间
    }

    Servo_Angle = angle;

    // 计算PWM占空比
    // 0.5ms对应的计数 = 10
    // 2.5ms对应的计数 = 50
    float min_count = 10.0f;
    float max_count = 50.0f;
    float range = max_count - min_count;
    float ServoAngle = min_count + (((float)angle / 180.0f) * range);

    DL_TimerG_setCaptureCompareValue(PWM_Steering_INST, (unsigned int)(ServoAngle + 0.5f), GPIO_PWM_Steering_C1_IDX);
}

/******************************************************************
 * 函 数 名 称：读取当前角度
 * 函 数 说 明：Steering_GetAngle
 * 函 数 形 参：无
 * 函 数 返 回：当前角度
 * 备       注：使用前必须确保之前使用过
                void Steering_SetAngle(unsigned int angle)
                函数设置过角度
******************************************************************/
unsigned int Steering_GetAngle(void)
{
    return Servo_Angle;
}

float Steering_AngleDiff(float a, float b)
{
    float diff = b - a;

    // 把差值规范到 [-180, 180] 范围
    while (diff > 180.0f)
    {
        diff -= 360.0f;
    }
    while (diff < -180.0f)
    {
        diff += 360.0f;
    }

    return fabsf(diff);  // 返回差值的绝对值
}