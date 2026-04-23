# Dark Farming Survival Game - Setup Guide

## PROJECT STATUS: ✅ READY FOR SCENE SETUP

All scripts are created, compiled, and error-free. Follow this guide to complete your Unity project setup.

---

## PART 1: FOLDER STRUCTURE ✅ COMPLETE

The following structure has been created:

```
Assets/
├── scene_00/                    # Main game scenes
├── scripts_01/                  # All game scripts
│   ├── core/                    # GameManager, TimeManager
│   ├── player/                  # Player controls and health
│   ├── enemies/                 # Enemy AI and behavior
│   ├── animals/                 # Farm animals
│   ├── systems/                 # Core systems (inventory, shop, spawn)
│   └── ui/                      # UI management
├── prefabs_02/                  # Reusable game objects
│   ├── player/
│   ├── enemies/
│   ├── animals/
│   ├── environment/
│   └── ui/
├── sprites_03/                  # 2D sprites
│   ├── player/
│   ├── enemies/
│   ├── animals/
│   ├── tiles/
│   └── ui/
├── animations_04/               # Animation clips
│   ├── player/
│   ├── enemies/
│   └── animals/
├── scriptableObjects_05/        # Scriptable object data
│   ├── animals/
│   ├── enemies/
│   └── items/
├── audio_06/                    # Sound files
│   ├── music/
│   └── sfx/
├── materials_07/                # Materials and shaders
├── resources_08/                # Runtime resources
├── plugins_09/                  # Third-party plugins
└── docs_10/                     # Documentation
```

---

## PART 2: CORE SCRIPTS ✅ CREATED

### Core Management Scripts
- **GameManager.cs**: Singleton that manages game state, pause/resume
- **TimeManager.cs**: Manages game time, day/night cycles, difficulty ramping

### Player Scripts
- **PlayerController.cs**: Handles movement (WASD) and input
- **PlayerCombat.cs**: Manages player attacks and hitbox detection
- **PlayerHealth.cs**: Implements IDamageable interface, tracks health

### Enemy Scripts
- **EnemyBase.cs**: Base class for all enemies
- **EnemyAI.cs**: AI behavior, pathfinding to player
- **PlantEnemy.cs**: Melee plant enemy
- **RangedPlant.cs**: Ranged plant enemy
- **Projectile.cs**: Enemy projectile behavior

### Animal Scripts
- **AnimalBase.cs**: Base class for farm animals
- **ResourceGenerator.cs**: Makes animals produce resources

### System Scripts
- **InventorySystem.cs**: Manages player resources (Wood, Stone, Food, Gold, Crops)
- **ShopSystem.cs**: Handles buying animals and tools
- **SpawnManager.cs**: Spawns enemies with difficulty scaling
- **IDamageable.cs**: Interface for taking damage

### UI Scripts
- **UIManager.cs**: Coordinates all UI elements
- **HealthBar.cs**: Display player health
- **ResourceUI.cs**: Display inventory resources

---

## PART 3: SETUP IN UNITY

### Step 1: Create Main Game Scene

1. **Create scene_00_main.unity**
   - Right-click in Assets/scene_00/ → Create → Scene
   - Name it: `scene_00_main`
   - Open the scene

2. **Setup Scene Hierarchy**
   ```
   Hierarchy:
   ├── Player (GameObject)
   │   ├── Sprite Renderer (with player sprite)
   │   ├── Collider 2D (Circle or Box)
   │   ├── Rigidbody 2D (Body Type: Dynamic, Gravity Scale: 0)
   │   ├── PlayerController (script)
   │   ├── PlayerCombat (script)
   │   ├── PlayerHealth (script)
   │   └── Hitbox (empty child)
   │       ├── Collider 2D (trigger, for attacks)
   │
   ├── Main Camera
   │   ├── Position: (0, 0, -10)
   │   ├── Orthographic Size: 5
   │
   ├── GameManager (empty GameObject)
   │   ├── GameManager (script)
   │   ├── TimeManager (script)
   │   ├── InventorySystem (script)
   │   ├── ShopSystem (script)
   │
   ├── SpawnManager (empty GameObject)
   │   ├── SpawnManager (script)
   │
   ├── Canvas (UI)
   │   ├── UIManager (script)
   │   ├── HealthBar (UI Image)
   │   ├── ResourceUI (UI Text/Image for resources)
   │   └── ShopPanel (UI Panel, initially inactive)
   │
   └── Enemies (empty parent for spawned enemies)
   ```

### Step 2: Setup Player GameObject

1. Create empty GameObject named "Player" at (0, 0, 0)
2. Add components:
   - **Sprite Renderer**: Add a player sprite (if available)
   - **Circle Collider 2D**: Radius 0.3
   - **Rigidbody 2D**: 
     - Body Type: Dynamic
     - Gravity Scale: 0
     - Collision Detection: Continuous
