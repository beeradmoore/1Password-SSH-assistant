using System;
using AsyncAwaitBestPractices;
using Avalonia.Controls;

namespace OPSSHAssistant.GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowModel(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        if (DataContext is MainWindowModel model)
        {
            model.InitialLoadAsync().SafeFireAndForget();
        }
    }
}