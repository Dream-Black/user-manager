# API接口设计文档

## 📍 基地址
- **开发环境**：`http://localhost:5000/api`
- **生产环境**：`https://api.xxx.com/api`

---

## 1. 电脑管理 API

### 1.1 获取当前电脑
> 自动识别请求来源的电脑

```
GET /api/computers/current
```

**请求头**
| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| X-Computer-Hostname | string | 是 | 客户端主机名 |

**响应**
```json
{
  "id": 1,
  "name": "我的台式机",
  "hostName": "DESKTOP-ABC123",
  "isOnline": true,
  "lastHeartbeat": "2026-04-14T20:00:00Z"
}
```

**逻辑说明**
1. 根据主机名查找Computer记录
2. 不存在则自动创建（Name=主机名）
3. 更新LastHeartbeat

---

### 1.2 获取所有电脑
```
GET /api/computers
```

**响应**
```json
{
  "items": [
    {
      "id": 1,
      "name": "我的台式机",
      "hostName": "DESKTOP-ABC123",
      "isOnline": true,
      "lastHeartbeat": "2026-04-14T20:00:00Z"
    }
  ],
  "total": 1
}
```

---

### 1.3 创建/更新电脑
```
POST /api/computers
```

**请求体**
```json
{
  "name": "我的台式机",
  "hostName": "DESKTOP-ABC123"
}
```

**响应**：`201 Created`

---

### 1.4 更新电脑信息
```
PUT /api/computers/{id}
```

**请求体**
```json
{
  "name": "办公室电脑"
}
```

---

### 1.5 发送心跳
```
POST /api/computers/{id}/heartbeat
```

**响应**
```json
{
  "success": true,
  "lastHeartbeat": "2026-04-14T20:05:00Z"
}
```

---

## 2. 资源路径 API

### 2.1 获取资源路径列表
```
GET /api/resource-paths?computerId={computerId}
```

**响应**
```json
{
  "items": [
    {
      "id": 1,
      "computerId": 1,
      "type": "comic",
      "path": "D:/MyComics/",
      "isEnabled": true,
      "createdAt": "2026-04-14T10:00:00Z"
    }
  ],
  "total": 1
}
```

---

### 2.2 创建资源路径
```
POST /api/resource-paths
```

**请求体**
```json
{
  "computerId": 1,
  "type": "comic",
  "path": "D:/MyComics/",
  "isEnabled": true
}
```

**验证规则**
- `path` 必须以 `/` 结尾
- `type` 必须为: `comic`, `video`, `novel`, `image` 之一
- 同一电脑同一type只能有一条记录

---

### 2.3 更新资源路径
```
PUT /api/resource-paths/{id}
```

**请求体**
```json
{
  "path": "E:/Comics/",
  "isEnabled": false
}
```

---

### 2.4 删除资源路径
```
DELETE /api/resource-paths/{id}
```

**级联删除**
- 同时删除该路径下的所有Comic和ComicChapter记录

---

## 3. 漫画 API

### 3.1 获取漫画列表
```
GET /api/comics?computerId={computerId}&type={type}
```

**查询参数**
| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| computerId | int | 是 | 电脑ID |
| type | string | 否 | 资源类型，默认comic |
| search | string | 否 | 搜索漫画名称 |

**响应**
```json
{
  "items": [
    {
      "id": 1,
      "resourcePathId": 1,
      "folderName": "咒术回战",
      "displayName": "咒术回战",
      "type": "manga",
      "thumbnailBase64": "data:image/jpeg;base64,...",
      "chapterCount": 5,
      "createdAt": "2026-04-14T10:30:00Z"
    }
  ],
  "total": 1
}
```

---

### 3.2 获取漫画详情
```
GET /api/comics/{id}
```

**响应**
```json
{
  "id": 1,
  "resourcePathId": 1,
  "folderName": "咒术回战",
  "displayName": "咒术回战",
  "type": "manga",
  "thumbnailBase64": "data:image/jpeg;base64,...",
  "createdAt": "2026-04-14T10:30:00Z",
  "updatedAt": "2026-04-14T12:00:00Z"
}
```

---

### 3.3 更新漫画信息
```
PUT /api/comics/{id}
```

**请求体**
```json
{
  "displayName": "咒术回战精装版",
  "type": "comic"
}
```

---

### 3.4 删除漫画
```
DELETE /api/comics/{id}
```

**级联删除**
- 同时删除该漫画的所有章节记录

---

### 3.5 上传封面
```
POST /api/comics/{id}/thumbnail
```

**请求体**
```json
{
  "thumbnailBase64": "data:image/jpeg;base64,/9j/4AAQSkZJRg..."
}
```

**限制**
- Base64大小限制：200KB
- 支持格式：image/jpeg, image/png, image/webp

**响应**
```json
{
  "success": true,
  "thumbnailBase64": "data:image/jpeg;base64,..."
}
```

---

### 3.6 扫描漫画文件夹
```
POST /api/comics/scan
```

**请求体**
```json
{
  "resourcePathId": 1
}
```

**逻辑说明**
1. 读取资源路径下的所有一级文件夹作为漫画
2. 读取每个漫画下的所有二级文件夹作为章节
3. 对比数据库，新增的创建，删除的跳过（保留记录）

**响应**
```json
{
  "success": true,
  "comicsFound": 10,
  "chaptersFound": 50,
  "comics": [
    {
      "id": 1,
      "folderName": "咒术回战",
      "displayName": "咒术回战",
      "chapters": [
        { "folderName": "第1话", "displayName": "第1话", "sortOrder": 1 },
        { "folderName": "第2话", "displayName": "第2话", "sortOrder": 2 }
      ]
    }
  ]
}
```

---

## 4. 章节 API

### 4.1 获取漫画章节列表
```
GET /api/comics/{comicId}/chapters
```

**响应**
```json
{
  "items": [
    {
      "id": 1,
      "comicId": 1,
      "folderName": "第1话",
      "displayName": "第1话",
      "sortOrder": 1,
      "pageCount": 24,
      "createdAt": "2026-04-14T10:30:00Z"
    }
  ],
  "total": 5
}
```

---

### 4.2 获取章节页面列表
```
GET /api/chapters/{id}/pages
```

**响应**
```json
{
  "chapterId": 1,
  "pages": [
    {
      "index": 0,
      "filename": "001.jpg",
      "url": "http://localhost:6789/files/read?path=D:/MyComics/咒术回战/第1话/001.jpg"
    }
  ]
}
```

**URL说明**
- 图片URL指向本地代理服务，不经过API
- 浏览器直接访问本地代理获取图片

---

## 5. 统一响应格式

### 成功响应
```json
{
  "success": true,
  "data": { ... }
}
```

### 错误响应
```json
{
  "success": false,
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "指定的资源不存在"
  }
}
```

### 分页响应
```json
{
  "items": [ ... ],
  "total": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

## ⚠️ 错误码说明

| 错误码 | HTTP状态码 | 说明 |
|--------|------------|------|
| VALIDATION_ERROR | 400 | 参数验证失败 |
| UNAUTHORIZED | 401 | 未授权 |
| RESOURCE_NOT_FOUND | 404 | 资源不存在 |
| DUPLICATE_RESOURCE | 409 | 资源重复 |
| INTERNAL_ERROR | 500 | 服务器内部错误 |