3. Add scripts:
   - PlayerController
   - PlayerCombat (with hitbox reference)
   - PlayerHealth (set Max Health: 10)

### Step 3: Setup GameManager

1. Create empty GameObject named "GameManager"
2. Add components:
   - GameManager
   - TimeManager
   - InventorySystem
   - ShopSystem
3. Set it as a child of GameManager in script (DontDestroyOnLoad)

### Step 4: Create Player Prefab

1. Drag Player GameObject from hierarchy to **Assets/prefabs_02/player/**
2. Name it: `Player`
3. Remove from scene (keep the prefab)

### Step 5: Create Enemy Prefab

1. Create empty GameObject named "PlantEnemy" at (5, 5, 0)
2. Add components:
   - Sprite Renderer (enemy sprite)
   - Circle Collider 2D (Radius 0.3)
   - Rigidbody 2D (Dynamic, Gravity Scale: 0)
   - EnemyBase
   - EnemyAI (Set Target to Player)
   - PlantEnemy
3. Drag to **Assets/prefabs_02/enemies/** → Name: `PlantEnemy`

### Step 6: Configure SpawnManager

1. Select SpawnManager GameObject
2. Set properties:
   - Player: Drag Player from hierarchy
   - Time Manager: Drag GameManager
   - Enemy Prefabs: Add PlantEnemy prefab
   - Max Alive: 10
   - Spawn Area Size: (30, 30)

### Step 7: Configure ShopSystem

1. Select GameManager
2. In ShopSystem component:
   - Set Inventory reference
   - Set Cow/Chicken/Pig prefabs (create them similar to PlantEnemy)

### Step 8: Setup Canvas & UI

1. Create Canvas (Right-click → UI → Canvas)
2. Add Panel for HealthBar
3. Add Text for Resources
4. Create Panel for Shop (initially inactive)
5. Add UIManager script to Canvas
6. Link all UI elements in inspector

---

## PART 4: CREATE ADDITIONAL SCENES

### Menu Scene (scene_01_menu.unity)
```
Canvas
├── Title Text
├── Start Button (calls LoadScene("scene_00_main"))
└── Quit Button (calls Application.Quit())
```

### Test Scene (scene_02_test.unity)
```
- Simple test environment
- Good for mechanics testing before main gameplay
```

---

## PART 5: RESOURCE SETUP

### Starting Resources (in InventorySystem)
- Wood: 100
- Stone: 50
- Food: 30
- Gold: 0
- Crops: 0

### Shop Prices
- Cow: 50 Gold
- Chicken: 30 Gold
- Pig: 40 Gold
- Hoe: 100 Gold
- Axe: 75 Gold
- Pickaxe: 80 Gold

---

## PART 6: KEY CONTROLS

| Key | Action |
|-----|--------|
| W/A/S/D | Move |
| Space / LMB | Attack |
| Tab | Open/Close Shop |
| Esc | Pause Game |

---

## PART 7: TROUBLESHOOTING

### Missing References
- Ensure all GameObject links in scripts are set in Inspector
- Use `Reset()` methods or `OnValidate()` to auto-link common components

### Scripts Not Working
- Check Console for errors (Ctrl+Shift+C)
- Verify all scripts are in correct folders
- Ensure Rigidbody2D is set to Dynamic (not Static)

### Performance Issues
- Check SpawnManager maxAlive limit
- Reduce physics quality in Project Settings
- Use object pooling for frequently spawned objects

---

## PART 8: NEXT STEPS TO EXPAND

1. **Add Combat System**
   - Implement damage types and resistances
   - Add particle effects for attacks

2. **Add Farming Mechanics**
   - Tilling soil (place with mouse)
   - Crop growth system
   - Harvesting crops

3. **Add Building System**
   - Place buildings with mouse
   - Building cost and requirements

4. **Add Audio**
   - Background music
   - SFX for actions (attack, harvest, etc.)

5. **Add Animations**
   - Character sprite animation controller
   - Enemy attack animations

6. **Add Difficulty Modes**
   - Easy, Normal, Hard
   - Scale TimeManager.difficultyRampPerMinute

---

## Quick Reference: Script Dependencies

```
GameManager
├── TimeManager
├── InventorySystem
└── ShopSystem
    └── Animal Prefabs

PlayerController
├── PlayerCombat
├── PlayerHealth
└── Animator

SpawnManager
├── Player (Transform)
├── TimeManager
└── Enemy Prefabs[]

InventorySystem
└── ResourceType enum

UIManager
├── HealthBar
├── ResourceUI
└── ShopSystem
```

---

## Build Settings

Before building, ensure:
1. Scenes are added to Build Settings (File → Build Settings)
2. Scene 0: scene_00_main.unity (Main gameplay)
3. Player settings configured (Resolution, company name, etc.)
4. All resources imported and not missing

---

**Created**: April 23, 2026
**Game**: Dark Farming Survival Game
**Framework**: Unity 2D
**Status**: ✅ Ready to Play!
