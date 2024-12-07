using System.Diagnostics;
using OPSSHAssistant.Core;

namespace OPSSHAssistant.GUI;

public partial class App : Application
{
    static OPManager? _opManager = null;
    public static OPManager OPManager => _opManager ??= new OPManager();
    
    public App()
    {
        InitializeComponent();
        
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MyCustomization", (handler, view) =>
        {
#if MACCATALYST
#pragma warning disable CA1416
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#pragma warning restore CA1416
#endif
        });

    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (this.Windows.Count > 0)
        {
            Debugger.Break();
        }
        return new Window(new NavigationPage(new Pages.MainPage()));
    }
}