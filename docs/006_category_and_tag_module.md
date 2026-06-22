# 分类与标签管理模块 - 数据字典与接口文档

> **模块版本**: v1.0.2
> **最后更新**: 2026-06-22
> **适用范围**: LeftoverShare 后端 API v1
> **文档用途**: 提供给前端、移动端及第三方接口调用方的协作参考
> **⚠️ 重要提示**: 本文档已与实际代码逐行核对，所有「未实现」的设计均已明确标注，请勿依赖标注为「预留设计」的功能

---

## 1. 模块概述

本模块为 LeftoverShare 剩食分享平台提供分类与标签的统一管理能力，包含三类核心业务对象：

| 对象 | 英文名 | 用途 | 管理权限 |
|------|--------|------|----------|
| 食物分类 | FoodCategory | 食物类别的层级分类体系 | 仅管理员增删改，查询公开 |
| 过敏原标签 | AllergenTag | 食品安全相关的过敏原警示标签 | 仅管理员增删改，查询公开 |
| 帖子标签 | PostTag | 附加在分享帖上的自定义/系统标签 | 系统标签仅管理员，用户标签管理员+创建者 |

### 1.1 实体关系图（ERD）

```
User (1) ──────── (N) PostTag (CreatedBy)
                    │
                    │
                    │ (N:N 中间表 SharePostPostTag)
                    ↓
SharePost (1) ──────── (N) PostTag
     │
     │ (FoodCategoryId 自引用树)
     └─────── (N:1) FoodCategory (ParentId)
                    │
                    │ (N:N 中间表 SharePostAllergenTag)
                    └─────────────────────── AllergenTag (M:N 关联 SharePost)
```

---

## 2. 枚举定义

### 2.1 UserRole（用户角色）

| 值 | 整数 | 描述 |
|----|------|------|
| User | 0 | 普通用户，默认角色 |
| Admin | 1 | 系统管理员，拥有分类/标签管理权限 |

> **Claim 类型**: `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`
> **JWT Token 中的值**: "User" / "Admin"（字符串形式）
> **Controller 权限验证方式**: `[Authorize(Roles = "Admin")]`

---

## 3. 数据表字段说明

### 3.1 FoodCategories（食物分类表）

**主键**: `Id INT AUTO_INCREMENT`

| 字段名 | 数据类型 | 可空 | 默认值 | 说明 | 约束 |
|--------|----------|------|--------|------|------|
| Id | INT | NO | AUTO | 主键自增 | PK |
| Name | NVARCHAR(50) | NO | - | 分类名称（展示用） | 必填，最大50字符 |
| Code | VARCHAR(50) | NO | - | 分类编码（英文标识，用于程序逻辑） | 唯一索引，字母开头，仅字母数字下划线 |
| ParentId | INT | YES | NULL | 父级分类ID（顶级分类为NULL） | FK→FoodCategories.Id，Restrict级联 |
| SortOrder | INT | NO | 0 | 显示排序（值越小越靠前） | ≥0 |
| Description | NVARCHAR(500) | YES | NULL | 分类描述 | 最大500字符 |
| Icon | VARCHAR(200) | YES | NULL | 图标URL或图标Class | - |
| IsActive | BIT | NO | 1 | 是否启用（false=逻辑停用，非删除） | 软停用机制 |
| IsDeleted | BIT | NO | 0 | 是否已删除（软删除标记） | 全局过滤器自动过滤 |
| CreatedAt | DATETIME | NO | GETDATE() | 创建时间（UTC） | - |
| UpdatedAt | DATETIME | NO | GETDATE() | 最后更新时间（UTC） | 自动更新 |
| DeletedAt | DATETIME | YES | NULL | 删除时间（UTC） | 软删除时填充 |
| DeletedBy | INT | YES | NULL | 删除操作人UserId | FK→Users.Id |

**索引**:
- `IX_FoodCategories_Code` UNIQUE（编码唯一）
- `IX_FoodCategories_ParentId`（加速查询子分类）
- `IX_FoodCategories_IsActive_IsDeleted`（加速列表查询）

**业务约束**:
- 自引用树，理论支持无限层级，**建议控制在3级以内**
- 删除父分类前必须先处理所有子分类（Restrict级联会阻止）
- 分类被SharePost引用时，删除操作会失败（需先解除引用）
- **创建和更新时强制循环引用检测**：
  - 更新时：父级不能是自己，也不能是自己的任意后代（子/孙/曾孙...）
  - 创建时：检测父级分类的祖先链是否存在循环引用，确保树结构健康

---

### 3.2 AllergenTags（过敏原标签表）

**主键**: `Id INT AUTO_INCREMENT`

