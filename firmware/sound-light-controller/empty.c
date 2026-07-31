#include "board.h"
#include "stdio.h"

int Velocity = 0;//+-20速度对应0-1000的pwm
float Turn = 0;
float s_f32Yaw_Init;
float s_f32Current_yaw;
float s_f32Base_Speed;
float temp_L,temp_R;
volatile float g_f32Current_YawDeg = 0.0f;

float g_f32TargetDistCM = 0.0f; // 行走目标距离，由用户传入设定 cm
bool DIST_adjust = false;
bool DIST_adjust_ok = false;

char deBugBuf[10]={};
	
int main(void)
{
    //实现板级初始化
    board_init();
    //关闭openmv的供电和使能，这是在干什么，暂时不知道
    //bsp_gpio.c
    OpenMV_Off();
    //点亮一个LED灯
    DL_GPIO_setPins(LED0_PORT, LED0_PIN_0_PIN);

    //key_Confirm 是一个需要被长按按钮所确认的标志位----哪一个按钮
    //bsp_key.c
   while (key_Confirm == false)
   {
       Key_Scan();
       OLED_Display();
       DIP_Task();
       Bluetooth_Task();
       TOF_Task();
   }
   
//		key_num = 4;//debug
	
   //第一个需要关注的变量出现了---g_f32Current_YawDeg
   //只是单纯记录作为此航角，作为后续一个基准
    s_f32Yaw_Init = g_f32Current_YawDeg;
    delay_ms(500);
//---怎么又给清标志位了---所以需要第二次长按吗，不需要吧，直接进while中了
//---这几个参数后续需要自己去了解相关情况---
    key_Confirm = false;
    Set_Num = 0;
    Key_Set_Yaw = 0;
    char chrbuf[20] = {0};
//    while (1)
//    {
//        pid_set_mode(&motor_RL_pid, MOTOR_SPEED);
//        pid_set_mode(&motor_RR_pid, MOTOR_SPEED);
//        printf("RL:%f,%f\r\n",motor_RL_pid.measure_speed,motor_RL_pid.target_speed);
//        // printf("\r\nRL:%f, %f\r\nRR:%f, %f\n\r\n\r",
//        //     motor_RL_pid.measure_speed,
//        //     motor_RL_pid.output,
//        //     motor_RR_pid.measure_speed,
//        //     motor_RR_pid.output);
////        motor_RL_pid.target_speed = 3;
////        motor_RR_pid.target_speed = 3;  
//        Print_Data_To_Uart0();
//    }
		
    while (1)
    {
        OLED_Display();
        DIP_Task();
        Bluetooth_Task();
        TOF_Task();
        delay_ms(20);       //因为OLED显示改成了每次显示一行，提高响应速度，因此这里需要延时30ms，才能保持原来的单次循环的时间。
        if (key_num == 0)
        {
            task_0();//这里是直接调用了一个函数，提示后续开发以编写任务函数展开
        }
        else if (key_num == 1)
        {
						while (key_Confirm == false)
						{
							Key_Scan();
							OLED_Display();
							OLED_ShowString(0, 5, "Set Num ...");
						}
//							Set_Num=1;//debug
					
						OLED_ShowString(0, 5, "Start!!      ");
            task_1();
        }
        else if (key_num == 2)
        {
						OLED_ShowString(0, 7, "Task2 Start!!");
            task_2();
        }
        else if (key_num == 3)
        {
						OLED_ShowString(0, 5, "Set Yaw");
						while (key_Confirm == false)
						{
							Set_Num = 0;
							Key_Scan();
							OLED_Display();
						}
            task_3();
        }
        else if (key_num == 4)
        {
						while (key_Confirm == false)
						{
							Key_Scan();
							OLED_Display();
							OLED_ShowString(0, 5, "Set Num ...");
						}
//							Set_Num=2;//debug
					
						OLED_ShowString(0, 5, "Start 4!!      ");
            task_4();
        }
        // printf("-->CurrentYawDeg: %3f\n\r",g_f32Current_YawDeg);
        Print_Data_To_Uart0();
        Print_Data_To_MS901M_Uart();
    }
}

