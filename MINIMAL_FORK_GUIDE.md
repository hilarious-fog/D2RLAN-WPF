# D2RLAN Minimal Fork Implementation Guide

## Project Scope: Ultra-Minimal Launcher

**Core Feature Set:**
- **TCP/IP Enablement** (1 byte memory patch at 0x749AC: 0x74 → 0xEB)
- **Player Count Scaling** via Difficulty Customizations (3 memory patches + UI controls)
- **Game Launch** with D2RHUD.dll injection and HUD configuration
- **Essential UI** for mod selection and difficulty customization

**Total Footprint:** ~15-20% of current codebase

---

## Architecture Overview

### DLL Injection Flow
```
User launches game
  ↓
ShellViewModel.ApplyUserDefinedQoLOptions()
  ↓
Injector.cs injects D2RHUD.dll into D2R.exe process
  ↓
HUDConfig_.json loaded (memory patches applied)
  ↓
Game launches with patches active
```

### Configuration System
- **HUDConfig_Template.json**: Only TCP/IP + Player Count Scaling patches (remove ~200 other patches)
- **UserSettings.cs**: Only essential toggles (remove ~40 properties)
- **DifficultyCustomizations**: Dictionary<string, DifficultyCustomizations> for Normal/Nightmare/Hell

### Memory Patches (Keep Only)
1. **TCP/IP Access** (0x749AC, 1 byte: 0x74→0xEB)
2. **Player Difficulty Scaling (Max/UI)** (addresses for UI/client checks)
3. **Player Difficulty Scaling (Command)** (player command address)
4. **Player Difficulty Scaling (Actual)** (server value address)

---

## Implementation Steps

### Step 1: Clean Up UserSettings.cs
**Location:** `src/D2RLAN/D2RLAN/Models/UserSettings.cs`

**Remove these properties:**
- All `eBackup` related (AutoBackups)
- All `eItemDisplay` related (ItemIcons)
- All `eSkillIconPack` related (SkillIcons)
- `BuffIcons`, `SkillBuffIconsEnabled`
- `ShowItemLevelsEnabled`, `ItemIlvls`
- `MonsterHP`, `MonsterStatsDisplay`
- `RunewordSorting`, `StringColoring`
- `HudDesign`, `UiTheme`, `ColorDye`
- `CinematicSubs`, `TextLanguage`
- `BeaconStartup`, `LootFilter`
- `ShortenedLevels`, `SuperTelekinesis`
- `MercIcons`, `SkillIconPack`
- `Font`, `BuffIconTemplate`
- `MonsterItemDrops` (if relevant)

**Keep only:**
```csharp
public int SelectedGroupSize { get; set; }  // Player count
public Dictionary<string, DifficultyCustomizations> DifficultyCustomizations { get; set; }
public bool LANOffline { get; set; }
public bool D2RDebugMode { get; set; }
public bool ForceHUDDebug { get; set; }
public bool D2RDebugModeNoErrors { get; set; }
public int CloseMinimized { get; set; }
```

---

### Step 2: Simplify HUDConfig_Template.json
**Location:** `src/D2RLAN/D2RLAN/HUDConfig_Template.json`

**Prune to only:**
```json
{
  "Excluded Grail Items": [...],
  "MemoryConfigs": [
    {
      "Name": "Enable TCP/IP Access",
      "Description": "...",
      "Category": "Important",
      "Address": "749AC",
      "Length": 1,
      "Type": "Hex",
      "UserType": "Boolean",
      "Values": "EB",
      "OriginalValues": "74",
      "ModdedValues": "EB"
    },
    {
      "Name": "Allow Character Levels 100+",
      "Address": "27B835",
      ...
    },
    {
      "Name": "Display Player Names When Trading",
      "Address": "BBFCF9",
      ...
    },
    {
      "Name": "Player Difficulty Scaling Override (Max/UI)",
      "Addresses": ["135EF8", "135F16", "1E31CCC", "1E31D64"],
      ...
    },
    {
      "Name": "Player Difficulty Scaling Override (Command)",
      "Address": "11FE12",
      ...
    },
    {
      "Name": "Player Difficulty Scaling Override (Actual)",
      "Address": "136910",
      ...
    }
  ]
}
```
**Remove:** All other entries (Color Dyes, Stash Gold, Character Gold, Monster Stats, Runewords, etc.)

---

### Step 3: Simplify ShellViewModel.cs
**Location:** `src/D2RLAN/D2RLAN/ViewModels/ShellViewModel.cs`

**Remove these methods entirely:**
- `ConfigureColorDyes()` / `DyesISC()` / `DyesProp()` / `DyesState()` / `DyesCube()`
- `ConfigureSuperTelekinesis()` / `CreateSuperTKSkill()` / `RemoveSuperTkSkill()`
- `ConfigureItemIcons()` / `ItemIconsShow()` / `ItemIconsHide()` / `RuneIconsShow()` / `RuneIconsHide()`
- `ConfigureBuffIcons()`
- `ConfigureCinematicSubs()` / `ConvertSDHToStandard()` / `RenumberIds()` / `NormalizeBlankLines()`
- `ConfigureRunewordSorting()`
- `ConfigureStringColoring()`
- `ConfigureMonsterStatsDisplay()`
- `ConfigureHideHelmets()`
- `ConfigureRuneDisplay()`
- `ConfigureMercIcons()`
- `ConfigureSkillIcons()`
- `ConfigureItemILvls()`
- `ConfigureHudDesign()` / `ReplaceStringsInFile()`
- `StartAutoBackup()` / `BackupRecentCharacter()` / related backup methods
- `SearchItemID()` / `SearchStateID()` / `RemoveColorDyes()`

