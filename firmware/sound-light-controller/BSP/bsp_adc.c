#include "bsp_adc.h"

volatile static bool gCheckADC0;        //AD0C采集成功标志位
volatile static bool gCheckADC1;        //AD0C采集成功标志位

void adc_init(void)
{
    //开启ADC0中断
    NVIC_EnableIRQ(ADC0_VOLTAGE_INST_INT_IRQN);
    //开启ADC1中断
    // NVIC_EnableIRQ(ADC1_VOLTAGE_INST_INT_IRQN);

    printf("ADC Init\r\n");
}

//读取ADC0的数据
unsigned int adc0_getValue(DL_ADC12_MEM_IDX adc_CH)
{
	unsigned int gAdcResult = 0;

	//软件触发ADC开始转换
	DL_ADC12_startConversion(ADC0_VOLTAGE_INST);
	//如果当前状态为正在转换中则等待转换结束
	while (false == gCheckADC0) {
		__WFE();
	}
	//获取数据
	gAdcResult = DL_ADC12_getMemResult(ADC0_VOLTAGE_INST, adc_CH);
	
	//清除标志位
	gCheckADC0 = false;

	return gAdcResult;
}

// //读取ADC1的数据
// unsigned int adc1_getValue(DL_ADC12_MEM_IDX adc_CH)
// {
// 	unsigned int gAdcResult = 0;

// 	//软件触发ADC开始转换
// 	DL_ADC12_startConversion(ADC1_VOLTAGE_INST);
// 	//如果当前状态为正在转换中则等待转换结束
// 	while (false == gCheckADC1) {
// 		__WFE();
// 	}
// 	//获取数据
// 	gAdcResult = DL_ADC12_getMemResult(ADC1_VOLTAGE_INST, adc_CH);
	
// 	//清除标志位
// 	gCheckADC1 = false;

// 	return gAdcResult;
// }

//ADC0中断服务函数
void ADC0_VOLTAGE_INST_IRQHandler(void)
{
	//查询并清除ADC中断
	switch (DL_ADC12_getPendingInterrupt(ADC0_VOLTAGE_INST)) 
	{
		//检查是否完成数据采集
		case DL_ADC12_IIDX_MEM0_RESULT_LOADED:
			gCheckADC0 = true;//将标志位置1
		break;
		default:
		break;
	}
}

// //ADC1中断服务函数
// void ADC1_VOLTAGE_INST_IRQHandler(void)
// {
// 	//查询并清除ADC中断
// 	switch (DL_ADC12_getPendingInterrupt(ADC1_VOLTAGE_INST)) 
// 	{
// 		//检查是否完成数据采集
// 		case DL_ADC12_IIDX_MEM3_RESULT_LOADED:
// 			gCheckADC1 = true;//将标志位置1
// 		break;
// 		default:
// 		break;
// 	}
// }








/* -------------------------------------- ADC 测试结果demo -------------------------------------- */
// // 所有需要采集的 ADC 通道
// static adc_channel_t adc_channels[] =
// {
//     { "PA27_ADC0_CH0", ADC_MODULE_0, ADC0_VOLTAGE_ADCMEM_ADC0_CH0, 0, 0 },
//     { "PA18_ADC1_CH3", ADC_MODULE_1, ADC1_VOLTAGE_ADCMEM_ADC1_CH3, 0, 0 },
//     { "PB19_ADC1_CH6", ADC_MODULE_1, ADC1_VOLTAGE_ADCMEM_ADC1_CH6, 0, 0 },
// };

// // 测试采样函数
// void adc_test(void)
// {
//     uint8_t i;

//     for (i = 0; i < sizeof(adc_channels) / sizeof(adc_channels[0]); i++)
//     {
//         adc_channel_t *ch = &adc_channels[i];

//         if (ch->module == ADC_MODULE_0)
//         {
//             ch->rawValue = adc0_getValue(ch->memIdx);
//         }
//         else
//         {
//             ch->rawValue = adc1_getValue(ch->memIdx);
//         }

//         ch->voltage = adc_raw_to_voltage(ch->rawValue);
//     }

//     printf("\r\n---------- ADC Test Result ----------\r\n");

//     for (i = 0; i < sizeof(adc_channels) / sizeof(adc_channels[0]); i++)
//     {
//         adc_channel_t *ch = &adc_channels[i];

//         printf("%-15s: Raw = %4d, Voltage = %d.%d%d V\r\n",
//                ch->name,
//                ch->rawValue,
//                ch->voltage / 100,
//                (ch->voltage / 10) % 10,
//                ch->voltage % 10);
//     }
// }
/* -------------------------------------- ADC 测试结果demo -------------------------------------- */




