# 分类与标签管理模块 - 数据字典与接口文档

> **模块版本**: v1.0.0
> **创建日期**: 2025
> **适用范围**: LeftoverShare 后端 API v1
> **文档用途**: 提供给前端、移动端及第三方接口调用方的协作参考

---

## 1. 模块概述

本模块为 LeftoverShare 剩食分享平台提供分类与标签的统一管理能力，包含三类核心业务对象：

| 对象 | 英文名 | 用途 | 管理权限 |
|------|--------|------|----------|
| 食物分类 | FoodCategory | 食物类别的层级分类体系 | 仅管理员 |
| 过敏原标签 | AllergenTag | 食品安全相关的过敏原警示标签 | 仅管理员 |
| 帖子标签 | PostTag | 附加在分享帖上的自定义/系统标签 | 管理员+用户(自有标签) |

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
- 分类被SharePost引用时，删除操作会失败（需先解除引用或改为SetNull）

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
| Code | VARCHAR(50) | NO | - | 标签编码（系统标签有，用户标签可自动生成） | 唯一索引 |
| Color | VARCHAR(7) | YES | NULL | 标签颜色（HEX格式，如#FF5500） | 正则匹配^#[0-9A-Fa-f]{6}$ |
| SortOrder | INT | NO | 0 | 显示排序 | ≥0 |
| Description | NVARCHAR(200) | YES | NULL | 标签用途说明 | 最大200字符 |
| UsageCount | INT | NO | 0 | 使用次数统计（热门标签排序用） | ≥0 |
| IsSystemDefined | BIT | NO | 0 | 是否系统预设标签（系统标签仅管理员可改删） | 默认false=用户自定义 |
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

---

### 3.5 SharePostPostTags（帖子-帖子标签关联表，多对多中间表）

**主键**: `(SharePostId, PostTagId) COMPOSITE PK`

| 字段名 | 数据类型 | 可空 | 说明 | 约束 |
|--------|----------|------|------|------|
| SharePostId | INT | NO | 帖子ID | PK, FK→SharePosts.Id (Cascade级联) |
| PostTagId | INT | NO | 帖子标签ID | PK, FK→PostTags.Id (Cascade级联清理关联) |

---

### 3.6 Users表（Role字段扩展）

| 新增字段 | 数据类型 | 可空 | 默认值 | 说明 |
|----------|----------|------|--------|------|
| Role | VARCHAR(20) | NO | 'User' | 用户角色（存储字符串形式的UserRole枚举值） |

---

## 4. API 接口列表

### 4.1 食物分类（FoodCategories）

> **基础路径**: `GET/POST/PUT/DELETE /api/food-categories`

| # | 方法 | 路径 | 权限 | 描述 |
|---|------|------|------|------|
| 4.1.1 | GET | `/api/food-categories` | 匿名 | 获取分类列表（支持树结构/扁平结构两种模式） |
| 4.1.2 | GET | `/api/food-categories/{id}` | 匿名 | 获取单个分类详情（含子分类） |
| 4.1.3 | GET | `/api/food-categories/{id}/children` | 匿名 | 获取指定分类的直接子分类 |
| 4.1.4 | GET | `/api/food-categories/{id}/path` | 匿名 | 获取从根到当前分类的完整路径链 |
| 4.1.5 | GET | `/api/food-categories/roots` | 匿名 | 获取所有顶级分类（ParentId=NULL） |
| 4.1.6 | POST | `/api/food-categories` | Admin | 新增分类 |
| 4.1.7 | PUT | `/api/food-categories/{id}` | Admin | 更新分类 |
| 4.1.8 | DELETE | `/api/food-categories/{id}` | Admin | 删除分类（软删除） |
| 4.1.9 | PATCH | `/api/food-categories/{id}/toggle` | Admin | 启用/停用分类（切换IsActive） |

