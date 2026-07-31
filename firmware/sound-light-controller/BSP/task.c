#include "board.h"
#include "stdio.h"

uint8_t Do_TASK=0;                 // 当前执行的任务编号
uint8_t YAW_adjust;              // 标志：是否处于角度矫正状态
uint8_t YAW_adjust_ok;           // 标志：角度矫正是否完成（保留未用）
static uint8_t track_confirm_cnt = 0; // 二次确认变量


#define TRACK_CONFIRM_TIMES         2
#define TASK0_MIN_DISTANCE_CM       100.0f
#define TASK0_FINISH_BLACK_COUNT    5U
#define TASK0_TRACK_SPEED           7
#define TASK0_DECEL_TIME_MS         1500U
#define TASK0_DISTANCE_DISPLAY_MS   100U
#define TASK0_DISTANCE_OLED_LINE    1U

static void task0_display_distance(float distance_cm)
{
    char display_text[18];
    uint32_t distance_tenth_cm;

    if (distance_cm < 0.0f)
    {
        distance_cm = 0.0f;
    }

    distance_tenth_cm = (uint32_t)(distance_cm * 10.0f + 0.5f);
    snprintf(display_text, sizeof(display_text), "DIST:%4lu.%1lucm  ",
        (unsigned long)(distance_tenth_cm / 10U),
        (unsigned long)(distance_tenth_cm % 10U));
    OLED_ShowString(0U, TASK0_DISTANCE_OLED_LINE, (uint8_t *)display_text);
}

static float speed_fabs(float val)
{
	float m_val;
	m_val = val;
	if(val<0)
	{
		m_val = -val;
	}
	return m_val;
}

