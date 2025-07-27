# AssetBundleManager 使用说明

欢迎来到 AssetBundleManager 的魔法课堂！这里教你怎么用几条神奇咒语，轻松管理你的资源世界！

---

## 使用方法

### 🌟 同步篇 — 立刻生效的魔法

- **加载AB包指定资源** —— `LoadABRes` 函数  
  一挥魔杖，资源瞬间现身眼前！

- **卸载指定的单个AB包资源** —— `UnLoadAB` 函数  
  轻轻一挥，把指定的包袱悄悄收回魔法背包。

- **卸载所有AB包资源** —— `UnLoadAllAB` 函数  
  一键清空，资源统统回归原位，魔法现场干净利落！

---

### ⚡ 异步篇 — 悄悄进行的后台魔法

- **加载AB包指定资源** —— `LoadABResAsync` 函数  
  让资源慢慢出现，主线程丝毫不受影响，流畅无比！

- **卸载指定的单个AB包资源** —— `UnLoadABAsync` 函数  
  在暗处悄悄收回目标包袱，不打扰当前魔法运行。

- **卸载所有AB包资源** —— `UnLoadAllABAsyncCor` 函数  
  静悄悄地清空全部包袱，让你的魔法世界焕然一新！

---

### 🧹 资源缓存清理术 — 让魔法背包永远整洁

- **清除已经被销毁资源的引用缓存** —— `ClearUnusedObjects` 函数  
  摒弃死气沉沉的僵尸资源，让背包时刻轻盈灵动！

- **清空所有已加载资源的引用缓存** —— `ClearObjDic` 函数  
  一键重置资源记忆，给管理器一个全新的开始！

---

跟着这些魔法咒语走，资产包管理变得轻松又好玩！  
快去召唤你的资源魔法吧！✨

# 🎩 Coroutine\_Manager\_ 使用说明

欢迎来到 **Coroutine\_Manager\_** 的时间控制魔法课堂！\
在这里，你将学会如何用几条咒语，轻松掌控协程的世界 —— 调度、暂停、停止，全由你说了算！

---

## 🚀 启动协程 — 唤醒你的时间魔法！

### ✨ `StartManagedCoroutine` 函数

召唤一段协程，并打上标签封印在你的魔法书里！\
只需提供一个协程工厂方法，指定标签，就能让协程悄然运行，完成后还能触发魔法回调！

```csharp
StartManagedCoroutine(() => MyRoutine(), CoroutineType.Type1, OnComplete);
```

📌 **附加特效**：

- 支持回调触发
- 可以指定“只有成功完成”时才触发回调

---

## 🧹 协程清理术 — 消灭失控的时间裂缝！

### 🛑 `StopSpecificCoroutine` 函数

想停止某个具体的协程？只要你知道它的类型和句柄，这个魔法就能一击命中！

```csharp
StopSpecificCoroutine(CoroutineType.Type1, myCoroutine);
```

---

### 🧨 `StopAllCoroutineByType` 函数

清空指定类型下的所有协程！是时候对一个标签说再见了！

```csharp
StopAllCoroutineByType(CoroutineType.Type1);
```w

📝 **注意事项**：

> 在遍历集合的同时修改集合（例如 Remove）会引发异常！
>
> ✅ 建议使用 `wrapperSet.ToList()` 创建副本再遍历和移除。

---

### 💥 `StopAllManagedCoroutines` 函数

终极大招！关闭所有正在运行的协程，一切归于平静！

```csharp
StopAllManagedCoroutines();
```

---

## 🔍 侦测魔法 — 洞察协程的运行状态！

### 🔄 `IsAnyCoroutineRunning` 函数

想知道某个标签下是否还有协程在偷偷运行？用它！

```csharp
bool isRunning = IsAnyCoroutineRunning(CoroutineType.Type1);
```

---

### 🔢 `GetRunningCoroutineCount` 函数

想知道某个类型下有几个协程正在活跃？问问这个魔法数字！

```csharp
int count = GetRunningCoroutineCount(CoroutineType.Type1);
```

---

### 🌍 `GetTotalRunningCoroutineCount` 函数

不止一个？全都查出来！返回当前所有标签下运行中的协程总数。

```csharp
int total = GetTotalRunningCoroutineCount();
```

---

## 🧾 日志魔法 — 查看你的协程卷轴！

### 📜 `LogCoroutineInfoByType` 函数

输出某个类型下所有协程的信息。可以筛选是否只显示正在运行的。

```csharp
LogCoroutineInfoByType(CoroutineType.Type1, true); // 只显示运行中的
```

---

### 📚 `LogAllCoroutineInfos` 函数

一次性输出所有协程的信息，想看协程现状？来这就对了！

```csharp
LogAllCoroutineInfos();
```

---

## 🧠 魔法笔记（小Tips）

- ✅ 每个协程启动都会绑定唯一的魔法 ID（使用 `Guid.NewGuid()`），可用于追踪管理。
- 🔒 所有操作都加了线程锁 `lock`，确保协程管理安全稳妥。
- 🏷️ 所有协程都有类型标签，便于分组操作，就像给魔法物品打了标记！
- 🔍 想监控 `Debug.Log` 调用？可以写一个 `LogInterceptor` 来劫持和输出所有日志。

---

快带上你的代码魔杖，在协程的时间之海里畅游吧！\
**Coroutine\_Manager\_，你的时间守护者！⏳🔮**

