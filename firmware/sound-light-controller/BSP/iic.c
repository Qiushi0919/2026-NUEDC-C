#include "iic.h"

void IIC_Init(IIC_Config_t *config)
{
	// 初始化状态，将SCL和SDA设置为高电平
	IIC_SCL_H(config);
	IIC_SDA_H(config);
}

/**
 * @brief 生成I²C起始条件。
 *
 * @param config    指向IIC配置结构体的指针。
 */
void IIC_Start(IIC_Config_t *config)
{
	IIC_SDA_OUT(config);
	IIC_SCL_H(config);
	delay_us(1);
	IIC_SDA_L(config);
	delay_us(1);
	IIC_SCL_L(config);
}

/**
 * @brief 生成I²C停止条件。
 *
 * @param config    指向IIC配置结构体的指针。
 */
void IIC_Stop(IIC_Config_t *config)
{
	IIC_SDA_OUT(config);
	IIC_SCL_L(config);
	IIC_SDA_L(config);
	delay_us(1);
	IIC_SCL_H(config);
	delay_us(1);
	IIC_SDA_H(config);
	delay_us(1);
}

/**
 * @brief 发送应答位（ACK）或非应答位（NACK）。
 *
 * @param config    指向IIC配置结构体的指针。
 * @param ack       是否发送ACK（1为ACK，0为NACK）。
 */
void IIC_Send_Ack(IIC_Config_t *config, uint8_t ack)
{
	IIC_SDA_OUT(config);
	IIC_SCL_L(config);
	delay_us(1);
	if (!ack)
	{
		IIC_SDA_L(config);
	}
	else
	{
		IIC_SDA_H(config);
	}
	IIC_SCL_H(config);
	delay_us(1);
	IIC_SCL_L(config);
	IIC_SDA_H(config);
}

/**
 * @brief 等待从设备发送的应答位（ACK）。
 *
 * @param config    指向IIC配置结构体的指针。
 *
 * @return uint8_t  返回是否成功接收到ACK（0为成功，1为失败）。
 */
uint8_t IIC_WaitAck(IIC_Config_t *config)
{
	uint8_t ucErrTime = 0;
	IIC_SCL_L(config);
	IIC_SDA_H(config);
	IIC_SDA_IN(config);
	IIC_SCL_H(config);

	while (IIC_SDA_Read(config))
	{
		ucErrTime++;
		if (ucErrTime > 250)
		{
			IIC_Stop(config);
			return 1; // 超时返回
		}
		delay_us(1);
	}

	IIC_SCL_L(config);
	IIC_SDA_OUT(config);
	return 0; // 成功接收ACK
}

/**
 * @brief 向I²C总线写入一个字节的数据。
 *
 * @param config    指向IIC配置结构体的指针。
 * @param ucDat     要写入的字节数据。
 */
void IIC_WriteByte(IIC_Config_t *config, uint8_t ucDat)
{
	IIC_SDA_OUT(config);
	IIC_SCL_L(config);

	for (int i = 0; i < 8; i++)
	{
		IIC_SDA_L(config);
		if (ucDat & 0x80)
		{
			IIC_SDA_H(config);
		}
		delay_us(1);
		IIC_SCL_H(config);
		delay_us(1);
		IIC_SCL_L(config);
		delay_us(1);
		ucDat <<= 1;
	}
}

/**
 * @brief 从I²C总线读取一个字节的数据，并发送ACK/NACK。
 *
 * @param config    指向IIC配置结构体的指针。
 * @param ack       是否发送ACK（1为ACK，0为NACK）。
 *
 * @return uint8_t  返回读取到的字节数据。
 */
uint8_t IIC_ReadByte(IIC_Config_t *config, uint8_t ack)
{
	uint8_t i, ucDat = 0;
	IIC_SDA_IN(config);
	
	for(i = 0; i < 8; i++)
	{
		ucDat <<= 1;
		IIC_SCL_L(config);
		delay_us(1);
		IIC_SCL_H(config);
		delay_us(1);
		if (IIC_SDA_Read(config))
		{
			ucDat |= 0x01;
		}
		delay_us(1);
	}

	IIC_SCL_L(config);
	IIC_Send_Ack(config, ack);
	IIC_SDA_OUT(config);
	return ucDat;
}

/**
 * @brief 向指定设备的寄存器写入多个字节的数据。
 *
 * @param config      指向IIC配置结构体的指针。
 * @param deviceAddr  设备的7位I²C地址。
 * @param regAddr     寄存器地址。
 * @param pData       要写入的数据数组。
 * @param length      数据数组中的元素数量。
 */
void IIC_WriteBytes_SW(IIC_Config_t *config, uint8_t deviceAddr, uint8_t regAddr, uint8_t *pData, uint16_t length)
{
	IIC_Start(config);
	IIC_WriteByte(config, (deviceAddr << 1)); // 发送设备地址+写命令
	if (IIC_WaitAck(config))
	{
		IIC_Stop(config);
		return;
	}
	IIC_WriteByte(config, regAddr); // 发送寄存器地址
	if (IIC_WaitAck(config))
	{
		IIC_Stop(config);
		return;
	}

	while(length--)
	{
		IIC_WriteByte(config, *pData++);
		if (IIC_WaitAck(config))
		{
			IIC_Stop(config);
			return;
		}
	}

	IIC_Stop(config);
}

