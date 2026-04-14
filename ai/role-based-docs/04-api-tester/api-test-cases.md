# API测试用例

## 📋 测试用例清单

### 计算机管理 API
| 用例ID | 用例名称 | 优先级 |
|--------|----------|--------|
| TC-COM-001 | 获取当前电脑-正常 | P0 |
| TC-COM-002 | 获取当前电脑-新电脑自动创建 | P0 |
| TC-COM-003 | 获取所有电脑列表 | P1 |
| TC-COM-004 | 创建电脑 | P0 |
| TC-COM-005 | 更新电脑名称 | P1 |
| TC-COM-006 | 发送心跳-正常 | P0 |

### 资源路径 API
| 用例ID | 用例名称 | 优先级 |
|--------|----------|--------|
| TC-PATH-001 | 获取资源路径列表 | P0 |
| TC-PATH-002 | 创建资源路径-正常 | P0 |
| TC-PATH-003 | 创建资源路径-路径格式验证 | P1 |
| TC-PATH-004 | 创建资源路径-重复类型 | P1 |
| TC-PATH-005 | 更新资源路径 | P0 |
| TC-PATH-006 | 删除资源路径 | P0 |

### 漫画 API
| 用例ID | 用例名称 | 优先级 |
|--------|----------|--------|
| TC-COMIC-001 | 获取漫画列表-正常 | P0 |
| TC-COMIC-002 | 获取漫画列表-按类型筛选 | P1 |
| TC-COMIC-003 | 获取漫画列表-搜索名称 | P1 |
| TC-COMIC-004 | 获取漫画详情 | P0 |
| TC-COMIC-005 | 更新漫画名称 | P0 |
| TC-COMIC-006 | 删除漫画 | P0 |
| TC-COMIC-007 | 上传封面-base64格式 | P0 |
| TC-COMIC-008 | 扫描漫画文件夹-正常 | P0 |

### 章节 API
| 用例ID | 用例名称 | 优先级 |
|--------|----------|--------|
| TC-CHAP-001 | 获取章节列表 | P0 |
| TC-CHAP-002 | 获取章节页面 | P0 |

---

## 📝 核心测试用例详情

### TC-COM-001: 获取当前电脑-正常

**请求**
```
GET /api/computers/current
Header: X-Computer-Hostname: DESKTOP-TEST123
```

**预期响应**
```json
{
  "id": 1,
  "name": "测试电脑",
  "hostName": "DESKTOP-TEST123",
  "isOnline": true,
  "lastHeartbeat": "2026-04-14T20:00:00Z"
}
```

---

### TC-COM-002: 获取当前电脑-新电脑自动创建

**请求**
```
GET /api/computers/current
Header: X-Computer-Hostname: NEW-HOST-001
```

**预期结果**
- 创建新电脑记录
- name = hostName = "NEW-HOST-001"
- 返回HTTP 201

---

### TC-PATH-002: 创建资源路径-正常

**请求**
```
POST /api/resource-paths
Content-Type: application/json

{
  "computerId": 1,
  "type": "comic",
  "path": "D:/MyComics/",
  "isEnabled": true
}
```

**预期响应**: HTTP 201 Created

---

### TC-PATH-003: 创建资源路径-路径格式验证

**请求**
```
POST /api/resource-paths
{
  "path": "D:/MyComics"  // 缺少结尾斜杠
}
```

**预期响应**: HTTP 400
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "路径必须以 / 结尾"
  }
}
```

---

### TC-COMIC-001: 获取漫画列表

**请求**
```
GET /api/comics?computerId=1&type=comic
```

**预期响应**
```json
{
  "items": [
    {
      "id": 1,
      "displayName": "咒术回战",
      "folderName": "咒术回战",
      "type": "manga",
      "thumbnailBase64": null,
      "chapterCount": 5
    }
  ],
  "total": 1
}
```

---

### TC-COMIC-007: 上传封面

**请求**
```
POST /api/comics/1/thumbnail
Content-Type: application/json

{
  "thumbnailBase64": "data:image/jpeg;base64,/9j/4AAQSkZJRg..."
}
```

**预期结果**
- HTTP 200
- 数据库 thumbnailBase64 字段已更新

---

### TC-CHAP-002: 获取章节页面

**请求**
```
GET /api/chapters/1/pages
```

**预期响应**
```json
{
  "chapterId": 1,
  "pages": [
    {
      "index": 0,
      "filename": "001.jpg",
      "url": "http://localhost:6789/files/read?path=..."
    }
  ]
}
```

---

## ⚠️ 异常测试用例

### TC-ERR-001: 缺少请求头
- GET /api/computers/current 无 X-Computer-Hostname
- 预期: HTTP 400

### TC-ERR-002: 资源不存在
- GET /api/comics/99999
- 预期: HTTP 404

### TC-ERR-003: 重复创建
- POST /api/resource-paths 同一电脑同一类型
- 预期: HTTP 409
