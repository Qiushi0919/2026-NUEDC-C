#include "stdlib.h"
#include "bsp_oled.h"
#include "bsp_oledfont.h"
#include "iic.h"

// #define	OLED_SW_IIC
#define	OLED_HW_IIC

#ifdef OLED_SW_IIC
IIC_Config_t iic_config_oled =
{
    .SCL_Port = I2C_PORT, // 指定SCL GPIO端口
    .SCL_Pin = I2C_SCL_PIN, // 指定SCL引脚
    .SCL_IOMUX = I2C_SCL_IOMUX,
    .SDA_Port = I2C_PORT, // 指定SDA GPIO端口
    .SDA_Pin = I2C_SDA_PIN,  // 指定SDA引脚
    .SDA_IOMUX = I2C_SDA_IOMUX,
};
#endif

void OLED_WR_Byte(unsigned char dat, Oled_Command_Type_e cmd)
{
    if (cmd == e_Oled_Cmd)
    {
#ifdef	OLED_HW_IIC
        IIC_WriteReg_HW(I2C_0_INST, 0x3C, 0x00, &dat, 1);
#elif defined(OLED_SW_IIC)
        IIC_WriteBytes_SW(&iic_config_oled, 0x3C, 0x00, &dat, 1);
#endif
    }
    else
    {
#ifdef	OLED_HW_IIC
        IIC_WriteReg_HW(I2C_0_INST, 0x3C, 0x40, &dat, 1);
#elif defined(OLED_SW_IIC)
        IIC_WriteBytes_SW(&iic_config_oled, 0x3C, 0x40, &dat, 1);
#endif
    }
}

/********************************************
// fill_Picture
********************************************/
void OLED_fill_picture(unsigned char fill_Data)
{
    unsigned char m, n;

    for (m = 0; m < 8; m++)
    {
        OLED_WR_Byte(0xb0 + m, 0);     // page0-page1
        OLED_WR_Byte(0x00, 0);         // low column start address
        OLED_WR_Byte(0x10, 0);         // high column start address

        for (n = 0; n < 128; n++)
        {
            OLED_WR_Byte(fill_Data, 1);
        }
    }
}

void OLED_Set_Pos(unsigned char x, unsigned char y)
{
    OLED_WR_Byte(0xb0 + y, e_Oled_Cmd);
    OLED_WR_Byte(((x & 0xf0) >> 4) | 0x10, e_Oled_Cmd);
    OLED_WR_Byte((x & 0x0f), e_Oled_Cmd);
}

void OLED_Display_On(void)
{
    OLED_WR_Byte(0X8D, e_Oled_Cmd);
    OLED_WR_Byte(0X14, e_Oled_Cmd);  // DCDC ON
    OLED_WR_Byte(0XAF, e_Oled_Cmd);  // DISPLAY ON
}

void OLED_Display_Off(void)
{
    OLED_WR_Byte(0X8D, e_Oled_Cmd);
    OLED_WR_Byte(0X10, e_Oled_Cmd);  // DCDC OFF
    OLED_WR_Byte(0XAE, e_Oled_Cmd);  // DISPLAY OFF
}

void OLED_Clear(void)
{
    uint8_t i, n;

    for (i = 0; i < 8; i++)
    {
        OLED_WR_Byte(0xb0 + i, e_Oled_Cmd);
        OLED_WR_Byte(0x00, e_Oled_Cmd);
        OLED_WR_Byte(0x10, e_Oled_Cmd);

        for (n = 0; n < 128; n++)
        {
            OLED_WR_Byte(0, e_Oled_Data);
        }
    }
}

void OLED_On(void)
{
    uint8_t i, n;

    for (i = 0; i < 8; i++)
    {
        OLED_WR_Byte(0xb0 + i, e_Oled_Cmd);
        OLED_WR_Byte(0x00, e_Oled_Cmd);
        OLED_WR_Byte(0x10, e_Oled_Cmd);

        for (n = 0; n < 128; n++)
        {
            OLED_WR_Byte(1, e_Oled_Data);
        }
    }
}

// x: 0~127
// y: 0~63
// size: 16/12
#define Max_Column		128
void OLED_ShowChar(uint8_t x, uint8_t y, uint8_t chr, uint8_t Char_Size)
{
    unsigned char c = 0, i = 0;
    c = chr - ' ';

    if (x > Max_Column - 1)
    {
        x = 0;
        y = y + 2;
    }

    if (Char_Size == 16)
    {
        OLED_Set_Pos(x, y);
        for (i = 0; i < 8; i++)
        {
            OLED_WR_Byte(F8X16[c * 16 + i], e_Oled_Data);
        }

        OLED_Set_Pos(x, y + 1);
        for (i = 0; i < 8; i++)
        {
            OLED_WR_Byte(F8X16[c * 16 + i + 8], e_Oled_Data);
        }
    }
    else
    {
        OLED_Set_Pos(x, y);
        for (i = 0; i < 6; i++)
        {
            OLED_WR_Byte(F6x8[c][i], e_Oled_Data);
        }
    }
}

/**************************************************************************
Function: Find m to the NTH power
Input   : m: base number, n: power number
Output  : none
函数功能：求m的n次方的函数
入口参数：m：底数，n：次方数
返回  值：无
**************************************************************************/
uint32_t OLED_Pow(uint8_t m, uint8_t n)
{
    uint32_t result = 1;

    while (n--)
    {
        result *= m;
    }

    return result;
}

