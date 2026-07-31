#include "bsp_motor.h"

volatile int32_t Encode_CNT_FL, Encode_CNT_FR, Encode_CNT_RL, Encode_CNT_RR;

volatile int32_t g_total_pulse_rl = 0;
volatile int32_t g_total_pulse_rr = 0;



void motor_init(void)
{
	// 编码器引脚外部中断
	NVIC_EnableIRQ(ENCODER_GPIOA_INT_IRQN);
	NVIC_EnableIRQ(ENCODER_GPIOB_INT_IRQN);
	// 定时器中断
	NVIC_ClearPendingIRQ(TIMER_0_INST_INT_IRQN);
	NVIC_EnableIRQ(TIMER_0_INST_INT_IRQN);
	printf("Motor Init OK\r\n");
}

//float Velcity_Kp = 3, Velcity_Ki = 4.5, Velcity_Kd = 0; //空载
float Velcity_Kp = 16, Velcity_Ki = 12, Velcity_Kd = 0; //带载
void motor_pid_init(void)
{
	pid_init(&motor_FL_pid, Velcity_Kp, Velcity_Ki, Velcity_Kd, 400, -400);
	pid_init(&motor_FR_pid, Velcity_Kp, Velcity_Ki, Velcity_Kd, 400, -400);
	pid_init(&motor_RL_pid, Velcity_Kp, Velcity_Ki, Velcity_Kd, 400, -400);
	pid_init(&motor_RR_pid, Velcity_Kp, Velcity_Ki, Velcity_Kd, 400, -400);
	pid_set_status(&motor_FL_pid, PID_ENABLE);
	pid_set_status(&motor_FR_pid, PID_ENABLE);
	pid_set_status(&motor_RL_pid, PID_ENABLE);
	pid_set_status(&motor_RR_pid, PID_ENABLE);
	pid_set_mode(&motor_FL_pid, MOTOR_SPEED);
	pid_set_mode(&motor_FR_pid, MOTOR_SPEED);
	pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
	pid_set_mode(&motor_RR_pid, MOTOR_SPEED);
}

// 前左电机PWM控制函数（Allman风格）
void Motor_PwmSet_FL(int Pwm_FrontLeft)
{
    // 静态变量记录电机方向状态
    static int last_dir_FL = 0;
    // 前左电机方向控制
    int current_dir_FL = (Pwm_FrontLeft > 0) ? 1 : -1;
    if (current_dir_FL != last_dir_FL)
    {
        if (Pwm_FrontLeft > 0)
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN2_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_AIN1_PIN);
        }
        else
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN1_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_AIN2_PIN);
        }
        last_dir_FL = current_dir_FL;
    }
    DL_TimerG_setCaptureCompareValue(PWM_MOTOR_FrontSide_INST, ABS(Pwm_FrontLeft), GPIO_PWM_MOTOR_FrontSide_C0_IDX);
}

// 前右电机PWM控制函数（Allman风格）
void Motor_PwmSet_FR(int Pwm_FrontRight)
{
    // 静态变量记录电机方向状态
    static int last_dir_FR = 0;
    // 前右电机方向控制
    int current_dir_FR = (Pwm_FrontRight > 0) ? 1 : -1;
    if (current_dir_FR != last_dir_FR)
    {
        if (Pwm_FrontRight > 0)
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN1_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_BIN2_PIN);
        }
        else
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN2_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_BIN1_PIN);
        }
        last_dir_FR = current_dir_FR;
    }
    DL_TimerG_setCaptureCompareValue(PWM_MOTOR_FrontSide_INST, ABS(Pwm_FrontRight), GPIO_PWM_MOTOR_FrontSide_C1_IDX);
}

// 后左电机PWM控制函数（Allman风格）
void Motor_PwmSet_RL(int Pwm_RearLeft)
{
    // 静态变量记录电机方向状态
    static int last_dir_RL = 0;
    // 后左电机方向控制
    int current_dir_RL = (Pwm_RearLeft > 0) ? 1 : -1;
    if (current_dir_RL != last_dir_RL)
    {
        if (Pwm_RearLeft > 0)
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN3_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_AIN4_PIN);
        }
        else
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN4_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_AIN3_PIN);
        }
        last_dir_RL = current_dir_RL;
    }
    DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST, ABS(Pwm_RearLeft), GPIO_PWM_MOTOR_RearSide_C0_IDX);
}

// 后右电机PWM控制函数（Allman风格）
void Motor_PwmSet_RR(int Pwm_RearRight)
{
    // 静态变量记录电机方向状态
    static int last_dir_RR = 0;
    // 后右电机方向控制
    int current_dir_RR = (Pwm_RearRight > 0) ? 1 : -1;
    if (current_dir_RR != last_dir_RR)
    {
        if (Pwm_RearRight > 0)
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN3_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_BIN4_PIN);
        }
        else
        {
            DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN4_PIN);
            DL_GPIO_clearPins(MOTOR_PORT, MOTOR_BIN3_PIN);
        }
        last_dir_RR = current_dir_RR;
    }
    DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST, ABS(Pwm_RearRight), GPIO_PWM_MOTOR_RearSide_C1_IDX);
}