| 字段名 | 数据类型 | 可空 | 默认值 | 说明 | 约束 |
|--------|----------|------|--------|------|------|
| Id | INT | NO | AUTO | 主键自增 | PK |
| Name | NVARCHAR(50) | NO | - | 过敏原名称（中文展示） | 必填，最大50字符 |
| Code | VARCHAR(50) | NO | - | 过敏原编码（英文标识） | 唯一索引 |
| SeverityLevel | INT | NO | 2 | 严重程度等级 | 1=轻度/提示, 2=中度/注意, 3=重度/危险 |
| SortOrder | INT | NO | 0 | 显示排序 | ≥0 |
| Description | NVARCHAR(500) | YES | NULL | 详细说明（含交叉过敏提示） | 最大500字符 |
| Icon | VARCHAR(200) | YES | NULL | 图标URL | - |
| IsActive | BIT | NO | 1 | 是否启用 | - |
| IsDeleted | BIT | NO | 0 | 是否已删除（软删除标记） | 全局过滤器 |
| CreatedAt | DATETIME | NO | GETDATE() | 创建时间（UTC） | - |
| UpdatedAt | DATETIME | NO | GETDATE() | 最后更新时间（UTC） | - |
| DeletedAt | DATETIME | YES | NULL | 删除时间（UTC） | - |
| DeletedBy | INT | YES | NULL | 删除操作人UserId | FK→Users.Id |

**索引**:
- `IX_AllergenTags_Code` UNIQUE
- `IX_AllergenTags_SeverityLevel`（加速按严重程度筛选）
- `IX_AllergenTags_IsActive_IsDeleted`

**业务约束**:
- SeverityLevel = 3（重度）的标签删除需二次确认
- 标签被帖子引用时，删除将自动解除关联（DeleteBehavior.Cascade清理中间表）

---

### 3.3 PostTags（帖子标签表）

**主键**: `Id INT AUTO_INCREMENT`

| 字段名 | 数据类型 | 可空 | 默认值 | 说明 | 约束 |
|--------|----------|------|--------|------|------|
| Id | INT | NO | AUTO | 主键自增 | PK |
| Name | NVARCHAR(30) | NO | - | 标签名称（展示用） | 必填，最大30字符 |
| Code | VARCHAR(50) | NO | - | 标签编码 | 唯一索引，**创建时必填** |
| Color | VARCHAR(7) | YES | NULL | 标签颜色（HEX格式，如#FF5500） | 正则匹配^#[0-9A-Fa-f]{6}$ |
| SortOrder | INT | NO | 0 | 显示排序 | ≥0 |
| Description | NVARCHAR(200) | YES | NULL | 标签用途说明 | 最大200字符 |
| UsageCount | INT | NO | 0 | 使用次数统计（热门标签排序用） | ≥0 |
| IsSystemDefined | BIT | NO | 0 | 是否系统预设标签 | 默认false=用户自定义 |
| CreatedBy | INT | YES | NULL | 创建者UserId（系统标签为NULL） | FK→Users.Id，SetNull级联 |
| IsActive | BIT | NO | 1 | 是否启用 | - |
| IsDeleted | BIT | NO | 0 | 是否已删除（软删除） | 全局过滤器 |
| CreatedAt | DATETIME | NO | GETDATE() | 创建时间（UTC） | - |
| UpdatedAt | DATETIME | NO | GETDATE() | 最后更新时间（UTC） | - |
| DeletedAt | DATETIME | YES | NULL | 删除时间（UTC） | - |
| DeletedBy | INT | YES | NULL | 删除操作人UserId | FK→Users.Id |

**索引**:
- `IX_PostTags_Code` UNIQUE
- `IX_PostTags_CreatedBy`（加速查询用户自建标签）
- `IX_PostTags_IsSystemDefined`
- `IX_PostTags_UsageCount`（加速热门标签排序）
- `IX_PostTags_IsActive_IsDeleted`

**业务约束**:
- 系统标签（IsSystemDefined=1）的修改/删除 **仅限管理员**
- 用户自定义标签的修改/删除允许 **管理员 或 标签创建者（CreatedBy）**
- 用户自定义标签同命名查重（Name+CreatedBy联合逻辑检查）

---

### 3.4 SharePostAllergenTags（帖子-过敏原关联表，多对多中间表）

**主键**: `(SharePostId, AllergenTagId) COMPOSITE PK`

| 字段名 | 数据类型 | 可空 | 说明 | 约束 |
|--------|----------|------|------|------|
| SharePostId | INT | NO | 帖子ID | PK, FK→SharePosts.Id (Cascade级联) |
| AllergenTagId | INT | NO | 过敏原标签ID | PK, FK→AllergenTags.Id (Cascade级联清理关联) |