//=========================== task_0 绕环一圈并停在 A 点 ===========================//
void task_0(void)
{
    static bool task_finished = false;
    bool decelerating = false;
    float distance_cm;
    uint8_t black_sensor_count;
    uint64_t decel_start_ms = 0;
    uint64_t decel_elapsed_ms;
    uint64_t decel_remaining_ms;
    uint64_t display_last_ms = 0;
    uint64_t now_ms;

    if (task_finished)
    {
        Velocity = 0;
        Motor_Stop();
        return;
    }

    g_total_pulse_rl = 0;
    g_total_pulse_rr = 0;
    track_confirm_cnt = 0;
    track_reset_pid();

    pid_set_mode(&motor_RL_pid, MOTOR_TRACK);
    pid_set_mode(&motor_RR_pid, MOTOR_TRACK);
    Velocity = TASK0_TRACK_SPEED;
    Motor_Kinematic_Analysis(Velocity, 0);

    while (!task_finished)
    {
        distance_cm = get_forward_distance_cm();

        if (!decelerating)
        {
            black_sensor_count = track_get_black_sensor_count();

            if ((distance_cm > TASK0_MIN_DISTANCE_CM) &&
                (black_sensor_count >= TASK0_FINISH_BLACK_COUNT))
            {
                if (++track_confirm_cnt >= TRACK_CONFIRM_TIMES)
                {
                    decelerating = true;
                    decel_start_ms = HAL_GetTick();
                    track_confirm_cnt = 0;
                }
            }
            else
            {
                track_confirm_cnt = 0;
            }
        }
        else
        {
            decel_elapsed_ms = HAL_GetTick() - decel_start_ms;
            if (decel_elapsed_ms >= TASK0_DECEL_TIME_MS)
            {
                Velocity = 0;
                task_finished = true;
                break;
            }

            decel_remaining_ms = TASK0_DECEL_TIME_MS - decel_elapsed_ms;
            Velocity = (int)(((uint64_t)TASK0_TRACK_SPEED * decel_remaining_ms +
                TASK0_DECEL_TIME_MS - 1U) / TASK0_DECEL_TIME_MS);
        }

        now_ms = HAL_GetTick();
        if ((now_ms - display_last_ms) >= TASK0_DISTANCE_DISPLAY_MS)
        {
            display_last_ms = now_ms;
            task0_display_distance(distance_cm);
        }

        DIP_Task();
        Bluetooth_Task();
        TOF_Task();
        delay_ms(10);
    }

    task0_display_distance(get_forward_distance_cm());
    Velocity = 0;
    motor_RL_pid.target_speed = 0;
    motor_RR_pid.target_speed = 0;
    motor_RL_pid.output = 0;
    motor_RR_pid.output = 0;
    pid_set_mode(&motor_RL_pid, MOTOR_STOP);
    pid_set_mode(&motor_RR_pid, MOTOR_STOP);
    Motor_Stop();
}
int pass_point;	//路过的转角数量,一圈4个
char chrbuf[20] = {0};
//=========================== task_1 任务一 ===========================//
void task_1()
{
		int m_point;
		m_point = Set_Num * 4;

    sprintf(chrbuf, "pass_point:%d   ", (int)pass_point);
    OLED_ShowString(0, 5, (unsigned char*)chrbuf);
    if(m_point > pass_point)
    {
			switch (Do_TASK)
			{
					//***A-B***//
					case 0:
					{
							pid_set_mode(&motor_RL_pid, MOTOR_TRACK);
							pid_set_mode(&motor_RR_pid, MOTOR_TRACK);
							Velocity = 5;
							if(pass_point_flag == true)
							{
								Do_TASK = 1;
								pass_point++;
								pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
								pid_set_mode(&motor_RR_pid, MOTOR_SPEED);

								motor_RL_pid.target_speed = 5;
								motor_RR_pid.target_speed = 5;
								delay_ms(400);			//识别90度弯后前进不够改这里
							
								motor_RL_pid.target_speed = -1.5;
								motor_RR_pid.target_speed = 1.5;
								line_flag=false;
							}
					}
					break;
					
					//***A-B***//
					case 1:
					{
							if(line_flag==true)
							{
								motor_RL_pid.target_speed = 0;
								motor_RR_pid.target_speed = 0;
								pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
								pid_set_mode(&motor_RR_pid, MOTOR_SPEED);								

								if(speed_fabs(motor_RL_pid.measure_speed)<0.10&&speed_fabs(motor_RR_pid.measure_speed)<0.10)
								{

									Do_TASK=0;
								}
							}
					}
					break;
			}
    }
    else
    {
        Velocity = 0;   Turn = 0;

				motor_RL_pid.target_speed = 0;
				motor_RR_pid.target_speed = 0;
    }
}

//=========================== task_2 ===========================//
void task_2()
{
	Velocity = 0;
	delay_ms(200);
	char SendToOpenMVBuf[8]={};//设置OpenMV的任务  *任务&
	
	
	
	for (int i = 0; i <= 5; i++)	//将TASK和TargetNum打包一次性发送给openmv
	{ 
		sprintf((char *)SendToOpenMVBuf, "*111&");
		uart1_send_data((uint8_t*)SendToOpenMVBuf,5);
		
		delay_ms(200);
//		Print_Data_To_Uart1();
	}
}

//=========================== task_3 ===========================//
bool set_yaw_flag = false;	//设置角度标志位
int set_yaw = 0;						//设置的角度
bool send_flag = false;			//发送角度标志位
void task_3()
{
	char SendToOpenMVBuf[8]={'*','0',0x00,0xB4,'&'};//设置OpenMV的任务  * 任务 角度高位 角度低位 &
		
	if(set_yaw_flag == false)
	{
		set_yaw_flag = true;
		set_yaw = (int)Key_Set_Yaw;
		sprintf(chrbuf, "set_yaw:%3d", (int)set_yaw);
		OLED_ShowString(0, 7, (unsigned char *)chrbuf);
	}

//	if(send_flag == false)
//	{
		SendToOpenMVBuf[2] = (set_yaw >> 8) & 0xFF;  // 高8位
		SendToOpenMVBuf[3] = set_yaw & 0xFF;          // 低8位
		
//		SendToOpenMVBuf[1] = '0';
//		uart1_send_data(SendToOpenMVBuf, 8);
	for (int i = 0; i <= 5; i++)	//将TASK和TargetNum打包一次性发送给openmv
	{ 
		SendToOpenMVBuf[1] = '2';
		uart1_send_data((uint8_t*)SendToOpenMVBuf,5);
		
		delay_ms(200);
	}
}




