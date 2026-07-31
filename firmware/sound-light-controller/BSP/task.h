#ifndef __TASK_H
#define __TASK_H


typedef enum
{
	left_90,
	right_90,
	back_180_l,
	back_180_r,
	straight,
	fianl,
	ojbk
}spin_dir_t;

void task_0(void);
void task_1();
void task_2();
void task_3();
void task_4();
void SetTurn(spin_dir_t m_select);
#endif
