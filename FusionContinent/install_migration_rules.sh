#!/bin/bash
# UIFramework 迁移规则包安装脚本
# 使用方式: ./install_migration_rules.sh [目标项目路径]

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 获取脚本所在目录
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# 目标项目路径
TARGET_DIR="${1:-.}"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  UIFramework 迁移规则包安装器${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# 检查目标目录
if [ ! -d "$TARGET_DIR" ]; then
    echo -e "${RED}错误: 目标目录不存在: $TARGET_DIR${NC}"
    exit 1
fi

# 检查是否是Unity项目
if [ ! -d "$TARGET_DIR/Assets" ]; then
    echo -e "${YELLOW}警告: 目标目录可能不是Unity项目 (没有找到Assets文件夹)${NC}"
    read -p "是否继续? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo -e "${YELLOW}安装位置: $TARGET_DIR${NC}"
echo ""

# 创建目录结构
echo -e "${YELLOW}创建目录结构...${NC}"
mkdir -p "$TARGET_DIR/.opencode/templates"
mkdir -p "$TARGET_DIR/Tools"
mkdir -p "$TARGET_DIR/Assets/Scripts/UIFramework"

# 复制规则文件
echo -e "${YELLOW}复制规则文件...${NC}"
cp "$SCRIPT_DIR/AGENTS.md" "$TARGET_DIR/" 2>/dev/null || echo "AGENTS.md 已存在，跳过"
cp "$SCRIPT_DIR/.opencode/ui-migration.json" "$TARGET_DIR/.opencode/" 2>/dev/null || echo "ui-migration.json 已存在，跳过"
cp "$SCRIPT_DIR/.opencode/templates/"*.template "$TARGET_DIR/.opencode/templates/" 2>/dev/null || echo "模板文件已存在，跳过"

# 复制文档
echo -e "${YELLOW}复制文档...${NC}"
cp "$SCRIPT_DIR/UI_MIGRATION_RULES_README.md" "$TARGET_DIR/" 2>/dev/null || echo "README 已存在，跳过"
cp "$SCRIPT_DIR/UI_Migration_Assistant.md" "$TARGET_DIR/" 2>/dev/null || echo "迁移助手文档已存在，跳过"

# 复制Python脚本
echo -e "${YELLOW}复制自动化脚本...${NC}"
cp "$SCRIPT_DIR/Tools/ui_migration_helper.py" "$TARGET_DIR/Tools/" 2>/dev/null || echo "自动化脚本已存在，跳过"

# 检查Python
echo ""
echo -e "${YELLOW}检查Python环境...${NC}"
if command -v python3 &> /dev/null; then
    PYTHON_CMD="python3"
elif command -v python &> /dev/null; then
    PYTHON_CMD="python"
else
    echo -e "${YELLOW}警告: 未找到Python，自动化脚本可能无法运行${NC}"
    PYTHON_CMD=""
fi

if [ -n "$PYTHON_CMD" ]; then
    echo -e "${GREEN}Python已安装: $PYTHON_CMD${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  安装完成!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "已安装的文件:"
echo "  - AGENTS.md (AI规则，自动加载)"
echo "  - .opencode/ui-migration.json (规则配置)"
echo "  - .opencode/templates/ (代码模板)"
echo "  - Tools/ui_migration_helper.py (自动化脚本)"
echo "  - UI_MIGRATION_RULES_README.md (使用说明)"
echo ""
echo "使用方法:"
echo "  1. 在opencode中说: '迁移 ShopPanel'"
echo "  2. 或使用脚本: python Tools/ui_migration_helper.py ShopPanel Shop UI/Shop"
echo ""
echo "详细文档:"
echo "  - UI_MIGRATION_RULES_README.md"
echo "  - UI_Migration_Assistant.md"
echo ""
