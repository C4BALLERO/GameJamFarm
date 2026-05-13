# Dark Farming Survival Game

A 2D top-down farming survival game built with Unity. Manage your farm, defend against enemies, and build your settlement.

## Quick Start

### Prerequisites
- Unity 2022.3 LTS or newer
- 2D project template
- Universal Render Pipeline (URP)

### Running the Game

1. Open the project in Unity
2. Open scene: `Assets/scene_00/scene_00_main.unity`
3. Press Play in Editor

### Controls

| Key | Action |
|-----|--------|
| **W/A/S/D** | Move character |
| **Space** or **LMB** | Attack |
| **Tab** | Open shop |
| **Esc** | Pause game |

## Project Structure

```
Assets/
├── scene_00/          → Game scenes
├── scripts_01/        → All C# scripts
├── prefabs_02/        → Reusable game objects
├── sprites_03/        → 2D graphics
├── animations_04/     → Animation clips
├── scriptableObjects_05/ → Scriptable data
├── audio_06/          → Sound files
├── materials_07/      → Materials & shaders
├── resources_08/      → Runtime resources
├── plugins_09/        → Third-party plugins
└── docs_10/           → Documentation
```

## Core Systems

### GameManager
- Singleton pattern for global access
- Handles pause/resume
- Manages game state

### InventorySystem
- Tracks resources: Wood, Stone, Food, Gold, Crops
- Notifies UI when resources change
- Validates transactions (buying, selling)

### TimeManager
- Day/night cycle
- Difficulty scaling over time
- Time-based events

### SpawnManager
- Enemy spawning with difficulty scaling
- Respects spawn area bounds
- Avoids spawning near player

### ShopSystem
- Buy farm animals (Cow, Chicken, Pig)
- Buy tools (Hoe, Axe, Pickaxe)
- Resource transactions

## Game Features

- ✅ Top-down movement and combat
- ✅ Enemy AI and spawning system
- ✅ Farm animal system
- ✅ Inventory and resource management
- ✅ Shop system
- ✅ Time/difficulty scaling
- ✅ Pause functionality

## Scripting Guide

### Adding a New Enemy

```csharp
public class MyEnemy : EnemyBase
{
    protected override void AttackBehavior()
    {
        // Implement custom attack
    }
}
```

### Adding Resources to Inventory

```csharp
InventorySystem inventory = GameManager.Instance.Inventory;
inventory.Add(ResourceType.Wood, 10);
```

### Listening to Inventory Changes

```csharp
InventorySystem.ResourceChanged += (type, amount) => {
    Debug.Log($"Resource {type} changed to {amount}");
};
```

## Performance Tips

1. **Optimize Sprite Atlasing**: Use sprite sheets instead of individual textures
2. **Use Object Pooling**: Reuse enemy instances instead of Instantiate/Destroy
3. **Batch UI Updates**: Avoid updating UI every frame
4. **Profile Regularly**: Use Unity Profiler to identify bottlenecks

## Expanding the Project

### Add Farming Mechanics
- Create soil tiles
- Implement crop growth system
- Add harvesting mechanic

### Add Building System
- Placement system with mouse
- Building cost validation
- Structure health

### Add More Enemies
- Create new EnemyBase subclasses
- Customize AttackBehavior()
- Add unique visual effects

### Add Progression
- Implement leveling system
- Skill trees
- Permanent upgrades

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Player not moving | Check PlayerController has Rigidbody2D |
| Enemies not spawning | Verify enemy prefab assigned in SpawnManager |
| UI not updating | Ensure InventorySystem.ResourceChanged event is linked |
| Script missing references | Use OnValidate() to auto-link components |

## Architecture Patterns

### Singleton Pattern
Used by GameManager, TimeManager for global access
```csharp
public static GameManager Instance { get; private set; }
```

### Event System
Used for loose coupling between systems
```csharp
public static event Action<ResourceType, int> ResourceChanged;
```

### Interface Pattern (IDamageable)
Allows any entity to implement damage logic
```csharp
public interface IDamageable { void TakeDamage(int amount, Vector2 knockback); }
```

### Component Pattern
Systems are MonoBehaviour components that can be added to GameObjects

## Code Quality Standards

- ✅ Sealed classes where inheritance isn't needed
- ✅ [DisallowMultipleComponent] on managers
- ✅ XML documentation comments
- ✅ Private fields with [SerializeField] for inspector access
- ✅ Clear naming conventions
- ✅ Proper error handling and validation

## Contributing

When adding features:
1. Follow existing code style
2. Add XML documentation
3. Test for compile errors
4. Update this README
5. Push to main branch

## License

This project is for educational purposes.

## Support

For issues or questions, refer to PROJECT_SETUP_GUIDE.md in Assets/docs_10/

---

**Version**: 1.0  
**Last Updated**: April 23, 2026  
**Engine**: Unity 2022.3 LTS  
**Status**: ✅ MVP Complete
