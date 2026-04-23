# Developer Quick Reference

## Essential Keyboard Shortcuts

| Shortcut | Function |
|----------|----------|
| `Ctrl+Shift+C` | Open Console (for debugging) |
| `Ctrl+S` | Save scene |
| `F5` / `Ctrl+P` | Play game in editor |
| `Ctrl+D` | Duplicate selected object |
| `V` | Toggle vertex snapping |

## Script Templates for New Components

### Basic Enemy
```csharp
public class MyEnemy : EnemyBase
{
    [SerializeField] private float attackCooldown = 2f;
    
    protected override void AttackBehavior()
    {
        // Attack implementation
    }
}
```

### Basic Animal
```csharp
public class MyAnimal : AnimalBase
{
    [SerializeField] private float resourceSpawnInterval = 5f;
    
    protected override void ProduceResource()
    {
        // Resource generation
    }
}
```

### UI Component
```csharp
[DisallowMultipleComponent]
public sealed class MyUI : MonoBehaviour
{
    [SerializeField] private InventorySystem inventory;
    
    private void OnEnable()
    {
        InventorySystem.ResourceChanged += OnResourceChanged;
    }
    
    private void OnDisable()
    {
        InventorySystem.ResourceChanged -= OnResourceChanged;
    }
    
    private void OnResourceChanged(ResourceType type, int amount)
    {
        // Update UI
    }
}
```

## Common Debugging Tips

### Check Compile Errors
```csharp
// Look in Console window or use:
// Window → General → Console
```

### Log Resource State
```csharp
Debug.Log($"Wood: {GameManager.Instance.Inventory.Get(ResourceType.Wood)}");
```

### Check Enemy Count
```csharp
int enemyCount = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length;
Debug.Log($"Enemies spawned: {enemyCount}");
```

### Monitor Performance
```
Window → Analysis → Profiler → CPU Usage
```

## Accessing Game Systems

### Get GameManager
```csharp
GameManager manager = GameManager.Instance;
```

### Get Inventory
```csharp
InventorySystem inventory = GameManager.Instance.Inventory;
```

### Get Time Manager
```csharp
TimeManager time = TimeManager.Instance;
float hour = time.GetCurrentHour();
```

## Event Subscription Examples

### Subscribe to Health Changes
```csharp
void Start()
{
    PlayerHealth health = GetComponent<PlayerHealth>();
    health.HealthChanged += (current, max) => Debug.Log($"HP: {current}/{max}");
    health.Died += () => Debug.Log("Player died!");
}
```

### Subscribe to Resource Changes
```csharp
void Start()
{
    InventorySystem inventory = GameManager.Instance.Inventory;
    InventorySystem.ResourceChanged += (type, amount) => {
        Debug.Log($"{type}: {amount}");
    };
}
```

### Subscribe to Game Pause
```csharp
void Start()
{
    GameManager.OnGamePaused += () => Debug.Log("Game paused");
    GameManager.OnGameResumed += () => Debug.Log("Game resumed");
}
```

## Inspector Tips

### Auto-Linking Components
Add this to any script to auto-link references:
```csharp
private void OnValidate()
{
    if (myRigidbody == null)
        myRigidbody = GetComponent<Rigidbody2D>();
}
```

### Organizing Inspector
Use [Header] and [Space] for readability:
```csharp
[Header("Combat")]
[SerializeField] private int damage = 5;

[Space]
[Header("Movement")]
[SerializeField] private float speed = 5f;
```

### Readonly in Inspector
```csharp
[field: SerializeField] public int ReadOnlyValue { get; private set; }
```

## Physics 2D Setup

### For Dynamic Objects (enemies, player)
- Body Type: Dynamic
- Gravity Scale: 0 (for isometric/topdown)
- Collision Detection: Continuous
- Constraints: Freeze Rotation Z

### For Triggers (hitboxes, pickups)
- Is Trigger: ✓ Checked
- Collider 2D component only

### For Static Objects (walls, terrain)
- Body Type: Static or Kinematic

## Animation Parameters

### Common Animator Parameters
```csharp
// Floats (for smooth transitions)
animator.SetFloat("MoveX", direction.x);
animator.SetFloat("MoveY", direction.y);

// Bools (for states)
animator.SetBool("IsMoving", isMoving);

// Triggers (one-time events)
animator.SetTrigger("Attack");
animator.SetTrigger("TakeDamage");
animator.SetTrigger("Death");
```

## Resource Management

### Current Resource Types
- `ResourceType.Wood` - Building material
- `ResourceType.Stone` - Building material
- `ResourceType.Food` - Consumable
- `ResourceType.Gold` - Currency
- `ResourceType.Crops` - Agricultural product

### Inventory Operations
```csharp
// Add resources
inventory.Add(ResourceType.Wood, 50);

// Remove resources
if (inventory.Remove(ResourceType.Gold, 100))
    Debug.Log("Purchase successful");

// Check amount
int wood = inventory.Get(ResourceType.Wood);

// Check affordability
bool canBuy = inventory.CanAfford(ResourceType.Gold, 100);

// Get total
int total = inventory.GetTotalResources();
```

## Prefab Workflow

### Creating a Prefab from GameObject
1. Drag GameObject from Hierarchy to Assets/prefabs_02/
2. Script will become a prefab
3. Changes in prefab affect all instances

### Editing Prefab
- Click "Open Prefab" in Inspector
- Make changes
- Click "Save" or close editor

### Prefab Overrides
- Blue text in Inspector = prefab override
- Right-click → Revert to reset

## Build Checklist

Before building for release:
- [ ] All scenes added to Build Settings
- [ ] Start scene is Main Menu
- [ ] No missing references
- [ ] No compile errors
- [ ] Spawn limits tuned for performance
- [ ] UI scales properly
- [ ] Audio volume balanced
- [ ] Resolution tested

## Performance Optimization

### Monitor Frame Time
- Profiler: Ctrl+Shift+F7
- Look for spikes in CPU/GPU time

### Reduce Draw Calls
- Use sprite atlases
- Batch static objects
- Reduce UI complexity

### Optimize Physics
- Reduce rigidbody count
- Use simple collision shapes
- Disable unused physics updates

## Useful Unity Project Settings

### Recommended Settings
- **Quality**: Fastest (for topdown 2D)
- **V Sync**: Disabled or Every Frame
- **Fixed Timestep**: 0.02 (50 FPS physics)
- **Max Particles**: Reduced for 2D

### Asset Settings
- **Sprite**: Filter Mode - Point (no blur)
- **Audio**: Compression to Vorbis

## Fast File Navigation

```
Assets/scripts_01/       → Game logic
Assets/prefabs_02/       → Reusable objects
Assets/sprites_03/       → Graphics
Assets/animations_04/    → Animation data
Assets/docs_10/          → Documentation
```

---

**Last Updated**: April 23, 2026  
**For**: Dark Farming Survival Game  
**Unity Version**: 2022.3 LTS+
