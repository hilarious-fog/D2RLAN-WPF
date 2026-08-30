# UserSettings.cs Removal Checklist

## Properties to REMOVE from UserSettings.cs

### Auto-Backup System (eBackup enum)
- [ ] `public int AutoBackups { get; set; }` - eBackup enum
- [ ] Enum file: `Models/Enums/eBackup.cs` - DELETE
- Remove from DefaultUserSettings.json: `"AutoBackups": {...}`

### Item Display (eItemDisplay enum)
- [ ] `public int ItemIcons { get; set; }` - eItemDisplay enum
- [ ] Enum file: `Models/Enums/eItemDisplay.cs` - DELETE
- Remove from DefaultUserSettings.json: `"ItemIcons": ...`

### Skill Icon Packs (eSkillIconPack enum)
- [ ] `public int SkillIcons { get; set; }` - eSkillIconPack enum
- [ ] Enum file: `Models/Enums/eSkillIconPack.cs` - DELETE
- Remove from DefaultUserSettings.json: `"SkillIcons": ...`

### Buff Icons (eEnabledDisabled enum usage)
- [ ] `public int BuffIcons { get; set; }` - eEnabledDisabled enum
- [ ] Remove from DefaultUserSettings.json: `"BuffIcons": ...`

### Monster Display (eMonsterHP enum)
- [ ] `public int MonsterHP { get; set; }` - eMonsterHP enum
- [ ] Enum file: `Models/Enums/eMonsterHP.cs` - DELETE
- Remove from DefaultUserSettings.json: `"MonsterHP": ...`

### Cinematic Subtitles (eCinematicSubs enum)
- [ ] `public int CinematicSubs { get; set; }` - eCinematicSubs enum
- [ ] `public int TextLanguage { get; set; }`
- [ ] Enum file: `Models/Enums/eCinematicSubs.cs` - DELETE
- Remove from DefaultUserSettings.json: `"CinematicSubs": ...`, `"TextLanguage": ...`

### Runeword Sorting (eRunewordSorting enum)
- [ ] `public int RunewordSorting { get; set; }` - eRunewordSorting enum
- [ ] Enum file: `Models/Enums/eRunewordSorting.cs` - DELETE
- Remove from DefaultUserSettings.json: `"RunewordSorting": ...`

### String Coloring (eStringColoring enum)
- [ ] `public int StringColoring { get; set; }` - eStringColoring enum
- [ ] Enum file: `Models/Enums/eStringColoring.cs` - DELETE
- Remove from DefaultUserSettings.json: `"StringColoring": ...`

### HUD Design (eHudDesign enum)
- [ ] `public int HudDesign { get; set; }` - eHudDesign enum
- [ ] Enum file: `Models/Enums/eHudDesign.cs` - DELETE
- Remove from DefaultUserSettings.json: `"HudDesign": ...`

### UI Theme (eUITheme enum or similar)
- [ ] `public int UiTheme { get; set; }`
- Remove from DefaultUserSettings.json: `"UiTheme": ...`

### Color Dyes (eEnabledDisabled enum usage)
- [ ] `public int ColorDye { get; set; }` - eEnabledDisabled enum
- Remove from DefaultUserSettings.json: `"ColorDye": ...`

### Mercenary Icons (eMercIdentifier enum)
- [ ] `public int MercIcons { get; set; }` - eMercIdentifier enum
- [ ] Enum file: `Models/Enums/eMercIdentifier.cs` - DELETE
- Remove from DefaultUserSettings.json: `"MercIcons": ...`

### Item Level Display (eEnabledDisabled enum usage)
- [ ] `public int ItemIlvls { get; set; }` - eEnabledDisabled enum
- Remove from DefaultUserSettings.json: `"ItemIlvls": ...`

### Helmet Display (eEnabledDisabled enum usage)
- [ ] `public int HideHelmets { get; set; }` - eEnabledDisabled enum
- Remove from DefaultUserSettings.json: `"HideHelmets": ...`

### Rune Display (eEnabledDisabled enum usage)
- [ ] `public int RuneDisplay { get; set; }` - eEnabledDisabled enum
- Remove from DefaultUserSettings.json: `"RuneDisplay": ...`

### Super Telekinesis (eEnabledDisabled enum usage)
- [ ] `public int SuperTelekinesis { get; set; }` - eEnabledDisabled enum
- Remove from DefaultUserSettings.json: `"SuperTelekinesis": ...`

### Expanded Storage (multiple)
- [ ] `public bool ExpandedInventory { get; set; }`
- [ ] `public bool ExpandedStash { get; set; }`
- [ ] `public bool ExpandedCube { get; set; }`
- [ ] `public bool ExpandedMerc { get; set; }`
- Remove from DefaultUserSettings.json: All of above

### Font Selection
- [ ] `public int Font { get; set; }`
- Remove from DefaultUserSettings.json: `"Font": ...`

### Buff Icon Template
- [ ] `public int BuffIconTemplate { get; set; }`
- Remove from DefaultUserSettings.json: `"BuffIconTemplate": ...`

### Monster Item Drops (eMonsterItemDrops enum)
- [ ] `public int MonsterItemDrops { get; set; }`
- [ ] Enum file: `Models/Enums/eMonsterItemDrops.cs` - DELETE (if not used elsewhere)
- Remove from DefaultUserSettings.json: `"MonsterItemDrops": ...`