void OLED_ShowNum(uint8_t x, uint8_t y, uint32_t num, uint8_t len, uint8_t size2)
{
    uint8_t t, temp;
    uint8_t enshow = 0;

    for (t = 0; t < len; t++)
    {
        temp = (num / OLED_Pow(10, len - t - 1)) % 10;

        if (enshow == 0 && t < (len - 1))
        {
            if (temp == 0)
            {
                OLED_ShowChar(x + (size2 / 2) * t, y, ' ', size2);
                continue;
            }
            else
            {
                enshow = 1;
            }
        }

        OLED_ShowChar(x + (size2 / 2) * t, y, temp + '0', size2);
    }
}

void OLED_ShowString(uint8_t x, uint8_t y, uint8_t *chr)
{
    unsigned char j = 0;

    while (chr[j] != '\0')
    {
        OLED_ShowChar(x, y, chr[j], 8);
        x += 8;

        if (x > 120)
        {
            x = 0;
            y += 2;
        }

        j++;
    }
}

void OLED_Init(void)
{
    OLED_WR_Byte(0xAE, e_Oled_Cmd);   // -- display off
    OLED_WR_Byte(0x00, e_Oled_Cmd);   // --- set low column address
    OLED_WR_Byte(0x10, e_Oled_Cmd);   // --- set high column address
    OLED_WR_Byte(0x40, e_Oled_Cmd);   // -- set start line address
    OLED_WR_Byte(0xB0, e_Oled_Cmd);   // -- set page address
    OLED_WR_Byte(0x81, e_Oled_Cmd);   // contract control
    OLED_WR_Byte(0xFF, e_Oled_Cmd);   // -- 128
    OLED_WR_Byte(0xA1, e_Oled_Cmd);   // set segment remap
    OLED_WR_Byte(0xA6, e_Oled_Cmd);   // -- normal / reverse
    OLED_WR_Byte(0xA8, e_Oled_Cmd);   // -- set multiplex ratio (1 to 64)
    OLED_WR_Byte(0x3F, e_Oled_Cmd);   // -- 1/32 duty
    OLED_WR_Byte(0xC8, e_Oled_Cmd);   // Com scan direction
    OLED_WR_Byte(0xD3, e_Oled_Cmd);   // - set display offset
    OLED_WR_Byte(0x00, e_Oled_Cmd);   //

    OLED_WR_Byte(0xD5, e_Oled_Cmd);   // set osc division
    OLED_WR_Byte(0x80, e_Oled_Cmd);   //

    OLED_WR_Byte(0xD8, e_Oled_Cmd);   // set area color mode off
    OLED_WR_Byte(0x05, e_Oled_Cmd);   //

    OLED_WR_Byte(0xD9, e_Oled_Cmd);   // Set Pre-Charge Period
    OLED_WR_Byte(0xF1, e_Oled_Cmd);   //

    OLED_WR_Byte(0xDA, e_Oled_Cmd);   // set com pin configuration
    OLED_WR_Byte(0x12, e_Oled_Cmd);   //

    OLED_WR_Byte(0xDB, e_Oled_Cmd);   // set Vcomh
    OLED_WR_Byte(0x30, e_Oled_Cmd);   //

    OLED_WR_Byte(0x8D, e_Oled_Cmd);   // set charge pump enable
    OLED_WR_Byte(0x14, e_Oled_Cmd);   //

    OLED_WR_Byte(0xAF, e_Oled_Cmd);   // -- turn on oled panel
    delay_ms(500);
    OLED_Clear();
    printf("OLED Init OK\r\n");
}

/**************************************************************************
Function: Displays 2 numbers
Input   : x,y: starting coordinate; Len: The number of digits; Size: font size; Mode: mode, 0, fill mode, 1, overlay mode; Num: value (0 ~ 4294967295);
Output  : none
函数功能：显示2个数字
入口参数：x,y :起点坐标; len :数字的位数; size:字体大小; mode:模式, 0,填充模式, 1,叠加模式; num:数值(0~4294967295);	 
返回  值：无
**************************************************************************/
void OLED_ShowNumber(uint8_t x, uint8_t y, uint32_t num, uint8_t len, uint8_t size)
{
    uint8_t t, temp;
    uint8_t enshow = 0;

    for (t = 0; t < len; t++)
    {
        temp = (num / OLED_Pow(10, len - t - 1)) % 10;

        if (enshow == 0 && t < (len - 1))
        {
            if (temp == 0)
            {
                OLED_ShowChar(x + (size / 2) * t, y, ' ', size);
                continue;
            }
            else
            {
                enshow = 1;
            }
        }
        OLED_ShowChar(x + (size / 2) * t, y, temp + '0', size);
    }
} 

void OLED_Display(void)
{
    static uint8_t s_u8DisplayLine = 0; // 当前显示的行，初始为0

    char chrbuf[20] = {0};

    switch (s_u8DisplayLine)
    {
        case 0:
            sprintf(chrbuf, "key_num:%d", key_num);
            OLED_ShowString(0, 0, (unsigned char *)chrbuf);
            break;
        case 1:
            sprintf(chrbuf, "TASK:%d", Do_TASK);
            OLED_ShowString(0, 2, (unsigned char *)chrbuf);
            break;
        case 2:
            sprintf(chrbuf, "Num:%d", (int)Set_Num);
            OLED_ShowString(0, 4, (unsigned char *)chrbuf);
            break;
        case 3:
            sprintf(chrbuf, "Yaw:%3d", (int)Key_Set_Yaw);
            OLED_ShowString(0, 6, (unsigned char *)chrbuf);
            break;
        default:
            break;
    }

    // 更新显示的行号，确保下一次显示的是不同的行
    s_u8DisplayLine++;
    if (s_u8DisplayLine >= 4)  // 如果行号超过最后一行，则重置为0
    {
        s_u8DisplayLine = 0;
    }
}
