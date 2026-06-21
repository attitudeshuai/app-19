# 功能介绍文档

## 📋 业务背景

在日常生活中，我们经常会遇到以下场景：

- **家庭聚会后**：准备了太多食物，吃不完只能倒掉
- **公司下午茶**：订购的点心、水果有剩余
- **节日庆祝**：烹饪了丰盛的菜肴，最后剩下很多
- **外卖点餐**：不小心点多了，吃不完浪费

据统计，全球每年约有 13 亿吨食物被浪费，占食物总产量的 1/3。而与此同时，全球还有 8.2 亿人面临饥饿问题。

**LeftoverShare** 正是为了解决这一问题而诞生的超本地化剩余食物分享平台。通过将有剩余食物的人和需要食物的人连接起来，我们希望：

- ✅ 减少食物浪费，保护环境
- ✅ 帮助有需要的人，传递爱心
- ✅ 增进邻里关系，建设和谐社区
- ✅ 培养节约意识，弘扬传统美德

## 👥 用户角色

### 分享者 (Sharer)
- 发布剩余食物分享帖
- 管理自己发布的分享帖
- 确认预约、生成取餐码
- 完成取餐验证
- 获得爱心积分奖励

### 领取者 (Claimer)
- 浏览附近的分享帖
- 预约想要领取的食物
- 获得取餐码
- 前往取餐地点领取食物
- 查看自己的预约记录

### 管理员 (Administrator)
- 管理所有用户账号
- 审核违规内容
- 查看平台统计数据
- 配置系统参数

## 🎯 核心用例

### 1. 发布分享帖
1. 分享者登录系统
2. 填写食物信息（名称、描述、类型、数量、照片等）
3. 设置取餐地址和有效期限
4. 发布分享帖
5. 系统自动将帖子标记为「可领取」状态

### 2. 预约领取
1. 领取者浏览分享帖列表
2. 查看感兴趣的食物详情
3. 提交预约申请
4. 系统自动将帖子标记为「已预约」状态
5. 分享者收到预约通知

### 3. 取餐验证
1. 分享者确认预约，生成 6 位取餐码
2. 领取者获得取餐码
3. 领取者前往取餐地点
4. 分享者核验取餐码
5. 确认取餐完成，双方获得积分奖励

### 4. 积分奖励
- 分享者成功分享食物：+10 积分
- 领取者完成取餐评价：+2 积分
- 连续分享 7 天：额外 +20 积分
- 积分可用于兑换社区福利或荣誉徽章

## 📦 功能模块详细说明

### 模块一：用户认证模块
**功能说明**：处理用户注册、登录、个人信息管理等认证相关操作。

**主要功能**：
- 用户注册：用户名、邮箱、密码验证
- 用户登录：支持用户名/邮箱 + 密码登录
- 获取当前用户信息
- 更新个人资料（头像、昵称等）
- 密码加密存储（BCrypt 算法）
- JWT Token 认证机制

**权限控制**：
- 注册、登录接口公开访问
- 其他接口需登录认证

### 模块二：分享帖管理模块
**功能说明**：管理剩余食物分享帖的创建、查询、更新、删除等操作。

**主要功能**：
- 发布分享帖：标题、描述、食物类型、数量、照片、取餐地址、有效期
- 分享帖列表：分页查询、关键词搜索
- 分享帖详情：查看完整信息和发布者信息
- 更新分享帖：仅帖主可修改
- 删除分享帖：仅帖主可删除
- 状态管理：可领取、已预约、已取餐、已过期、已完成

**状态流转**：
```
可领取 (Available)
    ↓ 有人预约
已预约 (Reserved)
    ↓ 确认取餐完成
已取餐 (PickedUp)
    ↓ 交易完成
已完成 (Completed)

可领取 (Available)
    ↓ 超过有效期
已过期 (Expired)
```

### 模块三：预约管理模块
**功能说明**：管理用户对分享帖的预约操作。

**主要功能**：
- 创建预约：选择分享帖，填写领取数量
- 预约列表：查看自己的预约记录
- 预约详情：查看预约详细信息
- 更新预约：修改领取数量（仅预约者）
- 取消预约：预约者或帖主可取消
- 状态管理：待确认、已确认、已完成、已取消

**权限控制**：
- 仅预约者和帖主可查看预约详情
- 仅预约者可更新预约信息
- 预约者和帖主均可取消预约

### 模块四：取餐码管理模块
**功能说明**：为已确认的预约生成安全的取餐验证码。

