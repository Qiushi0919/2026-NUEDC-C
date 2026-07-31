# 2026 电赛 C 题：数字钥匙实验系统

本仓库保存 C 题数字钥匙实验系统的上位机软件和声光控制模块固件。

## 系统链路

```text
120° 定位基站 → Windows 上位机 → 蓝牙串口 → MSPM0G3507 声光控制模块
                                           └→ DIP 拨码开关身份 ID
```

- 定位基站和蓝牙模块分别连接 Windows 串口。
- 蓝牙控制链路使用 `115200 8N1`。
- 声光控制模块周期上报 4 位 DIP 身份 ID，上位机将它与程序中设置的钥匙 ID 比对。
- 联机时以硬件 DIP ID 为准；控制模块离线时，上位机可使用软件设置的 ID 继续调试。
- 控制帧采用 `AA 55` 帧头和 CRC16 校验，具体见协议文档。

## 目录结构

```text
pc-software/
  src/DigitalKeyDisplay/       Windows WinForms 上位机源码
  tests/ProtocolSmoke/         串口协议冒烟测试
  CONTROL_LINK.md              上位机与控制模块联调说明

firmware/
  sound-light-controller/      MSPM0G3507 声光、蓝牙和 DIP 固件
```

## 上位机编译与测试

需要安装 .NET 7 SDK。在仓库根目录执行：

```powershell
dotnet build .\pc-software\src\DigitalKeyDisplay\DigitalKeyDisplay.csproj -c Release
dotnet run --project .\pc-software\tests\ProtocolSmoke\ProtocolSmoke.csproj -c Release
```

上位机启动后，选择定位基站串口和蓝牙控制串口，蓝牙串口波特率固定为 `115200`。两个设备应使用不同的 COM 口。

## 控制模块说明

- 蓝牙串口：`PA10` 为 TX，`PA11` 为 RX。
- `SW1～SW4` 对应身份 ID 的 `bit3～bit0`，低电平有效，拨到 ON 记为 `1`。
- `PA18`、`PB19`、`PA8` 分别控制三路指示灯，`PA27` 控制蜂鸣器。
- 控制模块约每 100 ms 上报一次 ID；上位机约每 100 ms 下发一次门锁状态。
- 超过约 500 ms 未收到有效控制帧时，固件进入安全状态并关闭声光输出。

固件工程入口位于 `firmware/sound-light-controller/keil/`，需要对应的 Keil/ARM 工具链编译。

## 协议文档

- 上位机联调说明：`pc-software/CONTROL_LINK.md`
- 控制模块协议：`firmware/sound-light-controller/Doc/Digital_Key_UART_Protocol.md`
- 蓝牙说明：`firmware/sound-light-controller/Doc/Bluetooth_Protocol.md`
