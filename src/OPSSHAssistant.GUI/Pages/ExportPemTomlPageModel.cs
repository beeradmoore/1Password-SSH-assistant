using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPSSHAssistant.Core;

namespace OPSSHAssistant.GUI.Pages;

public partial class ExportPemTomlPageModel : ObservableObject
{
    WeakReference<ExportPemTomlPage> _page;
    PreparedExport _preparedExport;
    
    [ObservableProperty]
    bool _isLoading = false;
    
    [ObservableProperty]
    bool _isError = false;

    [ObservableProperty]
    string _errorText = "An unknown error occured";

    [ObservableProperty]
    bool _goToStartPageEnabled = false;
    
    [ObservableProperty]
    string _publicKeyExportResultText = string.Empty;

    [ObservableProperty]
    bool _publicKeyExportSuccess = false;

    [ObservableProperty]
    string _sshConfigResultText = string.Empty;

    [ObservableProperty]
    bool _sshConfigSuccess = false;

    [ObservableProperty]
    string _sshAgentConfigResultText = string.Empty;

    [ObservableProperty]
    bool _sshAgentConfigSuccess = false;
    
    public ExportPemTomlPageModel(ExportPemTomlPage page, PreparedExport preparedExport)
    {
        _page = new WeakReference<ExportPemTomlPage>(page);
        _preparedExport = preparedExport;
    }

    public async Task ExportDataAsync()
    {
        var exportResult = await App.OPManager.PerformExportAsync(_preparedExport);
        
        if (exportResult.PublicKeyGenerationSuccess)
        {
            PublicKeyExportResultText = "Public keys exported successfully.";
            PublicKeyExportSuccess = true;
        }
        else
        {
            PublicKeyExportResultText = $"Public keys failed to export.\n{exportResult.PublicKeyGenerationSummary}";
        }
        
        if (exportResult.AppendSSHConfigSuccess)
        {
            SshConfigResultText = "SSH config appended successfully.";
            SshConfigSuccess = true;
        }
        else
        {
            SshConfigResultText = $"SSH config failed to append.\n{exportResult.SSHConfigSummary}";
        }
        
        
        if (exportResult.AppendAgentTomlSuccess)
        {
            SshAgentConfigResultText = "Agent config appended successfully.";
            SshAgentConfigSuccess = true;
        }
        else
        {
            SshAgentConfigResultText = $"Agent config failed to append.\n{exportResult.AgentTomlSummary}";
        }

        IsLoading = false;
        GoToStartPageEnabled = true;
    }

    [RelayCommand]
    async Task GoToStartPageAsync()
    {
        if (_page.TryGetTarget(out ExportPemTomlPage? page))
        {
            await page.Dispatcher.DispatchAsync(async () =>
            {
                await page.Navigation.PopToRootAsync();
            });
        }
    }
}