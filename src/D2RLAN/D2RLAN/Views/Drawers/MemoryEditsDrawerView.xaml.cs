using System.Windows;
using System.Windows.Controls;
using D2RLAN.ViewModels.Drawers;

namespace D2RLAN.Views.Drawers;

public partial class MemoryEditsDrawerView : UserControl
{
    public MemoryEditsDrawerView()
    {
        InitializeComponent();
        Loaded += MemoryEditsDrawerView_Loaded;
    }

    private async void MemoryEditsDrawerView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MemoryEditsDrawerView_Loaded;

        if (DataContext is MemoryEditsDrawerViewModel vm)
            await vm.Initialize();
    }

    private void AddToModOverridesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MemoryEditItemViewModel item })
            return;

        if (DataContext is MemoryEditsDrawerViewModel vm)
            vm.AddToModOverrides(item);
    }
}