**⚠️ 预留设计**: 此表结构已在 DbContext 中配置，但 SharePost 创建/编辑接口暂不支持关联过敏原标签，需后续扩展。

---

### 3.5 SharePostPostTags（帖子-帖子标签关联表，多对多中间表）

**主键**: `(SharePostId, PostTagId) COMPOSITE PK`

| 字段名 | 数据类型 | 可空 | 说明 | 约束 |
|--------|----------|------|------|------|
| SharePostId | INT | NO | 帖子ID | PK, FK→SharePosts.Id (Cascade级联) |
| PostTagId | INT | NO | 帖子标签ID | PK, FK→PostTags.Id (Cascade级联清理关联) |

**⚠️ 预留设计**: 此表结构已在 DbContext 中配置，但 SharePost 创建/编辑接口暂不支持关联帖子标签，需后续扩展。

---

### 3.6 Users表（Role字段扩展）

| 新增字段 | 数据类型 | 可空 | 默认值 | 说明 |
|----------|----------|------|--------|------|
| Role | VARCHAR(20) | NO | 'User' | 用户角色（存储字符串形式的UserRole枚举值） |

---

## 4. API 接口列表（已与实际代码核对 ✅）

> **路由约定**: Controller 基路径为 `api/[controller]`，即类名去掉"Controller"后缀，**大小写敏感**
> **权限约定**: `[AllowAnonymous]` = 匿名，`[Authorize]` = 登录用户，`[Authorize(Roles = "Admin")]` = 仅管理员

---

### 4.1 食物分类（FoodCategories）

> **基路径**: `api/FoodCategories`

| # | 方法 | 路径 | 权限 | 描述 | 实现状态 |
|---|------|------|------|------|----------|
| 4.1.1 | GET | `/api/FoodCategories/tree` | 匿名 | 获取分类树（递归嵌套Children） | ✅ 已实现 |
| 4.1.2 | GET | `/api/FoodCategories/roots` | 匿名 | 获取所有顶级分类（ParentId=NULL） | ✅ 已实现 |
| 4.1.3 | GET | `/api/FoodCategories/{parentId}/children` | 匿名 | 获取指定分类的直接子分类 | ✅ 已实现 |
| 4.1.4 | GET | `/api/FoodCategories/{id}` | 匿名 | 获取单个分类详情 | ✅ 已实现 |
| 4.1.5 | POST | `/api/FoodCategories` | Admin | 新增分类 | ✅ 已实现 |
| 4.1.6 | PUT | `/api/FoodCategories/{id}` | Admin | 更新分类（含循环引用检测） | ✅ 已实现 |
| 4.1.7 | DELETE | `/api/FoodCategories/{id}` | Admin | 删除分类（软删除，含子分类/引用检查） | ✅ 已实现 |
| 4.1.8 | PATCH | `/api/FoodCategories/{id}/toggle-active` | Admin | 启用/禁用分类（切换IsActive） | ✅ 已实现 |
| 4.1.9 | PATCH | `/api/FoodCategories/sort-order` | Admin | 批量更新排序 | ✅ 已实现 |
| 4.1.❌ | GET | `/api/FoodCategories` | 匿名 | 获取分类列表（分页） | ❌ 未实现 |
| 4.1.❌ | GET | `/api/FoodCategories/{id}/path` | 匿名 | 获取从根到当前分类的完整路径链 | ❌ 未实现 |

**GET /api/FoodCategories/tree 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| includeInactive | bool | false | 是否包含已停用的分类（仅管理员可传true，但Controller层无限制） |

**GET /api/FoodCategories/roots 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| includeInactive | bool | false | 是否包含已停用的分类 |

**GET /api/FoodCategories/{parentId}/children 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| includeInactive | bool | false | 是否包含已停用的子分类 |

**POST /api/FoodCategories 请求体示例**:
```json
{
  "name": "素食专享",
  "code": "vegetarian",
  "parentId": null,
  "sortOrder": 10,
  "description": "不含任何肉类成分的素食料理",
  "icon": "🥗",
  "isActive": true
}
```

**PUT /api/FoodCategories/{id} 循环引用检测逻辑**：
1. 检测 `request.ParentId == id` → 不能设为自己
2. 调用 `GetAllDescendantIdsAsync(id)` BFS遍历获取所有后代ID，检测 `request.ParentId` 是否在列表中 → 不能设为自己的子孙后代
3. 调用 `GetAllAncestorIdsAsync(request.ParentId.Value)` 检测父级祖先链是否存在循环 → 确保父级所在树结构健康
4. 以上任一检测不通过返回 `400 Bad Request`

