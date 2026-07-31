#ifndef	__BSP_SG_H__
#define __BSP_SG_H__
#include "board.h"

void Steering_SetAngle(unsigned int angle);
unsigned int Steering_GetAngle(void);
float Steering_AngleDiff(float a, float b);

#endif
