"""
配置管理模块
"""
import os
import json
from pathlib import Path

# 项目根目录
BASE_DIR = Path(__file__).parent
CONFIG_FILE = BASE_DIR / "config.json"

# 默认配置
DEFAULT_CONFIG = {
    "port": 6789,
    "host": "0.0.0.0",
    "allowed_paths": [],  # 允许访问的路径列表
    "max_file_size": 50 * 1024 * 1024,  # 50MB
    "image_extensions": [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"],
    "auto_start": True,
    "log_level": "INFO"
}


class Config:
    """配置类"""

    def __init__(self):
        self._config = DEFAULT_CONFIG.copy()
        self.load()

    def load(self):
        """从文件加载配置"""
        if CONFIG_FILE.exists():
            try:
                with open(CONFIG_FILE, "r", encoding="utf-8") as f:
                    user_config = json.load(f)
                    self._config.update(user_config)
            except Exception as e:
                print(f"加载配置文件失败: {e}")

    def save(self):
        """保存配置到文件"""
        try:
            with open(CONFIG_FILE, "w", encoding="utf-8") as f:
                json.dump(self._config, f, indent=2, ensure_ascii=False)
            return True
        except Exception as e:
            print(f"保存配置文件失败: {e}")
            return False

    def get(self, key: str, default=None):
        """获取配置项"""
        return self._config.get(key, default)

    def set(self, key: str, value):
        """设置配置项"""
        self._config[key] = value

    @property
    def port(self) -> int:
        return self._config.get("port", 6789)

    @property
    def host(self) -> str:
        return self._config.get("host", "0.0.0.0")

    @property
    def allowed_paths(self) -> list:
        return self._config.get("allowed_paths", [])

    @property
    def image_extensions(self) -> list:
        return self._config.get("image_extensions", DEFAULT_CONFIG["image_extensions"])

    def add_allowed_path(self, path: str):
        """添加允许访问的路径"""
        if path not in self._config["allowed_paths"]:
            self._config["allowed_paths"].append(path)
            self.save()

    def is_path_allowed(self, path: str) -> bool:
        """检查路径是否在允许列表中"""
        if not self.allowed_paths:
            # 如果没有配置，允许所有本地路径
            return True
        return any(path.startswith(allowed) for allowed in self.allowed_paths)


# 全局配置实例
config = Config()