**POST /api/FoodCategories 循环引用检测逻辑**：
1. 调用 `GetAllAncestorIdsAsync(request.ParentId.Value)` 检测父级祖先链是否存在循环 → 确保父级所在树结构健康
2. 检测不通过返回 `400 Bad Request`

---

### 4.2 过敏原标签（AllergenTags）

> **基路径**: `api/AllergenTags`

| # | 方法 | 路径 | 权限 | 描述 | 实现状态 |
|---|------|------|------|------|----------|
| 4.2.1 | GET | `/api/AllergenTags/active` | 匿名 | 获取所有已启用的过敏原标签（供前端展示） | ✅ 已实现 |
| 4.2.2 | GET | `/api/AllergenTags` | **Admin** | 分页获取标签列表（后台管理用） | ✅ 已实现 |
| 4.2.3 | GET | `/api/AllergenTags/{id}` | 匿名 | 获取单个标签详情 | ✅ 已实现 |
| 4.2.4 | POST | `/api/AllergenTags` | Admin | 新增过敏原标签 | ✅ 已实现 |
| 4.2.5 | PUT | `/api/AllergenTags/{id}` | Admin | 更新标签 | ✅ 已实现 |
| 4.2.6 | DELETE | `/api/AllergenTags/{id}` | Admin | 删除标签（软删除，含引用检查） | ✅ 已实现 |
| 4.2.7 | PATCH | `/api/AllergenTags/{id}/toggle-active` | Admin | 启用/禁用标签 | ✅ 已实现 |
| 4.2.8 | PATCH | `/api/AllergenTags/sort-order` | Admin | 批量更新排序 | ✅ 已实现 |

**GET /api/AllergenTags （仅管理员）查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| pageNumber | int | 1 | 页码，≥1 |
| pageSize | int | 10 | 每页数量，1-100 |
| includeInactive | bool | false | 是否包含已停用的标签 |
| severityLevel | int? | NULL | 按严重程度过滤: 1/2/3 |
| searchTerm | string | NULL | 按名称/编码/描述模糊搜索 |

**GET /api/AllergenTags/active 无参数**，返回所有 `IsActive = true` 的标签，按 `SortOrder → SeverityLevel（降序）→ Name` 排序。

---

### 4.3 帖子标签（PostTags）

> **基路径**: `api/PostTags`

| # | 方法 | 路径 | 权限 | 描述 | 实现状态 |
|---|------|------|------|------|----------|
| 4.3.1 | GET | `/api/PostTags/active` | 匿名 | 获取所有已启用的标签 | ✅ 已实现 |
| 4.3.2 | GET | `/api/PostTags/popular` | 匿名 | 获取热门标签榜（按UsageCount降序） | ✅ 已实现 |
| 4.3.3 | GET | `/api/PostTags/search` | 匿名 | 搜索标签（供发帖时联想输入） | ✅ 已实现 |
| 4.3.4 | GET | `/api/PostTags` | **Admin** | 分页获取标签列表（后台管理用） | ✅ 已实现 |
| 4.3.5 | GET | `/api/PostTags/mine` | User | 获取当前登录用户创建的自定义标签 | ✅ 已实现 |
| 4.3.6 | GET | `/api/PostTags/{id}` | 匿名 | 获取单个标签详情 | ✅ 已实现 |
| 4.3.7 | POST | `/api/PostTags/system` | Admin | 创建系统预设标签 | ✅ 已实现 |
| 4.3.8 | POST | `/api/PostTags/user` | User | 用户创建自定义标签（归属当前用户） | ✅ 已实现 |
| 4.3.9 | PUT | `/api/PostTags/{id}` | Admin + Owner | 更新标签（系统标签仅Admin，用户标签需是创建者） | ✅ 已实现 |
| 4.3.10 | DELETE | `/api/PostTags/{id}` | Admin + Owner | 删除标签（同上权限，含引用检查） | ✅ 已实现 |
| 4.3.11 | PATCH | `/api/PostTags/{id}/toggle-active` | Admin | 启用/禁用标签 | ✅ 已实现 |
| 4.3.12 | PATCH | `/api/PostTags/sort-order` | Admin | 批量更新排序 | ✅ 已实现 |
| 4.3.❌ | GET | `/api/PostTags/system` | 匿名 | 单独获取系统预设标签列表 | ❌ 未实现（通过分页接口筛选） |
| 4.3.❌ | GET | `/api/PostTags/user/{userId}` | Admin | 获取指定用户创建的标签 | ❌ 未实现（通过分页接口筛选） |

