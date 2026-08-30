# ShellViewModel.cs Simplification Guide

## Overview
**Current file:** ~6000+ lines  
**Target:** ~2000 lines (67% reduction)  
**Primary cuts:** Remove all feature configuration methods

---

## Methods to DELETE (90% of file)

### Color Dyes System (DELETE ALL)
Search and delete these method definitions:
```csharp
private async Task ConfigureColorDyes()
private async Task DyesISC()
private async Task DyesProp()
private async Task DyesState()
private async Task DyesCube()
private int SearchItemID(string filePath, string searchTerm)
private int SearchStateID(string filePath, string searchTerm)
private void RemoveColorDyes(string filePath, string searchString, int rowsToDelete)
```
**Lines affected:** ~1500 lines

### Super Telekinesis System (DELETE ALL)
```csharp
private async Task ConfigureSuperTelekinesis()
private async Task CreateSuperTKSkill()
private void RemoveSuperTkSkill()
```
**Lines affected:** ~200 lines

### Item Display System (DELETE ALL)
```csharp
private async Task ConfigureItemIcons()
private void ItemIconsShow(string itemNameOriginalJsonFilePath)
private void ItemIconsHide(string itemNameOriginalJsonFilePath, string itemNameJsonFilePath)
private void RuneIconsShow(string itemRuneJsonFilePath)
private void RuneIconsHide(string itemRuneJsonFilePath)
```
**Lines affected:** ~400 lines

### Cinematic Subtitles System (DELETE ALL)
```csharp
private async Task ConfigureCinematicSubs()
private void ConvertSDHToStandard(string folderPath)
private string RenumberIds(string content)
private string NormalizeBlankLines(string content)
public class SubtitleExtractor
```
**Lines affected:** ~300 lines

### Auto-Backup System (DELETE ALL)
```csharp
public async Task StartAutoBackup()
public async Task<(string characterName, bool passed)> BackupRecentCharacter()
private string ComputeMD5(string filePath)
private void BackupStashFile(string savePath, string backupFolder, string stashFileName)
```
**Lines affected:** ~200 lines

### UI/Visual Features (DELETE ALL)
```csharp
private async Task ConfigureHudDesign()
private async Task ConfigureBuffIcons()
private async Task ConfigureMercIcons()
private async Task ConfigureSkillIcons()
static void ReplaceStringsInFile(string filePath, string[] searchStrings, string replacementString)
```
**Lines affected:** ~500 lines

### Monster & Item Features (DELETE ALL)
```csharp
private async Task ConfigureMonsterStatsDisplay()
private async Task ConfigureHideHelmets()
private async Task ConfigureRuneDisplay()
private async Task ConfigureItemILvls()
private async Task ConfigureRunewordSorting()
private async Task ConfigureStringColoring()
```
**Lines affected:** ~800 lines

---

## Method to RADICALLY SIMPLIFY

### OnApplyUserDefinedQoLOptions() Method

**Current implementation (~300 lines):**
- Calls 20+ configuration methods
- Manages many feature toggles
- Complex conditional logic

**New implementation (~20 lines):**
```csharp
private async Task OnApplyUserDefinedQoLOptions()
{
    try
    {
        _logger.Info("Applying QoL options...");
        
        // Load HUD configuration JSON (includes TCP/IP and player scaling patches)
        await LoadHudConfigJson();
        
        // Verify TCP patch was applied (optional, for debugging)
        _logger.Info("QoL options applied successfully");
    }
    catch (Exception ex)
    {
        _logger.Error($"Error applying QoL options: {ex.Message}");
    }
}
```

### OnLoaded() Method

**Current implementation (~150 lines):**
- Launcher update checks
- Special events checker
- Complex initialization

**New implementation (~50 lines):**
```csharp
[UsedImplicitly]
public async Task OnLoaded(object args)
{
    eLanguage appLanguage = ((eLanguage)Settings.Default.AppLanguage);
    CultureInfo culture = new CultureInfo(appLanguage.GetAttributeOfType<DisplayAttribute>().Name.Split(' ')[1].Trim(new[] { '(', ')' }));
    CultureResources.ChangeCulture(culture);

    GamePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName + @"\D2R\";

    if (!Directory.Exists(GamePath))
        Directory.CreateDirectory(GamePath);

    Settings.Default.InstallPath = GamePath;
    DiabloInstallDetected = true;

    HomeDrawerViewModel vm = new HomeDrawerViewModel(this, _windowManager);
    await vm.Initialize();
    UserControl = new HomeDrawerView() { DataContext = vm };
    await SaveUserSettings();
}
```

---

## OnItemClicked() Method - Remove Menu Items

**Current:** Handles 20+ menu items  
**New:** Handle only essential items

