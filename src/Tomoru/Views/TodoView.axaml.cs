using Avalonia.Controls;
using Avalonia.Interactivity;
using Tomoru.ViewModels;

namespace Tomoru.Views;

public partial class TodoView : UserControl
{
    public TodoView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TodoItemViewModel row } &&
            DataContext is TodoViewModel vm)
        {
            vm.BeginEdit(row);
        }
    }
}
