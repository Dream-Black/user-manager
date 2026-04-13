# ProjectHub 部署指南

## 项目架构

```
user-manager/
├── api/              # ASP.NET Core 9 后端
│   └── Dockerfile
├── projecthub/       # Vue 3 + Vite 前端
│   ├── Dockerfile
│   └── nginx.conf    # Nginx 配置（含反向代理 /api）
├── docker-compose.yml
├── .env.example
└── .github/
    └── workflows/
        └── deploy.yml  # GitHub Actions CI/CD
```

**技术栈**：ASP.NET Core 9 | Vue 3 | MySQL 8 | Docker Compose | Nginx | GitHub Actions

---

## 第一步：服务器环境准备

登录你的腾讯云服务器，执行以下命令：

```bash
# 1. 安装 Docker（如果没有）
curl -fsSL https://get.docker.com | sh
systemctl enable docker && systemctl start docker

# 2. 安装 Docker Compose Plugin（如果没有）
apt-get install -y docker-compose-plugin   # Ubuntu/Debian
# 或：yum install -y docker-compose-plugin  # CentOS

# 3. 创建项目目录并克隆代码
mkdir -p /opt/projecthub
cd /opt/projecthub
git clone git@github.com:Dream-Black/user-manager.git .
```

---

## 第二步：配置环境变量

```bash
# 复制示例文件
cp .env.example .env

# 编辑，填入真实值
vim .env
```

`.env` 内容示例：
```env
MYSQL_ROOT_PASSWORD=你的强密码
DEEPSEEK_API_KEY=sk-xxxxxxxx   # 如果用 AI 功能才需要
```

---

## 第三步：首次手动部署

```bash
cd /opt/projecthub

# 构建并启动所有服务
docker compose up -d --build

# 查看运行状态
docker compose ps

# 查看日志
docker compose logs -f
```

启动成功后：
- 前端：`http://你的服务器IP`
- 后端 API：`http://你的服务器IP:5000`
- Swagger：`http://你的服务器IP:5000/swagger`

---

## 第四步：配置 GitHub Actions CI/CD

### 4.1 生成服务器 SSH Key

在**服务器**上执行：
```bash
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github_deploy
cat ~/.ssh/github_deploy.pub >> ~/.ssh/authorized_keys
cat ~/.ssh/github_deploy   # 复制这个私钥，下一步用
```

### 4.2 在 GitHub 仓库添加 Secrets

进入仓库 → Settings → Secrets and variables → Actions → New repository secret

| Secret 名称 | 值 |
|-------------|-----|
| `SERVER_HOST` | 腾讯云服务器公网 IP |
| `SERVER_USER` | 登录用户名（通常是 `ubuntu` 或 `root`） |
| `SERVER_SSH_KEY` | 上一步的**私钥**内容（`github_deploy` 文件内容） |

> `GITHUB_TOKEN` 是 GitHub 自动提供的，不需要手动添加。

### 4.3 让服务器可以拉取 GHCR 镜像

在服务器上预先登录一次：
```bash
docker login ghcr.io -u 你的GitHub用户名 -p 你的GitHub_Token
```
> GitHub Token 在 GitHub → Settings → Developer settings → Personal access tokens 创建，勾选 `read:packages`

### 4.4 触发 CI/CD

将所有新增文件推送到 GitHub：
```bash
git add .
git commit -m "feat: add Docker & CI/CD configuration"
git push origin main
```

推送后，GitHub Actions 会自动：
1. ✅ 编译检查后端 .NET 代码
2. ✅ 构建前端 Vue 3 项目
3. ✅ 打包 Docker 镜像推送到 GHCR
4. ✅ SSH 到腾讯云服务器，拉取新镜像并滚动更新

---

## 常用运维命令

```bash
# 查看所有容器状态
docker compose ps

# 查看某个服务日志
docker compose logs -f api
docker compose logs -f frontend

# 重启某个服务
docker compose restart api

# 完全重建（代码有大改动时）
docker compose down && docker compose up -d --build

# 进入数据库
docker compose exec mysql mysql -u root -p projecthub
```

---

## 故障排查

| 问题 | 排查方式 |
|------|---------|
| 前端 502 / 无法访问 API | `docker compose logs frontend` 检查 Nginx，确认 api 容器健康 |
| 数据库连接失败 | `docker compose logs api`，检查 `.env` 中密码是否正确 |
| CI/CD SSH 连接失败 | 检查 `SERVER_HOST`/`SERVER_USER`/`SERVER_SSH_KEY` 三个 Secret |
| 镜像拉取 403 | 确认 GHCR 登录成功，`docker login ghcr.io` |

---

## 整体流程图

```
开发者 push → GitHub Actions
                  ├─ 编译检查（后端 .NET / 前端 npm build）
                  ├─ 构建 Docker 镜像
                  ├─ 推送到 GHCR
                  └─ SSH 到腾讯云
                          └─ docker compose pull + up -d
                                  ├─ MySQL（数据持久化）
                                  ├─ API（ASP.NET Core）
                                  └─ Frontend（Nginx + Vue）
```
