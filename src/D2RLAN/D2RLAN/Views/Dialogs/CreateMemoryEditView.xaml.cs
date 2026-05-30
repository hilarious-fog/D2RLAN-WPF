using System.Windows;

namespace D2RLAN.Views.Dialogs;

public partial class CreateMemoryEditView : Window
{
    public CreateMemoryEditView()
    {
        InitializeComponent();
        Loaded += CreateMemoryEditView_Loaded;
    }

    private void CreateMemoryEditView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Owner == null)
            return;

        Left = Owner.Left + (Owner.Width - Width) / 2;
        Top = Owner.Top + (Owner.Height - Height) / 2;
    }
}