**GET /api/PostTags/active 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| includeUserDefined | bool | true | 是否包含用户自定义标签（false=仅系统标签） |

**GET /api/PostTags/popular 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| topN | int | 20 | 返回数量，1-100 |

**GET /api/PostTags/search 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| keyword | string | - | **必填**，搜索关键词（空时返回热门） |
| limit | int | 20 | 返回数量 |

**GET /api/PostTags （仅管理员）查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| pageNumber | int | 1 | 页码 |
| pageSize | int | 10 | 每页数量 |
| includeInactive | bool | false | 是否包含已停用的标签 |
| isSystemDefined | bool? | NULL | NULL=全部, true=仅系统, false=仅用户 |
| createdBy | int? | NULL | 仅查询指定用户创建的标签 |
| searchTerm | string | NULL | 按名称/编码/描述模糊搜索 |

**POST /api/PostTags/user 请求体示例**:
```json
{
  "name": "学生优先",
  "code": "student_priority",
  "color": "#6366F1",
  "description": "优先留给在校学生领取",
  "isActive": true
}
```

> **⚠️ 重要说明**: `code` 字段**不可为空**，需由调用方传入，不支持服务端自动生成。

---

## 5. 权限矩阵（Permission Matrix）

| 操作 | 匿名用户 | 普通登录用户 | 管理员(Admin) |
|------|:--------:|:------------:|:-------------:|
| 查询食物分类（树/顶级/子分类/详情） | ✅ | ✅ | ✅ |
| 查询过敏原标签（active列表/详情） | ✅ | ✅ | ✅ |
| 查询帖子标签（active/popular/search/详情） | ✅ | ✅ | ✅ |
| 查询「我的」自定义帖子标签 | ❌ | ✅（仅自己的） | ✅（全部） |
| 过敏原/帖子标签后台分页列表 | ❌ | ❌ | ✅ |
| 创建食物分类/过敏原标签 | ❌ | ❌ | ✅ |
| 创建系统预设帖子标签 | ❌ | ❌ | ✅ |
| 创建用户自定义帖子标签 | ❌ | ✅（归属自己） | ✅（可通过/system接口创建系统标签） |
| 更新**系统定义**的分类/过敏原/帖子标签 | ❌ | ❌ | ✅ |
| 更新**自己创建**的用户帖子标签 | ❌ | ✅ | ✅ |
| 更新**他人创建**的用户帖子标签 | ❌ | ❌ | ✅ |
| 启用/停用任意分类/标签 | ❌ | ❌ | ✅ |
| 软删除任意分类/标签（含子分类/引用检查） | ❌ | ❌（自己的用户标签可） | ✅ |
| 批量调整排序 | ❌ | ❌ | ✅ |

---

## 6. 数据校验规则（FluentValidation）

所有写操作均在服务端自动执行校验，以下为**已实现**的校验规则：

### 6.1 已实现的通用校验规则

| 字段 | 适用对象 | 校验规则 |
|------|----------|----------|
| Name | 全部三类 | 必填，去空格后非空，最大长度见数据表 |
| Code | 全部三类 | 必填，字母开头，仅允许字母、数字、下划线，长度≤50，全局唯一 |
| Description | 全部三类 | 若填写，最大长度500字符 |
| SortOrder | 全部三类 | ≥0的整数 |
| Color | PostTag | 若填写，必须符合正则 `^#[0-9A-Fa-f]{6}$` |
| SeverityLevel | AllergenTag | 必填，值∈[1, 2, 3] |
| ParentId | FoodCategory（Create和Update时） | 禁止指向自己（仅Update）；禁止指向其任意后代（循环引用检测 ✅ 已实现）；检测父级祖先链是否存在循环 ✅ 已实现 |

### 6.2 业务层附加校验（已实现 ✅）

| 校验场景 | 说明 |
|----------|------|
| Code重复 | 三类实体创建/更新时均校验全局唯一 |
| 父分类不存在 | ParentId有效性校验 |
| 形成循环引用 | 分类更新时检测父级不能是自己或后代（通过 `GetAllDescendantIdsAsync` BFS遍历）；分类创建和更新时检测父级祖先链是否存在循环（通过 `GetAllAncestorIdsAsync` 遍历） |
| 删除存在子分类的父分类 | 必须先删除/转移所有子分类（`HasChildrenAsync`） |
| 删除被帖子引用的分类/标签 | 拒绝删除（`IsUsedByPostsAsync`） |
| 修改/删除系统标签非管理员 | 权限不足，拒绝 |
| 修改/删除他人用户标签非管理员 | 权限不足，拒绝 |

### 6.3 ⚠️ 预留设计（未实现）

