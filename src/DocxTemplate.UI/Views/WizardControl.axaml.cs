using Avalonia.Controls;
using Avalonia.Input;
using System.Reactive.Linq;

namespace DocxTemplate.UI.Views;

public partial class WizardControl : UserControl
{
    public WizardControl()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ViewModels.WizardViewModel viewModel)
        {
            if (viewModel.NextCommand.CanExecute.FirstAsync().Wait())
            {
                viewModel.NextCommand.Execute().Subscribe();
            }
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}