**主要功能**：
- 生成取餐码：6 位随机数字，24 小时有效
- 取餐码列表：查看自己生成的取餐码
- 取餐码详情：查看取餐码状态和有效期
- 验证取餐码：帖主核验取餐码有效性
- 删除取餐码：仅帖主可删除

**安全机制**：
- 取餐码一次性使用，使用后立即失效
- 24 小时自动过期
- 仅帖主和预约者可查看取餐码

### 模块五：爱心积分模块
**功能说明**：管理用户的爱心积分，鼓励分享行为。

**主要功能**：
- 积分记录查询：查看积分变动明细
- 积分获取：分享食物、完成取餐等
- 积分使用：兑换福利（可扩展）
- 积分排行榜：查看社区积分排名

**积分规则**：
| 行为 | 积分奖励 | 说明 |
|------|---------|------|
| 发布分享帖 | +5 | 发布时获得 |
| 完成分享 | +10 | 取餐完成后获得 |
| 完成取餐评价 | +2 | 领取者评价后获得 |
| 连续分享7天 | +20 | 连续7天发布分享帖 |

### 模块六：统计分析模块
**功能说明**：提供平台运营数据统计和分析。

**主要功能**：
- 概览统计：用户总数、分享帖总数、已完成分享数、节省食物重量
- 趋势统计：按日/周/月查看分享趋势
- 用户排行：积分排行榜、分享次数排行
- 食物类型统计：各类食物分享占比

## 🗄️ 数据库 ER 图

### 数据表关系说明

本系统共包含 5 张核心数据表，关系如下：

```
┌─────────────┐       ┌───────────────┐       ┌──────────────┐
│    User     │       │   SharePost   │       │  Reservation │
├─────────────┤       ├───────────────┤       ├──────────────┤
│ Id (PK)     │◄──┐   │ Id (PK)       │◄──┐   │ Id (PK)      │
│ Username    │   │   │ PosterId (FK) │───┘   │ PostId (FK)  │───┐
│ Email       │   │   │ Title         │       │ ClaimerId(FK)│──┐│
│ PasswordHash│   │   │ Description   │       │ Quantity     │  ││
│ Avatar      │   │   │ FoodType      │       │ Status       │  ││
│ Phone       │   │   │ Quantity      │       │ CreatedAt    │  ││
│ Address     │   │   │ Location      │       └──────────────┘  ││
│ TotalPoints │   │   │ Latitude      │                         ││
│ IsActive    │   │   │ Longitude     │       ┌──────────────┐  ││
│ CreatedAt   │   │   │ ExpiryTime    │       │  PickupCode  │  ││
│ UpdatedAt   │   │   │ Photos        │       ├──────────────┤  ││
└─────────────┘   │   │ Status        │       │ Id (PK)      │  ││
                  │   │ CreatedAt     │       │ ReservationId│──┘│
                  │   └───────────────┘       │ Code         │   │
                  │                           │ ExpiryTime   │   │
                  │   ┌───────────────┐       │ IsUsed       │   │
                  │   │  KarmaPoint   │       │ UsedAt       │   │
                  │   ├───────────────┤       │ CreatedAt    │   │
                  │   │ Id (PK)       │       └──────────────┘   │
                  └───│ UserId (FK)   │                          │
                      │ Points        │                          │
                      │ Reason        │                          │
                      │ TransType     │   ┌───────────────┐      │
                      │ RelatedPostId │───│ SharePost     │      │
                      │ CreatedAt     │   └───────────────┘      │
                      └───────────────┘                          │
                                                                 │
                      ┌─────────────┐                            │
                      │    User     │◄───────────────────────────┘
                      └─────────────┘
```

### 各表字段说明

#### 1. User（用户表）
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| Id | INT | 用户ID | 主键，自增 |
| Username | VARCHAR(50) | 用户名 | 唯一，非空 |
| Email | VARCHAR(100) | 邮箱 | 唯一，非空 |
| PasswordHash | VARCHAR(255) | 密码哈希 | 非空 |
| Avatar | VARCHAR(255) | 头像URL | 可空 |
| Phone | VARCHAR(20) | 手机号 | 可空 |
| Address | VARCHAR(255) | 地址 | 可空 |
| TotalKarmaPoints | INT | 总积分 | 默认0 |
| IsActive | TINYINT(1) | 是否激活 | 默认1 |
| CreatedAt | DATETIME | 创建时间 | 默认当前时间 |
| UpdatedAt | DATETIME | 更新时间 | 默认当前时间 |

