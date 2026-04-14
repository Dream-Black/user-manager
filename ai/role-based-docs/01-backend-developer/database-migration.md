# 数据库迁移指南

## 📋 迁移概述

本次迁移将创建4张新表用于资源管理模块：
1. `Computers` - 电脑表
2. `ResourcePaths` - 资源路径表
3. `Comics` - 漫画表
4. `ComicChapters` - 漫画章节表

---

## 🚀 迁移步骤

### 1. 创建迁移
```bash
cd api
dotnet ef migrations add AddResourceManagementTables
```

### 2. 检查生成的迁移文件
迁移文件位于 `api/Migrations/` 目录，文件名类似：
```
<timestamp>_AddResourceManagementTables.cs
```

### 3. 预览SQL（可选）
```bash
dotnet ef migrations script --output migration.sql
```

### 4. 应用迁移到数据库
```bash
# 开发环境
dotnet ef database update

# 生产环境（使用SQL脚本）
dotnet ef migrations script --output migration.sql
# 手动执行SQL脚本
```

---

## 📄 预期SQL结构

### Computers 表
```sql
CREATE TABLE `Computers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` nvarchar(100) NOT NULL,
    `HostName` nvarchar(255) NOT NULL,
    `IsOnline` tinyint(1) NOT NULL,
    `LastHeartbeat` datetime NULL,
    `CreatedAt` datetime NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Computers_HostName` (`HostName`)
);
```

### ResourcePaths 表
```sql
CREATE TABLE `ResourcePaths` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ComputerId` int NOT NULL,
    `Type` nvarchar(50) NOT NULL,
    `Path` nvarchar(1000) NOT NULL,
    `IsEnabled` tinyint(1) NOT NULL,
    `CreatedAt` datetime NOT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_ResourcePaths_ComputerId` (`ComputerId`),
    UNIQUE INDEX `IX_ResourcePaths_ComputerId_Type` (`ComputerId`, `Type`),
    CONSTRAINT `FK_ResourcePaths_Computers_ComputerId`
        FOREIGN KEY (`ComputerId`) REFERENCES `Computers` (`Id`) ON DELETE CASCADE
);
```

### Comics 表
```sql
CREATE TABLE `Comics` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ResourcePathId` int NOT NULL,
    `FolderName` nvarchar(255) NOT NULL,
    `DisplayName` nvarchar(255) NOT NULL,
    `Type` nvarchar(50) NOT NULL,
    `ThumbnailBase64` nvarchar(max) NULL,
    `CreatedAt` datetime NOT NULL,
    `UpdatedAt` datetime NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Comics_ResourcePathId` (`ResourcePathId`),
    INDEX `IX_Comics_DisplayName` (`DisplayName`),
    CONSTRAINT `FK_Comics_ResourcePaths_ResourcePathId`
        FOREIGN KEY (`ResourcePathId`) REFERENCES `ResourcePaths` (`Id`) ON DELETE CASCADE
);
```

### ComicChapters 表
```sql
CREATE TABLE `ComicChapters` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ComicId` int NOT NULL,
    `FolderName` nvarchar(255) NOT NULL,
    `DisplayName` nvarchar(255) NOT NULL,
    `SortOrder` int NOT NULL,
    `CreatedAt` datetime NOT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_ComicChapters_ComicId` (`ComicId`),
    UNIQUE INDEX `IX_ComicChapters_ComicId_SortOrder` (`ComicId`, `SortOrder`),
    CONSTRAINT `FK_ComicChapters_Comics_ComicId`
        FOREIGN KEY (`ComicId`) REFERENCES `Comics` (`Id`) ON DELETE CASCADE
);
```

---

## 🔄 升级策略

### 开发环境
直接使用 `dotnet ef database update`，支持多次migration

### 生产环境
1. 生成SQL脚本：`dotnet ef migrations script`
2. 人工review脚本
3. DBA执行脚本
4. 记录迁移版本号

---

## ↩️ 回滚方案

### 回滚到上一个迁移
```bash
dotnet ef database update PreviousMigrationName
```

### 生成回滚SQL
```bash
dotnet ef migrations script <from_migration> <to_migration>
```

---

## ⚠️ 注意事项

1. **级联删除**：ResourcePath删除时会删除关联的Comic和Chapter
2. **外键约束**：删除Computer前需先删除关联的ResourcePath
3. **数据迁移**：如有旧数据需要迁移，需编写数据迁移脚本
4. **索引顺序**：复合索引字段顺序很重要，按查询频率排列

---

## 🧪 验证清单

迁移完成后执行以下验证：

- [ ] Computers表创建成功
- [ ] ResourcePaths表创建成功，外键约束正确
- [ ] Comics表创建成功，外键约束正确
- [ ] ComicChapters表创建成功，外键约束正确
- [ ] 所有索引创建成功
- [ ] EF Core追踪版本记录到 __EFMigrationsHistory 表