void Motor_Stop()
{
	// 前左电机停止
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN1_PIN);
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN2_PIN);
	DL_TimerG_setCaptureCompareValue(PWM_MOTOR_FrontSide_INST, 0, GPIO_PWM_MOTOR_FrontSide_C0_IDX);

	// 前右电机停止
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN1_PIN);
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN2_PIN);
	DL_TimerG_setCaptureCompareValue(PWM_MOTOR_FrontSide_INST, 0, GPIO_PWM_MOTOR_FrontSide_C1_IDX);

	// 后左电机停止
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN3_PIN);
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_AIN4_PIN);
	DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST, 0, GPIO_PWM_MOTOR_RearSide_C0_IDX);

	// 后右电机停止
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN3_PIN);
	DL_GPIO_setPins(MOTOR_PORT, MOTOR_BIN4_PIN);
	DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST, 0, GPIO_PWM_MOTOR_RearSide_C1_IDX);
}

/*******************************************************
函数功能：外部中断模拟编码器信号
入口函数：无
返回  值：无
***********************************************************/
void GROUP1_IRQHandler(void)
{
    // 读取触发的 GPIOA 和 GPIOB 中断状态
    uint32_t PortA_interrup = DL_GPIO_getRawInterruptStatus(GPIOA, ENCODER_OB1_PIN | ENCODER_OA4_PIN);
    uint32_t PortB_interrup = DL_GPIO_getRawInterruptStatus(GPIOB, ENCODER_OB2_PIN | ENCODER_OB3_PIN);

    // 处理 Front 左轮（OB1 触发中断）
    if (PortA_interrup & ENCODER_OB1_PIN)
    {
        if (DL_GPIO_readPins(ENCODER_OA1_PORT, ENCODER_OA1_PIN) & ENCODER_OA1_PIN)
            Encode_CNT_FL++;  // 正转
        else
            Encode_CNT_FL--;  // 反转
    }

    // 处理 Front 右轮（OB2 触发中断）
    if (PortB_interrup & ENCODER_OB2_PIN)
    {
        if (DL_GPIO_readPins(ENCODER_OA2_PORT, ENCODER_OA2_PIN) & ENCODER_OA2_PIN)
            Encode_CNT_FR++;  // 正转
        else
            Encode_CNT_FR--;  // 反转
    }

    // 处理 Rear 左轮（OB3 触发中断）
    if (PortB_interrup & ENCODER_OB3_PIN)
    {
        if (DL_GPIO_readPins(ENCODER_OA3_PORT, ENCODER_OA3_PIN) & ENCODER_OA3_PIN)
            Encode_CNT_RL++;  // 正转
        else
            Encode_CNT_RL--;  // 反转
    }

    // 处理 Rear 右轮（OA4 触发中断）
    if (PortA_interrup & ENCODER_OA4_PIN)
    {
        if (DL_GPIO_readPins(ENCODER_OB4_PORT, ENCODER_OB4_PIN) & ENCODER_OB4_PIN)
            Encode_CNT_RR--;  // 正转
        else
            Encode_CNT_RR++;  // 反转
    }

    // 清除已触发的中断
    DL_GPIO_clearInterruptStatus(GPIOA, PortA_interrup & (ENCODER_OB1_PIN | ENCODER_OA4_PIN));
    DL_GPIO_clearInterruptStatus(GPIOB, PortB_interrup & (ENCODER_OB2_PIN | ENCODER_OB3_PIN));
}

void Motor_Kinematic_Analysis(float velocity, float turn)
{
	#if AKM_WHEELS
		float m_angle;
		m_angle = 2 * Pi * turn / 360;
        motor_RL_pid.target_speed = velocity * (1 - T * tan(m_angle) * K_1 / 2 / L);
        motor_RR_pid.target_speed = velocity * (1 + T * tan(m_angle) * K_1 / 2 / L);
		// SetServoAngle(turn); 

	#elif TWO_WHEELS
		motor_RL_pid.target_speed = velocity - turn;
		motor_RR_pid.target_speed = velocity + turn;
	
	#elif FOUR_WHEELS
		motor_RR_pid.target_speed  = velocity + turn;
		motor_RL_pid.target_speed = velocity - turn;
		motor_FR_pid.target_speed = velocity + turn;
		motor_FL_pid.target_speed = velocity - turn;
		
	#endif
}

// ------------------- 计算当前累计行进距离 -------------------
float get_forward_distance_cm(void)
{
    float pulse_avg = (g_total_pulse_rl + g_total_pulse_rr) / 2.0f;
	if(pulse_avg<0)
	{
		pulse_avg = -pulse_avg;
	}
	
	
    return pulse_avg / CM_TO_PULSE;
}
