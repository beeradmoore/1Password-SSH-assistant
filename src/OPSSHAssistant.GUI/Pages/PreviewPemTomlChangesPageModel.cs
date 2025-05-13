using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPSSHAssistant.Core;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class PreviewPemTomlChangesPageModel : ObservableObject
{
    readonly WeakReference<PreviewPemTomlChangesPage> _page;
    readonly MenuMode _mode;
    readonly Account _account;
    readonly Vault _vault;
    readonly List<Item> _items;
    PreparedExport? _preparedExport;
    
    [ObservableProperty]
    bool _isLoading = false;
    
    [ObservableProperty]
    bool _isError = false;

    [ObservableProperty]
    string _errorText = "An unknown error occured";

    [ObservableProperty]
    string _publicKeyExportText = string.Empty;

    [ObservableProperty]
    string _sshConfigText = string.Empty;

    [ObservableProperty]
    string _sshAgentConfigText = string.Empty;
    
    [ObservableProperty]
    bool _goToNextPageEnabled = false; 
    
    public PreviewPemTomlChangesPageModel(PreviewPemTomlChangesPage page, MenuMode mode, Account account, Vault vault, List<Item> items)
    {
        _page = new WeakReference<PreviewPemTomlChangesPage>(page);
        _mode = mode;
        _account = account;
        _vault = vault;
        _items = items;
    }

    public async Task GenerateOutputAsync()
    {
        IsLoading = true;
        
        _preparedExport = await App.OPManager.PrepareExportAsync(_account, _vault, _items);
        
        if (_preparedExport.Success == false)
        {
            IsLoading = false;
            
            if (_page.TryGetTarget(out PreviewPemTomlChangesPage? page))
            {
                await page.Dispatcher.DispatchAsync(async () =>
                {
                    await page.DisplayAlert("Error", $"{_preparedExport.ErrorMessage}.\n{_preparedExport.ErrorMessageDetails}.", "Okay");
                });
            }

            IsError = true;
            ErrorText = _preparedExport.ErrorMessage;
            return;
        }
        
        var publicKeysExportStringBuilder = new StringBuilder();
        var sshConfigStringBuilder = new StringBuilder();
        var sshAgentConfigStringBuilder = new StringBuilder();
        //stringBuilder.AppendLine("IMPORTANT: 1Password SSH Assistant will append the below configs even if the new changes are already present in their respective config files. If you run an export on the same items multiple times you may need to manually repair your config files.");

        if (_preparedExport.PublicKeysToExport.Any())
        {
            publicKeysExportStringBuilder.AppendLine("The following public keys need to be exported:");
            foreach (var selectedItemObject in _preparedExport.PublicKeysToExport)
            {
                publicKeysExportStringBuilder.AppendLine($"- {selectedItemObject.Title} as {Path.GetFileName(selectedItemObject.PublicKeyPath)}");
            }
        }
        else
        {
            publicKeysExportStringBuilder.AppendLine("No public keys need to be exported.");
        }
        
        
        
        if (_preparedExport.SSHConfigToBeCreated)
        {
            sshConfigStringBuilder.AppendLine("The following config will be created:");
        }
        else
        {
            sshConfigStringBuilder.AppendLine("The following config will be appended to:");
        }
        sshConfigStringBuilder.AppendLine(App.OPManager.GetSSHConfigPath());
        sshConfigStringBuilder.AppendLine("\n");
        sshConfigStringBuilder.AppendLine(_preparedExport.SSHConfigToAppend);
        sshConfigStringBuilder.AppendLine("\n");

        if (_preparedExport.AgentTomlToBeCreated)
        {
            sshAgentConfigStringBuilder.AppendLine("The following config will be created:");
        }
        else
        {
            sshAgentConfigStringBuilder.AppendLine("The following config will be appended to:");
        }
        sshAgentConfigStringBuilder.AppendLine(App.OPManager.GetAgentTomlPath());
        sshAgentConfigStringBuilder.AppendLine("\n");
        sshAgentConfigStringBuilder.AppendLine(_preparedExport.AgentTomlToAppend);

        PublicKeyExportText = publicKeysExportStringBuilder.ToString().Trim();
        SshConfigText = sshConfigStringBuilder.ToString().Trim();
        SshAgentConfigText = sshAgentConfigStringBuilder.ToString().Trim();

        IsLoading = false;
        GoToNextPageEnabled = true;
    }

    [RelayCommand]
    async Task GoToNextPageAsync()
    {
        if (_preparedExport is not null)
        {
            if (_page.TryGetTarget(out PreviewPemTomlChangesPage? page))
            {
                await page.Dispatcher.DispatchAsync(async () => { await page.Navigation.PushAsync(new ExportPemTomlPage(_preparedExport)); });
            }
        }
    }
}