**GET /api/food-categories 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| includeInactive | bool | false | 是否包含已停用的分类（仅管理员可传true） |
| viewMode | string | "tree" | "tree"=树形结构, "flat"=扁平结构 |
| parentId | int? | NULL | 仅查询指定父级下的分类（仅flat模式生效） |
| keyword | string | NULL | 按名称/编码模糊搜索 |
| sortBy | string | "sortorder" | 排序字段: sortorder/name/createdAt |
| sortDirection | string | "asc" | 排序方向: asc/desc |
| pageIndex | int | 1 | 页码（仅flat模式） |
| pageSize | int | 20 | 每页数量（仅flat模式） |

**POST /api/food-categories 请求体示例**:
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

---

### 4.2 过敏原标签（AllergenTags）

> **基础路径**: `/api/allergen-tags`

| # | 方法 | 路径 | 权限 | 描述 |
|---|------|------|------|------|
| 4.2.1 | GET | `/api/allergen-tags` | 匿名 | 获取过敏原标签列表（支持按严重程度筛选） |
| 4.2.2 | GET | `/api/allergen-tags/{id}` | 匿名 | 获取单个标签详情 |
| 4.2.3 | POST | `/api/allergen-tags` | Admin | 新增过敏原标签 |
| 4.2.4 | PUT | `/api/allergen-tags/{id}` | Admin | 更新标签 |
| 4.2.5 | DELETE | `/api/allergen-tags/{id}` | Admin | 删除标签（软删除） |
| 4.2.6 | PATCH | `/api/allergen-tags/{id}/toggle` | Admin | 启用/停用标签 |

**GET /api/allergen-tags 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| includeInactive | bool | false | 包含已停用的（仅管理员） |
| severityLevel | int? | NULL | 按严重程度过滤: 1/2/3 |
| keyword | string | NULL | 按名称/编码搜索 |
| sortBy | string | "severity" | 排序: severity/sortorder/name/usageCount |

---

### 4.3 帖子标签（PostTags）

> **基础路径**: `/api/post-tags`

| # | 方法 | 路径 | 权限 | 描述 |
|---|------|------|------|------|
| 4.3.1 | GET | `/api/post-tags` | 匿名 | 获取标签列表（支持分页/搜索/筛选） |
| 4.3.2 | GET | `/api/post-tags/{id}` | 匿名 | 获取单个标签详情 |
| 4.3.3 | GET | `/api/post-tags/popular` | 匿名 | 获取热门标签榜（按UsageCount降序） |
| 4.3.4 | GET | `/api/post-tags/search` | 匿名 | 搜索标签（供发帖时联想输入） |
| 4.3.5 | GET | `/api/post-tags/system` | 匿名 | 获取所有系统预设标签 |
| 4.3.6 | GET | `/api/post-tags/user/mine` | User(本人) | 获取当前登录用户创建的自定义标签 |
| 4.3.7 | GET | `/api/post-tags/user/{userId}` | Admin | 获取指定用户创建的标签（仅管理员） |
| 4.3.8 | POST | `/api/post-tags` | User | 新增自定义标签（用户创建自动归属） |
| 4.3.9 | PUT | `/api/post-tags/{id}` | Admin + Owner | 更新标签（系统标签仅Admin，用户标签需是创建者） |
| 4.3.10 | DELETE | `/api/post-tags/{id}` | Admin + Owner | 删除标签（同上权限） |
| 4.3.11 | PATCH | `/api/post-tags/{id}/toggle` | Admin + Owner | 启用/停用标签 |

**GET /api/post-tags 查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| scope | string | "all" | all=全部, system=仅系统, user=仅用户自定义 |
| createdBy | int? | NULL | 仅查询指定用户创建的标签（仅管理员可查他人） |
| keyword | string | NULL | 按名称/编码模糊搜索 |
| color | string | NULL | 按颜色HEX精确匹配 |
| sortBy | string | "usage" | 排序: usage=使用次数/name=名称/createdAt=创建时间 |
| sortDirection | string | "desc" | asc/desc |
| pageIndex | int | 1 | 页码 |
| pageSize | int | 20 | 每页数量 |
| includeInactive | bool | false | 包含已停用的（仅管理员） |