| 校验场景 | 说明 |
|----------|------|
| 同层级下食物分类Name重复 | 代码中有此错误码描述，但**未实现**实际校验 |
| 用户自定义标签与本人其他标签同名 | 代码中有此错误码描述，但**未实现**实际校验 |
| 过敏原Code/Name重复 | 仅校验了Code重复，**未实现**Name全局唯一校验 |
| 系统标签Name全局唯一 | 仅校验了Code重复，**未实现**Name全局唯一校验 |

---

## 7. 异常处理与错误码

### 7.1 统一响应格式

所有接口均返回如下标准包装（`ApiResponse<T>`）：

```json
{
  "code": 20000,
  "message": "操作成功",
  "data": { ... }
}
```

| code 范围 | 含义 |
|-----------|------|
| 20000 - 29999 | 成功（20000=通用成功） |
| 40000 - 49999 | 客户端参数校验失败 |
| 40100 - 40199 | 未认证（未登录或Token过期） |
| 40300 - 40399 | 无权限（角色不足或非资源所有者） |
| 40400 - 40499 | 资源不存在 |
| 40900 - 40999 | 业务冲突（重复、循环引用、被引用等） |
| 50000+ | 服务端内部错误 |

### 7.2 本模块实际返回的错误信息（非固定错误码）

> ⚠️ **重要**: 代码中**未使用固定错误码值**，而是通过 `ApiResponse.Fail(message, httpCode)` 返回。以下为实际返回的错误消息：

| 场景 | 返回的 message | HTTP Status |
|------|--------------|-------------|
| 非管理员尝试管理分类/过敏原 | "无权限操作，仅管理员可xxx" | 403 |
| 非标签创建者尝试修改用户标签 | "无权限操作，仅管理员或标签创建者可修改" | 403 |
| 非管理员尝试修改系统预设标签 | "系统预设标签仅管理员可修改" | 403 |
| Code编码已存在 | "标签编码 xxx 已存在" / "已被其他标签使用" | 400 |
| 父分类ID不存在 | "指定的父级分类不存在" | 400 |
| 将自己设为父级 | "不能将自己设为父级分类" | 400 |
| 父级设置形成循环引用树 | "不能将父级分类设置为自身的后代分类（循环引用）" | 400 |
| 父级分类祖先链存在循环 | "检测到分类树存在循环引用，分类ID: xxx" | 400 |
| 要删除的分类存在子分类 | "该分类下存在子分类，请先删除子分类" | 400 |
| 要删除的分类/标签被帖子引用中 | "该标签已被分享帖使用，无法删除（可先禁用）" | 400 |
| 资源不存在 | "xxx不存在" | 404 |

---

## 8. 搜索过滤衔接设计

### 8.1 已实现的数据库索引

所有高频过滤字段均已建立数据库索引：
- `FoodCategories.Code`（唯一索引，按编码精确查找）
- `FoodCategories.ParentId`（按父级筛选子分类）
- `AllergenTags.SeverityLevel`（按严重程度过滤过敏原）
- `PostTags.UsageCount`（热门标签排序）
- `PostTags.IsSystemDefined`（系统/用户标签筛选）
- `PostTags.CreatedBy`（按创建者筛选）
- 全部三类实体的 `(IsActive, IsDeleted)` 组合索引

### 8.2 ⚠️ 预留设计（未在SharePost接口中实现）

以下为数据层和索引层已预留，但**SharePost业务接口尚未支持**的过滤参数：

| 参数名 | 类型 | 说明 | 预计实现位置 |
|--------|------|------|--------------|
| `foodCategoryId` | int | 按食物分类过滤帖子 | SharePostsController.GetList |
| `foodCategoryIds` | int[] | 按食物分类批量过滤（多选） | SharePostsController.GetList |
| `excludeAllergenIds` | int[] | 排除含指定过敏原的帖子 | SharePostsController.GetList |
| `postTagIds` | int[] | 按帖子标签批量过滤 | SharePostsController.GetList |
| `categoryPathContains` | int | 筛选某分类及其所有子类（递归树） | SharePostsController.GetList |

### 8.3 UsageCount 维护机制 ⚠️ 预留设计

**代码中已定义 `UsageCount` 字段并建立索引，但自动增减逻辑尚未实现**，因为：
- SharePost 创建/编辑接口暂不支持关联标签
- 缺少中间表维护的触发点

**预计实现方式**：每次帖子关联/取消关联帖子标签时，Service层自动递增/递减：

| 场景（待实现） | 操作 |
|--------------|------|
| 帖子创建时添加X个标签 | X个标签各+1 |
| 帖子编辑时新增Y个标签 | Y个标签各+1 |
| 帖子编辑时移除Z个标签 | Z个标签各-1 |
| 帖子被删除（软删除） | 该帖子所有标签各-1 |

