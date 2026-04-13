#!/bin/bash
# 版本号自动递增脚本
# 遵循语义化版本规范: https://semver.org/lang/zh-CN/

VERSION_FILE="VERSION"

# 读取当前版本
CURRENT_VERSION=$(cat "$VERSION_FILE" | tr -d '[:space:]')
echo "当前版本: $CURRENT_VERSION"

# 解析版本号
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# 递增修订号 (PATCH)
PATCH=$((PATCH + 1))

# 处理进位
if [ "$PATCH" -ge 10 ]; then
    PATCH=0
    MINOR=$((MINOR + 1))
fi

if [ "$MINOR" -ge 10 ]; then
    MINOR=0
    MAJOR=$((MAJOR + 1))
fi

# 生成新版本
NEW_VERSION="$MAJOR.$MINOR.$PATCH"
echo "新版本: $NEW_VERSION"

# 写入版本文件
echo "$NEW_VERSION" > "$VERSION_FILE"

# 生成版本信息 JSON
echo "{
  \"version\": \"$NEW_VERSION\",
  \"buildTime\": \"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\",
  \"buildDate\": \"$(date -u +"%Y-%m-%d")\",
  \"commit\": \"$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')\"
}" > "version.json"

echo "版本号已更新到 $NEW_VERSION"
