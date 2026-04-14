# 数据模型设计

## 📊 数据库概览

本模块涉及4张核心数据表，用于管理多电脑资源路径和漫画数据。

```
┌─────────────┐       ┌─────────────────┐       ┌─────────────┐
│  Computer   │──────<│  ResourcePath   │──────<│    Comic    │
│  (电脑)     │  1:N  │  (资源路径)      │  1:N  │   (漫画)    │
└─────────────┘       └─────────────────┘       └──────┬──────┘
                                                       │ 1:N
                                                ┌──────┴──────┐
                                                │ComicChapter │
                                                │  (章节)     │
                                                └─────────────┘
```

---

## 1. Computer 电脑表

### 用途
识别和追踪不同电脑，区分资源归属。

### 字段设计
```csharp
public class Computer
{
    public int Id { get; set; }
    
    /// <summary>用户自定义的电脑名称</summary>
    public string Name { get; set; }
    
    /// <summary>电脑主机名（用于自动识别）</summary>
    public string HostName { get; set; }
    
    /// <summary>是否在线（心跳检测）</summary>
    public bool IsOnline { get; set; }
    
    /// <summary>最后心跳时间</summary>
    public DateTime? LastHeartbeat { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public ICollection<ResourcePath> ResourcePaths { get; set; }
}
```

### 索引设计
- `HostName` - 唯一索引（自动识别当前电脑）
- `Name` - 普通索引

---

## 2. ResourcePath 资源路径表

### 用途
存储每台电脑上的资源根目录路径。

### 字段设计
```csharp
public class ResourcePath
{
    public int Id { get; set; }
    
    /// <summary>所属电脑ID</summary>
    public int ComputerId { get; set; }
    
    /// <summary>资源类型: comic/video/novel/image</summary>
    public string Type { get; set; }
    
    /// <summary>资源根目录路径</summary>
    public string Path { get; set; }
    
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public Computer Computer { get; set; }
    public ICollection<Comic> Comics { get; set; }
}
```

### 索引设计
- `ComputerId + Type` - 唯一复合索引（每台电脑每种资源只有一个路径）

---

## 3. Comic 漫画表

### 用途
存储漫画基本信息。

### 字段设计
```csharp
public class Comic
{
    public int Id { get; set; }
    
    /// <summary>所属资源路径ID</summary>
    public int ResourcePathId { get; set; }
    
    /// <summary>文件夹名称（原始名称）</summary>
    public string FolderName { get; set; }
    
    /// <summary>显示名称（用户可编辑）</summary>
    public string DisplayName { get; set; }
    
    /// <summary>漫画类型: manga/comic/novel/picture</summary>
    public string Type { get; set; }
    
    /// <summary>封面图Base64（前端压缩后上传）</summary>
    public string ThumbnailBase64 { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt { get; set; }
    
    // 导航属性
    public ResourcePath ResourcePath { get; set; }
    public ICollection<ComicChapter> Chapters { get; set; }
}
```

### 索引设计
- `ResourcePathId` - 普通索引
- `DisplayName` - 普通索引（支持搜索）

---

## 4. ComicChapter 漫画章节表

### 用途
存储漫画的章节信息。

### 字段设计
```csharp
public class ComicChapter
{
    public int Id { get; set; }
    
    /// <summary>所属漫画ID</summary>
    public int ComicId { get; set; }
    
    /// <summary>文件夹名称（原始名称）</summary>
    public string FolderName { get; set; }
    
    /// <summary>显示名称（用户可编辑）</summary>
    public string DisplayName { get; set; }
    
    /// <summary>排序顺序</summary>
    public int SortOrder { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public Comic Comic { get; set; }
}
```

### 索引设计
- `ComicId + SortOrder` - 复合索引（按顺序获取章节）

---

## 🔗 ER关系图

```
┌─────────────────────────────────────────────────────────────────────┐
│                           数据模型关系                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Computer ──────┐                                                   │
│    │            │ 1                                                   │
│    │            │                                                     │
│    │ N          ▼                                                     │
│  ResourcePath <─────── 1                                             │
│    │                                                                  │
│    │ N          1                                                     │
│    ▼          ┌──┴──┐                                                 │
│   Comic <─────────── ComicChapter                                   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 📝 数据库约束

| 表名 | 字段 | 约束类型 | 说明 |
|------|------|----------|------|
| Computer | HostName | UNIQUE | 主机名唯一 |
| ResourcePath | ComputerId+Type | UNIQUE | 每台电脑每种资源唯一 |
| Comic | FolderName | - | 非唯一，允许重名 |
| ComicChapter | ComicId+SortOrder | UNIQUE | 每本漫画章节顺序唯一 |

---

## 🚀 迁移脚本生成命令

```bash
cd api
dotnet ef migrations add AddResourceManagementTables
```

迁移将创建：
1. `Computers` 表
2. `ResourcePaths` 表
3. `Comics` 表
4. `ComicChapters` 表
5. 所有索引和外键