**POST /api/post-tags 请求体示例**:
```json
{
  "name": "学生优先",
  "code": null,
  "color": "#6366F1",
  "description": "优先留给在校学生领取"
}
```
> 用户创建标签时Code可留空，服务端自动生成如 `custom_学生优先_user123_t8k2` 格式的唯一编码。

---

## 5. 权限矩阵（Permission Matrix）

| 操作 | 匿名用户 | 普通登录用户 | 管理员(Admin) |
|------|:--------:|:------------:|:-------------:|
| 查询三类数据（列表/详情/搜索/热门） | ✅ | ✅ | ✅ |
| 查询系统预设帖子标签 | ✅ | ✅ | ✅ |
| 查询**自己**创建的自定义标签 | ❌ | ✅ | ✅ |
| 查询**任意用户**创建的自定义标签 | ❌ | ❌ | ✅ |
| 创建食物分类/过敏原标签 | ❌ | ❌ | ✅ |
| 创建用户自定义帖子标签 | ❌ | ✅（归属当前用户） | ✅（可创建系统标签） |
| 更新**系统定义**的FoodCategory/AllergenTag/PostTag | ❌ | ❌ | ✅ |
| 更新**自己创建**的用户PostTag | ❌ | ✅ | ✅ |
| 更新**他人创建**的用户PostTag | ❌ | ❌ | ✅ |
| 停用/启用任意标签/分类 | ❌ | ❌（自己的可） | ✅ |
| 软删除任意分类/标签 | ❌ | ❌（自己的可） | ✅ |
| 回收站操作（恢复/硬删） | ❌ | ❌ | ✅ |

---

## 6. 数据校验规则（FluentValidation）

所有写操作均在服务端自动执行校验，以下为各字段通用校验规则：

### 6.1 通用校验规则

| 字段 | 适用对象 | 校验规则 |
|------|----------|----------|
| Name | 全部三类 | 必填，去空格后非空，最大长度见数据表 |
| Code | 全部三类（创建时） | 必填（用户PostTag可选），字母开头，仅允许字母、数字、下划线，长度≤50，全局唯一 |
| Description | 全部三类 | 若填写，最大长度500字符 |
| SortOrder | 全部三类 | ≥0的整数 |
| Color | PostTag | 若填写，必须符合正则 `^#[0-9A-Fa-f]{6}$`，如#FF5500 |
| SeverityLevel | AllergenTag | 必填，值∈[1, 2, 3] |
| ParentId | FoodCategory | 若填写，必须指向存在且未删除的父分类；**禁止指向自己（防止循环引用）**；禁止指向其子代（防止循环树） |

### 6.2 业务层附加校验

| 校验场景 | 说明 | 错误码 |
|----------|------|--------|
| 食物分类Code重复 | Code必须全局唯一 | CATEGORY_CODE_DUPLICATE |
| 同层级下食物分类Name重复 | 同一ParentId下子分类名称不能重复 | CATEGORY_NAME_DUPLICATE_SAME_PARENT |
| 过敏原Code/Name重复 | 全局唯一 | ALLERGEN_DUPLICATE |
| 系统标签Code/Name重复 | 系统标签名称全局唯一 | POSTTAG_SYSTEM_DUPLICATE |
| 用户自定义标签与本人其他标签同名 | CreatedBy + Name联合唯一 | POSTTAG_USER_DUPLICATE |
| 父分类不存在或已删除 | ParentId有效性校验 | PARENT_CATEGORY_NOT_FOUND |
| 形成循环引用 | 父级不能是自己或后代 | CATEGORY_CYCLE_DETECTED |
| 删除存在子分类的父分类 | 必须先删除/转移子分类 | CATEGORY_HAS_CHILDREN |
| 删除被帖子引用的FoodCategory | 需先解除引用（SetNull后删除） | CATEGORY_IN_USE |
| 尝试修改/删除系统标签但非管理员 | 权限不足 | SYSTEM_TAG_NOT_EDITABLE |
| 尝试修改/删除他人用户标签非管理员 | 权限不足 | TAG_NOT_OWNER |
| 停用分类后关联帖子的处理 | 不影响历史关联数据，新发帖时不再展示 | 静默处理 |

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
| 20000 - 29999 | 成功（20000=通用成功，20100=创建成功） |
| 40000 - 49999 | 客户端参数校验失败 |
| 40100 - 40199 | 未认证（未登录或Token过期） |
| 40300 - 40399 | 无权限（角色不足或非资源所有者） |
| 40400 - 40499 | 资源不存在 |
| 40900 - 40999 | 业务冲突（重复、循环引用、被引用等） |
| 50000+ | 服务端内部错误 |

