#ifndef _IIC_H
#define _IIC_H
#include "board.h"
#include "stdio.h"


typedef struct {
	GPIO_Regs* SCL_Port;  // SCL使用的GPIO端口
	unsigned int SCL_Pin;        // SCL引脚编号
	IOMUX_PINCM SCL_IOMUX;
	GPIO_Regs* SDA_Port;  // SDA使用的GPIO端口
	unsigned int SDA_Pin;        // SDA引脚编号
	IOMUX_PINCM SDA_IOMUX;
} IIC_Config_t;

extern IIC_Config_t iic_config;


// 定义内联函数来控制引脚状态
static inline void IIC_SCL_H(IIC_Config_t *config) {
    DL_GPIO_setPins(config->SCL_Port,config->SCL_Pin);
}

static inline void IIC_SCL_L(IIC_Config_t *config) {
    DL_GPIO_clearPins(config->SCL_Port,config->SCL_Pin);
}

static inline void IIC_SDA_H(IIC_Config_t *config) {
    DL_GPIO_setPins(config->SDA_Port,config->SDA_Pin);
}

static inline void IIC_SDA_L(IIC_Config_t *config) {
    DL_GPIO_clearPins(config->SDA_Port,config->SDA_Pin);
}

static inline uint8_t IIC_SDA_Read(IIC_Config_t *config) {
    return DL_GPIO_readPins(config->SDA_Port,config->SDA_Pin);
}

// 定义宏来切换SDA引脚的输入输出模式
#define IIC_SDA_IN(config)		do { \
									DL_GPIO_initDigitalInput((config)->SDA_IOMUX);\
								} while(0)

#define IIC_SDA_OUT(config)		do { \
									DL_GPIO_initDigitalOutput((config)->SDA_IOMUX);     \
									DL_GPIO_setPins((config)->SDA_Port, (config)->SDA_Pin);      \
									DL_GPIO_enableOutput((config)->SDA_Port, (config)->SDA_Pin); \
								} while(0)



void IIC_Init(IIC_Config_t *config);
void IIC_WriteBytes_SW(IIC_Config_t *config, uint8_t deviceAddr, uint8_t regAddr, uint8_t *pData, uint16_t length);
void IIC_ReadBytes_SW(IIC_Config_t *config, uint8_t deviceAddr, uint8_t regAddr, uint8_t *pData, uint16_t length);



void IIC_ReadBytes_HW(I2C_Regs *hi2c, uint8_t addr, uint8_t regaddr, uint8_t *regdata, uint16_t num);
void IIC_WriteReg_HW(I2C_Regs *hi2c, uint8_t addr, uint8_t regaddr, uint8_t* regdata, uint16_t num);
#endif
