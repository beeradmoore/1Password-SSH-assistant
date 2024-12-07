using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class MainPageModel : ObservableObject
{
    readonly WeakReference<MainPage> _page;

    public List<MenuOption> MenuOptions { get; set; } = new List<MenuOption>
    {
        new MenuOption(MenuMode.ExportPPK),
        new MenuOption(MenuMode.ExportPubAppendSSHConfigAndAgentToml),
    };

    [ObservableProperty]
    MenuOption? _selectedItem = null;
    
    public MainPageModel(MainPage page)
    {
        _page = new WeakReference<MainPage>(page);
    }
    
    internal async Task CheckFor1PasswordAsync()
    {
        if (_page.TryGetTarget(out MainPage? mainPage))
        {
            if (await App.OPManager.CheckFor1PasswordCLIAsync())
            {
                await mainPage.Dispatcher.DispatchAsync(async () =>
                {
                    var alertResponse = await mainPage.DisplayAlert(string.Empty, "This tool will use the 1Password CLI to list accounts, vaults, and items. You will be prompted to authorise access multiple times in this process.\n\nAre you sure you want to continue?", "Yes", "Quit");
                    if (alertResponse == false)
                    {
                        Environment.Exit(1);
                        return;
                    }

                    if (Directory.Exists(App.OPManager.GetSSHPath()) == false)
                    {
                        await mainPage.DisplayAlert("Warning", $"SSH directory ({App.OPManager.GetSSHPath()}) does not exist. SSH pathing needs to be configured for public key generation to work.", "Ok");
                    }

                    //await GoToStage1();
                });
            }
            else
            {
                await mainPage.Dispatcher.DispatchAsync(async () =>
                {
                    var alertResponse = await mainPage.DisplayAlert("Error", "1Password CLI could not be found. Please ensure it is installed and enabled by following the instructions here,", "Get help", "Close");
                    if (alertResponse)
                    {
                        await Launcher.OpenAsync("https://developer.1password.com/docs/cli/get-started/");
                    }

                    Environment.Exit(1);
                });
            }
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedItem))
        {
            if (SelectedItem is null)
            {
                return;
            }

            var newItem = SelectedItem;
            SelectedItem = null;
            
            if (newItem.Mode == MenuMode.ExportPPK || newItem.Mode == MenuMode.ExportPubAppendSSHConfigAndAgentToml)
            {
                if (_page.TryGetTarget(out MainPage? mainPage))
                {
                    mainPage.Navigation.PushAsync(new SelectAccountPage(newItem.Mode));
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}