#ifndef	__BSP_ADC_H__
#define __BSP_ADC_H__

#include "board.h"
#include "ti_msp_dl_config.h"
#include <stdint.h>
#include <stdbool.h>


// 枚举类型定义 ADC 来源
typedef enum
{
    ADC_MODULE_0 = 0,
    ADC_MODULE_1
} adc_module_e;

// ADC通道数据结构
typedef struct
{
    const char     *name;       // 通道描述名
    adc_module_e    module;     // 属于哪个 ADC 模块
    uint8_t         memIdx;     // ADC 通道索引
    uint16_t        rawValue;   // 原始采样值
    uint16_t        voltage;    // 电压值（放大100倍，单位0.01V）
} adc_channel_t;


// 电压计算函数（返回单位：0.01V）
static inline uint16_t adc_raw_to_voltage(uint16_t raw)
{
    return (uint16_t)((raw / 4095.0 * 3.3) * 100);
}

void adc_init(void);

extern unsigned int adc0_getValue(DL_ADC12_MEM_IDX adc_CH);
extern unsigned int adc1_getValue(DL_ADC12_MEM_IDX adc_CH);
extern void adc_test(void);

#endif
