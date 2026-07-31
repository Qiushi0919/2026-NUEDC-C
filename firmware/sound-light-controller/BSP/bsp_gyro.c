#include "bsp_gyro.h"
#include "bsp_motor.h"
#include "stdio.h"

volatile Gyro_Struct Gyro_Structure;

float gyro_Kp = 150, gyro_Ki=0, gyro_Kd =5.5;//PID参数
float gyro_P = 0, gyro_I = 0, gyro_D = 0, gyro_PID_value = 0;
float gyro_new_error = 0, gyro_previous_error = 0;
static int gyro_initial_motor_speed = 3000;//基础速度

void gyro_init(void)
{
	/*================Z轴归零==================*/
	// 寄存器解锁
	uint8_t unlock_reg1[2] = {0x88,0xB5};
	Jy61p_writeData(IIC_ADDR,UN_REG,unlock_reg1,2);
	delay_ms(200);
	// Z轴归零
	uint8_t z_axis_reg[2] = {0x04,0x00};
	Jy61p_writeData(IIC_ADDR,ANGLE_REFER_REG,z_axis_reg,2);	
	delay_ms(200);	
	// 保存
	uint8_t save_reg1[2] = {0x00,0x00};
	Jy61p_writeData(IIC_ADDR,SAVE_REG,save_reg1,2);
	delay_ms(200);

	/*================角度归零==================*/
	// 寄存器解锁
	uint8_t unlock_reg[2] = {0x88,0xB5};
	Jy61p_writeData(IIC_ADDR,UN_REG,unlock_reg,2);
	delay_ms(200);
	// 角度归零
	uint8_t angle_reg[2] = {0x08,0x00};
	Jy61p_writeData(IIC_ADDR,ANGLE_REFER_REG,angle_reg,2);	
	delay_ms(200);
	// 保存
	uint8_t save_reg[2] = {0x00,0x00};
	Jy61p_writeData(IIC_ADDR,SAVE_REG,save_reg,2);
	delay_ms(200);

	// 清空结构体
	memset((unsigned char*)&Gyro_Structure,0,sizeof(Gyro_Structure));
}

/******************************************************************
 * 函 数 名 称：IIC_Start
 * 函 数 说 明：IIC起始时序
 * 函 数 形 参：无
 * 函 数 返 回：无
 * 作       者：LC
 * 备       注：无
******************************************************************/
void gyro_IIC_Start(void)
{
    SDA_OUT();

    SCL(0);
    SDA(1);
    SCL(1);

    delay_us(5);

    SDA(0);
    delay_us(5);
    SCL(0);
    delay_us(5);
}

/******************************************************************
 * 函 数 名 称：IIC_Stop
 * 函 数 说 明：IIC停止信号
 * 函 数 形 参：无
 * 函 数 返 回：无
 * 作       者：LC
 * 备       注：无
******************************************************************/
void gyro_IIC_Stop(void)
{
    SDA_OUT();
    SCL(0);
    SDA(0);

    SCL(1);
    delay_us(5);
    SDA(1);
    delay_us(5);
}

/******************************************************************
 * 函 数 名 称：IIC_Send_Ack
 * 函 数 说 明：主机发送应答或者非应答信号
 * 函 数 形 参：0发送应答  1发送非应答
 * 函 数 返 回：无
 * 作       者：LC
 * 备       注：无
******************************************************************/
void gyro_IIC_Send_Ack(unsigned char ack)
{
    SDA_OUT();
    SCL(0);
    SDA(0);
    delay_us(5);
    if(!ack) SDA(0);
    else     SDA(1);
    SCL(1);
    delay_us(5);
    SCL(0);
    SDA(1);
}

/******************************************************************
 * 函 数 名 称：IIC_WaitAck
 * 函 数 说 明：等待从机应答
 * 函 数 形 参：无
 * 函 数 返 回：0有应答  1超时无应答
 * 作       者：LC
 * 备       注：无
******************************************************************/
unsigned char gyro_IIC_WaitAck(void)
{
    char ack = 0;
    char ack_flag = 50;

    SDA_IN();

    SDA(1);
    while ((SDA_GET() == 1) && (ack_flag))
    {
        ack_flag--;
        delay_us(5);
    }

    if (ack_flag == 0)
    {
        gyro_IIC_Stop();
        return 1;
    }
    else
    {
        SCL(1);
        delay_us(5);
        SCL(0);
        SDA_OUT();
    }
    return ack;
}