void task_4()
{
		int m_point;
		m_point = Set_Num * 4 + 1;

    sprintf(chrbuf, "pass_point:%d   ", (int)pass_point);
    OLED_ShowString(0, 5, (unsigned char*)chrbuf);
	
    if(m_point > pass_point)
    {
			switch (Do_TASK)
			{
					//***A-B***//
					case 0:
					{

							motor_RL_pid.ki = 6;
							motor_RR_pid.ki = 6;
						
							pid_set_mode(&motor_RL_pid, MOTOR_TRACK);
							pid_set_mode(&motor_RR_pid, MOTOR_TRACK);
							if(pass_point % 2 == 1)
							{
									Velocity = 6;
							}
							else
							{
									Velocity = 3;
							}
							if(pass_point_flag == true)
							{		
								Do_TASK = 1;
								pass_point++;
								pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
								pid_set_mode(&motor_RR_pid, MOTOR_SPEED);
								
								//在最后一部不会走歪
								motor_RL_pid.target_speed = 2;
								motor_RR_pid.target_speed = 2;
								delay_ms(700);			//识别90度弯后前进不够改这里

//							
								motor_RL_pid.target_speed = -1;
								motor_RR_pid.target_speed = 1;
								//delay_ms(2300);
								//delay_ms(200);
								line_flag = false;
							}
					}
					break;
					
					//***A-B***//
					case 1:
					{

							if(line_flag==true)
							{
								motor_RL_pid.target_speed = 0;
								motor_RR_pid.target_speed = 0;
								pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
								pid_set_mode(&motor_RR_pid, MOTOR_SPEED);								
								delay_ms(500);
								if(speed_fabs(motor_RL_pid.measure_speed)<0.10&&speed_fabs(motor_RR_pid.measure_speed)<0.10)
								{
									motor_RL_pid.output = 0;
									motor_RR_pid.output = 0;
									Do_TASK=0;
								}
							}
							else
							{
									motor_RL_pid.target_speed = 0;
									motor_RR_pid.target_speed = 0;
									delay_ms(10);
									motor_RL_pid.target_speed = -1;
									motor_RR_pid.target_speed = 1;
							}
					}
					break;
			}
    }
    else
    {
        Velocity = 0;   Turn = 0;
				motor_RL_pid.target_speed = 0;
				motor_RR_pid.target_speed = 0;
    }
//		OpenMV_On();//PA24
//		char setturn=0;
//		extern bool DIST_adjust;
//    extern bool DIST_adjust_ok;
//	
//		pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
//		pid_set_mode(&motor_RR_pid, MOTOR_SPEED);
//		motor_RL_pid.target_speed = 5;
//		motor_RR_pid.target_speed = 5;
//		delay_ms(550);			//识别90度弯后前进不够改这里
//	
//    g_f32TargetDistCM = 115;
//    Velocity = 0;
//    pid_set_mode(&motor_RL_pid, MOTOR_DEG);
//    pid_set_mode(&motor_RR_pid, MOTOR_DEG);
//    s_f32Yaw_Init = ms901m_yaw(s_f32Yaw_Init - 90);  // 设置目标角度
//    delay_ms(1200);
//	

//				// 启动角度与距离控制
//    YAW_adjust = true;
//    YAW_adjust_ok = false;
//    DIST_adjust = true;
//    DIST_adjust_ok = false;
//    // 重置累计脉冲
//    g_total_pulse_rl = 0;
//    g_total_pulse_rr = 0;
//    Velocity = -5;
//		
//		while(Velocity != 0)
//    {
//        delay_ms(30);
//        float dist = get_forward_distance_cm();
//        printf("Distance_Walked: %.2f cm  Pulse_L: %ld, Pulse_R: %ld\n", dist, g_total_pulse_rl, g_total_pulse_rr);
//        Print_Data_To_Uart0();
//    }
//	
//	while(1)
//	{
//		if(setturn==0)
//		{		
//			pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
//			pid_set_mode(&motor_RR_pid, MOTOR_SPEED);
//			motor_RL_pid.target_speed = 3;
//			motor_RR_pid.target_speed = -3;
//			line_flag=false;
//			Do_TASK =1;
//			setturn=1;
//			delay_ms(500);	
//		}
//    if (4 > pass_point)
//    {
//			switch (Do_TASK)
//			{
//					//***A-B***//
//					case 0:
//					{
//							pid_set_mode(&motor_RL_pid, MOTOR_TRACK);
//							pid_set_mode(&motor_RR_pid, MOTOR_TRACK);
//							Velocity = 7;
//							if(pass_point_flag == true)
//							{		
//								pass_point_flag = false;
//								Do_TASK = 1;
//								pass_point++;
//								pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
//								pid_set_mode(&motor_RR_pid, MOTOR_SPEED);
//								motor_RL_pid.target_speed = 5;
//								motor_RR_pid.target_speed = 5;
//								delay_ms(250);			//识别90度弯后前进不够改这里
//								motor_RL_pid.target_speed = -4;
//								motor_RR_pid.target_speed = 4;
//								line_flag=false;
//							}
//					}
//					break;
//					
//					//***A-B***//
//					case 1:
//					{
//							if(line_flag==true)
//							{
//							motor_RL_pid.target_speed = 0;
//							motor_RR_pid.target_speed = 0;
//							Do_TASK=0;
//							}
//					}
//					break;
//			}
//    }
//    else
//    {
//        Velocity = 0;   Turn = 0;

//				motor_RL_pid.target_speed = 0;
//				motor_RR_pid.target_speed = 0;
//    }
//	}
	
//		int m_point;
//		m_point = Set_Num * 4;

//    sprintf(chrbuf, "pass_point:%d   ", (int)pass_point);
//    OLED_ShowString(0, 5, (unsigned char*)chrbuf);
//	
//    if (m_point > pass_point)
//    {
//			switch (Do_TASK)
//			{
//					//***A-B***//
//					case 0:
//					{
//							pid_set_mode(&motor_RL_pid, MOTOR_TRACK);
//							pid_set_mode(&motor_RR_pid, MOTOR_TRACK);
//							Velocity = 7;
//							if(pass_point_flag == true)
//							{		
//								pass_point_flag = false;
//								Do_TASK = 1;
//								pass_point++;
//								pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
//								pid_set_mode(&motor_RR_pid, MOTOR_SPEED);
//								motor_RL_pid.target_speed = 5;
//								motor_RR_pid.target_speed = 5;
//								delay_ms(250);			//识别90度弯后前进不够改这里
//								motor_RL_pid.target_speed = -4;
//								motor_RR_pid.target_speed = 4;
//								line_flag=false;
//							}
//					}
//					break;
//					
//					//***A-B***//
//					case 1:
//					{
//							if(line_flag==true)
//							{
//							motor_RL_pid.target_speed = 0;
//							motor_RR_pid.target_speed = 0;
//							Do_TASK=0;
//							}
//					}
//					break;
//			}
//    }
//    else
//    {
//        Velocity = 0;   Turn = 0;

//				motor_RL_pid.target_speed = 0;
//				motor_RR_pid.target_speed = 0;
//    }
	
}

void SetTurn(spin_dir_t m_select)
{
	if(m_select == left_90)
	{

	}
}
