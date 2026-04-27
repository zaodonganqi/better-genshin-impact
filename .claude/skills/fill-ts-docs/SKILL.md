---
name: fill-ts-docs
description: 当用户要求为 .d.ts 文件补充 JSDoc 文档、填充 @see 标记、或提到 "fill docs"、"补文档" 时使用此 skill。
---

# Fill TypeScript Documentation for @see-marked declarations

## Instructions

当用户要求为生成的 `.d.ts` 文件补充 JSDoc 文档时，执行以下步骤：

### 步骤

1. 读取目标 `.d.ts` 文件，找到所有只包含 `@see` 标记（没有 summary 内容）的声明
2. 对于每个 `@see` 标记，解析路径和行号，格式为 `@see filepath:line`
3. 根据路径和行号，读取对应的 C# 源码上下文：
   - 读取声明所在行
   - 向上查找 `/// <summary>` 块（最多 20 行）
   - 提取 `<summary>`、`<param>`、`<returns>`、`<remarks>` 的内容
4. 结合方法名、参数类型、返回值和源码逻辑，生成中文 JSDoc
5. 替换原有 `@see` 注释，**删除 @see 行**，不保留

### 编辑策略（重要）

**不要使用多个 agent 并行编辑同一个文件**。Edit 工具会检测文件级别的修改哈希，任何一个 agent 的写入都会导致其他 agent 的 Read 失效，即使它们编辑的是不同的行区域。按 C# 源文件分组**不能**避免此问题。

正确的策略：

1. **只启动 1 个 agent** 负责读取 C# 源码并生成 JSDoc 内容
2. 该 agent 只做**研究和收集**，不直接编辑文件，而是将所有替换内容输出为列表
3. **主 agent（你自己）串行执行所有 Edit 操作**，每次 Read 确认上下文后立即 Edit
4. 如果标记数量多（>50），分批串行处理，每批 15-20 个 Edit 调用

**禁止**：同时启动多个 agent 编辑 `bettergi.d.ts`，必定导致大量失败和重复劳动。

### JSDoc 格式

```typescript
/**
 * [中文描述，简洁一行]
 * @param paramName [中文参数描述]
 * @returns [中文返回值描述]
 */
```

对于简单属性（无参数/返回值）：
```typescript
/**
 * [中文描述]
 */
```

### 语言规则

- **使用中文**，与文件其余部分保持一致
- 保留专业术语不翻译（如 RecognitionObject、BvLocator、Mat）
- 简洁明了，一行内完成描述
- **不要保留 `@see filepath:line` 行**，填充完 JSDoc 后删除它

### 重载方法

对于同名重载方法，每个重载都应有自己的 JSDoc，根据各自的参数组合描述差异。

### 示例

输入：
```typescript
/** @see BetterGenshinImpact/Core/Script/Dependence/Genshin.cs:64 */
tp(x: number, y: number): Promise<void>;
```

输出：
```typescript
/**
 * 传送到指定位置
 * @param x 目标X坐标
 * @param y 目标Y坐标
 */
tp(x: number, y: number): Promise<void>;
```