/******************************************************************
 * 函 数 名 称：Send_Byte
 * 函 数 说 明：写入一个字节
 * 函 数 形 参：dat要写人的数据
 * 函 数 返 回：无
 * 作       者：LC
 * 备       注：无
******************************************************************/
void gyro_Send_Byte(uint8_t dat)
{
    int i = 0;
    SDA_OUT();
    SCL(0);//拉低时钟开始数据传输

    for( i = 0; i < 8; i++ )
    {
        SDA( (dat & 0x80) >> 7 );
        delay_us(2);

        SCL(1);
        delay_us(5);

        SCL(0);
        delay_us(5);

        dat<<=1;
    }
}

/******************************************************************
 * 函 数 名 称：Read_Byte
 * 函 数 说 明：IIC读时序
 * 函 数 形 参：无
 * 函 数 返 回：读到的数据
 * 作       者：LC
 * 备       注：无
******************************************************************/
unsigned char gyro_Read_Byte(void)
{
    unsigned char i,receive=0;
    SDA_IN();//SDA设置为输入
    for(i=0;i<8;i++ )
    {
        SCL(0);
        delay_us(5);
        SCL(1);
        delay_us(5);
        receive<<=1;
        if( SDA_GET() )
        {
            receive|=1;
        }
        delay_us(5);
    }

    return receive;
}

/******************************************************************
 * 函 数 名 称：Jy61p_writeData
 * 函 数 说 明：写数据
 * 函 数 形 参：dev 设备地址
				reg 寄存器地址
				data 数据首地址
				length 数据长度
 * 函 数 返 回：返回0则写入成功
 * 作       者：LC
 * 备       注：无
******************************************************************/
uint8_t Jy61p_writeData(uint8_t dev, uint8_t reg, uint8_t* data, uint32_t length)
{
    uint32_t count = 0;

    gyro_IIC_Start();

    gyro_Send_Byte(dev << 1);
    if (gyro_IIC_WaitAck() == 1)
    {
        return 0;
    }

    gyro_Send_Byte(reg);
    if (gyro_IIC_WaitAck() == 1)
    {
        return 0;
    }

    for (count = 0; count < length; count++)
    {
        gyro_Send_Byte(data[count]);
        if (gyro_IIC_WaitAck() == 1)
        {
            return 0;
        }
    }

    gyro_IIC_Stop();

    return 1;
}

/******************************************************************
 * 函 数 名 称：Jy61p_readData
 * 函 数 说 明：读数据数据
 * 函 数 形 参：dev 设备地址
				reg 寄存器地址
				data 数据存储地址
				length 数据长度
 * 函 数 返 回：返回0则写入成功
 * 作       者：LC
 * 备       注：无
******************************************************************/
uint8_t Jy61p_readData(uint8_t dev, uint8_t reg, uint8_t *data, uint32_t length)
{
    uint32_t count = 0;

    gyro_IIC_Start();

    gyro_Send_Byte((dev << 1) | 0);
    if (gyro_IIC_WaitAck() == 1)
    {
        return 0;
    }

    gyro_Send_Byte(reg);
    if (gyro_IIC_WaitAck() == 1)
    {
        return 0;
    }

    delay_us(5);

    gyro_IIC_Start();

    gyro_Send_Byte((dev << 1) | 1);
    if (gyro_IIC_WaitAck() == 1)
    {
        return 0;
    }

    for (count = 0; count < length; count++)
    {
        if (count != length - 1)
        {
            data[count] = gyro_Read_Byte();
            gyro_IIC_Send_Ack(0);
        }
        else
        {
            data[count] = gyro_Read_Byte();
            gyro_IIC_Send_Ack(1);
        }
    }

    gyro_IIC_Stop();

    return 1;
}

