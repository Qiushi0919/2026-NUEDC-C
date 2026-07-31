#ifndef __OLED_H
#define __OLED_H 

#include "board.h"
#include "stdlib.h"	

typedef enum{
    e_Oled_Cmd  = 0,
    e_Oled_Data = 1
}Oled_Command_Type_e;

void OLED_Init(void);
void OLED_Clear(void);
void OLED_fill_picture(unsigned char fill_Data);
void OLED_ShowString(uint8_t x,uint8_t y, uint8_t *p);
void OLED_ShowNumber(uint8_t x,uint8_t y,uint32_t num,uint8_t len,uint8_t size);
void OLED_Display(void);

#endif
