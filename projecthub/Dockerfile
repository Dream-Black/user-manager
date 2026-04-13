# ==================== 构建阶段 ====================
FROM node:22-alpine AS build
WORKDIR /app

# 先复制依赖文件，利用缓存
COPY package.json package-lock.json* ./
RUN npm install

# 复制源码并构建
COPY . .
RUN npm run build

# ==================== 运行阶段（Nginx 静态托管）====================
FROM nginx:alpine AS runtime

# 拷贝构建产物
COPY --from=build /app/dist /usr/share/nginx/html

# 拷贝 Nginx 配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 确保版本文件存在（从构建上下文复制或生成默认版本）
COPY version.json /usr/share/nginx/html/version.json

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
