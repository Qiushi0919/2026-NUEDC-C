#ifndef __FINE_LINE_H
#define __FINE_LINE_H

#include "ti_msp_dl_config.h"
#include "stdio.h"
#include "bsp_motor.h"
#include "bsp_pid.h"

#define  u8 unsigned char 
#define  u32 unsigned int 

#define READ_FIND_LINE_OUT1 DL_GPIO_readPins(TRACK_S1_PORT, TRACK_S1_PIN) ? 1 : 0
#define READ_FIND_LINE_OUT2 DL_GPIO_readPins(TRACK_S2_PORT, TRACK_S2_PIN) ? 1 : 0
#define READ_FIND_LINE_OUT3 DL_GPIO_readPins(TRACK_S3_PORT, TRACK_S3_PIN) ? 1 : 0
#define READ_FIND_LINE_OUT4 DL_GPIO_readPins(TRACK_S4_PORT, TRACK_S4_PIN) ? 1 : 0
#define READ_FIND_LINE_OUT5 DL_GPIO_readPins(TRACK_S5_PORT, TRACK_S5_PIN) ? 1 : 0
#define READ_FIND_LINE_OUT6 DL_GPIO_readPins(TRACK_S6_PORT, TRACK_S6_PIN) ? 1 : 0
#define READ_FIND_LINE_OUT7 DL_GPIO_readPins(TRACK_S7_PORT, TRACK_S7_PIN) ? 1 : 0
#define READ_FIND_LINE_OUT8 DL_GPIO_readPins(TRACK_S8_PORT, TRACK_S8_PIN) ? 1 : 0
#define GPIO_PIN_SET 1

static uint8_t mask_line=0;

uint8_t get_LedFind_Scan(void);
int Find_Line_Begins(void);


#endif