**Simplify `OnApplyUserDefinedQoLOptions()`:**
```csharp
private async Task OnApplyUserDefinedQoLOptions()
{
    // Only load essential configs
    await LoadHudConfigJson();
    await ApplyTCPPatch();
    await CheckStashSearchFiles();
}
```

**Simplify `OnLoaded()`:**
- Remove all event checker code for special events
- Remove launcher update check (optional)
- Keep only basic initialization

---

### Step 4: Remove/Simplify ViewModels and Views

**Remove entirely:**
- `CASCExtractorViewModel.cs` + `CASCExtractorView.xaml`
- `RestoreBackupViewModel.cs` + `RestoreBackupView.xaml`
- `StashTabSettingsViewModel.cs` + `StashTabSettingsView.xaml`
- `LootFilterViewModel.cs` + `LootFilterView.xaml`
- `SpecialEventsViewModel.cs` + `SpecialEventsView.xaml`
- `ChatSettingsViewModel.cs` (if exists)
- `HotkeysViewModel.cs` (if exists)

**Simplify:**
- **CustomizationsDrawerViewModel.cs**: Keep ONLY difficulty customization logic
  - Remove: Color dyes, item icons, skill icons, etc.
  - Keep: `DifficultyCustomizations` property, Act multiplier/density/champion logic
  - Remove: 90% of Configure* methods

- **HomeDrawerViewModel.cs**: Minimal, only mod selection + launch
  - Remove: All backup restoration, stash initialization
  - Keep: Basic mod selection UI

---

### Step 5: Update App.xaml Navigation

**Location:** `src/D2RLAN/D2RLAN/App.xaml` / `ShellView.xaml`

**Simplify side menu to:**
```xml
<MenuItem Tag="HOME" Header="Home" />
<MenuItem Tag="CUSTOMIZATIONS" Header="Difficulty Settings" />
<MenuItem Tag="LAUNCH" Header="Launch Game" />
<MenuItem Tag="ERROR LOGS" Header="Error Logs" />
```

**Remove:** All other menu items (Beacon, Discord, Wiki, etc.)

---

### Step 6: Trim Resource Files

**Keep in Resources/:**
- `DefaultUserSettings.json` (updated, minimal)
- `HUDConfig_Template.json` (pruned)
- `appSettings.EXAMPLE.json`
- `SharedStash*.d2i` files (if used)
- `BuffIcons/` (remove if not used)
- `Icons/` (minimal, only essentials)
- `Images/` (logo only)
- `Fonts/` (can remove if not displaying custom icons)

**Remove entirely:**
- `Resources/Options/` (all feature-specific configs)
- `Resources/CASC/` (unnecessary game data)
- `Resources/Preview/` (preview images for features)
- Any mod-specific data not used

---

## Testing Checklist

- [ ] **TCP/IP**: Verify single byte patch at 0x749AC is applied
- [ ] **Player Scaling**: Test player-count scaling with 1, 2, 4, 8 player settings
- [ ] **Static Maps**: Test each retained map preset adds the expected `-seed` launch argument
- [ ] **Battle.net Blocking**: Verify launch still sets Battle.net connection strings to `127.0.0.1`
- [ ] **Game Launch**: DLL injection successful, game starts normally
- [ ] **Settings Persistence**: UserSettings saves/loads correctly
- [ ] **No Save Corruption**: Play a character with minimal settings, save/load works
- [ ] **Clean Shutdown**: No crashes on game exit
- [ ] **No Console Errors**: Check Application Output for missing resources

---

## Reign of the Warlock Compatibility Goal

Reign of the Warlock adds an official Warlock class and DLC-era stash/character changes. The minimal fork should therefore treat DLC-era mods as authoritative for their own game data and layout files.

Compatibility rules now enforced in code:

- Do not apply old QoL TXT mutations such as Color Dyes or Super Telekinesis.
- Do not apply the old implicit skill-index/controller hotfix; Warlock-aware tooling uses a new skill offset, and the minimal fork does not need this patch path.
- Do not seed legacy stash-search layout JSON; Reign of the Warlock and modern mods keep their own stash layouts.
- Apply only the minimal memory-edit allowlist: TCP/IP plus player difficulty scaling.
- Prune stale `HUDConfig_*.json` memory edits at launch so older local configs cannot re-enable removed patches.
- Read and verify process bytes before writing. Entries without known `OriginalValues` are skipped unless the process already contains the target bytes.
- Keep Battle.net registry blocking in the launch path because it supports the TCP/IP-with-friends goal.

---

## Expected File Size Reduction

| Component | Original | Minimal | Reduction |
|-----------|----------|---------|-----------|
| UserSettings.cs | ~400 lines | ~150 lines | 62% |
| ShellViewModel.cs | ~6000 lines | ~2000 lines | 67% |
| HUDConfig_Template.json | ~5000 lines | ~400 lines | 92% |
| Resources/ | 50+ files | ~15 files | 70% |
| **Total Codebase** | 100% | **~20%** | **80%** |

---

## Deployment Recommendations

1. **Create new git branch:** `minimal-fork`
2. **Make cuts incrementally**, test after each step
3. **Commit regularly** for easy rollback
4. **Document removed dependencies** (in case future features needed)
5. **Test with 2-3 characters** before release
6. **Publish to GitHub** with clear README explaining purpose + limitations

---

## Future Enhancement Opportunities

If users request features, they're easy to add back:
- **Expanded Storage**: Re-add via D2RHUD.dll DLL patches only (memory-safe if DLL maintained)
- **Monster Scaling**: Expand DifficultyCustomizations system with additional fields
- **Map Seed Reroll**: Add single memory patch if needed
- **LAN Mode Toggle**: Simple UserSettings boolean + memory patch

The minimal fork provides a **rock-solid foundation** with zero save corruption risk.
