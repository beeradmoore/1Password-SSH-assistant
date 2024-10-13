using System;
using AsyncAwaitBestPractices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OPSSHAssistant.GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowModel(this);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        KeyboardNavigation.SetTabNavigation(SelectedItemsRepeater, KeyboardNavigationMode.Continue);

    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        if (DataContext is MainWindowModel model)
        {
            model.InitialLoadAsync().SafeFireAndForget();
        }
    }

    void SetDetails_TextBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (textBox.Text?.StartsWith("UPDATE_", StringComparison.Ordinal) == true)
            {
                textBox.SelectAll();
            }
        }
    }
}