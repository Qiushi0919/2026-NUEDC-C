#include "bsp_track.h"
#include "board.h"
#include "stdio.h"
#include "bsp_motor.h"

// PID 控制参数
float Kp = 30, Ki = 0, Kd = 1.5;
float P = 0, I = 0, D = 0, PID_value = 0;
float new_error = 0, previous_error = 0;
static int initial_motor_speed = 300;
int line_detected = 0;
bool pass_point_flag = false;
int loss_line_count = 0;
bool line_flag = false;
void track_init(void)
{
    printf("Track initialized OK\r\n");
}
int count_line=0;

uint8_t track_get_black_sensor_count(void)
{
    uint8_t detected_count = 0;
    uint32_t sensor_values[8] = {S1, S2, S3, S4, S5, S6, S7, S8};

    for (uint8_t i = 0; i < 8; i++)
    {
        if (sensor_values[i] > 0)
        {
            detected_count++;
        }
    }

    return detected_count;
}

void track_reset_pid(void)
{
    P = 0;
    I = 0;
    D = 0;
    PID_value = 0;
    new_error = 0;
    previous_error = 0;
}

// 读取传感器状态并计算误差

//巡线摆动改这个
//float error_weight[8]  = {-2.3, -1.7, -1.2, -0.35, 0.35, 1.2, 1.7, 2.3};//空载
float error_weight[8]  = {-4, -3, -2.0, -0.5, 0.5, 2.0, 3, 4}; //带载
//原来4.5是4
int track_scan(void)
{
    int sensor_values[8] = {S1, S2, S3, S4, S5, S6, S7, S8};
    int detected_count = 0;
    float total_error = 0;

    for (int i = 0; i < 8; i++)
    {
        if (sensor_values[i] > 0)
        {
            total_error += error_weight[i]; 
            detected_count++;
        }
    }
		
		if(detected_count != 0)//have line
		{
			if(count_line < 6)//识别到线后继续转的时间(巡线20ms扫描一次)
			{
				count_line++;
			}
			else if(line_flag!=true && (sensor_values[3] > 0 || sensor_values[4] > 0))
			{
				line_flag = true;
			}
		}
		else
		{
			count_line = 0;
		}
		
		//巡线20ms扫描一次,速度太快可能来不及识别路口
		if (detected_count > 3 || detected_count == 0)//识别到路口(4个灯灭)或没有线
    {
        pass_point_flag = true;
    }
    else
    {
//			if(sensor_values[8] != 0)
//			{
        pass_point_flag = false;
//			}
    }

    if (detected_count > 0)
    {
        new_error = total_error / detected_count;
        //line_detected = 1;
    }
    else
    {
        new_error = previous_error;
//        line_detected = 0;
    }

    return 0;
}

// 计算 PID
int track_pid(float m_speed)
{
    float final_pidout, m_calibration;

    P = new_error;
    I += new_error;
    D = new_error - previous_error;

    // 积分限幅
    if (I > 30) I = 30;
    if (I < -30) I = -30;

    // 调整 PID 参数（更适合高速）
    float Kp_dyn = 25.0f;
    float Ki_dyn = 0.0f;
    float Kd_dyn = 3.5f;  // 增强微分抑制速度快时的抖动

    PID_value = (Kp_dyn * P) + (Ki_dyn * I) + (Kd_dyn * D);
		PID_value *= 1.4;

    previous_error = new_error;

    // 校准因子与速度成比例缩放（但不易太小）
    m_calibration = 150.0f / (m_speed + 1);  // 避免除以 0

    final_pidout = -PID_value / m_calibration;

    return final_pidout;
}


// 电机运动
void track_motorsWrite(int speedL, int speedR)
{
    Motor_PwmSet_RL(speedL);
    Motor_PwmSet_RR(speedR);
}

// 设置 PWM 输出
void track_Set_PWM(int pwm_value)
{
    static float last_pwm = 0;
    float filtered_pwm = (last_pwm * 0.7f) + (pwm_value * 0.3f);
    last_pwm = filtered_pwm;

    // 优化校正因子：误差不应直接放大调节力度
    float correction_factor = 1.0f + 0.2f * ABS(new_error);  // 降低灵敏度

    // 高速时适当降速避免过冲
    int base_speed = 280 - (int)(ABS(new_error) * 35);
    if (base_speed < 160) base_speed = 160;

    int left_motor_speed  = base_speed + (int)(correction_factor * filtered_pwm);
    int right_motor_speed = base_speed - (int)(correction_factor * filtered_pwm);

    // 限幅保护
    if (left_motor_speed > Track_SPEED) left_motor_speed = Track_SPEED;
    else if (left_motor_speed < -Track_SPEED) left_motor_speed = -Track_SPEED;

    if (right_motor_speed > Track_SPEED) right_motor_speed = Track_SPEED;
    else if (right_motor_speed < -Track_SPEED) right_motor_speed = -Track_SPEED;

	if(Velocity>=0)
		track_motorsWrite(left_motor_speed, right_motor_speed);
	else
		track_motorsWrite(right_motor_speed, left_motor_speed);
}


// 主控函数，带二次确认
int track_control(void)
{
    track_scan();

    // 如果第一次没检测到线，稍作延时后再检测一次
    if (line_detected == 0)
    {
        // delay_ms(10);       // 可根据具体情况调整时间
        // track_scan();
    }

    return line_detected;
}
