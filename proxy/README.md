# 本地代理服务

用于解决浏览器无法直接访问本地文件的问题。

## 功能

- 📁 文件浏览：列出目录内容
- 📂 文件夹树：获取多层级目录结构
- 🖼️ 图片读取：流式读取图片文件
- 📚 漫画扫描：扫描漫画文件夹结构
- ⚙️ 配置管理：动态添加允许访问的路径

## 快速开始

### 方式1：直接运行Python

```bash
# 安装依赖
pip install -r requirements.txt

# 运行服务
python main.py
```

### 方式2：打包成EXE运行

```bash
# 运行打包脚本
build.bat

# 运行生成的exe
dist\ProxyService.exe
```

## 访问地址

- 本地访问：http://localhost:6789
- 局域网访问：http://你的IP:6789
- API文档：http://localhost:6789/docs

## API接口

### 健康检查
```
GET /health
```

### 系统信息
```
GET /system/info
```

### 列出文件
```
GET /files/list?path=D:/MyComics
```

### 获取文件夹树
```
GET /files/tree?path=D:/MyComics&depth=2
```

### 读取文件
```
GET /files/read?path=D:/MyComics/咒术回战/第1话/001.jpg
```

### 扫描漫画
```
GET /comics/scan?path=D:/MyComics
```

## 配置说明

编辑 `config.json`:

```json
{
  "port": 6789,           // 服务端口
  "host": "0.0.0.0",     // 监听地址
  "allowed_paths": [],    // 允许访问的路径（空=全部）
  "max_file_size": 52428800,  // 最大文件大小(50MB)
  "image_extensions": [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"]
}
```

## 开机自启

### Windows任务计划程序
1. 打开"任务计划程序"
2. 创建基本任务
3. 触发器：计算机启动时
4. 操作：启动程序，选择 ProxyService.exe
5. 完成

### 快捷方式方式
1. 右键 ProxyService.exe
2. 创建快捷方式
3. 把快捷方式放入：
   `C:\Users\你的用户名\AppData\Roaming\Microsoft\Windows\开始菜单\程序\启动`

## 端口被占用？

修改 `config.json` 中的 `port` 为其他端口，如 `6790`。

## 常见问题

### Q: 提示"路径不在允许列表中"
A: 在 config.json 中添加路径，或清空 allowed_paths 允许所有路径

### Q: 图片加载失败
A: 检查：1. 路径是否正确 2. 文件是否存在 3. 文件大小是否超限

### Q: 服务无法启动
A: 检查端口是否被占用：`netstat -ano | findstr 6789`