```csharp
[UsedImplicitly]
public async void OnItemClicked(NavigationItemClickedEventArgs args)
{
    switch (((string)args.Item.Tag).ToUpperInvariant())
    {
        case "HOME":
            {
                HomeDrawerViewModel vm = new HomeDrawerViewModel(this, _windowManager);
                await vm.Initialize();
                UserControl = new HomeDrawerView() { DataContext = vm };
                break;
            }
        case "CUSTOMIZATIONS":
            {
                CustomizationsDrawerViewModel vm = new CustomizationsDrawerViewModel(this);
                UserControl = new CustomizationsDrawerView() { DataContext = vm };
                break;
            }
        case "SAVE FILES":
            {
                if (ModInfo == null || UserSettings == null)
                    break;

                string modPath = ModInfo.SavePath.Contains("\"../\"")
                    ? BaseSaveFilesFilePath
                    : SaveFilesFilePath;

                if (Directory.Exists(modPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = modPath,
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                }
                break;
            }
        case "ERROR LOGS":
            {
                if (ModInfo == null || UserSettings == null)
                    break;

                string folderPath = "Error Logs";
                if (Directory.Exists(folderPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = folderPath,
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                }
                break;
            }
    }
}
```

**Remove cases:**
- LOOT FILTER
- MEMORY EDITS
- HOTKEYS
- CHAT
- RENAME CHARACTER
- COMMUNITY DISCORD
- WIKI
- COMMUNITY PATREON
- MOD FILES
- LAUNCH FILES
- BEACON
- PATREON
- All D2R website/Discord/YouTube links

---

## CopyAllFiles() Method

**Status:** SAFE TO KEEP  
**Used by:** Mod folder initialization (if used)  
**Decision:** Keep unless you never copy entire directories

---

## Properties Section - KEEP ALL

Don't modify the `#region ---Properties---` section.  
All property getters/setters are essential for MVVM binding.

---

## Fields to REMOVE from top of class

Search for these private field declarations and delete:

```csharp
private DispatcherTimer _autoBackupDispatcherTimer;
// Any other field related to removed features
```

**Most other fields are safe to keep** - they relate to core UI state.

---

## Region Organization After Cleanup

Your regions should look like:
```csharp
#region ---Methods---
    // OnLoaded()
    // OnItemClicked()
    // OnApplyUserDefinedQoLOptions()
    // LaunchGame()
    // SaveUserSettings()
    // LoadHudConfigJson()
    // ApplyTCPPatch()
    // GetSavePath()
    // OnForceHUDDebug()
    // OnD2RDebugMode()
    // OnD2RDebugModeNoErrors()

#region ---Properties---
    // All property definitions

#endregion
```

---

## Helper Methods - Keep or Delete?

**Keep these:**
- `CopyAllFiles()` - Needed for mod installation
- `GetSavePath()` - Path utility
- `LaunchGame()` - Game launch logic
- `SaveUserSettings()` - Persistence
- `LoadHudConfigJson()` - Config loading
- `ApplyTCPPatch()` - TCP patch application

**Delete:**
- `MergeHudTemplateMemoryConfigs()` - Only used by removed features (probably)
- Any helper methods only called by deleted methods

---

## Compile & Test Checkpoints

### After deleting 500 lines (Color Dyes)
- [ ] Code compiles without errors
- [ ] No references to deleted methods elsewhere
- [ ] UserSettings doesn't reference ColorDye property

### After deleting 1000 lines (more features)
- [ ] Still compiles
- [ ] ShellViewModel still functional
- [ ] Game still launches

### After simplifying OnApplyUserDefinedQoLOptions
- [ ] Compiles
- [ ] TCP patch still applied
- [ ] Game launches normally

### Final integration test
- [ ] Code compiles cleanly
- [ ] Zero warnings about unused code
- [ ] App launches without errors
- [ ] Game launches and plays normally
- [ ] Settings save/load correctly

---

## Search & Replace Commands (for IDE)

Use your IDE's find & replace to verify all deletions:

**Search for these and verify 0 results:**
```
ConfigureColorDyes
DyesISC
DyesProp
DyesState
DyesCube
SearchItemID
SearchStateID
RemoveColorDyes
ConfigureSuperTelekinesis
CreateSuperTKSkill
RemoveSuperTkSkill
ConfigureItemIcons
ItemIconsShow
ItemIconsHide
RuneIconsShow
RuneIconsHide
ConfigureCinematicSubs
ConvertSDHToStandard
RenumberIds
NormalizeBlankLines
SubtitleExtractor
StartAutoBackup
BackupRecentCharacter
ComputeMD5
BackupStashFile
ConfigureHudDesign
ConfigureBuffIcons
ConfigureMercIcons
ConfigureSkillIcons
ReplaceStringsInFile
ConfigureMonsterStatsDisplay
ConfigureHideHelmets
ConfigureRuneDisplay
ConfigureItemILvls
ConfigureRunewordSorting
ConfigureStringColoring
```

---

## Summary

| Section | Original Lines | New Lines | Reduction |
|---------|---|---|---|
| Color Dyes methods | 1500 | 0 | 100% |
| Auto-Backup methods | 200 | 0 | 100% |
| Telekinesis methods | 200 | 0 | 100% |
| Item Display methods | 400 | 0 | 100% |
| Subtitles methods | 300 | 0 | 100% |
| Visual features methods | 500 | 0 | 100% |
| Monster/Item features | 800 | 0 | 100% |
| OnApplyUserDefinedQoLOptions | 300 | 20 | 93% |
| OnLoaded | 150 | 50 | 67% |
| OnItemClicked | 200 | 50 | 75% |
| Remaining methods | 450 | 450 | 0% |
| Properties section | 1200 | 1200 | 0% |
| **TOTAL** | ~6000 | ~2000 | **67%** |

**Result: ShellViewModel shrinks from 6000+ lines to approximately 2000 lines**