### 7.2 本模块专属错误码

| 错误码 | HTTP 状态 | 场景 | 建议前端处理 |
|--------|-----------|------|--------------|
| 40301 | 403 | 非管理员尝试管理分类/过敏原 | 提示"无权限，需管理员账号" |
| 40302 | 403 | 非标签创建者尝试修改用户标签 | 提示"只能修改自己创建的标签" |
| 40303 | 403 | 非管理员尝试修改系统预设标签 | 提示"系统标签仅管理员可编辑" |
| 40901 | 409 | Code编码已存在 | 聚焦到Code输入框，提示换一个 |
| 40902 | 409 | 同层级分类名称重复 | 提示"同级分类下名称已存在" |
| 40903 | 409 | 父分类ID不存在 | 提示"请选择有效的父分类" |
| 40904 | 409 | 父级设置形成循环引用树 | 提示"不能选择自己的子分类作为父级" |
| 40905 | 409 | 要删除的分类存在未处理的子分类 | 提示"请先删除或转移所有子分类" |
| 40906 | 409 | 要删除的分类/标签被帖子引用中 | 弹出二次确认，或先解除引用 |
| 40907 | 409 | 用户自定义标签与本人已有标签重名 | 提示"你已创建过同名标签" |

---

## 8. 搜索过滤衔接设计（Search & Filter Ready）

为了后续搜索模块的无缝衔接，本模块已预置以下设计：

### 8.1 字段索引覆盖

所有高频过滤字段均已建立数据库索引：
- `FoodCategories.Code`（唯一索引，按编码精确查找）
- `FoodCategories.ParentId`（按父级筛选子分类）
- `AllergenTags.SeverityLevel`（按严重程度过滤过敏原）
- `PostTags.UsageCount`（热门标签排序）
- `PostTags.IsSystemDefined`（系统/用户标签筛选）
- `PostTags.CreatedBy`（按创建者筛选）
- 全部三类实体的 `(IsActive, IsDeleted)` 组合索引

### 8.2 预留查询参数

所有列表接口均预留了以下可扩展参数：

| 参数名 | 类型 | 说明 | 搜索模块使用方式 |
|--------|------|------|------------------|
| `foodCategoryIds` | int[] | 按食物分类批量过滤（多选） | 前端筛选器勾选项 |
| `excludeAllergenIds` | int[] | 排除含指定过敏原的帖子（过敏用户保护） | 用户偏好设置自动注入 |
| `severityLevelGte` | int | 过敏原严重程度≥N | 过敏风险预警 |
| `postTagIds` | int[] | 按帖子标签批量过滤（OR匹配） | 标签云/兴趣筛选 |
| `tagScopes` | string[] | 限定标签范围: ["system", "user_custom"] | 只按系统标签过滤 |
| `categoryPathContains` | int | 筛选某分类及其所有子类（递归树） | 支持"中餐"下川菜/粤菜一并显示 |
| `inactiveOnly` | bool | 仅查询已停用的（管理员后台） | 数据管理功能 |

### 8.3 热门标签与联想搜索

- **`POST /api/share-posts`（发帖）** 时，前端可异步调用 `GET /api/post-tags/search?keyword=X` 获取联想建议
- **列表页顶部标签云**：调用 `GET /api/post-tags/popular?topN=30`
- 所有搜索查询均已支持中文LIKE（EF Core + Pomelo MySQL 兼容）

### 8.4 UsageCount 维护机制

每次帖子关联/取消关联帖子标签时，Service层自动递增/递减 `PostTags.UsageCount`：