### Beacon Startup (eBeaconStartup enum)
- [ ] `public int BeaconStartup { get; set; }` - eBeaconStartup enum
- [ ] Enum file: `Models/Enums/eBeaconStartup.cs` - DELETE
- Remove from DefaultUserSettings.json: `"BeaconStartup": ...`

### Loot Filter (eLootFilter enum)
- [ ] `public int LootFilter { get; set; }` - eLootFilter enum
- [ ] Enum file: `Models/Enums/eLootFilter.cs` - DELETE
- Remove from DefaultUserSettings.json: `"LootFilter": ...`

### Shortened Levels (eEnabledDisabled enum usage)
- [ ] `public int ShortenedLevels { get; set; }` - eEnabledDisabled enum
- Remove from DefaultUserSettings.json: `"ShortenedLevels": ...`

---

## Properties to KEEP in UserSettings.cs

### Essential Core
- [ ] `public Dictionary<string, DifficultyCustomizations> DifficultyCustomizations { get; set; }`
- [ ] `public int SelectedGroupSize { get; set; }` // Player count (1-8)
- [ ] `public bool LANOffline { get; set; }`

### Debug/Advanced
- [ ] `public bool D2RDebugMode { get; set; }`
- [ ] `public bool ForceHUDDebug { get; set; }`
- [ ] `public bool D2RDebugModeNoErrors { get; set; }`
- [ ] `public int CloseMinimized { get; set; }`

---

## Enum Files to DELETE

```
Models/Enums/eBackup.cs
Models/Enums/eBeaconStartup.cs
Models/Enums/eBuffIcons.cs
Models/Enums/eChampionPacks.cs
Models/Enums/eCinematicSubs.cs
Models/Enums/eHudDesign.cs
Models/Enums/eItemDisplay.cs
Models/Enums/eLootFilter.cs
Models/Enums/eMercIdentifier.cs
Models/Enums/eMonsterHP.cs
Models/Enums/eMonsterItemDrops.cs
Models/Enums/eRunewordSorting.cs
Models/Enums/eSkillIconPack.cs
Models/Enums/eStringColoring.cs
Models/Enums/eUITheme.cs (if it exists)

KEEP:
Models/Enums/eEnabledDisabled.cs
Models/Enums/eChampionPacks.cs (may use with DifficultyCustomizations)
```

---

## DefaultUserSettings.json Cleanup

Remove these JSON entries:
```json
{
  "AutoBackups": {},
  "BeaconStartup": 0,
  "BuffIcons": 0,
  "BuffIconTemplate": 0,
  "CinematicSubs": 0,
  "ColorDye": 0,
  "Font": 0,
  "HideHelmets": 0,
  "HudDesign": 0,
  "ItemIlvls": 0,
  "ItemIcons": 0,
  "LootFilter": 0,
  "MercIcons": 0,
  "MonsterHP": 0,
  "MonsterItemDrops": 0,
  "RunewordSorting": 0,
  "RuneDisplay": 0,
  "ShortenedLevels": 0,
  "SkillIcons": 0,
  "StringColoring": 0,
  "SuperTelekinesis": 0,
  "TextLanguage": 0,
  "UiTheme": 0,
  "ExpandedInventory": false,
  "ExpandedStash": false,
  "ExpandedCube": false,
  "ExpandedMerc": false
}
```

Keep only:
```json
{
  "DifficultyCustomizations": {
    "Normal": {
      "SelectedChampionPack": 0,
      "SelectedExpRate": 0,
      "SelectedShortenedLevel": 0,
      "ActOneDensity": 1,
      "ActTwoDensity": 1,
      "ActThreeDensity": 1,
      "ActFourDensity": 1,
      "ActFiveDensity": 1,
      "ActOneSpawnChance": 0.0,
      "ActTwoSpawnChance": 0.0,
      "ActThreeSpawnChance": 0.0,
      "ActFourSpawnChance": 0.0,
      "ActFiveSpawnChance": 0.0,
      "ActOneMultiplier": 1.0,
      "ActTwoMultiplier": 1.0,
      "ActThreeMultiplier": 1.0,
      "ActFourMultiplier": 1.0,
      "ActFiveMultiplier": 1.0
    },
    "Nightmare": { ... },
    "Hell": { ... }
  },
  "SelectedGroupSize": 1,
  "LANOffline": false,
  "D2RDebugMode": false,
  "ForceHUDDebug": false,
  "D2RDebugModeNoErrors": false,
  "CloseMinimized": 0
}
```

---

## Verification Steps

1. **Compile Check**: Code should compile without errors
2. **Search & Replace**: 
   - Search for removed property names in entire codebase
   - Should find 0 results in code (only in comments)
3. **Settings Load/Save**: 
   - Load existing UserSettings JSON (old format)
   - Should only consume recognized keys
   - Should save minimal JSON on close
4. **Test Launch**: Game should launch normally with minimal settings

---

## Summary

- **Properties to Remove:** ~28 properties
- **Enum Files to Delete:** ~14 files
- **JSON Entries to Remove:** ~20 entries
- **Expected Result:** UserSettings.cs shrinks from ~400 lines to ~150 lines
