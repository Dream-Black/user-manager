"""
本地代理服务 - FastAPI
用于解决浏览器无法直接访问本地文件的问题
端口: 6789
"""
import os
import socket
import platform
import uvicorn
from pathlib import Path
from fastapi import FastAPI, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse, StreamingResponse, JSONResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
import urllib.parse

from config import config
from scanner import scanner

# 创建FastAPI应用
app = FastAPI(
    title="本地代理服务",
    description="用于访问本地文件的代理服务",
    version="1.0.0"
)

# 添加CORS中间件
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # 允许所有来源
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ==================== 系统接口 ====================

@app.get("/health")
async def health_check():
    """健康检查"""
    return {
        "status": "ok",
        "service": "LocalProxy",
        "version": "1.0.0"
    }


@app.get("/system/info")
async def get_system_info():
    """获取系统信息"""
    hostname = socket.gethostname()

    # 获取本机IP
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        local_ip = s.getsockname()[0]
        s.close()
    except:
        local_ip = "127.0.0.1"

    return {
        "hostname": hostname,
        "platform": platform.system(),
        "platform_version": platform.version(),
        "local_ip": local_ip,
        "allowed_paths": config.allowed_paths
    }


# ==================== 文件接口 ====================

@app.get("/files/list")
async def list_files(path: str = Query(..., description="文件路径")):
    """
    列出目录内容
    """
    decoded_path = urllib.parse.unquote(path)

    # 安全检查
    if not config.is_path_allowed(decoded_path):
        raise HTTPException(status_code=403, detail="路径不在允许列表中")

    result = scanner.list_directory(decoded_path)

    if not result.get("exists"):
        raise HTTPException(status_code=404, detail="路径不存在")

    return JSONResponse(result)


@app.get("/files/tree")
async def get_folder_tree(path: str = Query(...), depth: int = Query(2, ge=0, le=5)):
    """
    获取文件夹树结构
    depth: 递归深度，默认2层
    """
    decoded_path = urllib.parse.unquote(path)

    if not config.is_path_allowed(decoded_path):
        raise HTTPException(status_code=403, detail="路径不在允许列表中")

    result = scanner.get_folder_tree(decoded_path, depth)

    if not result.get("exists"):
        raise HTTPException(status_code=404, detail="路径不存在")

    return JSONResponse(result)


@app.get("/files/read")
async def read_file(path: str = Query(..., description="文件路径")):
    """
    读取文件（流式传输）
    用于图片预览
    """
    decoded_path = urllib.parse.unquote(path)

    if not config.is_path_allowed(decoded_path):
        raise HTTPException(status_code=403, detail="路径不在允许列表中")

    if not os.path.exists(decoded_path):
        raise HTTPException(status_code=404, detail="文件不存在")

    if not os.path.isfile(decoded_path):
        raise HTTPException(status_code=400, detail="不是文件")

    # 获取文件扩展名
    ext = Path(decoded_path).suffix.lower()

    # MIME类型映射
    mime_types = {
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".png": "image/png",
        ".gif": "image/gif",
        ".webp": "image/webp",
        ".bmp": "image/bmp",
        ".svg": "image/svg+xml",
        ".pdf": "application/pdf",
        ".txt": "text/plain",
    }

    media_type = mime_types.get(ext, "application/octet-stream")

    # 检查文件大小
    file_size = os.path.getsize(decoded_path)
    max_size = config.get("max_file_size", 50 * 1024 * 1024)

    if file_size > max_size:
        raise HTTPException(status_code=413, detail=f"文件过大 ({file_size} bytes)")

    # 流式读取
    def iterfile():
        with open(decoded_path, "rb") as f:
            while True:
                chunk = f.read(1024 * 1024)  # 1MB chunks
                if not chunk:
                    break
                yield chunk

    return StreamingResponse(
        iterfile(),
        media_type=media_type,
        headers={
            "Content-Length": str(file_size),
            "Content-Disposition": f'inline; filename="{os.path.basename(decoded_path)}"',
            "Cache-Control": "public, max-age=3600"
        }
    )


@app.get("/comics/scan")
async def scan_comics(path: str = Query(..., description="漫画根目录路径")):
    """
    扫描漫画文件夹
    返回漫画列表及其章节信息
    """
    decoded_path = urllib.parse.unquote(path)

    if not config.is_path_allowed(decoded_path):
        raise HTTPException(status_code=403, detail="路径不在允许列表中")

    if not os.path.exists(decoded_path):
        raise HTTPException(status_code=404, detail="路径不存在")

    comics = scanner.scan_comics(decoded_path)

    return JSONResponse({
        "path": decoded_path,
        "comicCount": len(comics),
        "comics": comics
    })


# ==================== 配置接口 ====================

class AddPathRequest(BaseModel):
    path: str


class TestPathRequest(BaseModel):
    path: str


@app.post("/resource-paths/test")
async def test_resource_path(request: TestPathRequest):
    """
    测试资源路径是否存在
    """
    test_path = request.path

    if not os.path.exists(test_path):
        return JSONResponse({
            "success": False,
            "message": "路径不存在",
            "path": test_path
        })

    return JSONResponse({
        "success": True,
        "message": "路径存在",
        "path": test_path
    })


@app.post("/config/paths")
async def add_allowed_path(request: AddPathRequest):
    """添加允许访问的路径"""
    path = request.path
    if not os.path.exists(path):
        raise HTTPException(status_code=404, detail="路径不存在")

    if not os.path.isdir(path):
        raise HTTPException(status_code=400, detail="路径不是目录")

    config.add_allowed_path(path)
    return {"success": True, "allowed_paths": config.allowed_paths}


@app.get("/config/paths")
async def get_allowed_paths():
    """获取允许访问的路径列表"""
    return {"allowed_paths": config.allowed_paths}


# ==================== 主程序 ====================

def get_local_ip():
    """获取本机IP地址"""
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except:
        return "127.0.0.1"


def main():
    """主函数（标准模式，用于直接 python main.py）"""
    host = config.host
    port = config.port
    local_ip = get_local_ip()

    print("=" * 50)
    print("  本地代理服务 v1.0.0")
    print("=" * 50)
    print(f"  端口: {port}")
    print(f"  地址: http://localhost:{port}")
    print(f"  本机IP: http://{local_ip}:{port}")
    print(f"  允许路径: {config.allowed_paths if config.allowed_paths else '全部'}")
    print("=" * 50)
    print()
    print("  接口文档: http://localhost:{}/docs".format(port))
    print()
    print("  💡 提示: 如需热更新开发，请使用:")
    print("     python -m uvicorn main:app --reload --host 0.0.0.0 --port 6789")
    print()

    # 启动服务
    uvicorn.run(
        app,
        host=host,
        port=port,
        log_level="info"
    )


if __name__ == "__main__":
    main()
