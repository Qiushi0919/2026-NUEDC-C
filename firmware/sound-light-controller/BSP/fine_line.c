#include "fine_line.h"

char have_line =0;

uint8_t find_line_state = 1;
uint16_t find_time=0;	
uint8_t intersection=0;
uint8_t final_intersection=0;
uint8_t low_pin_count = 0;
//uint8_t low_pin_time = 0;//丢失目标时间


uint8_t get_LedFind_Scan(void)
{
	low_pin_count = 0;
	//有一个传感器识别到色块就+1
	if (READ_FIND_LINE_OUT1 == GPIO_PIN_SET) low_pin_count++;
	if (READ_FIND_LINE_OUT2 == GPIO_PIN_SET) low_pin_count++;
	if (READ_FIND_LINE_OUT3 == GPIO_PIN_SET) low_pin_count++;
	if (READ_FIND_LINE_OUT4 == GPIO_PIN_SET) low_pin_count++;
	if (READ_FIND_LINE_OUT5 == GPIO_PIN_SET) low_pin_count++;
	if (READ_FIND_LINE_OUT6 == GPIO_PIN_SET) low_pin_count++;
	if (READ_FIND_LINE_OUT7 == GPIO_PIN_SET) low_pin_count++;
	if (READ_FIND_LINE_OUT8 == GPIO_PIN_SET) low_pin_count++;
	
	if (low_pin_count >= 4) final_intersection+=2;				//识别到4个以上色块就可以判断为路口
  else if	(low_pin_count == 0)//没有识别到色块也可以判断为路口
	{
		final_intersection++;
	}
	else final_intersection=0;
	if(final_intersection>100)
	{
		intersection = 1;
		final_intersection = 0;
//		HAL_Delay(200);
	}

	if(READ_FIND_LINE_OUT4 == 1)
	{
		return 3;
	}
	else if(READ_FIND_LINE_OUT5 == 1)
	{
		return 4;
	}
	else if(READ_FIND_LINE_OUT6 == 1)
	{
		return 5;
	}
	else if(READ_FIND_LINE_OUT3 == 1)
	{
		return 2;
	}
	else if(READ_FIND_LINE_OUT2 == 1)
	{
		return 1;
	}
	else if(READ_FIND_LINE_OUT7 == 1)
	{
		return 6;
	}
	else if(READ_FIND_LINE_OUT1 == 1)
	{
		return 0;
	}
	else if(READ_FIND_LINE_OUT8 == 1)
	{
		return 7;
	}
	else
	{
		return 3;
	}	
}

static int Linechange_list[8]=
{
	-3,
	-2,
	-1,
	0,
	0,
	1,
	2,
	3,
};

char value_led[20];
char show_intersection[20];
int Find_Line_Begins(void)
{
	static int find_value;
	find_value = Linechange_list[get_LedFind_Scan()];
	Turn = find_value;
}