---

## 9. 种子数据清单（已实现 ✅）

### 9.1 用户

| 用户名 | 邮箱 | 密码 | 角色 | 用途 |
|--------|------|------|------|------|
| admin | admin@leftovershare.com | Admin@123456 | Admin | 管理员登录测试 |
| zhangsan | zhangsan@example.com | Password123! | User | 普通用户 |
| lisi / wangwu / zhaoliu / sunqi | 见代码 | Password123! | User | 业务交互测试 |

### 9.2 食物分类（顶级10个 + 二级12个）

| 顶级分类 | Code | 二级子分类 |
|----------|------|------------|
| 中餐 | chinese_food | 川菜 / 粤菜 / 家常菜 |
| 西餐 | western_food | 披萨意面 / 汉堡牛排 |
| 日料韩餐 | asian_food | 寿司刺身 / 便当 |
| 主食米面 | staple_food | 面包 |
| 蔬菜水果 | fruits_veggies | 水果 |
| 肉类海鲜 | meat_seafood | - |
| 乳制品 | dairy | 牛奶 |
| 甜点零食 | dessert_snacks | 蛋糕 / 零食 |
| 饮品 | beverages | - |
| 其他 | other | - |

### 9.3 过敏原标签（10种常见）

| 名称 | Code | 严重程度 |
|------|------|----------|
| 花生 | peanut | 3（重度） |
| 坚果 | tree_nut | 3 |
| 甲壳类海鲜 | shellfish | 3 |
| 鱼类 | fish | 3 |
| 乳制品 | dairy_allergen | 2（中度） |
| 鸡蛋 | egg | 2 |
| 小麦/麸质 | wheat_gluten | 2 |
| 大豆 | soy | 2 |
| 芝麻 | sesame | 2 |
| 芥末 | mustard | 1（轻度） |

### 9.4 系统预设帖子标签（10个，带推荐颜色）

| 名称 | Code | 颜色 |
|------|------|------|
| 急出 | urgent | #EF4444 |
| 免费 | free | #22C55E |
| 限自提 | pickup_only | #3B82F6 |
| 可配送 | delivery_available | #8B5CF6 |
| 素食友好 | vegetarian_friendly | #10B981 |
| 清真 | halal | #06B6D4 |
| 新鲜现做 | fresh_made | #F59E0B |
| 量大 | large_quantity | #EC4899 |
| 冷藏保存 | keep_refrigerated | #0EA5E9 |
| 加热即食 | heat_and_eat | #F97316 |

---

## 10. 接口调用示例（Postman 快速上手）

### 10.1 管理员登录（获取带Role=Admin的Token）

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@leftovershare.com",
  "password": "Admin@123456"
}
```

> **Response.data.token**: 解析后payload包含 `"role": "Admin"` 字样。

### 10.2 管理员创建食物分类

```http
POST /api/FoodCategories
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "name": "素食专享",
  "code": "vegetarian",
  "parentId": null,
  "sortOrder": 10,
  "description": "不含任何肉类成分的素食料理",
  "icon": "🥗",
  "isActive": true
}
```

### 10.3 普通用户创建自定义帖子标签

```http
POST /api/PostTags/user
Authorization: Bearer <user_token>
Content-Type: application/json