| 场景 | 操作 |
|------|------|
| 帖子创建时添加X个标签 | X个标签各+1 |
| 帖子编辑时新增Y个标签 | Y个标签各+1 |
| 帖子编辑时移除Z个标签 | Z个标签各-1 |
| 帖子被删除（软删除） | 该帖子所有标签各-1 |

> 此设计保证热门标签榜单基于"当前活跃帖子数"而非历史累计，更加真实有效。

---

## 9. 种子数据清单（Initializer 预置数据）

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
| 急出 | urgent | #EF4444 红 |
| 免费 | free | #22C55E 绿 |
| 限自提 | pickup_only | #3B82F6 蓝 |
| 可配送 | delivery_available | #8B5CF6 紫 |
| 素食友好 | vegetarian_friendly | #10B981 翠绿 |
| 清真 | halal | #06B6D4 青 |
| 新鲜现做 | fresh_made | #F59E0B 黄 |
| 量大 | large_quantity | #EC4899 粉 |
| 冷藏保存 | keep_refrigerated | #0EA5E9 天蓝 |
| 加热即食 | heat_and_eat | #F97316 橙 |

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
POST /api/food-categories
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
POST /api/post-tags
Authorization: Bearer <user_token>
Content-Type: application/json

{
  "name": "学生优先",
  "color": "#6366F1",
  "description": "优先留给在校学生领取"
}
```

---

## 11. 版本兼容与演进策略

| 策略 | 说明 |
|------|------|
| **向后兼容** | SharePost.FoodType 字符串字段**保留**，用于过渡期展示，新接口应使用 FoodCategoryId |
| **字段扩展** | 所有实体预留了 Description 字段，可承载附加元信息，无需频繁改表 |
| **编码优先** | 所有业务筛选、权限判断、前后端约定的固定值均通过 Code 字段，Id仅用于数据库关联（避免硬编码ID） |
| **软删除优先** | 所有删除操作默认软删除，数据可在回收站（RecycleBin）模块恢复 |

---

## 12. 关键源文件索引（供后端维护者）

| 文件类型 | 路径 | 说明 |
|----------|------|------|
| 实体 | [FoodCategory.cs](../src/LeftoverShare.API/Entities/FoodCategory.cs) | 食物分类实体 |
| 实体 | [AllergenTag.cs](../src/LeftoverShare.API/Entities/AllergenTag.cs) | 过敏原标签实体 |
| 实体 | [PostTag.cs](../src/LeftoverShare.API/Entities/PostTag.cs) | 帖子标签实体 |
| 枚举 | [UserRole.cs](../src/LeftoverShare.API/Entities/Enums/UserRole.cs) | 用户角色枚举 |
| DbContext | [AppDbContext.cs](../src/LeftoverShare.API/Data/AppDbContext.cs) | 表结构FluentAPI配置 |
| 初始化 | [DbInitializer.cs](../src/LeftoverShare.API/Data/DbInitializer.cs) | 种子数据逻辑 |
| 控制器 | [FoodCategoriesController.cs](../src/LeftoverShare.API/Controllers/FoodCategoriesController.cs) | 分类API |
| 控制器 | [AllergenTagsController.cs](../src/LeftoverShare.API/Controllers/AllergenTagsController.cs) | 过敏原API |
| 控制器 | [PostTagsController.cs](../src/LeftoverShare.API/Controllers/PostTagsController.cs) | 帖子标签API |
| 服务实现 | [FoodCategoryService.cs](../src/LeftoverShare.API/Services/Impl/FoodCategoryService.cs) | 业务逻辑（权限校验/循环检测） |
| 服务实现 | [AllergenTagService.cs](../src/LeftoverShare.API/Services/Impl/AllergenTagService.cs) | 业务逻辑 |
| 服务实现 | [PostTagService.cs](../src/LeftoverShare.API/Services/Impl/PostTagService.cs) | 业务逻辑（双重所有者权限） |
| DTO验证 | */Validators/* 目录下6个Validator | FluentValidation规则 |

---

> **文档维护责任人**: 后端团队
> **下次审核日期**: 搜索过滤模块上线后
