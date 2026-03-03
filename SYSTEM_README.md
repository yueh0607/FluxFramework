# FluxFramework 系统架构

## 🎯 核心理念

**一切皆节点 + ECS 架构 + 纯事件驱动**

```
FluxRoot (ID:0)
├── PoolContainerNode (对象池容器)
│   └── TypePoolNode<T> [池内节点...]
├── SystemContainerNode (系统容器)
│   ├── MoveSystem (移动系统)
│   ├── HealthSystem (生命值系统)
│   └── CollisionSystem (碰撞系统)
└── UserRoot (用户根节点)
    └── ThreadNode (主线程)
        └── 业务节点...
```

---

## 📦 系统特性

### 1. **内存优化**
- 1000个节点共享1个系统实例
- 节省 ~87% 内存（vs 传统组件模式）

### 2. **完全解耦**
- 系统之间通过事件通信
- 系统不知道彼此存在
- 易于测试和维护

### 3. **灵活组合**
- 节点可动态附加/移除系统
- 不同节点可自由组合不同系统

### 4. **框架纯净**
- 框架只提供 `System` 基类和容器
- 具体系统和事件由用户定义

---

## 🚀 使用流程

### 步骤 1：初始化框架并注册系统

```csharp
void Start()
{
    // 1. 初始化框架
    Tree.Initialize();

    // 2. 注册全局系统（在 Example 中定义）
    var systems = Tree.SystemContainer;
    systems.RegisterSystem<MoveSystem>();
    systems.RegisterSystem<HealthSystem>();
    systems.RegisterSystem<CollisionSystem>();
    systems.RegisterSystem<InputSystem>();

    // 3. 创建游戏节点
    var mainThread = Tree.Root.AddChild<ThreadNode>();
    mainThread.Configure("MainThread");

    var player = mainThread.AddChild<PlayerNodeWithSystem>();
    player.Initialize();
}
```

### 步骤 2：定义游戏事件

```csharp
// Example/ExampleEvents.cs
namespace FluxFramework.Example
{
    public class MoveRequestEvent : EventArgs
    {
        public Vector3 Direction;
        public float SpeedMultiplier = 1f;
    }

    public class DamageEvent : EventArgs
    {
        public int Damage;
        public Node Source;
    }
}
```

### 步骤 3：实现系统

```csharp
// Example/ExampleSystems.cs
public class MoveSystem : System
{
    public override string SystemType => "Move";
    public override int Priority => 50;

    public override void OnAttach(Node owner)
    {
        base.OnAttach(owner);
        // 监听事件
        owner.On<MoveRequestEvent>(OnMoveRequest);
    }

    private void OnMoveRequest(MoveRequestEvent e)
    {
        // 处理移动逻辑
    }
}
```

### 步骤 4：创建节点并附加系统

```csharp
public class PlayerNode : ViewNode, IInputHandler
{
    // 数据字段（存储在节点上）
    public float MoveSpeed = 5f;
    public int Hp = 100;

    public override void OnSpawn()
    {
        base.OnSpawn();

        // 附加系统（自由组合）
        AttachSystem<InputSystem>();
        AttachSystem<MoveSystem>();
        AttachSystem<HealthSystem>();

        // 监听事件
        On<MoveRequestEvent>(OnMoveRequest);
        On<DamageEvent>(OnDamage);
        On<TickEventArgs>(OnTick);
    }

    private void OnTick(TickEventArgs e)
    {
        // 执行系统（按优先级）
        TickSystems(e);
    }

    private void OnMoveRequest(MoveRequestEvent e)
    {
        if (e.Handled) return;
        
        // 节点自己处理移动
        Position += e.Direction * MoveSpeed * Time.deltaTime;
        e.Handled = true;
    }

    private void OnDamage(DamageEvent e)
    {
        if (e.Handled) return;
        
        Hp -= e.Damage;
        e.Handled = true;
    }
}
```

---

## 🎮 通信流程

```
InputSystem
  → 读取输入
  → 发送 MoveRequestEvent

PlayerNode
  → 监听 MoveRequestEvent
  → 修改 Position

CollisionSystem
  → 检测碰撞
  → 发送 CollisionEvent

PlayerNode
  → 监听 CollisionEvent
  → 发送 DamageEvent

HealthSystem
  → 监听 DamageEvent
  → 处理生命值
```

**关键：系统之间完全解耦，不知道彼此存在！**

---

## 📊 系统优先级

数字越大越先执行：

| 系统 | 优先级 | 说明 |
|------|--------|------|
| InputSystem | 200 | 最先读取输入 |
| CollisionSystem | 100 | 检测碰撞 |
| MoveSystem | 50 | 处理移动 |
| HealthSystem | 30 | 处理生命值 |

---

## 🎯 设计原则

### 1. **数据存储在节点**
```csharp
// ✅ 正确
public class PlayerNode : ViewNode
{
    public float MoveSpeed = 5f;  // 数据在节点
    public int Hp = 100;
}

// ❌ 错误
public class MoveSystem : System
{
    public float Speed = 5f;  // 系统不存储数据
}
```

### 2. **系统通过事件通信**
```csharp
// ✅ 正确
owner.OwnerThread?.EmitPooled<DamageEvent>(e => {
    e.Damage = 10;
});

// ❌ 错误
var healthSystem = owner.GetSystem<HealthSystem>();
healthSystem.TakeDamage(10);  // 直接调用会耦合
```

### 3. **系统通过接口访问节点**
```csharp
// ✅ 正确
if (owner is ICollidable collidable)
{
    var radius = collidable.CollisionRadius;
}

// ✅ 也可以类型转换
if (owner is PlayerNode player)
{
    player.Hp -= 10;
}
```

---

## 🌟 框架文件结构

```
FluxFramework/
├── Core/
│   ├── Node.cs                  # 节点基类
│   ├── FluxRoot.cs              # 框架根
│   ├── Tree.cs                  # API 入口
│   ├── NodePool.cs              # 对象池
│   ├── ThreadNode.cs            # 线程节点
│   └── ...
├── System/
│   ├── System.cs                # 系统基类 ⭐
│   └── SystemContainerNode.cs   # 系统容器 ⭐
├── Event/
│   └── EventArgs.cs             # 事件基类
└── Example/
    ├── ExampleEvents.cs         # 示例事件 (用户自定义)
    ├── ExampleSystems.cs        # 示例系统 (用户自定义)
    └── NodeWithSystemExample.cs # 使用示例
```

---

## 🎉 总结

FluxFramework 现在支持：

✅ 一切皆节点（对象池、系统都是节点）  
✅ ECS 架构（Node = Entity, 字段 = Component, System = System）  
✅ 纯事件驱动（完全解耦）  
✅ 内存优化（系统共享）  
✅ 灵活组合（动态附加系统）  
✅ 框架纯净（系统和事件由用户定义）

**核心设计：框架提供架构，用户定义逻辑**