{
  "name": "学生优先",
  "code": "student_priority",
  "color": "#6366F1",
  "description": "优先留给在校学生领取",
  "isActive": true
}
```

---

## 11. 版本兼容与演进策略

| 策略 | 说明 |
|------|------|
| **向后兼容** | SharePost.FoodType 字符串字段**保留**，用于过渡期展示，新接口应使用 FoodCategoryId |
| **字段扩展** | 所有实体预留了 Description 字段，可承载附加元信息，无需频繁改表 |
| **编码优先** | 所有业务筛选、权限判断、前后端约定的固定值均通过 Code 字段，Id仅用于数据库关联 |
| **软删除优先** | 所有删除操作默认软删除，数据可在回收站（RecycleBin）模块恢复 |

---

## 12. 关键源文件索引（供后端维护者）

| 文件类型 | 路径 | 说明 |
|----------|------|------|
| 实体 | [FoodCategory.cs](../src/LeftoverShare.API/Entities/FoodCategory.cs) | 食物分类实体 |
| 实体 | [AllergenTag.cs](../src/LeftoverShare.API/Entities/AllergenTag.cs) | 过敏原标签实体 |
| 实体 | [PostTag.cs](../src/LeftoverShare.API/Entities/PostTag.cs) | 帖子标签实体 |
| 枚举 | [UserRole.cs](../src/LeftoverShare.API/Entities/Enums/UserRole.cs) | 用户角色枚举 |
| 循环引用检测（后代） | [FoodCategoryRepository.cs](../src/LeftoverShare.API/Repositories/FoodCategoryRepository.cs#L62-L100) | `GetAllDescendantIdsAsync` BFS遍历实现 |
| 循环引用检测（祖先） | [FoodCategoryRepository.cs](../src/LeftoverShare.API/Repositories/FoodCategoryRepository.cs#L102-L130) | `GetAllAncestorIdsAsync` 向上遍历实现 |
| DbContext | [AppDbContext.cs](../src/LeftoverShare.API/Data/AppDbContext.cs) | 表结构FluentAPI配置 |
| 初始化 | [DbInitializer.cs](../src/LeftoverShare.API/Data/DbInitializer.cs) | 种子数据逻辑 |
| 控制器 | [FoodCategoriesController.cs](../src/LeftoverShare.API/Controllers/FoodCategoriesController.cs) | 分类API |
| 控制器 | [AllergenTagsController.cs](../src/LeftoverShare.API/Controllers/AllergenTagsController.cs) | 过敏原API |
| 控制器 | [PostTagsController.cs](../src/LeftoverShare.API/Controllers/PostTagsController.cs) | 帖子标签API |
| 服务实现 | [FoodCategoryService.cs](../src/LeftoverShare.API/Services/Impl/FoodCategoryService.cs#L97-L113) | 创建时循环引用检测逻辑 |
| 服务实现 | [FoodCategoryService.cs](../src/LeftoverShare.API/Services/Impl/FoodCategoryService.cs#L144-L171) | 更新时循环引用检测逻辑 |
| 服务实现 | [AllergenTagService.cs](../src/LeftoverShare.API/Services/Impl/AllergenTagService.cs) | 业务逻辑 |
| 服务实现 | [PostTagService.cs](../src/LeftoverShare.API/Services/Impl/PostTagService.cs) | 业务逻辑（双重所有者权限） |
| DTO验证 | */Validators/* 目录下6个Validator | FluentValidation规则 |

---

## 13. 本次修复内容记录（v1.0.0 → v1.0.2）

### v1.0.1 → v1.0.2（本次补充修复）

| # | 修复内容 | 相关文件 |
|---|----------|----------|
| 1 | **创建时循环引用检测**：在 `CreateAsync` 中添加父级祖先链循环引用检测，确保新分类的父级所在树结构健康 | [FoodCategoryService.cs](../src/LeftoverShare.API/Services/Impl/FoodCategoryService.cs#L97-L113) |
| 2 | **祖先链检测方法**：新增 `GetAllAncestorIdsAsync` 方法，向上遍历祖先链并检测循环 | [IFoodCategoryRepository.cs](../src/LeftoverShare.API/Repositories/IFoodCategoryRepository.cs#L46-L50)、[FoodCategoryRepository.cs](../src/LeftoverShare.API/Repositories/FoodCategoryRepository.cs#L102-L130) |
| 3 | **更新时祖先链检测**：在 `UpdateAsync` 中也加入祖先链健康检测，双重保障 | [FoodCategoryService.cs](../src/LeftoverShare.API/Services/Impl/FoodCategoryService.cs#L163-L170) |
| 4 | **文档更新**：补充创建时的检测逻辑说明，更新版本号至v1.0.2 | [006_category_and_tag_module.md](../docs/006_category_and_tag_module.md) |

---

### v1.0.0 → v1.0.1（之前修复）

| # | 修复内容 | 相关文件 |
|---|----------|----------|
| 1 | **食物分类循环引用检测**：更新分类时检测父级不能是自身或任意后代（BFS遍历所有后代ID） | [FoodCategoryService.cs](../src/LeftoverShare.API/Services/Impl/FoodCategoryService.cs#L144-L155)、[FoodCategoryRepository.cs](../src/LeftoverShare.API/Repositories/FoodCategoryRepository.cs#L62-L100) |
| 2 | **接口权限收紧**：`GET /api/AllergenTags` 和 `GET /api/PostTags` 分页列表接口从 `[Authorize]` 改为 `[Authorize(Roles = "Admin")]`，所有写操作接口也统一明确角色 | 三个Controller的所有写操作接口 |
| 3 | **文档修正**：对照实际代码逐行核对，删除11个未实现的接口描述，标注所有「预留设计」功能，修正所有接口路径和参数 | [006_category_and_tag_module.md](../docs/006_category_and_tag_module.md) |

---

> **文档维护责任人**: 后端团队
> **审核状态**: ✅ 已与实际代码逐行核对
