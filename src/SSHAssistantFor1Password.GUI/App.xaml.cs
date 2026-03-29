using System.Diagnostics;
using SSHAssistantFor1Password.Core;

namespace SSHAssistantFor1Password.GUI;

public partial class App : Application
{
    static OPManager? _opManager = null;
    public static OPManager OPManager => _opManager ??= new OPManager();

    public App()
    {
        InitializeComponent();
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