/**
 * @brief 从指定设备的寄存器读取多个字节的数据。
 *
 * @param config      指向IIC配置结构体的指针。
 * @param deviceAddr  设备的7位I²C地址。
 * @param regAddr     寄存器地址。
 * @param pData       存储读取数据的数组。
 * @param length      要读取的数据字节数。
 */
void IIC_ReadBytes_SW(IIC_Config_t *config, uint8_t deviceAddr, uint8_t regAddr, uint8_t *pData, uint16_t length)
{
	IIC_Start(config);
	IIC_WriteByte(config, (deviceAddr << 1)); // 发送设备地址+写命令
	if (IIC_WaitAck(config))
	{
		IIC_Stop(config);
		return;
	}
	IIC_WriteByte(config, regAddr); // 发送寄存器地址
	if (IIC_WaitAck(config))
	{
		IIC_Stop(config);
		return;
	}
	
	IIC_Start(config);
	IIC_WriteByte(config, (deviceAddr << 1) | 0x01); // 发送设备地址+读命令
	if (IIC_WaitAck(config))
	{
		IIC_Stop(config);
		return;
	}

	if (length > 1)
	{
		for(; length > 1; length--)
		{
			*pData++ = IIC_ReadByte(config, 0); // 读取数据并发送ACK
		}
	}
	*pData = IIC_ReadByte(config, 1); // 最后一次读取发送NACK

	IIC_Stop(config);
}

/**
 * @brief 向指定设备的寄存器写入多个字节的数据。
 *
 * @param hi2c      指向 I²C 外设实例的指针（如 I2C_0_INST）。
 * @param addr      设备的 7 位 I²C 地址。
 * @param regaddr   寄存器地址。
 * @param regdata   指向存储写入数据的数组。
 * @param num       要写入的数据字节数。
 */
void IIC_WriteReg_HW(I2C_Regs *hi2c, uint8_t addr, uint8_t regaddr, uint8_t* regdata, uint16_t num)
{
    uint8_t temp[num + 1]; // 用于存储寄存器地址和数据的临时缓冲区
    temp[0] = regaddr;     // 第一个字节为寄存器地址

    // 拷贝数据到缓冲区
    for (uint16_t i = 0; i < num; i++)
    {
        temp[i + 1] = regdata[i];
    }

    // 1. 填充数据到 TX FIFO
    DL_I2C_fillControllerTXFIFO(hi2c, temp, num + 1);

    // 2. 启动传输到目标设备
    DL_I2C_startControllerTransfer(hi2c, addr, DL_I2C_CONTROLLER_DIRECTION_TX, num + 1);

    // 3. 等待传输完成
    while (DL_I2C_getControllerStatus(hi2c) & DL_I2C_CONTROLLER_STATUS_BUSY_BUS)
    {
        ;
    }
    while (!(DL_I2C_getControllerStatus(hi2c) & DL_I2C_CONTROLLER_STATUS_IDLE))
    {
        ;
    }
}

/**
 * @brief 从指定设备的寄存器读取多个字节的数据。
 *
 * @param hi2c      指向 I²C 外设实例的指针（如 I2C_0_INST）。
 * @param addr      设备的 7 位 I²C 地址。
 * @param regaddr   寄存器地址。
 * @param regdata   指向存储读取数据的数组。
 * @param num       要读取的数据字节数。
 */
void IIC_ReadBytes_HW(I2C_Regs *hi2c, uint8_t addr, uint8_t regaddr, uint8_t *regdata, uint16_t num)
{
    // 1. 写入寄存器地址
    DL_I2C_fillControllerTXFIFO(hi2c, &regaddr, 1); // 填充寄存器地址到 TX FIFO
    DL_I2C_startControllerTransfer(hi2c, addr, DL_I2C_CONTROLLER_DIRECTION_TX, 1); // 启动写操作

    // 等待写完成
    while (DL_I2C_getControllerStatus(hi2c) & DL_I2C_CONTROLLER_STATUS_BUSY_BUS)
    {
        ;
    }
    while (!(DL_I2C_getControllerStatus(hi2c) & DL_I2C_CONTROLLER_STATUS_IDLE))
    {
        ;
    }

    // 2. 启动读取操作
    DL_I2C_startControllerTransfer(hi2c, addr, DL_I2C_CONTROLLER_DIRECTION_RX, num); // 启动读操作

    // 循环读取数据
    for (uint16_t i = 0; i < num; i++)
    {
        // 等待读数据到 RX FIFO
        while (DL_I2C_isControllerRXFIFOEmpty(hi2c))
        {
            ;
        }
        regdata[i] = DL_I2C_receiveControllerData(hi2c); // 读取数据到缓冲区
    }

    // 等待总线空闲
    while (DL_I2C_getControllerStatus(hi2c) & DL_I2C_CONTROLLER_STATUS_BUSY_BUS)
    {
        ;
    }
    while (!(DL_I2C_getControllerStatus(hi2c) & DL_I2C_CONTROLLER_STATUS_IDLE))
    {
        ;
    }
}

