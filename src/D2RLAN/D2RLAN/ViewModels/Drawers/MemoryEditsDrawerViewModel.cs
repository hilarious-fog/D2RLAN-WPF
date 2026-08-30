using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using D2RLAN.ViewModels.Dialogs;
using D2RLAN.Views.Dialogs;
using JetBrains.Annotations;
using static D2RLAN.ViewModels.ShellViewModel;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace D2RLAN.ViewModels.Drawers;

public class MemoryEditsDrawerViewModel : INotifyPropertyChanged
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(MemoryEditsDrawerViewModel));
    private static readonly JsonSerializerOptions MemoryConfigJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private string _statusMessage = "Loading memory edits...";
    private string _debugStatus = string.Empty;
    private bool _showDebugStatus;
    private bool _isLoading;
    private string _resolvedTemplatePath = string.Empty;
    private readonly Dictionary<string, bool> _categoryExpandedState = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _activityExpandedState = new(StringComparer.OrdinalIgnoreCase);
    private string _resolvedConfigPath = string.Empty;
    private bool _isInitialized;
    private bool _skipTemplateMergeOnNextLoad;
    private int _selectedTabIndex;

    public ShellViewModel ShellViewModel { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex == value)
                return;
            _selectedTabIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentCategoryGroups));
            OnPropertyChanged(nameof(LoadedEditCount));
            OnPropertyChanged(nameof(IsAppliedTabSelected));
            OnPropertyChanged(nameof(IsOverrideTabSelected));
        }
    }

    public bool IsAppliedTabSelected => SelectedTabIndex == 0;
    public bool IsOverrideTabSelected => SelectedTabIndex == 1;

    public ObservableCollection<MemoryEditCategoryGroupViewModel> CurrentCategoryGroups =>
        IsOverrideTabSelected ? OverrideCategoryGroups : CategoryGroups;

    public ObservableCollection<MemoryEditItemViewModel> MemoryEdits
    {
        get => _memoryEdits;
        private set
        {
            _memoryEdits = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LoadedEditCount));
        }
    }

    private ObservableCollection<MemoryEditItemViewModel> _memoryEdits = new();

    public ObservableCollection<MemoryEditCategoryGroupViewModel> CategoryGroups
    {
        get => _categoryGroups;
        private set
        {
            _categoryGroups = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentCategoryGroups));
            OnPropertyChanged(nameof(LoadedEditCount));
        }
    }

    private ObservableCollection<MemoryEditCategoryGroupViewModel> _categoryGroups = new();

    public ObservableCollection<MemoryEditItemViewModel> OverrideMemoryEdits
    {
        get => _overrideMemoryEdits;
        private set
        {
            _overrideMemoryEdits = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<MemoryEditItemViewModel> _overrideMemoryEdits = new();

    public ObservableCollection<MemoryEditCategoryGroupViewModel> OverrideCategoryGroups
    {
        get => _overrideCategoryGroups;
        private set
        {
            _overrideCategoryGroups = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentCategoryGroups));
            OnPropertyChanged(nameof(LoadedEditCount));
        }
    }

    private ObservableCollection<MemoryEditCategoryGroupViewModel> _overrideCategoryGroups = new();

    public string OverrideStatusMessage
    {
        get => _overrideStatusMessage;
        private set
        {
            if (_overrideStatusMessage == value)
                return;
            _overrideStatusMessage = value;
            OnPropertyChanged();
        }
    }

    private string _overrideStatusMessage = string.Empty;

    public bool HasModOverrides
    {
        get => _hasModOverrides;
        private set
        {
            if (_hasModOverrides == value)
                return;
            _hasModOverrides = value;
            OnPropertyChanged();
        }
    }

    private bool _hasModOverrides;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
                return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
                return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string DebugStatus
    {
        get => _debugStatus;
        private set
        {
            if (_debugStatus == value)
                return;
            _debugStatus = value;
            OnPropertyChanged();
        }
    }

    public bool ShowDebugStatus
    {
        get => _showDebugStatus;
        set
        {
            if (_showDebugStatus == value)
                return;
            _showDebugStatus = value;
            OnPropertyChanged();
        }
    }

    public int LoadedEditCount => CurrentCategoryGroups.Sum(group => group.EditCount);

    public event PropertyChangedEventHandler PropertyChanged;

    public MemoryEditsDrawerViewModel()
    {
        if (Execute.InDesignMode)
            LoadDesignTimeData();
    }

    public MemoryEditsDrawerViewModel(ShellViewModel shellViewModel)
    {
        ShellViewModel = shellViewModel;
    }

    public async Task Initialize()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        IsLoading = true;
        SetLoadState("Loading memory edits...", "Starting load...");
        await PumpUiAsync();

        try
        {
            var debugLog = new StringBuilder();
            AppendEnvironmentInfo(debugLog);
            SetLoadState("Resolving config paths...", debugLog.ToString());
            await PumpUiAsync();

            try
            {
                _resolvedConfigPath = GetHudConfigPath(debugLog);
                _resolvedTemplatePath = ResolveTemplatePath(debugLog);
            }
            catch (Exception ex)
            {
                debugLog.AppendLine($"Path resolution error: {ex}");
                SetLoadState($"Failed to resolve paths: {ex.Message}", debugLog.ToString());
                return;
            }

            SetLoadState("Reading HUD config files...", debugLog.ToString());
            await PumpUiAsync();

            bool skipTemplateMerge = _skipTemplateMergeOnNextLoad;
            _skipTemplateMergeOnNextLoad = false;

            try
            {
                EnsureOverridesSyncedToHudConfig(debugLog);
            }
            catch (Exception ex)
            {
                debugLog.AppendLine($"Override sync to HUD config: FAILED — {ex.Message}");
                Logger.Warn($"Failed to sync overrides to HUD config: {ex.Message}");
            }

            string[] templateCandidates = GetTemplatePathCandidatesForMod().ToArray();

            MemoryEditsLoadResult result = await Task.Run(() =>
                BuildMemoryEditItems(_resolvedConfigPath, debugLog, skipTemplateMerge, templateCandidates));

            string overridePath = GetMemoryOverridesPath(debugLog);
            MemoryEditsLoadResult overrideResult = await Task.Run(() =>
                BuildOverrideMemoryEditItems(overridePath, debugLog, templateCandidates));

            MarkModOverrides(result.Items, overrideResult.Items);

            debugLog.AppendLine();
            debugLog.AppendLine($"Built view models: {result.Items.Count}");
            debugLog.AppendLine($"Mod override view models: {overrideResult.Items.Count}");
            SetLoadState(
                result.Items.Count > 0
                    ? $"Building list view for {result.Items.Count} edits..."
                    : "No memory edits loaded — see debug status below",
                debugLog.ToString());
            await PumpUiAsync();

            await PopulateEditsAsync(result.Items, overrideResult.Items, debugLog);

            SetLoadState(
                result.Items.Count > 0
                    ? $"{result.Items.Count} memory edits loaded"
                    : "No memory edits loaded — see debug status below",
                debugLog.ToString());
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load memory edits: {ex}");
            SetLoadState(
                $"Failed to load memory edits: {ex.Message}",
                $"{DebugStatus}{Environment.NewLine}{Environment.NewLine}EXCEPTION:{Environment.NewLine}{ex}");
        }
        finally
        {
            IsLoading = false;
            await PumpUiAsync();
        }
    }

    private void SetLoadState(string statusMessage, string debugStatus)
    {
        StatusMessage = statusMessage;
        DebugStatus = debugStatus;
    }

    private static async Task PumpUiAsync()
    {
        if (Application.Current?.Dispatcher == null)
        {
            await Task.Yield();
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.Background);
    }

    private async Task PopulateEditsAsync(
        IReadOnlyList<MemoryEditItemViewModel> items,
        IReadOnlyList<MemoryEditItemViewModel> overrideItems,
        StringBuilder debugLog)
    {
        CategoryGroups = new ObservableCollection<MemoryEditCategoryGroupViewModel>();
        OverrideCategoryGroups = new ObservableCollection<MemoryEditCategoryGroupViewModel>();
        await PumpUiAsync();

        debugLog.AppendLine("Replacing in-memory edit collection...");
        foreach (MemoryEditItemViewModel item in MemoryEdits)
            item.PropertyChanged -= OnMemoryEditItemPropertyChanged;

        MemoryEdits = new ObservableCollection<MemoryEditItemViewModel>(items);
        foreach (MemoryEditItemViewModel item in MemoryEdits)
            item.PropertyChanged += OnMemoryEditItemPropertyChanged;
        debugLog.AppendLine($"UI collection count: {MemoryEdits.Count}");
        await PumpUiAsync();

        debugLog.AppendLine("Building category groups...");
        BuildCategoryGroups();
        debugLog.AppendLine($"Category groups: {CategoryGroups.Count}");
        debugLog.AppendLine($"Displayed edit count: {LoadedEditCount}");

        OverrideMemoryEdits = new ObservableCollection<MemoryEditItemViewModel>(overrideItems);
        BuildOverrideCategoryGroups();
        debugLog.AppendLine($"Override category groups: {OverrideCategoryGroups.Count}");
        debugLog.AppendLine($"Override edit count: {OverrideMemoryEdits.Count}");

        HasModOverrides = overrideItems.Count > 0;
        OverrideStatusMessage = HasModOverrides
            ? $"{overrideItems.Count} mod override edit(s) loaded from memory_overrides.json"
            : "No mod override file found for this mod.";

        OnPropertyChanged(nameof(CurrentCategoryGroups));
        OnPropertyChanged(nameof(LoadedEditCount));
    }

    private void AppendEnvironmentInfo(StringBuilder log)
    {
        log.AppendLine($"=== Memory Edits Load {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        log.AppendLine($"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
        log.AppendLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
        log.AppendLine($"Mod: {ShellViewModel?.ModInfo?.Name ?? "(none selected)"}");
        log.AppendLine();
    }

    private void LoadDesignTimeData()
    {
        MemoryEdits.Add(new MemoryEditItemViewModel(new MemoryConfig
        {
            Name = "Enable TCP/IP Access",
            Description = "Without this feature, you will always receive a 'Failed to Join' error when hosting a TCP/IP game",
            Category = "Important",
            Address = "749AC",
            Length = 1,
            Type = "Hex",
            UserType = "Boolean",
            Values = "EB",
            OriginalValues = "74",
            ModdedValues = "EB"
        }, isModOverridden: true));
        MemoryEdits.Add(new MemoryEditItemViewModel(new MemoryConfig
        {
            Name = "Skill Icon Size",
            Description = "Adjust the size of skill icons on the skill bar",
            Category = "Enhancement",
            Address = "123456",
            Length = 4,
            Type = "Int32",
            UserType = "Adjustable",
            Values = "32",
            OriginalValues = "24",
            ModdedValues = "32"
        }));
        OverrideMemoryEdits.Add(new MemoryEditItemViewModel(new MemoryConfig
        {
            Name = "Enable TCP/IP Access",
            Description = "Mod override for TCP/IP access",
            Category = "Important",
            Address = "749AC",
            Length = 1,
            Type = "Hex",
            UserType = "Boolean",
            Values = "90",
            OriginalValues = "74",
            ModdedValues = "EB"
        }, isReadOnly: true));
        BuildCategoryGroups();
        BuildOverrideCategoryGroups();
        HasModOverrides = true;
        OverrideStatusMessage = "1 mod override edit(s) loaded from memory_overrides.json";
        foreach (MemoryEditItemViewModel item in MemoryEdits)
            item.PropertyChanged += OnMemoryEditItemPropertyChanged;
        StatusMessage = "Design-time sample loaded";
        DebugStatus = "Design-time mode";
        IsLoading = false;
    }

    private sealed class MemoryEditsLoadResult
    {
        public List<MemoryEditItemViewModel> Items { get; init; } = new();
    }

    private sealed class LoadMemoryConfigsResult
    {
        public List<MemoryConfig> Configs { get; } = new();
        public string SourcePath { get; set; } = string.Empty;
    }

    private IEnumerable<string> GetTemplatePathCandidatesForMod()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(ShellViewModel?.GamePath))
        {
            string gameTemplate = Path.Combine(ShellViewModel.GamePath, "HUDConfig_Template.json");
            if (seen.Add(gameTemplate))
                yield return gameTemplate;
        }

        foreach (string candidate in GetTemplatePathCandidates())
        {
            if (seen.Add(candidate))
                yield return candidate;
        }
    }

    private static MemoryEditsLoadResult BuildMemoryEditItems(
        string configPath,
        StringBuilder log,
        bool skipTemplateMerge,
        string[] templateCandidates)
    {
        var result = new MemoryEditsLoadResult();

        log.AppendLine("--- File availability ---");
        log.AppendLine($"Mod config path: {configPath}");
        log.AppendLine($"Mod config exists: {File.Exists(configPath)}");
        log.AppendLine($"Skip template merge: {skipTemplateMerge}");
        log.AppendLine();

        LoadMemoryConfigsResult templateLoad = LoadBestTemplateCatalog(log, templateCandidates);
        if (templateLoad.Configs.Count == 0 && !File.Exists(configPath))
        {
            log.AppendLine("RESULT: No template catalog or mod config found.");
            return result;
        }

        if (!skipTemplateMerge && templateLoad.Configs.Count > 0 && !string.IsNullOrEmpty(configPath))
        {
            string mergeTemplatePath = ResolveMergeTemplatePath(templateLoad, log);
            if (!string.IsNullOrEmpty(mergeTemplatePath) && File.Exists(mergeTemplatePath))
            {
                EnsureHudConfigExists(configPath, mergeTemplatePath);
                if (File.Exists(configPath))
                {
                    try
                    {
                        MergeHudTemplateMemoryConfigs(mergeTemplatePath, configPath);
                        log.AppendLine("MergeHudTemplateMemoryConfigs: completed");
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine($"MergeHudTemplateMemoryConfigs: FAILED — {ex.Message}");
                    }
                }
            }
        }
        else if (skipTemplateMerge)
        {
            log.AppendLine("MergeHudTemplateMemoryConfigs: skipped (fresh rebuild)");
        }

        LoadMemoryConfigsResult modLoad = File.Exists(configPath)
            ? LoadMemoryConfigsFromFile(configPath, log, "Mod config")
            : new LoadMemoryConfigsResult();

        LoadMemoryConfigsResult metadataLoad = ResolveTemplateMetadataSource(
            log, templateLoad, modLoad, skipTemplateMerge);

        List<MemoryConfig> catalog = BuildMergedCatalog(templateLoad.Configs, modLoad.Configs, log);
        log.AppendLine($"Merged catalog count: {catalog.Count}");

        if (catalog.Count == 0)
        {
            log.AppendLine("RESULT: MemoryConfigs catalog is empty.");
            return result;
        }

        foreach (MemoryConfig catalogEntry in catalog)
        {
            MemoryConfig templateRef = FindMatchingMemoryConfig(metadataLoad.Configs, catalogEntry)
                ?? FindMatchingMemoryConfig(templateLoad.Configs, catalogEntry)
                ?? catalogEntry;
            MemoryConfig modMatch = FindMatchingMemoryConfig(modLoad.Configs, catalogEntry);
            MemoryConfig activeEntry = MergeMemoryConfig(templateRef, modMatch ?? templateRef);
            result.Items.Add(new MemoryEditItemViewModel(activeEntry));
        }

        log.AppendLine($"Built view models: {result.Items.Count}");
        if (result.Items.Count > 0)
            log.AppendLine($"First entry: {result.Items[0].Name}");

        bool hasTcpIp = result.Items.Any(i =>
            i.Name.Contains("Enable TCP/IP Access", StringComparison.OrdinalIgnoreCase));
        log.AppendLine($"Contains 'Enable TCP/IP Access': {hasTcpIp}");

        return result;
    }

    private static MemoryEditsLoadResult BuildOverrideMemoryEditItems(
        string overridePath,
        StringBuilder log,
        string[] templateCandidates)
    {
        var result = new MemoryEditsLoadResult();

        log.AppendLine("--- Mod override file ---");
        log.AppendLine($"Override path: {overridePath}");
        log.AppendLine($"Override exists: {File.Exists(overridePath)}");
        log.AppendLine();

        if (!File.Exists(overridePath))
        {
            log.AppendLine("RESULT: No mod override file found.");
            return result;
        }

        LoadMemoryConfigsResult overrideLoad = LoadMemoryConfigsFromFile(overridePath, log, "Mod overrides");
        if (overrideLoad.Configs.Count == 0)
        {
            log.AppendLine("RESULT: Mod override file has no MemoryConfigs entries.");
            return result;
        }

        LoadMemoryConfigsResult templateLoad = LoadBestTemplateCatalog(log, templateCandidates);
        LoadMemoryConfigsResult metadataLoad = ResolveTemplateMetadataSource(log, templateLoad);

        foreach (MemoryConfig overrideEntry in overrideLoad.Configs)
        {
            MemoryConfig templateRef = FindMatchingMemoryConfig(metadataLoad.Configs, overrideEntry)
                ?? FindMatchingMemoryConfig(templateLoad.Configs, overrideEntry)
                ?? overrideEntry;
            MemoryConfig merged = MergeMemoryConfig(templateRef, overrideEntry);
            result.Items.Add(new MemoryEditItemViewModel(merged, isReadOnly: true));
        }

        log.AppendLine($"Built override view models: {result.Items.Count}");
        return result;
    }

    private static void MarkModOverrides(
        IReadOnlyList<MemoryEditItemViewModel> appliedItems,
        IReadOnlyList<MemoryEditItemViewModel> overrideItems)
    {
        if (overrideItems.Count == 0)
            return;

        foreach (MemoryEditItemViewModel applied in appliedItems)
            applied.SetModOverridden(overrideItems.Any(overrideItem =>
                MemoryConfigsMatch(overrideItem.SourceConfig, applied.SourceConfig)));
    }

    private static List<MemoryConfig> BuildMergedCatalog(
        IReadOnlyList<MemoryConfig> templateConfigs,
        IReadOnlyList<MemoryConfig> modConfigs,
        StringBuilder log)
    {
        var catalog = new List<MemoryConfig>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddEntry(MemoryConfig config, string source)
        {
            string key = GetConfigKey(config);
            if (string.IsNullOrEmpty(key) || !seenKeys.Add(key))
                return;

            catalog.Add(config);
            log.AppendLine($"  Catalog + [{source}] {config.Name} ({key})");
        }

        log.AppendLine("Building merged catalog...");
        foreach (MemoryConfig entry in templateConfigs)
            AddEntry(entry, "template");

        foreach (MemoryConfig entry in modConfigs)
        {
            if (catalog.Any(existing => MemoryConfigsMatch(existing, entry)))
                continue;

            AddEntry(entry, "mod-only");
        }

        return catalog;
    }

    private static MemoryConfig MergeMemoryConfig(MemoryConfig templateEntry, MemoryConfig modEntry)
    {
        return new MemoryConfig
        {
            Name = templateEntry.Name ?? modEntry.Name,
            Description = templateEntry.Description ?? modEntry.Description,
            Category = templateEntry.Category ?? modEntry.Category,
            Address = templateEntry.Address ?? modEntry.Address,
            Addresses = templateEntry.Addresses ?? modEntry.Addresses,
            Length = templateEntry.Length > 0 ? templateEntry.Length : modEntry.Length,
            Type = templateEntry.Type ?? modEntry.Type,
            UserType = NormalizeMemoryUserType(templateEntry.UserType ?? modEntry.UserType),
            Values = modEntry.Values ?? templateEntry.Values,
            OriginalValues = templateEntry.OriginalValues ?? modEntry.OriginalValues,
            ModdedValues = templateEntry.ModdedValues ?? modEntry.ModdedValues
        };
    }

    private static string GetConfigKey(MemoryConfig config)
    {
        string primaryAddress = GetMemoryConfigAddresses(config)
            .Select(NormalizeMemoryAddress)
            .FirstOrDefault(address => !string.IsNullOrEmpty(address));

        if (!string.IsNullOrEmpty(primaryAddress))
            return $"addr:{primaryAddress}";

        return !string.IsNullOrWhiteSpace(config.Name) ? $"name:{config.Name.Trim()}" : string.Empty;
    }

    private static LoadMemoryConfigsResult LoadBestTemplateCatalog(StringBuilder log, string[] templateCandidates)
    {
        log.AppendLine("--- Template catalog sources (disk preferred) ---");
        LoadMemoryConfigsResult best = new();
        string bestLabel = "none";
        DateTime bestWriteTime = DateTime.MinValue;

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> paths = (templateCandidates ?? Array.Empty<string>())
            .Concat(GetTemplatePathCandidates());

        foreach (string candidate in paths)
        {
            if (!seenPaths.Add(candidate))
                continue;

            if (!File.Exists(candidate))
            {
                log.AppendLine($"  [miss] {candidate}");
                continue;
            }

            LoadMemoryConfigsResult load = LoadMemoryConfigsFromFile(candidate, log, "Template file");
            DateTime writeTime = File.GetLastWriteTimeUtc(candidate);
            log.AppendLine($"  [file] {candidate} => {load.Configs.Count} entries, modified {writeTime:u}");

            bool isBetter = load.Configs.Count > best.Configs.Count
                || (load.Configs.Count == best.Configs.Count
                    && load.Configs.Count > 0
                    && writeTime > bestWriteTime);

            if (isBetter)
            {
                best = load;
                best.SourcePath = candidate;
                bestLabel = candidate;
                bestWriteTime = writeTime;
            }
        }

        if (best.Configs.Count == 0)
        {
            LoadMemoryConfigsResult embedded = LoadEmbeddedTemplate(log);
            log.AppendLine($"  [embedded fallback] => {embedded.Configs.Count} entries");
            if (embedded.Configs.Count > 0)
            {
                best = embedded;
                bestLabel = "embedded resource (no disk template)";
                best.SourcePath = ResolveMergeTemplatePath(best, log);
            }
        }

        log.AppendLine($"Selected template catalog: {bestLabel} ({best.Configs.Count} entries)");
        log.AppendLine();
        return best;
    }

    private static LoadMemoryConfigsResult ResolveTemplateMetadataSource(
        StringBuilder log,
        LoadMemoryConfigsResult templateLoad,
        LoadMemoryConfigsResult modLoad = null,
        bool preferModConfig = false)
    {
        if (preferModConfig && modLoad != null && modLoad.Configs.Count > 0)
        {
            log.AppendLine("Metadata source: mod HUD config (post-rebuild)");
            return modLoad;
        }

        if (templateLoad.Configs.Count > 0)
        {
            log.AppendLine($"Metadata source: disk template ({templateLoad.SourcePath})");
            return templateLoad;
        }

        LoadMemoryConfigsResult embedded = LoadEmbeddedTemplate(log);
        if (embedded.Configs.Count > 0)
        {
            log.AppendLine("Metadata source: embedded template (fallback)");
            return embedded;
        }

        log.AppendLine("Metadata source: none");
        return templateLoad;
    }

    private static string ResolveMergeTemplatePath(LoadMemoryConfigsResult templateLoad, StringBuilder log)
    {
        if (!string.IsNullOrEmpty(templateLoad.SourcePath) && File.Exists(templateLoad.SourcePath))
        {
            log.AppendLine($"Merge template path: {templateLoad.SourcePath}");
            return templateLoad.SourcePath;
        }

        string fallback = GetTemplatePathCandidates().FirstOrDefault(File.Exists)
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HUDConfig_Template.json");

        log.AppendLine($"Merge template path (fallback): {fallback}");
        return fallback;
    }

    private static IEnumerable<string> GetTemplatePathCandidates()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return new[]
        {
            Path.Combine(baseDir, "HUDConfig_Template.json"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "D2R", "HUDConfig_Template.json")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "HUDConfig_Template.json")),
            Path.Combine(Directory.GetCurrentDirectory(), "HUDConfig_Template.json")
        }.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static LoadMemoryConfigsResult LoadEmbeddedTemplate(StringBuilder log)
    {
        var result = new LoadMemoryConfigsResult { SourcePath = string.Empty };
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("HUDConfig_Template.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                log.AppendLine("Embedded HUDConfig_Template.json not found.");
                return result;
            }

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return result;

            using StreamReader reader = new StreamReader(stream);
            string jsonContent = StripJsonComments(reader.ReadToEnd());
            return ParseMemoryConfigsJson(jsonContent, log, "Embedded template", string.Empty);
        }
        catch (Exception ex)
        {
            log.AppendLine($"Embedded template load error: {ex.Message}");
            return result;
        }
    }

    private string ResolveTemplatePath(StringBuilder log)
    {
        log.AppendLine("--- Template path (for merge/write) ---");
        string[] candidates = GetTemplatePathCandidatesForMod().ToArray();
        string resolved = ResolveBestTemplateFilePath(log, candidates);
        log.AppendLine($"Selected template path: {resolved}");
        log.AppendLine();
        return resolved;
    }

    private static string ResolveBestTemplateFilePath(StringBuilder log, params string[] templateCandidates)
    {
        log?.AppendLine("--- Resolving best HUD template file (disk preferred) ---");
        string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HUDConfig_Template.json");

        LoadMemoryConfigsResult catalog = LoadBestTemplateCatalog(log ?? new StringBuilder(), templateCandidates);
        if (!string.IsNullOrEmpty(catalog.SourcePath) && File.Exists(catalog.SourcePath))
        {
            log?.AppendLine($"  Selected: {catalog.SourcePath}");
            return catalog.SourcePath;
        }

        if (catalog.Configs.Count > 0 && TryReadEmbeddedTemplateContent(out string embeddedContent))
        {
            File.WriteAllText(fallback, embeddedContent);
            log?.AppendLine($"  Wrote embedded fallback to: {fallback}");
            return fallback;
        }

        log?.AppendLine($"  Selected: {fallback}");
        return fallback;
    }

    private static int CountMemoryConfigsInFile(string path)
    {
        try
        {
            return CountMemoryConfigsInJson(StripJsonComments(File.ReadAllText(path)));
        }
        catch
        {
            return 0;
        }
    }

    private static int CountMemoryConfigsInJson(string jsonContent)
    {
        try
        {
            JsonNode? root = JsonNode.Parse(jsonContent);
            return root?["MemoryConfigs"] is JsonArray array ? array.Count : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static IReadOnlyList<string> LoadCategoriesFromTemplate(string templatePath)
    {
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
            return CreateMemoryEditViewModel.DefaultCategories;

        LoadMemoryConfigsResult load = LoadMemoryConfigsFromFile(
            templatePath,
            new StringBuilder(),
            "Category list");

        var categories = load.Configs
            .Select(config => config.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (categories.Count == 0)
            return CreateMemoryEditViewModel.DefaultCategories;

        if (!categories.Any(category => category.Equals("Uncategorized", StringComparison.OrdinalIgnoreCase)))
            categories.Add("Uncategorized");

        return categories;
    }

    private static bool TryReadEmbeddedTemplateContent(out string content)
    {
        content = string.Empty;
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("HUDConfig_Template.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
                return false;

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return false;

            using StreamReader reader = new StreamReader(stream);
            content = reader.ReadToEnd();
            return !string.IsNullOrWhiteSpace(content);
        }
        catch
        {
            return false;
        }
    }

    private void BuildCategoryGroups()
    {
        CategoryGroups = BuildCategoryGroupsFrom(MemoryEdits);
    }

    private void BuildOverrideCategoryGroups()
    {
        OverrideCategoryGroups = BuildCategoryGroupsFrom(OverrideMemoryEdits);
    }

    private ObservableCollection<MemoryEditCategoryGroupViewModel> BuildCategoryGroupsFrom(
        IEnumerable<MemoryEditItemViewModel> edits)
    {
        var groups = edits
            .GroupBy(edit => edit.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetCategorySortOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var activityGroups = new ObservableCollection<MemoryEditActivityGroupViewModel>();

                foreach (string activity in new[] { "Active", "Inactive" })
                {
                    var items = new ObservableCollection<MemoryEditItemViewModel>(
                        group
                            .Where(edit => edit.IsActive == string.Equals(activity, "Active", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(edit => GetUserTypeSortOrder(edit))
                            .ThenBy(edit => edit.Name, StringComparer.OrdinalIgnoreCase));

                    if (items.Count == 0)
                        continue;

                    string activityKey = GetActivityStateKey(group.Key, activity);
                    bool isExpanded = _activityExpandedState.TryGetValue(activityKey, out bool expanded) && expanded;

                    var activityGroup = new MemoryEditActivityGroupViewModel(group.Key, activity, items, isExpanded);
                    activityGroup.PropertyChanged += OnActivityGroupPropertyChanged;
                    activityGroups.Add(activityGroup);
                }

                bool isCategoryExpanded = _categoryExpandedState.TryGetValue(group.Key, out bool categoryExpanded)
                    && categoryExpanded;

                var categoryGroup = new MemoryEditCategoryGroupViewModel(group.Key, activityGroups, isCategoryExpanded);
                categoryGroup.PropertyChanged += OnCategoryGroupPropertyChanged;
                return categoryGroup;
            })
            .ToList();

        return new ObservableCollection<MemoryEditCategoryGroupViewModel>(groups);
    }

    private static string GetActivityStateKey(string category, string activity) => $"{category}|{activity}";

    private void OnMemoryEditItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MemoryEditItemViewModel.Values)
            or nameof(MemoryEditItemViewModel.IsModdedEnabled)
            or nameof(MemoryEditItemViewModel.IsCustomValue)
            or nameof(MemoryEditItemViewModel.IsActive))
        {
            BuildCategoryGroups();
        }
    }

    private void OnActivityGroupPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MemoryEditActivityGroupViewModel.IsExpanded)
            || sender is not MemoryEditActivityGroupViewModel group)
            return;

        _activityExpandedState[GetActivityStateKey(group.Category, group.Activity)] = group.IsExpanded;
    }

    private void OnCategoryGroupPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MemoryEditCategoryGroupViewModel.IsExpanded)
            || sender is not MemoryEditCategoryGroupViewModel group)
            return;

        _categoryExpandedState[group.Category] = group.IsExpanded;
    }

    private static int GetCategorySortOrder(string category) => category?.ToUpperInvariant() switch
    {
        "IMPORTANT" => 0,
        "OPTIONAL FIX" => 1,
        _ => 2
    };

    private static int GetUserTypeSortOrder(MemoryEditItemViewModel edit) =>
        edit.IsAdjustable ? 1 : 0;

    [UsedImplicitly]
    public void OnSave()
    {
        try
        {
            string configPath = GetHudConfigPath();
            if (!File.Exists(configPath))
            {
                MessageBox.Show("HUD config file not found.", "Memory Edits", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string configJson = StripJsonComments(File.ReadAllText(configPath));
            JsonObject configRoot = JsonNode.Parse(configJson)?.AsObject();
            if (configRoot?["MemoryConfigs"] is not JsonArray memArray)
            {
                MessageBox.Show("HUD config has no MemoryConfigs section.", "Memory Edits", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (MemoryEditItemViewModel item in MemoryEdits)
            {
                if (item.IsModOverridden)
                    continue;

                JsonObject target = memArray
                    .OfType<JsonObject>()
                    .FirstOrDefault(node => MemoryConfigMatches(node, item.SourceConfig));

                if (target == null)
                {
                    target = ToMemoryConfigJsonObject(item.SourceConfig);
                    if (target != null)
                        memArray.Add(target);
                    continue;
                }

                target["Values"] = item.Values;
                target["UserType"] = NormalizeMemoryUserType(item.UserType);
            }

            File.WriteAllText(
                configPath,
                configRoot.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
            );

            StatusMessage = $"Saved {MemoryEdits.Count} memory edits to {Path.GetFileName(configPath)}";
            Logger.Info(StatusMessage);
            MessageBox.Show("Memory edit settings saved.", "Memory Edits", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save memory edits: {ex}");
            MessageBox.Show($"Failed to save memory edits:\n{ex.Message}", "Memory Edits", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [UsedImplicitly]
    public async void OnCreateNew()
    {
        try
        {
            var dialog = new CreateMemoryEditViewModel();
            dialog.SetCategories(LoadCategoriesFromTemplate(_resolvedTemplatePath));
            var window = new CreateMemoryEditView
            {
                DataContext = dialog,
                Owner = Application.Current?.MainWindow
            };

            ViewModelBinder.Bind(dialog, window, null);

            bool? accepted = window.ShowDialog();
            if (accepted != true || dialog.CreatedConfig == null)
                return;

            MemoryConfig? existingHudEdit = FindExistingHudConfigEntry(dialog.CreatedConfig);
            if (existingHudEdit != null)
            {
                MessageBox.Show(
                    $"A memory edit with this address already exists in your HUD config as \"{existingHudEdit.Name}\".\n\n" +
                    "Open the Applied Memory Edits tab and use \"Add to Mod Overrides\" on that entry instead of creating a new one here.",
                    "Memory Edits",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            AppendMemoryConfigToOverrideFile(dialog.CreatedConfig);
            EnsureOverridesSyncedToHudConfig();
            _isInitialized = false;
            await Initialize();
            SelectedTabIndex = 0;
            string hudFileName = Path.GetFileName(GetHudConfigPath());
            StatusMessage = $"Added \"{dialog.CreatedConfig.Name}\" to overrides and {hudFileName}";
            OverrideStatusMessage = $"Added memory edit \"{dialog.CreatedConfig.Name}\" to overrides";
            MessageBox.Show(
                $"Memory edit \"{dialog.CreatedConfig.Name}\" was saved as an override and merged into {hudFileName}.",
                "Memory Edits",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create memory edit: {ex}");
            MessageBox.Show($"Failed to create memory edit:\n{ex.Message}", "Memory Edits", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private MemoryConfig? FindExistingHudConfigEntry(MemoryConfig config)
    {
        try
        {
            string configPath = GetHudConfigPath();
            if (!File.Exists(configPath))
                return null;

            LoadMemoryConfigsResult hudLoad = LoadMemoryConfigsFromFile(
                configPath,
                new StringBuilder(),
                "HUD config check");

            HashSet<string> newAddresses = GetMemoryConfigAddresses(config)
                .Select(NormalizeMemoryAddress)
                .Where(address => !string.IsNullOrEmpty(address))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (newAddresses.Count == 0)
                return null;

            foreach (MemoryConfig hudEntry in hudLoad.Configs)
            {
                HashSet<string> hudAddresses = GetMemoryConfigAddresses(hudEntry)
                    .Select(NormalizeMemoryAddress)
                    .Where(address => !string.IsNullOrEmpty(address))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (hudAddresses.Count > 0 && hudAddresses.Overlaps(newAddresses))
                    return hudEntry;
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to check HUD config for existing memory edit address: {ex.Message}");
            return null;
        }
    }

    [UsedImplicitly]
    public async void AddToModOverrides(MemoryEditItemViewModel item)
    {
        if (item == null || !item.IsEditable)
            return;

        try
        {
            MemoryConfig config = CreateOverrideConfigFromItem(item);
            UpsertMemoryConfigInOverrideFile(config);
            EnsureOverridesSyncedToHudConfig();
            item.SetModOverridden(true);
            await RefreshOverrideItemsAsync();
            OverrideStatusMessage = $"Added \"{config.Name}\" to memory_overrides.json";
            StatusMessage = $"\"{config.Name}\" is now locked — edit it on the Mod Overrides tab.";

            MessageBox.Show(
                $"\"{config.Name}\" was added to memory_overrides.json, merged into {Path.GetFileName(GetHudConfigPath())}, and is now locked on the Applied tab.",
                "Memory Edits",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to add memory edit to mod overrides: {ex}");
            MessageBox.Show(
                $"Failed to add memory edit to mod overrides:\n{ex.Message}",
                "Memory Edits",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void EnsureOverridesSyncedToHudConfig(StringBuilder log = null)
    {
        string overridePath = GetMemoryOverridesPath(log);
        if (!File.Exists(overridePath))
            return;

        string configPath = GetHudConfigPath(log);
        string[] templateCandidates = GetTemplatePathCandidatesForMod().ToArray();
        var syncLog = log ?? new StringBuilder();
        LoadMemoryConfigsResult templateLoad = LoadBestTemplateCatalog(syncLog, templateCandidates);
        string mergeTemplatePath = ResolveMergeTemplatePath(templateLoad, syncLog);

        if (!string.IsNullOrEmpty(mergeTemplatePath) && File.Exists(mergeTemplatePath))
            EnsureHudConfigExists(configPath, mergeTemplatePath);

        if (!File.Exists(configPath))
            throw new InvalidOperationException(
                "HUD config file not found. Use Rebuild HUDConfig or launch the mod once to create it.");

        log?.AppendLine("--- Syncing mod overrides to HUD config ---");
        ApplyMemoryOverridesToHudConfig(configPath, overridePath);
        log?.AppendLine($"Synced overrides from {overridePath} to {configPath}");
    }

    private async Task RefreshOverrideItemsAsync()
    {
        string overridePath = GetMemoryOverridesPath();
        string[] templateCandidates = GetTemplatePathCandidatesForMod().ToArray();
        var log = new StringBuilder();
        MemoryEditsLoadResult overrideResult = await Task.Run(() =>
            BuildOverrideMemoryEditItems(overridePath, log, templateCandidates));

        OverrideMemoryEdits = new ObservableCollection<MemoryEditItemViewModel>(overrideResult.Items);
        BuildOverrideCategoryGroups();
        HasModOverrides = OverrideMemoryEdits.Count > 0;
        OverrideStatusMessage = HasModOverrides
            ? $"{OverrideMemoryEdits.Count} mod override edit(s) loaded from memory_overrides.json"
            : "No mod override file found for this mod.";
        OnPropertyChanged(nameof(CurrentCategoryGroups));
    }

    private static MemoryConfig CreateOverrideConfigFromItem(MemoryEditItemViewModel item)
    {
        MemoryConfig source = item.SourceConfig;
        bool useMultipleAddresses = source.Addresses != null && source.Addresses.Count > 0;

        var config = new MemoryConfig
        {
            Name = source.Name,
            Description = source.Description,
            Category = source.Category,
            Length = source.Length,
            Type = source.Type,
            UserType = NormalizeMemoryUserType(item.UserType),
            Values = item.Values,
            OriginalValues = source.OriginalValues,
            ModdedValues = source.ModdedValues
        };

        if (useMultipleAddresses)
        {
            config.Addresses = source.Addresses.ToList();
            config.Address = null;
        }
        else
        {
            config.Address = source.Address;
            config.Addresses = null;
        }

        return config;
    }

    private void UpsertMemoryConfigInOverrideFile(MemoryConfig config)
    {
        string overridePath = GetMemoryOverridesPath();
        EnsureMemoryOverridesFileExists(overridePath);

        string configJson = StripJsonComments(File.ReadAllText(overridePath));
        JsonObject configRoot = JsonNode.Parse(configJson)?.AsObject()
            ?? throw new InvalidOperationException("Mod override file is not valid JSON.");

        if (configRoot["MemoryConfigs"] is not JsonArray memArray)
            throw new InvalidOperationException("Mod override file has no MemoryConfigs section.");

        JsonObject? newEntry = ToMemoryConfigJsonObject(config);
        if (newEntry == null)
            throw new InvalidOperationException("Failed to serialize the memory edit.");

        JsonObject? existing = memArray
            .OfType<JsonObject>()
            .FirstOrDefault(node =>
            {
                MemoryConfig? existingConfig = node.Deserialize<MemoryConfig>(MemoryConfigJsonOptions);
                return existingConfig != null && MemoryConfigsMatch(existingConfig, config);
            });

        if (existing == null)
            memArray.Add(newEntry);
        else
        {
            int index = memArray.IndexOf(existing);
            memArray[index] = newEntry;
        }

        File.WriteAllText(
            overridePath,
            configRoot.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
        );

        Logger.Info($"Saved memory edit \"{config.Name}\" to {overridePath}");
    }

    private void AppendMemoryConfigToOverrideFile(MemoryConfig config)
    {
        string overridePath = GetMemoryOverridesPath();
        EnsureMemoryOverridesFileExists(overridePath);

        string configJson = StripJsonComments(File.ReadAllText(overridePath));
        JsonObject configRoot = JsonNode.Parse(configJson)?.AsObject()
            ?? throw new InvalidOperationException("Mod override file is not valid JSON.");

        if (configRoot["MemoryConfigs"] is not JsonArray memArray)
            throw new InvalidOperationException("Mod override file has no MemoryConfigs section.");

        bool duplicate = memArray
            .OfType<JsonObject>()
            .Any(node =>
            {
                MemoryConfig? existing = node.Deserialize<MemoryConfig>(MemoryConfigJsonOptions);
                return existing != null && MemoryConfigsMatch(existing, config);
            });

        if (duplicate)
            throw new InvalidOperationException("A memory edit with the same name or address already exists in memory_overrides.json.");

        JsonObject? newEntry = ToMemoryConfigJsonObject(config);
        if (newEntry == null)
            throw new InvalidOperationException("Failed to serialize the new memory edit.");

        memArray.Add(newEntry);

        File.WriteAllText(
            overridePath,
            configRoot.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
        );

        Logger.Info($"Added memory edit \"{config.Name}\" to {overridePath}");
    }

    private static void EnsureMemoryOverridesFileExists(string overridePath)
    {
        string directory = Path.GetDirectoryName(overridePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(overridePath))
            return;

        var root = new JsonObject
        {
            ["MemoryConfigs"] = new JsonArray()
        };

        File.WriteAllText(
            overridePath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
        );
    }

    [UsedImplicitly]
    public async void OnReload()
    {
        _isInitialized = false;
        await Initialize();
    }

    [UsedImplicitly]
    public async void OnRebuildHudConfig()
    {
        MessageBoxResult confirm = MessageBox.Show(
            "This will delete your current HUD config and rebuild it from HUDConfig_Template.json.\n\n" +
            "Mod-specific edits from memory_overrides.json will be reapplied afterward.\n\n" +
            "All other applied memory edit customizations will be lost.\n\nContinue?",
            "Rebuild HUDConfig",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            var rebuildLog = new StringBuilder();
            string[] templateCandidates = GetTemplatePathCandidatesForMod().ToArray();
            string configPath = GetHudConfigPath(rebuildLog);
            string templatePath = ResolveBestTemplateFilePath(rebuildLog, templateCandidates);
            string overridePath = GetMemoryOverridesPath(rebuildLog);

            RebuildHudConfigFromTemplate(configPath, templatePath, overridePath);

            _categoryExpandedState.Clear();
            _activityExpandedState.Clear();
            _skipTemplateMergeOnNextLoad = true;
            _isInitialized = false;
            await Initialize();

            StatusMessage = $"Rebuilt {Path.GetFileName(configPath)} from template";
            OverrideStatusMessage = File.Exists(overridePath)
                ? "Mod overrides reapplied from memory_overrides.json"
                : "No memory_overrides.json found to reapply";

            MessageBox.Show(
                $"HUD config rebuilt successfully.\n\n{StatusMessage}",
                "Memory Edits",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to rebuild HUD config: {ex}");
            MessageBox.Show(
                $"Failed to rebuild HUD config:\n{ex.Message}",
                "Memory Edits",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private string GetHudConfigPath() => GetHudConfigPath(null);

    private string GetHudConfigPath(StringBuilder log)
    {
        string modName = ShellViewModel?.ModInfo?.Name;
        if (string.IsNullOrWhiteSpace(modName))
            throw new InvalidOperationException("No mod is selected.");

        if (string.IsNullOrWhiteSpace(ShellViewModel?.GamePath))
            throw new InvalidOperationException("Game path is not available.");

        string resolved = Path.Combine(ShellViewModel.GamePath, $"HUDConfig_{modName}.json");
        log?.AppendLine("--- Mod HUD config path ---");
        log?.AppendLine($"  GamePath: {ShellViewModel.GamePath}");
        log?.AppendLine($"  [{(File.Exists(resolved) ? "FOUND" : "miss")}] {resolved}");
        log?.AppendLine($"Selected config path: {resolved}");
        log?.AppendLine();
        return resolved;
    }

    private string GetMemoryOverridesPath(StringBuilder log = null)
    {
        string modName = ShellViewModel?.ModInfo?.Name;
        if (string.IsNullOrWhiteSpace(modName))
            throw new InvalidOperationException("No mod is selected.");

        if (string.IsNullOrWhiteSpace(ShellViewModel?.GamePath))
            throw new InvalidOperationException("Game path is not available.");

        string resolved = Path.Combine(
            ShellViewModel.GamePath,
            "Mods",
            modName,
            $"{modName}.mpq",
            "data",
            "D2RLAN",
            "memory_overrides.json");

        log?.AppendLine("--- Mod override path ---");
        log?.AppendLine($"  [{(File.Exists(resolved) ? "FOUND" : "miss")}] {resolved}");
        log?.AppendLine($"Selected override path: {resolved}");
        log?.AppendLine();
        return resolved;
    }

    private static void EnsureHudConfigExists(string configPath, string templatePath)
    {
        if (File.Exists(configPath) || !File.Exists(templatePath))
            return;

        string configDirectory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            Directory.CreateDirectory(configDirectory);

        File.Copy(templatePath, configPath);
    }

    private static LoadMemoryConfigsResult LoadMemoryConfigsFromFile(string path, StringBuilder log, string label)
    {
        var result = new LoadMemoryConfigsResult { SourcePath = path };
        log.AppendLine($"--- {label} parse ---");
        log.AppendLine($"Path: {path}");

        if (!File.Exists(path))
        {
            log.AppendLine("File not found.");
            log.AppendLine();
            return result;
        }

        try
        {
            FileInfo fileInfo = new FileInfo(path);
            log.AppendLine($"File size: {fileInfo.Length} bytes");
            string jsonContent = StripJsonComments(File.ReadAllText(path));
            return ParseMemoryConfigsJson(jsonContent, log, label, path);
        }
        catch (Exception ex)
        {
            log.AppendLine($"ERROR: {ex}");
            log.AppendLine();
            Logger.Error($"Error loading memory configs from {path}: {ex.Message}");
            return result;
        }
    }

    private static LoadMemoryConfigsResult ParseMemoryConfigsJson(
        string jsonContent,
        StringBuilder log,
        string label,
        string sourcePath)
    {
        var result = new LoadMemoryConfigsResult { SourcePath = sourcePath };

        try
        {
            JsonNode root = JsonNode.Parse(jsonContent);
            if (root == null)
            {
                log.AppendLine("JSON parse returned null root.");
                log.AppendLine();
                return result;
            }

            if (root is JsonObject rootObject)
                log.AppendLine($"Root keys: {string.Join(", ", rootObject.Select(p => p.Key))}");

            if (root["MemoryConfigs"] is not JsonArray memArray)
            {
                log.AppendLine("MemoryConfigs property missing or not an array.");
                log.AppendLine();
                return result;
            }

            log.AppendLine($"MemoryConfigs array length: {memArray.Count}");

            int index = 0;
            int skipped = 0;
            foreach (JsonNode node in memArray)
            {
                if (node == null)
                {
                    skipped++;
                    log.AppendLine($"  [{index}] skipped (null node)");
                    index++;
                    continue;
                }

                try
                {
                    MemoryConfig config = node.Deserialize<MemoryConfig>(MemoryConfigJsonOptions);
                    if (config == null || string.IsNullOrWhiteSpace(config.Name))
                    {
                        skipped++;
                        log.AppendLine($"  [{index}] skipped (empty name)");
                    }
                    else
                    {
                        config.UserType = NormalizeMemoryUserType(config.UserType);
                        if (IsMinimalMemoryConfig(config))
                            result.Configs.Add(config);
                        else
                            skipped++;
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    log.AppendLine($"  [{index}] deserialize error: {ex.Message}");
                }

                index++;
            }

            log.AppendLine($"{label} loaded entries: {result.Configs.Count}, skipped: {skipped}");
            if (result.Configs.Count > 0)
                log.AppendLine($"First loaded entry: {result.Configs[0].Name}");
            log.AppendLine();

            Logger.Info($"Loaded {result.Configs.Count} memory configs from {sourcePath ?? label}");
            return result;
        }
        catch (Exception ex)
        {
            log.AppendLine($"ERROR parsing {label}: {ex}");
            log.AppendLine();
            return result;
        }
    }

    private static bool MemoryConfigMatches(JsonObject node, MemoryConfig config)
    {
        MemoryConfig nodeConfig = node.Deserialize<MemoryConfig>(MemoryConfigJsonOptions);
        return nodeConfig != null && MemoryConfigsMatch(nodeConfig, config);
    }

    private static JsonObject ToMemoryConfigJsonObject(MemoryConfig config)
    {
        JsonObject? node = JsonSerializer.SerializeToNode(config, MemoryConfigJsonOptions)?.AsObject();
        if (node == null)
            return null;

        bool useMultipleAddresses = config.Addresses != null && config.Addresses.Count > 0;
        if (useMultipleAddresses)
            node.Remove("Address");
        else
            node.Remove("Addresses");

        return node;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MemoryEditCategoryGroupViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;

    public MemoryEditCategoryGroupViewModel(
        string category,
        ObservableCollection<MemoryEditActivityGroupViewModel> activityGroups,
        bool isExpanded = false)
    {
        Category = category;
        ActivityGroups = activityGroups;
        _isExpanded = isExpanded;
    }

    public string Category { get; }
    public ObservableCollection<MemoryEditActivityGroupViewModel> ActivityGroups { get; }
    public int EditCount => ActivityGroups.Sum(group => group.EditCount);

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MemoryEditActivityGroupViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;

    public MemoryEditActivityGroupViewModel(
        string category,
        string activity,
        ObservableCollection<MemoryEditItemViewModel> items,
        bool isExpanded = false)
    {
        Category = category;
        Activity = activity;
        Items = items;
        _isExpanded = isExpanded;
    }

    public string Category { get; }
    public string Activity { get; }
    public ObservableCollection<MemoryEditItemViewModel> Items { get; }
    public int EditCount => Items.Count;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MemoryEditItemViewModel : INotifyPropertyChanged
{
    private string _values;
    private bool _isCustomValue;
    private bool _isModOverridden;

    public string UserType { get; }
    public bool IsAdjustable => string.Equals(UserType, "Adjustable", StringComparison.OrdinalIgnoreCase);
    public bool IsBoolean => !IsAdjustable;
    public bool IsReadOnly { get; }
    public bool IsEditable => !IsReadOnly && !IsModOverridden;

    public MemoryEditItemViewModel(MemoryConfig source, bool isReadOnly = false, bool isModOverridden = false)
    {
        SourceConfig = source;
        IsReadOnly = isReadOnly;
        _isModOverridden = isModOverridden;
        Name = source.Name ?? string.Empty;
        Description = source.Description ?? string.Empty;
        Category = source.Category ?? "Uncategorized";
        Type = source.Type ?? string.Empty;
        UserType = NormalizeMemoryUserType(source.UserType);
        Length = source.Length;
        OriginalValues = source.OriginalValues ?? string.Empty;
        ModdedValues = source.ModdedValues ?? string.Empty;
        _values = source.Values ?? string.Empty;
        UpdateIsCustomValue();
    }

    public MemoryConfig SourceConfig { get; }

    public bool IsModOverridden
    {
        get => _isModOverridden;
        private set
        {
            if (_isModOverridden == value)
                return;
            _isModOverridden = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditable));
        }
    }

    public void SetModOverridden(bool isModOverridden) => IsModOverridden = isModOverridden;

    public string Name { get; }
    public string Description { get; }
    public string Category { get; }
    public string Type { get; }
    public int Length { get; }
    public string OriginalValues { get; }
    public string ModdedValues { get; }

    public bool HasMultipleAddresses => GetDisplayAddresses(SourceConfig).Count > 1;

    public string AddressSummary => BuildAddressSummary(GetDisplayAddresses(SourceConfig));

    public string AddressSummaryToolTip
    {
        get
        {
            List<string> addresses = GetDisplayAddresses(SourceConfig);
            if (addresses.Count <= 1)
                return null;

            return string.Join(Environment.NewLine, addresses.Select(address => $"0x{address}"));
        }
    }

    public string Values
    {
        get => _values;
        set
        {
            if (_values == value)
                return;
            _values = value;
            SourceConfig.Values = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsModdedEnabled));
            OnPropertyChanged(nameof(IsActive));
            UpdateIsCustomValue();
        }
    }

    public bool IsActive
    {
        get
        {
            if (IsUnknownValue(Values))
                return false;

            if (IsUnknownValue(OriginalValues))
                return true;

            return !string.Equals(Values, OriginalValues, StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool IsModdedEnabled
    {
        get
        {
            if (IsUnknownValue(OriginalValues))
                return !IsUnknownValue(Values);
            return !string.Equals(Values, OriginalValues, StringComparison.OrdinalIgnoreCase);
        }
        set => Values = value ? ModdedValues : OriginalValues;
    }

    public bool IsCustomValue
    {
        get => _isCustomValue;
        private set
        {
            if (_isCustomValue == value)
                return;
            _isCustomValue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsActive));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void UpdateIsCustomValue()
    {
        IsCustomValue = IsAdjustable
            && !IsUnknownValue(Values)
            && !string.Equals(Values, OriginalValues, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Values, ModdedValues, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> GetDisplayAddresses(MemoryConfig config) =>
        GetMemoryConfigAddresses(config)
            .Select(NormalizeMemoryAddress)
            .Where(address => !string.IsNullOrEmpty(address))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string BuildAddressSummary(IReadOnlyList<string> addresses)
    {
        if (addresses.Count == 0)
            return "N/A";

        if (addresses.Count == 1)
            return $"0x{addresses[0]}";

        return $"{addresses.Count} addresses";
    }

    private static bool IsUnknownValue(string value) =>
        string.IsNullOrWhiteSpace(value) || value == "???";

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