#### 2. SharePost（分享帖表）
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| Id | INT | 分享帖ID | 主键，自增 |
| UserId | INT | 发布者ID | 外键，关联User.Id |
| Title | VARCHAR(200) | 标题 | 非空 |
| Description | TEXT | 描述 | 非空 |
| FoodType | VARCHAR(50) | 食物类型 | 非空 |
| Quantity | INT | 数量 | 非空 |
| Location | VARCHAR(255) | 取餐地址 | 非空 |
| Latitude | DOUBLE | 纬度 | 非空 |
| Longitude | DOUBLE | 经度 | 非空 |
| ExpiryTime | DATETIME | 过期时间 | 非空 |
| Photos | TEXT | 照片URL列表 | 可空，JSON格式 |
| Status | VARCHAR(20) | 状态 | Available/Reserved/PickedUp/Expired/Completed |
| CreatedAt | DATETIME | 创建时间 | 默认当前时间 |

#### 3. Reservation（预约表）
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| Id | INT | 预约ID | 主键，自增 |
| SharePostId | INT | 分享帖ID | 外键，关联SharePost.Id |
| UserId | INT | 领取者ID | 外键，关联User.Id |
| Quantity | INT | 领取数量 | 非空 |
| CreatedAt | DATETIME | 创建时间 | 默认当前时间 |
| ConfirmedAt | DATETIME | 确认时间 | 可空 |
| PickedUpAt | DATETIME | 取餐时间 | 可空 |
| Status | VARCHAR(20) | 状态 | Pending/Confirmed/Completed/Cancelled |
| Note | TEXT | 备注 | 可空 |

#### 4. PickupCode（取餐码表）
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| Id | INT | 取餐码ID | 主键，自增 |
| ReservationId | INT | 预约ID | 外键，关联Reservation.Id |
| Code | VARCHAR(6) | 取餐码 | 6位数字，非空 |
| ExpiryTime | DATETIME | 过期时间 | 非空 |
| IsUsed | TINYINT(1) | 是否已使用 | 默认0 |
| UsedAt | DATETIME | 使用时间 | 可空 |
| CreatedAt | DATETIME | 创建时间 | 默认当前时间 |

#### 5. KarmaPoint（积分记录表）
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| Id | INT | 记录ID | 主键，自增 |
| UserId | INT | 用户ID | 外键，关联User.Id |
| Points | INT | 积分数值 | 非空 |
| Reason | VARCHAR(255) | 积分原因 | 非空 |
| TransactionType | VARCHAR(20) | 交易类型 | Earn/Spend |
| RelatedSharePostId | INT | 关联分享帖ID | 外键，可空 |
| CreatedAt | DATETIME | 创建时间 | 默认当前时间 |

## ⚙️ 关键业务规则

### 1. 状态流转规则

#### 分享帖状态流转
| 当前状态 | 可转换到 | 触发条件 | 操作人 |
|----------|---------|---------|--------|
| Available | Reserved | 用户提交预约 | 系统自动 |
| Available | Expired | 超过有效期 | 系统自动 |
| Reserved | Available | 预约被取消且无其他有效预约 | 系统自动 |
| Reserved | PickedUp | 取餐完成 | 帖主 |
| Reserved | Expired | 超过有效期 | 系统自动 |
| PickedUp | Completed | 交易完成 | 系统自动 |

#### 预约状态流转
| 当前状态 | 可转换到 | 触发条件 | 操作人 |
|----------|---------|---------|--------|
| Pending | Confirmed | 帖主确认预约 | 帖主 |
| Pending | Cancelled | 取消预约 | 预约者/帖主 |
| Confirmed | Completed | 取餐完成 | 帖主 |
| Confirmed | Cancelled | 取消预约 | 预约者/帖主 |

### 2. 权限规则

#### 数据访问权限
- **分享帖**：所有人可读，仅帖主可修改/删除
- **预约**：仅预约者和帖主可见，仅预约者可修改
- **取餐码**：仅预约者和帖主可见，仅帖主可生成/删除
- **积分记录**：仅用户本人可见自己的记录
- **用户信息**：公开信息（用户名、头像、积分）所有人可见，隐私信息（邮箱、手机号）仅本人可见

#### 操作权限矩阵
| 操作 | 匿名用户 | 普通用户 | 帖主 | 管理员 |
|------|---------|---------|------|--------|
| 注册/登录 | ✅ | ✅ | ✅ | ✅ |
| 浏览分享帖 | ✅ | ✅ | ✅ | ✅ |
| 发布分享帖 | ❌ | ✅ | ✅ | ✅ |
| 修改分享帖 | ❌ | ❌ | ✅ | ✅ |
| 删除分享帖 | ❌ | ❌ | ✅ | ✅ |
| 提交预约 | ❌ | ✅ | ❌ | ✅ |
| 确认预约 | ❌ | ❌ | ✅ | ✅ |
| 生成取餐码 | ❌ | ❌ | ✅ | ✅ |
| 验证取餐码 | ❌ | ❌ | ✅ | ✅ |
| 查看统计 | ❌ | ❌ | ❌ | ✅ |

