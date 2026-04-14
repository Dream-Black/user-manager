"""
文件扫描模块
"""
import os
import re
from pathlib import Path
from typing import List, Dict, Optional
from config import config


class NaturalSort:
    """自然排序比较器"""

    @staticmethod
    def natural_key(path: str) -> List:
        """生成自然排序的key"""
        return [
            int(text) if text.isdigit() else text.lower()
            for text in re.split(r'(\d+)', path)
        ]

    @staticmethod
    def sort_list(items: List[str]) -> List[str]:
        """对列表进行自然排序"""
        return sorted(items, key=NaturalSort.natural_key)


class FileScanner:
    """文件扫描器"""

    def __init__(self):
        self.image_extensions = config.image_extensions

    def is_image_file(self, filename: str) -> bool:
        """检查是否为图片文件"""
        ext = Path(filename).suffix.lower()
        return ext in self.image_extensions

    def list_directory(self, path: str) -> Dict:
        """
        列出目录内容
        返回: { folders: [...], files: [...], images: [...] }
        """
        path = self._normalize_path(path)

        if not os.path.exists(path):
            return {"exists": False, "path": path, "folders": [], "files": [], "images": []}

        if not os.path.isdir(path):
            return {"exists": False, "path": path, "error": "不是目录"}

        folders = []
        files = []
        images = []

        try:
            for item in os.listdir(path):
                item_path = os.path.join(path, item)
                if os.path.isdir(item_path):
                    folders.append(item)
                else:
                    files.append(item)
                    if self.is_image_file(item):
                        images.append(item)
        except PermissionError:
            return {"exists": True, "path": path, "error": "权限不足"}
        except Exception as e:
            return {"exists": True, "path": path, "error": str(e)}

        # 自然排序
        folders = NaturalSort.sort_list(folders)
        files = NaturalSort.sort_list(files)
        images = NaturalSort.sort_list(images)

        return {
            "exists": True,
            "path": path,
            "folderCount": len(folders),
            "fileCount": len(files),
            "folders": folders,
            "files": files,
            "images": images
        }

    def get_folder_tree(self, path: str, depth: int = 2) -> Dict:
        """
        获取文件夹树结构
        depth: 递归深度，0=仅当前目录
        """
        path = self._normalize_path(path)

        if not os.path.exists(path):
            return {"exists": False, "path": path}

        if not os.path.isdir(path):
            return {"exists": False, "path": path, "error": "不是目录"}

        result = {
            "name": os.path.basename(path) or path,
            "path": path,
            "folders": [],
            "images": []
        }

        if depth <= 0:
            return result

        try:
            for item in os.listdir(path):
                item_path = os.path.join(path, item)
                if os.path.isdir(item_path):
                    sub_tree = self.get_folder_tree(item_path, depth - 1)
                    result["folders"].append(sub_tree)
                elif self.is_image_file(item):
                    result["images"].append(item)
        except PermissionError:
            result["error"] = "权限不足"
        except Exception as e:
            result["error"] = str(e)

        return result

    def read_file(self, path: str) -> Optional[bytes]:
        """
        读取文件内容
        返回: bytes 或 None
        """
        path = self._normalize_path(path)

        if not os.path.exists(path):
            return None

        if not os.path.isfile(path):
            return None

        # 检查文件大小
        file_size = os.path.getsize(path)
        max_size = config.get("max_file_size", 50 * 1024 * 1024)
        if file_size > max_size:
            return None

        try:
            with open(path, "rb") as f:
                return f.read()
        except Exception:
            return None

    def scan_comics(self, base_path: str) -> List[Dict]:
        """
        扫描漫画文件夹
        结构: 根目录/漫画名/章节/图片
        返回漫画列表
        """
        base_path = self._normalize_path(base_path)

        if not os.path.exists(base_path):
            return []

        if not os.path.isdir(base_path):
            return []

        comics = []

        try:
            # 一级目录 = 漫画
            for comic_name in os.listdir(base_path):
                comic_path = os.path.join(base_path, comic_name)
                if not os.path.isdir(comic_path):
                    continue

                chapters = []
                image_count = 0

                # 扫描二级内容
                for chapter_name in os.listdir(comic_path):
                    chapter_path = os.path.join(comic_path, chapter_name)
                    
                    if os.path.isdir(chapter_path):
                        # 子文件夹 = 章节
                        pics = [
                            f for f in os.listdir(chapter_path)
                            if os.path.isfile(os.path.join(chapter_path, f))
                            and self.is_image_file(f)
                        ]
                        if pics:
                            # 自然排序
                            pics = NaturalSort.sort_list(pics)
                            chapters.append({
                                "name": chapter_name,
                                "path": chapter_path,
                                "imageCount": len(pics),
                                "images": pics  # 返回排序后的图片列表
                            })
                            image_count += len(pics)
                    elif self.is_image_file(chapter_name):
                        # 直接是图片 = 单章节（统计到 image_count）
                        image_count += 1

                # 如果有章节或图片，就返回
                if chapters or image_count > 0:
                    # 如果没有章节但有图片，创建虚拟章节
                    if not chapters and image_count > 0:
                        # 单章节：收集所有图片
                        all_pics = NaturalSort.sort_list([
                            f for f in os.listdir(comic_path)
                            if os.path.isfile(os.path.join(comic_path, f))
                            and self.is_image_file(f)
                        ])
                        chapters.append({
                            "name": comic_name,  # 单章节用漫画名
                            "path": comic_path,
                            "imageCount": len(all_pics),
                            "images": all_pics  # 返回图片列表
                        })
                    
                    comics.append({
                        "name": comic_name,
                        "path": comic_path,
                        "chapterCount": len(chapters),
                        "imageCount": image_count,
                        "chapters": chapters
                    })

        except PermissionError:
            pass
        except Exception:
            pass

        return comics

    def _normalize_path(self, path: str) -> str:
        """标准化路径"""
        if not path:
            return ""
        # 转换正斜杠为反斜杠
        path = path.replace("/", "\\")
        # 处理URL编码
        import urllib.parse
        path = urllib.parse.unquote(path)
        return path


# 全局扫描器实例
scanner = FileScanner()
