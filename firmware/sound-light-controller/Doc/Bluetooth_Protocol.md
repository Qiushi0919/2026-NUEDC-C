# 小车蓝牙串口显示协议

## 硬件与串口参数

- MCU：MSPM0G3507
- 蓝牙串口：UART0
- PA10：UART0 TX，连接蓝牙模块 RXD
- PA11：UART0 RX，连接蓝牙模块 TXD
- 波特率：115200
- 数据格式：8 数据位、无校验、1 停止位（8N1）
- 无硬件流控

本文件中的 `FF 2F ... CC` 为早期 OLED 通信测试帧。比赛使用的正式双向
协议见 `Digital_Key_UART_Protocol.md`。

蓝牙模块与小车控制板必须共地。

## 帧格式

| 字段 | 长度 | 说明 |
| --- | ---: | --- |
| HEAD | 1 字节 | 固定为 `FF` |
| TYPE | 1 字节 | OLED 显示命令固定为 `2F` |
| LEN | 1 字节 | PAYLOAD 长度，范围 `01`～`10`（1～16 字节） |
| PAYLOAD | LEN 字节 | 可显示 ASCII，范围 `20`～`7E` |
| XOR | 1 字节 | `TYPE ^ LEN ^ PAYLOAD[0] ^ ...` |
| TAIL | 1 字节 | 固定为 `CC` |

接收端只有在帧头、类型、长度、可显示字符、异或校验和帧尾全部正确时，
才会把 PAYLOAD 更新到 OLED 的 `y=3` 独立显示行。现有周期显示使用
`y=0/2/4/6`，因此不会覆盖蓝牙内容；错误帧和半帧也不会清除上一条有效数据。

## 测试帧

显示 `1ab`：

```text
FF 2F 03 31 61 62 1E CC
```

校验计算：

```text
2F ^ 03 ^ 31 ^ 61 ^ 62 = 1E
```

显示 `HELLO`：

```text
FF 2F 05 48 45 4C 4C 4F 68 CC
```

故意使用错误校验、验证 OLED 保留上一条有效数据：

```text
FF 2F 03 31 61 62 00 CC
```

随后显示 `2cd`、验证最新有效帧覆盖旧内容：

```text
FF 2F 03 32 63 64 19 CC
```

串口调试助手必须选择“十六进制发送”，不要把上述十六进制数字作为普通文本发送。

## 校验生成参考

```c
uint8_t checksum = 0x2F ^ payload_length;

for (uint8_t i = 0; i < payload_length; i++)
{
    checksum ^= payload[i];
}
```