/******************************************************************
 * 函 数 名 称：Jy61p_get_angle
 * 函 数 说 明：读角度数据
 * 函 数 形 参：无
 * 函 数 返 回：返回结构体
 * 作       者：LC
 * 备       注：无
******************************************************************/
float Jy61p_get_angle(void)
{
    // 数据缓存
    volatile uint8_t sda_angle[6] = {0};

    int ret = 0;

    // 清空数据缓存
    memset((unsigned char*)sda_angle, 0, sizeof(sda_angle));

    // 读取寄存器数据
    ret = Jy61p_readData(IIC_ADDR, 0x3D, (uint8_t*)sda_angle, 6);

    if (ret == 0)
    {
        // 读取失败
        printf("Read Error\r\n");
    }

    #if GYRO_DEBUG
    {
        printf("RollL = %x\r\n", sda_angle[0]);
        printf("RollH = %x\r\n", sda_angle[1]);
        printf("PitchL = %x\r\n", sda_angle[2]);
        printf("PitchH = %x\r\n", sda_angle[3]);
        printf("YawL = %x\r\n", sda_angle[4]);
        printf("YawH = %x\r\n", sda_angle[5]);
    }
    #endif

    // 计算 RollX, PitchY 和 YawZ 并确保它们在 -180 到 180 的范围内
    float RollX = (float)(((sda_angle[1] << 8) | sda_angle[0]) / 32768.0 * 180.0);
    if (RollX > 180.0)
    {
        RollX -= 360.0;
    }
    else if (RollX < -180.0)
    {
        RollX += 360.0;
    }

    float PitchY = (float)(((sda_angle[3] << 8) | sda_angle[2]) / 32768.0 * 180.0);
    if (PitchY > 180.0)
    {
        PitchY -= 360.0;
    }
    else if (PitchY < -180.0)
    {
        PitchY += 360.0;
    }

    float YawZ = (float)(((sda_angle[5] << 8) | sda_angle[4]) / 32768.0 * 180.0);
    if (YawZ > 180.0)
    {
        YawZ -= 360.0;
    }
    else if (YawZ < -180.0)
    {
        YawZ += 360.0;
    }

    // 将计算结果保存到结构体中
    Gyro_Structure.x = RollX;
    Gyro_Structure.y = PitchY;
    Gyro_Structure.z = YawZ;

    // 返回角度数据
    return YawZ;
}



float gyro_pid(int angle)
{
    gyro_new_error = Jy61p_get_angle();
    gyro_new_error = gyro_new_error - angle;
    // printf("gyro_new_error = %f\n",gyro_new_error);
    gyro_P = gyro_new_error;	        //当前误差
    gyro_I = gyro_I+gyro_new_error;	        //误差累加
    gyro_D = gyro_new_error-gyro_previous_error;	//当前误差与之前误差的误差

    gyro_PID_value=(gyro_Kp*gyro_P)+(gyro_Ki*gyro_I)+(gyro_Kd*gyro_D);

    gyro_previous_error=gyro_new_error;//更新之前误差

    return gyro_PID_value; //返回控制值
}

//电机动作
void  gyro_motorsWrite(int speedL,int speedR)
{
    if(speedR > 0)
    {
        //右轮前进
        DL_GPIO_setPins(MOTOR_PORT,MOTOR_BIN3_PIN);
        DL_GPIO_clearPins(MOTOR_PORT,MOTOR_BIN4_PIN);
        DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST,ABS(speedR),GPIO_PWM_MOTOR_RearSide_C1_IDX);
    }
    else
    {
        //右轮后退
        DL_GPIO_setPins(MOTOR_PORT,MOTOR_BIN4_PIN);
        DL_GPIO_clearPins(MOTOR_PORT,MOTOR_BIN3_PIN);
        DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST,ABS(speedR),GPIO_PWM_MOTOR_RearSide_C1_IDX);
    }

    if(speedL > 0)
    {
        //左轮前进
        DL_GPIO_setPins(MOTOR_PORT,MOTOR_AIN4_PIN);
        DL_GPIO_clearPins(MOTOR_PORT,MOTOR_AIN3_PIN);
        DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST,ABS(speedL),GPIO_PWM_MOTOR_RearSide_C0_IDX);
    }
    else
    {
        //左轮后退
        DL_GPIO_setPins(MOTOR_PORT,MOTOR_AIN3_PIN);
        DL_GPIO_clearPins(MOTOR_PORT,MOTOR_AIN4_PIN);
        DL_TimerG_setCaptureCompareValue(PWM_MOTOR_RearSide_INST,ABS(speedL),GPIO_PWM_MOTOR_RearSide_C0_IDX);
    }
}
int gyro_Set_PWM(float pwm_value)
{
    //基础速度+PID值
    int left_motor_speed = pwm_value;
    int right_motor_speed = -pwm_value;

    printf("%.2f,  %d,  %d\n",gyro_new_error,left_motor_speed,right_motor_speed);

    if(left_motor_speed>9999) left_motor_speed=9999;
    else if(left_motor_speed<-9999) left_motor_speed=-9999;
    if(right_motor_speed>9999) right_motor_speed=9999;
    else if(right_motor_speed<-9999) right_motor_speed=-9999;

    //旋转到目标角度则停止
    if( gyro_new_error >= -0.5 && gyro_new_error <= 0.5 )
    {
        Motor_Stop();
        return 1;
    }
    gyro_motorsWrite(left_motor_speed,right_motor_speed);
    return 0;
}

//返回1说明旋转到位  返回0说明还在旋转
int gyro_control(int angle)
{
    return gyro_Set_PWM(gyro_pid(angle));
}