//1ms公共定时器；电机编码器脉冲计数
void TIMER_0_INST_IRQHandler(void)
{
    extern volatile int32_t Encode_CNT_FL, Encode_CNT_FR, Encode_CNT_RL, Encode_CNT_RR;
    static uint64_t EncodeCNT = 0;
    EncodeCNT++;

    if (EncodeCNT % 15 == 0)//-----程序控制主循环------
    {
        //编码器速度计算
        if (DL_TimerG_getPendingInterrupt(TIMER_0_INST) == DL_TIMER_IIDX_ZERO)
        {
            //---track_control是一个什么函数，这里可以看看
            track_control();

            motor_FL_pid.measure_speed =  Encode_CNT_FL; //两个电机安装相反，所以编码器值也要相反
            motor_FR_pid.measure_speed = -Encode_CNT_FR;
            motor_RL_pid.measure_speed =  Encode_CNT_RL; //两个电机安装相反，所以编码器值也要相反
            motor_RR_pid.measure_speed = -Encode_CNT_RR;
            //编码器计数值清零
            Encode_CNT_FL = 0;
            Encode_CNT_FR = 0;
            Encode_CNT_RL = 0;
            Encode_CNT_RR = 0;

            // 累计总脉冲用于行进距离估算
            g_total_pulse_rl += motor_RL_pid.measure_speed;
            g_total_pulse_rr += motor_RR_pid.measure_speed;

            if (motor_RL_pid.pid_mode == MOTOR_TRACK || motor_RR_pid.pid_mode == MOTOR_TRACK)
            {
                // 执行巡线控制
                //电机
                pid_compute(&motor_RL_pid);
                Motor_PwmSet_RL(motor_RL_pid.output);
                pid_compute(&motor_RR_pid);
                Motor_PwmSet_RR(motor_RR_pid.output);
                //转向
                track_control();
                Turn = track_pid(Velocity);
                Motor_Kinematic_Analysis(Velocity, Turn);
            }
//          else
//          {
//              Motor_Kinematic_Analysis(Velocity, Turn);
//          }

            //如果当前是速度环
            if (motor_FL_pid.pid_mode == MOTOR_SPEED)
            {
                pid_compute(&motor_FL_pid);
                Motor_PwmSet_FL(motor_FL_pid.output);
            }
            if (motor_FR_pid.pid_mode == MOTOR_SPEED)
            {
                pid_compute(&motor_FR_pid);
                Motor_PwmSet_FR(motor_FR_pid.output);
            }
            if (motor_RL_pid.pid_mode == MOTOR_SPEED)
            {
                pid_compute(&motor_RL_pid);
                Motor_PwmSet_RL(motor_RL_pid.output);
            }
            if (motor_RR_pid.pid_mode == MOTOR_SPEED)
            {
                pid_compute(&motor_RR_pid);
                Motor_PwmSet_RR(motor_RR_pid.output);
            }

            if (motor_RL_pid.pid_mode == MOTOR_DEG || motor_RR_pid.pid_mode == MOTOR_DEG)
            {
                pid_compute(&motor_RL_pid);
                Motor_PwmSet_RL(motor_RL_pid.output);
                temp_L = motor_RL_pid.output;

                pid_compute(&motor_RR_pid);
                Motor_PwmSet_RR(motor_RR_pid.output);
                temp_R = motor_RR_pid.output;

                // 这里使用上电记录的 s_f32Yaw_Init 作为目标角度
                if (ms901m_control(g_f32Current_YawDeg, s_f32Yaw_Init, s_f32Base_Speed) && YAW_adjust)
                {
                    YAW_adjust_ok = true;
                }

                // ------------------- 增加：方向对准后开始距离判断 -------------------
                // ------------------- 修改部分开始 -------------------
                if (YAW_adjust_ok && DIST_adjust && !DIST_adjust_ok)
                {
                    float dist_current = get_forward_distance_cm();
										
                    // 提前减速逻辑：当接近目标时降低速度
                    float remaining_dist = g_f32TargetDistCM - dist_current;
                    if (remaining_dist < 5.0f && remaining_dist > 0.0f)
                    {
                        // 渐减速度
                        float speed_factor = remaining_dist / 5.0f;  // 在最后 5cm 内线性减速
                        motor_RL_pid.target_speed *= speed_factor;
                        motor_RR_pid.target_speed *= speed_factor;
                    }

                    // 判断是否已到达目标距离
                    if (dist_current >= g_f32TargetDistCM - DIST_REACH_TOLERANCE)
                    {
                        DIST_adjust_ok = true;

                        // 紧急刹车（可选）或平滑减速
                        Velocity = 0;
                        motor_RL_pid.target_speed = 0;
                        motor_RR_pid.target_speed = 0;

                        // 可选：立即停止电机输出
                        Motor_PwmSet_RL(0);
                        Motor_PwmSet_RR(0);

                        // // 切换回巡线模式或其他模式
                        // pid_set_mode(&motor_RL_pid, MOTOR_TRACK);
                        // pid_set_mode(&motor_RR_pid, MOTOR_TRACK);
                    }
                }
            }

//	sprintf((char *)deBugBuf, "output=%d\n", (int)motor_RL_pid.output);
//	uart1_send_data((uint8_t*)deBugBuf,10);
            // printf("\r\nFL:%3d, %4d\r\nFR:%3d, %4d\r\nRL:%3d, %4d\r\nRR:%3d, %4d\n\r\n\r",
            //                 motor_FL_pid.measure_speed,
            //                 motor_FL_pid.output,
            //                 motor_FR_pid.measure_speed,
            //                 motor_FR_pid.output,
            //                 motor_RL_pid.measure_speed,
            //                 motor_RL_pid.output,
            //                 motor_RR_pid.measure_speed,
            //                 motor_RR_pid.output);
        }
    }

    if (EncodeCNT % 5 == 0)
    {
        if (ringbuffer_data_len(&g_rb_Uart_MS901M_RevBuf))
        {
            ms901m_get_yaw_deg((float *)&g_f32Current_YawDeg);
        }
    }

    extern volatile uint64_t SystemTickCNT;
    SystemTickCNT++;
    TOF_Tick1ms();
}
