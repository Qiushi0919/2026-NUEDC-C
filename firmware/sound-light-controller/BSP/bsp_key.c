#include "bsp_key.h"
#include "board.h"
#include "stdio.h"

/**按键采集文件**/
uint8_t key_Confirm, key_num;
uint8_t Set_Num = 0;//设置圈数
bool Set_Num_Flag = false;//设置圈数标志位
int Key_Set_Yaw = 0;//设置角度
bool Key_Set_Yaw_Flag = false;//设置角度标志位

// 按键相关IO和回调函数初始化
static keyPortStructdef keyPortlist1 =
{
    KEY_PORT,
    KEY_KEY1_PIN,
    Key_SW1_ShortPress,
    Key_SW1_LongPress,
};

static keystateStructdef keystatelist[] =
{
    {
        0,
        0,
        0,
        0,
        SINGLE_RESPONSE,
        &keyPortlist1,
    }
};

// SW1短按回调函数
static void Key_SW1_ShortPress()
{
    // printf("short press\n");
    // OLED_ShowString(0, 1, (unsigned char *)"short press");
	if(Set_Num_Flag == false)
	{
    key_num++;
    if (key_num > 4)
    {
        key_num = 0;
    }
	}
	
	if(Set_Num_Flag == true)
	{
    Set_Num++;
    if (Set_Num > 5)
    {
        Set_Num = 0;
    }
	}
	
	if(Key_Set_Yaw_Flag == true)
	{
    Key_Set_Yaw += 10;
    if (Key_Set_Yaw > 270)
    {
        Key_Set_Yaw = 0;
    }
	}
	return;
}

// SW1长按回调函数
static void Key_SW1_LongPress()
{
    // printf("long press\n");
    // OLED_ShowString(0, 1, (unsigned char *)"long press");
    key_Confirm = true;
		Set_Num_Flag = true;
		Key_Set_Yaw_Flag = true;
    return;
}

void Key_Scan() // 按键扫描,通过按键表获取按键状态
{
    extern uint64_t HAL_GetTick(void);
    uint32_t currentTime = HAL_GetTick(); // 获取当前时间戳

    for (uint8_t i = 0; i < sizeof(keystatelist) / sizeof(keystatelist[0]); i++)
    {
        keystatelist[i].state = DL_GPIO_readPins(keystatelist[i].keyPortPdu->port, keystatelist[i].keyPortPdu->pin);

        // 状态消抖,通过位移实现延迟消抖
        if ((0 == keystatelist[i].state)) // 周期扫描7次都是按下状态则认为按下
        {
            keystatelist[i].mask |= 0x01;
            if (0x7f == keystatelist[i].mask)
            {
                // printf("key press\n");
                keystatelist[i].press = PRESS;
                keystatelist[i].startTime = currentTime; // 记录按键按下的起始时间戳
            }
            keystatelist[i].mask <<= 1;
        }
        else // 周期扫描7次都是释放状态则认为释放
        {
            keystatelist[i].mask >>= 1;
            if (0x01 == keystatelist[i].mask)
            {
                // printf("key release\n");
                keystatelist[i].press = RELEASE;
            }
        }

        // 状态机扫描按键状态
        switch (keystatelist[i].press)
        {
            case PRESS:
            {
                keystatelist[i].press = SHORTPRESS;
                break;
            }

            case SHORTPRESS: // 短按
            {
                if (0 == keystatelist[i].state) // 扫描期间未释放按键
                {
                    if (currentTime - keystatelist[i].startTime >= LONGPRESS_TIME) // 判断是否达到长按时间
                    {
                        keystatelist[i].press = LONGPRESS;
                    }
                }
                else
                {
                    if (NULL != keystatelist[i].keyPortPdu->keyshortpress_fun) // 扫描期间释放了按键，则认为是短按
                    {
                        keystatelist[i].keyPortPdu->keyshortpress_fun(); // 短按响应函数
                        keystatelist[i].press = NONE;
                    }
                }
                break;
            }

            case LONGPRESS: // 长按
            {
                if (NULL != keystatelist[i].keyPortPdu->keylongpress_fun)
                {
                    if (keystatelist[i].iscontinuousLongpress) // 连续响应长按
                    {
                    }
                    else // 单次长按响应
                    {
                        keystatelist[i].press = NONE;
                    }
                    keystatelist[i].keyPortPdu->keylongpress_fun(); // 长按响应函数
                }
                break;
            }

            case RELEASE: // 按键释放
            {
                keystatelist[i].press = NONE;
                break;
            }
        }
    }
}