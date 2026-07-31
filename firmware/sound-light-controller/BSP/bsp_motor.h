#ifndef	__BSP_MOTOR_H__
#define __BSP_MOTOR_H__

#include "board.h"

#define ABS(a)      (a>0 ? a:(-a))


extern volatile int32_t g_total_pulse_rl;
extern volatile int32_t g_total_pulse_rr;




void motor_init(void);
void Motor_PwmSet_FL(int Pwm_FrontLeft);
void Motor_PwmSet_FR(int Pwm_FrontRight);
void Motor_PwmSet_RL(int Pwm_RearLeft);
void Motor_PwmSet_RR(int Pwm_RearRight);

void Motor_Stop();
void Motor_Kinematic_Analysis(float velocity, float turn);
void motor_pid_init(void);

float get_forward_distance_cm(void);

#endif