### 3. 时间计算逻辑

#### 有效期计算
- 分享帖有效期：由用户在发布时指定，最长不超过 7 天
- 取餐码有效期：生成后 24 小时自动过期
- 系统每日凌晨自动扫描过期的分享帖和取餐码

#### 积分计算
- 分享帖发布时：+5 积分（立即到账）
- 取餐完成时：分享者 +10 积分（立即到账）
- 领取者评价后：+2 积分（立即到账）
- 连续分享奖励：每连续 7 天发布分享帖，额外 +20 积分（系统自动计算）

#### 时区处理
- 数据库存储：统一使用 UTC 时间
- 接口返回：自动转换为用户所在时区
- 时间比较：所有时间比较均基于 UTC 时间

## 📡 接口调用示例

### 示例一：用户注册

**接口**：`POST /api/auth/register`

**请求头**：
```
Content-Type: application/json
```

**请求体**：
```json
{
  "username": "zhangshan",
  "email": "zhangshan@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!"
}
```

**成功响应** (200 OK)：
```json
{
  "success": true,
  "message": "注册成功",
  "data": {
    "user": {
      "id": 1,
      "username": "zhangshan",
      "email": "zhangshan@example.com",
      "avatar": null,
      "totalKarmaPoints": 0,
      "createdAt": "2026-06-20T05:30:00Z"
    },
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**失败响应** (400 Bad Request)：
```json
{
  "success": false,
  "message": "用户名已存在",
  "data": null
}
```

---

### 示例二：用户登录

**接口**：`POST /api/auth/login`

**请求头**：
```
Content-Type: application/json
```

**请求体**：
```json
{
  "usernameOrEmail": "zhangshan",
  "password": "Password123!"
}
```

**成功响应** (200 OK)：
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "user": {
      "id": 1,
      "username": "zhangshan",
      "email": "zhangshan@example.com",
      "avatar": null,
      "totalKarmaPoints": 0,
      "createdAt": "2026-06-20T05:30:00Z"
    },
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**失败响应** (400 Bad Request)：
```json
{
  "success": false,
  "message": "用户名或密码错误",
  "data": null
}
```

---

### 示例三：发布分享帖

**接口**：`POST /api/shareposts`

**请求头**：
```
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**请求体**：
```json
{
  "title": "家庭聚餐剩余的红烧肉",
  "description": "今天家里聚餐做了太多红烧肉，大约还有2斤，味道很好，希望有人能来取走。",
  "foodType": "家常菜",
  "quantity": 2,
  "pickupAddress": "北京市朝阳区XX小区3号楼2单元501",
  "latitude": 39.9042,
  "longitude": 116.4074,
  "availableUntil": "2026-06-21T12:00:00Z",
  "photos": [
    "https://example.com/photos/hongshaorou1.jpg",
    "https://example.com/photos/hongshaorou2.jpg"
  ]
}
```

**成功响应** (201 Created)：
```json
{
  "success": true,
  "message": "创建成功",
  "data": {
    "id": 1,
    "posterId": 1,
    "posterName": "zhangshan",
    "title": "家庭聚餐剩余的红烧肉",
    "description": "今天家里聚餐做了太多红烧肉，大约还有2斤，味道很好，希望有人能来取走。",
    "foodType": "家常菜",
    "quantity": 2,
    "pickupAddress": "北京市朝阳区XX小区3号楼2单元501",
    "latitude": 39.9042,
    "longitude": 116.4074,
    "availableUntil": "2026-06-21T12:00:00Z",
    "photos": [
      "https://example.com/photos/hongshaorou1.jpg",
      "https://example.com/photos/hongshaorou2.jpg"
    ],
    "status": "Available",
    "createdAt": "2026-06-20T06:00:00Z"
  }
}
```

**失败响应** (401 Unauthorized)：
```json
{
  "success": false,
  "message": "用户未认证",
  "data": null
}
```

---

### 注意事项

1. 所有需要认证的接口必须在请求头中携带 `Authorization: Bearer <token>`
2. Token 有效期为 24 小时，过期后需要重新登录
3. 所有时间字段均使用 ISO 8601 格式（UTC 时间）
4. 请求和响应均使用 UTF-8 编码
