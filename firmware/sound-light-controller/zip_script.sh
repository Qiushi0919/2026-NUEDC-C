#!/bin/bash

# 定义清理函数
cleanup() {
    if [ $? -eq 0 ]; then  # 只有成功时才删除日志
        rm -f "log.txt"
    fi
    exit
}

# 捕获退出信号
trap cleanup EXIT

# ======================
# 清理Keil编译生成的文件
# ======================
echo "===== 清理Keil编译文件 =====" > log.txt
echo "正在清理Keil编译生成文件..."

[ -d "keil/Listings" ] && rm -rf "keil/Listings" && echo "✅ 已删除 keil/Listings" | tee -a log.txt
[ -d "keil/Objects" ] && rm -rf "keil/Objects" && echo "✅ 已删除 keil/Objects" | tee -a log.txt

uvguix_files=(keil/empty_LP_MSPM0G3507_nortos_keil.uvguix.*)
if [ -e "${uvguix_files[0]}" ]; then
  rm -f keil/empty_LP_MSPM0G3507_nortos_keil.uvguix.*
  echo "✅ 已删除 keil/empty_LP_*.uvguix.*" | tee -a log.txt
else
  echo "⚠️ 未找到 uvguix 文件" | tee -a log.txt
fi

# ======================
# 压缩指定文件和目录
# ======================
echo -e "\n===== 压缩操作 =====" >> log.txt

# 检查压缩工具
if ! command -v zip &> /dev/null; then
    echo "❌ 错误: 未找到zip程序" | tee -a log.txt
    echo "请先执行以下命令安装:"
    echo "1. Debian/Ubuntu: sudo apt install zip"
    echo "2. CentOS/RHEL: sudo yum install zip"
    echo "3. Windows Git Bash: 下载zip.exe放到/usr/bin目录"
    read -n 1 -s -r -p "按任意键退出..."
    exit 1
fi

# 获取目录信息
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PARENT_FOLDER_NAME="$(basename "$SCRIPT_DIR")"
OUTPUT_FILE="${PARENT_FOLDER_NAME}_$(date +%Y%m%d_%H%M%S).zip"

# 检查文件/文件夹是否存在
MISSING_ITEMS=()
for folder in "BSP" "keil" "ti"; do
    [ ! -d "$folder" ] && MISSING_ITEMS+=("文件夹: $folder")
done

for file in "board.c" "board.h" "empty.c" "empty.syscfg" "ti_msp_dl_config.c" "ti_msp_dl_config.h"; do
    [ ! -f "$file" ] && MISSING_ITEMS+=("文件: $file")
done

if [ ${#MISSING_ITEMS[@]} -gt 0 ]; then
    echo "❌ 缺失项目:" | tee -a log.txt
    printf ' - %s\n' "${MISSING_ITEMS[@]}" | tee -a log.txt
    read -n 1 -s -r -p "按任意键退出..."
    exit 1
fi

# 执行压缩
echo "正在压缩到: $OUTPUT_FILE" | tee -a log.txt
if zip -r -P "RYO_TECH" "$OUTPUT_FILE" "BSP" "keil" "ti" \
    "board.c" "board.h" "empty.c" "empty.syscfg" "ti_msp_dl_config.c" "ti_msp_dl_config.h" "README.md">> log.txt 2>&1
then
    echo -e "\n✅ 压缩成功!" | tee -a log.txt
    echo "文件位置: $SCRIPT_DIR/$OUTPUT_FILE" | tee -a log.txt
    echo "压缩时间: $(date '+%Y-%m-%d %H:%M:%S')" >> log.txt
    echo -e "\n=== 压缩包内容 ===" >> log.txt
    unzip -l "$OUTPUT_FILE" | tail -n +4 | head -n -2 >> log.txt
else
    echo -e "\n❌ 压缩失败!" | tee -a log.txt
    echo "错误日志已保存到 log.txt" | tee -a log.txt
    read -n 1 -s -r -p "按任意键退出..."
    exit 1
fi

# 显示关键日志
echo -e "\n=== 执行摘要 ==="
grep -E '✅|❌|⚠️|正在|文件位置' log.txt

# 按任意键退出
echo -e "\n"
read -n 1 -s -r -p "操作完成，按任意键退出..."
exit 0