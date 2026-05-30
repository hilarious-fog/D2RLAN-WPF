using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using JetBrains.Annotations;
using static D2RLAN.ViewModels.ShellViewModel;

namespace D2RLAN.ViewModels.Dialogs;

public class CreateMemoryEditViewModel : Screen
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _category = "Alteration";
    private string _address = string.Empty;
    private string _addressesText = string.Empty;
    private bool _useMultipleAddresses;
    private int _length = 1;
    private string _type = "Hex";
    private string _userType = "Boolean";
    private string _values = string.Empty;
    private string _originalValues = string.Empty;
    private string _moddedValues = string.Empty;
    private string _validationMessage = string.Empty;

    public CreateMemoryEditViewModel()
    {
        DisplayName = "New Memory Edit";
    }

    public MemoryConfig CreatedConfig { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;
            _name = value;
            NotifyOfPropertyChange();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value)
                return;
            _description = value;
            NotifyOfPropertyChange();
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            if (_category == value)
                return;
            _category = value;
            NotifyOfPropertyChange();
        }
    }

    public string Address
    {
        get => _address;
        set
        {
            if (_address == value)
                return;
            _address = value;
            NotifyOfPropertyChange();
        }
    }

    public string AddressesText
    {
        get => _addressesText;
        set
        {
            if (_addressesText == value)
                return;
            _addressesText = value;
            NotifyOfPropertyChange();
        }
    }

    public bool UseMultipleAddresses
    {
        get => _useMultipleAddresses;
        set
        {
            if (_useMultipleAddresses == value)
                return;
            _useMultipleAddresses = value;
            NotifyOfPropertyChange();
        }
    }

    public int Length
    {
        get => _length;
        set
        {
            if (_length == value)
                return;
            _length = value;
            NotifyOfPropertyChange();
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;
            _type = value;
            NotifyOfPropertyChange();
        }
    }

    public string UserType
    {
        get => _userType;
        set
        {
            if (_userType == value)
                return;
            _userType = value;
            NotifyOfPropertyChange();
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
            NotifyOfPropertyChange();
        }
    }

    public string OriginalValues
    {
        get => _originalValues;
        set
        {
            if (_originalValues == value)
                return;
            _originalValues = value;
            NotifyOfPropertyChange();
        }
    }

    public string ModdedValues
    {
        get => _moddedValues;
        set
        {
            if (_moddedValues == value)
                return;
            _moddedValues = value;
            NotifyOfPropertyChange();
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage == value)
                return;
            _validationMessage = value;
            NotifyOfPropertyChange();
        }
    }

    public static IReadOnlyList<string> DefaultCategories { get; } = new[]
    {
        "Important",
        "Enhancement",
        "Alteration",
        "Optional Fix",
        "Uncategorized"
    };

    public IReadOnlyList<string> Categories { get; private set; } = DefaultCategories;

    public void SetCategories(IEnumerable<string> categories)
    {
        List<string> list = categories?
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        Categories = list.Count > 0 ? list : DefaultCategories;
        Category = Categories.Contains(Category, StringComparer.OrdinalIgnoreCase)
            ? Category
            : Categories[0];
        NotifyOfPropertyChange(nameof(Categories));
        NotifyOfPropertyChange(nameof(Category));
    }

    public IReadOnlyList<string> Types { get; } = new[] { "Hex", "Integer" };

    public IReadOnlyList<string> UserTypes { get; } = new[] { "Boolean", "Adjustable" };

    [UsedImplicitly]
    public async void OnSave()
    {
        if (!TryBuildConfig(out MemoryConfig config, out string error))
        {
            ValidationMessage = error;
            return;
        }

        CreatedConfig = config;
        await CloseDialogAsync(true);
    }

    [UsedImplicitly]
    public async void OnCancel() => await CloseDialogAsync(false);

    private async Task CloseDialogAsync(bool accepted)
    {
        if (GetView() is Window window)
        {
            window.DialogResult = accepted;
            window.Close();
            return;
        }

        await TryCloseAsync(accepted);
    }

    public bool TryBuildConfig(out MemoryConfig config, out string error)
    {
        config = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Name is required.";
            return false;
        }

        List<string> addresses = ParseAddresses();
        if (addresses.Count == 0)
        {
            error = "At least one valid address is required.";
            return false;
        }

        if (Length <= 0)
        {
            error = "Length must be greater than zero.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Type))
        {
            error = "Type is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Values))
        {
            error = "Current value is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OriginalValues))
        {
            error = "Original value is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ModdedValues))
        {
            error = "Modded value is required.";
            return false;
        }

        config = new MemoryConfig
        {
            Name = Name.Trim(),
            Description = Description?.Trim() ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(Category) ? "Alteration" : Category.Trim(),
            Length = Length,
            Type = Type.Trim(),
            UserType = NormalizeMemoryUserType(UserType),
            Values = Values.Trim(),
            OriginalValues = OriginalValues.Trim(),
            ModdedValues = ModdedValues.Trim()
        };

        if (UseMultipleAddresses)
        {
            config.Addresses = addresses;
            config.Address = null;
        }
        else
        {
            if (addresses.Count > 1)
            {
                error = "Only one address is allowed unless multiple addresses is enabled.";
                config = null;
                return false;
            }

            config.Address = addresses[0];
            config.Addresses = null;
        }

        return true;
    }

    private List<string> ParseAddresses()
    {
        string raw = UseMultipleAddresses ? AddressesText : Address;
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        char[] separators = { '\r', '\n', ',', ';', ' ' };
        return raw
            .Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeMemoryAddress)
            .Where(address => !string.IsNullOrEmpty(address) && Regex.IsMatch(address, @"^[0-9A-Fa-f]+$"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
