using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CliWrap;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using OPSSHAssistant.Core;
using OPSSHAssistant.Core.Data;

namespace OPSSHAssistant.GUI;

public partial class MainWindowModel : ObservableObject
{
    // Not bothering to make this weak, its the only page in the entire app.
    MainWindow mainWindow;
    OPManager opManager;
    PreparedExport? preparedExport;

    [ObservableProperty]
    bool stage1Selected = false;
        
    [ObservableProperty]
    bool stage1Enabled = false;
    
    [ObservableProperty]
    bool stage2Selected = false;
        
    [ObservableProperty]
    bool stage2Enabled = false;
    
    [ObservableProperty]
    bool stage3Selected = false;
        
    [ObservableProperty]
    bool stage3Enabled = false;
    
    [ObservableProperty]
    bool stage4Selected = false;
        
    [ObservableProperty]
    bool stage4Enabled = false;
    
    [ObservableProperty]
    bool stage5Selected = false;
        
    [ObservableProperty]
    bool stage5Enabled = false;

    public ObservableCollection<Account> Accounts { get; set; } = new ObservableCollection<Account>();
    public ObservableCollection<Vault> Vaults { get; set; } = new ObservableCollection<Vault>();
    public ObservableCollection<CheckboxItem> Items { get; set; } = new ObservableCollection<CheckboxItem>();

    [ObservableProperty]
    Account? selectedAccount = null;

    [ObservableProperty]
    Vault? selectedVault = null;
    
    [ObservableProperty]
    bool isLoading = false;

    [ObservableProperty]
    string loadingText = string.Empty;
    
    [ObservableProperty]
    bool isError = false;

    [ObservableProperty]
    string errorText = "An unknown error occured";

    [ObservableProperty]
    string exportPreviewText = string.Empty;

    [ObservableProperty]
    string exportSummaryText = string.Empty;
    
    public MainWindowModel(MainWindow mainWindow)
    {
        this.mainWindow = mainWindow;
        opManager = new OPManager();
    }

    public async Task InitialLoadAsync()
    {
        if (await opManager.CheckFor1PasswordCLIAsync())
        {
            var messageBox = MessageBoxManager.GetMessageBoxCustom(
                new MessageBoxCustomParams
                {
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "Yes", },
                        new ButtonDefinition { Name = "Quit", }
                    },
                    //ContentTitle = "",
                    ContentMessage = "This tool will use the 1Password CLI to list accounts, vaults, and items. You will be prompted to authorise access multiple times in this process.\n\nAre you sure you want to continue?",
                    Icon = Icon.Question,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    MaxWidth = 500,
                    MaxHeight = 800,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInCenter = true,
                    Topmost = false,
                });
            
            var messageBoxResult = await messageBox.ShowWindowDialogAsync(mainWindow);
            if (messageBoxResult == "Quit")
            {
                Environment.Exit(1);
                return;
            }

            if (Directory.Exists(opManager.GetSSHPath()) == false)
            {
                var warningMessageBox = MessageBoxManager.GetMessageBoxStandard("Warning", $"SSH directory ({opManager.GetSSHPath()}) does not exist. SSH pathing needs to be configured for public key generation to work.", ButtonEnum.Ok, Icon.Warning);
                await warningMessageBox.ShowWindowDialogAsync(mainWindow);
            }

            await GoToStage1();
        }
        else
        {
            var messageBox = MessageBoxManager.GetMessageBoxCustom(
                new MessageBoxCustomParams
                {
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "Close", }
                    },
                    ContentTitle = "Error",
                    ContentMessage = "1Password CLI could not be found. Please ensure it is installed and enabled by following the instructions here,",
                    Icon = Icon.Error,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    MaxWidth = 500,
                    MaxHeight = 800,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInCenter = true,
                    Topmost = false,
                    HyperLinkParams = new HyperLinkParams
                    {
                        Text = "https://developer.1password.com/docs/cli/get-started/",
                        Action = new Action(() =>
                        {
                            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            var url = "https://developer.1password.com/docs/cli/get-started/";
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                //https://stackoverflow.com/a/2796367/241446
                                using var proc = new Process { StartInfo = { UseShellExecute = true, FileName = url } };
                                proc.Start();
                                return;
                            }

                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                            {
                                Process.Start("x-www-browser", url);
                                return;
                            }

                            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                            {
                                Process.Start("open", url);
                                return;
                            }
                        })
                    }
                });
            
            await messageBox.ShowWindowDialogAsync(mainWindow);
            Environment.Exit(1);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedAccount))
        {
            SelectedVault = null;

            if (SelectedAccount is null)
            {
                // ???
            }
            else
            {
                GoToStage2().SafeFireAndForget();
            }
        }
        else if (e.PropertyName == nameof(SelectedVault))
        {
            if (SelectedVault is null)
            {
                // ???
            }
            else
            {
                GoToStage3().SafeFireAndForget();
            }
        }
    }


    void UpdateStageButtons(int stageNumber)
    {
        Stage5Enabled = (stageNumber >= 5);
        Stage5Selected = (stageNumber == 5);

        Stage4Enabled = (stageNumber >= 4);
        Stage4Selected = (stageNumber == 4);

        Stage3Enabled = (stageNumber >= 3);
        Stage3Selected = (stageNumber == 3);

        Stage2Enabled = (stageNumber >= 2);
        Stage2Selected = (stageNumber == 2);

        Stage1Enabled = (stageNumber >= 1);
        Stage1Selected = (stageNumber == 1);
    }

    void ResetError()
    {
        IsError = false;
        ErrorText = string.Empty;
    }
    
    async Task GoToStage1()
    {
        UpdateStageButtons(1);
        
        Accounts.Clear();
        Vaults.Clear();

        SelectedAccount = null;
        SelectedVault = null;

        ResetError();
        LoadingText = "Loading accounts...";
        IsLoading = true;
        var loadedAccounts = await opManager.LoadAccountsAsync();
        //AccountComboBoxPlaceholder = "Select an account";
        IsLoading = false;
        
        if (loadedAccounts is null || loadedAccounts.Count == 0)
        {
            var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not list accounts.\n{opManager.LastError}", ButtonEnum.Ok, Icon.Error);
            await errorMessageBox.ShowWindowDialogAsync(mainWindow);
            
            IsError = true;
            ErrorText = "Could not list accounts";
            
            return;
        }

        foreach (var account in loadedAccounts)
        {
            Accounts.Add(account);
        }
    }
    
    async Task GoToStage2()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        UpdateStageButtons(2);

        Vaults.Clear();
        
        
        ResetError();
        LoadingText = "Loading vaults...";
        IsLoading = true;
        var loadedVaults = await opManager.LoadVaultsAsync(SelectedAccount);
        IsLoading = false;
        
        if (loadedVaults is null || loadedVaults.Count == 0)
        {
            var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not list vaults.\n{opManager.LastError}", ButtonEnum.Ok, Icon.Error);
            await errorMessageBox.ShowWindowDialogAsync(mainWindow);
            
            IsError = true;
            ErrorText = "No vaults found";
            
            return;
        }

        foreach (var vault in loadedVaults)
        {
            Vaults.Add(vault);
        }
    }
    
    async Task GoToStage3()
    {
        if (SelectedVault is null || SelectedAccount is null)
        {
            return;
        }
        
        UpdateStageButtons(3);
        
        
        ResetError();
        //mainWindow.ItemsStackPanel.Opacity = 1;
        LoadingText = "Loading items...";
        IsLoading = true;
        Items.Clear();
        var loadedItems = await opManager.LoadItemsAsync(SelectedAccount, SelectedVault);
        IsLoading = false;

        if (loadedItems is null)
        {
            var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not list items.\n{opManager.LastError}", ButtonEnum.Ok, Icon.Error);
            await errorMessageBox.ShowWindowDialogAsync(mainWindow);

            ErrorText = "Could not list items";
            IsError = true;
            
            return;
        }

        if (loadedItems.Any() == false)
        {
            ErrorText = "No SSH keys found in vault. Please select a different vault";
            IsError = true;
            return;
        }

        foreach (var loadedItem in loadedItems)
        {
            Items.Add(new CheckboxItem(loadedItem));
        }
    }
    
    async Task GoToStage4()
    {
        if (SelectedAccount is null || SelectedVault is null)
        {
            return;
        }
        
        var selectedItemObjects = new List<Item>();
        foreach (var item in Items)
        {
            if (item.IsChecked)
            {
                selectedItemObjects.Add(item.Item);
            }
        }

        if (selectedItemObjects.Count == 0)
        {
            return;
        }
        
        UpdateStageButtons(4);

        ExportPreviewText = string.Empty;

        ResetError();
        LoadingText = "Generating output...";
        IsLoading = true;
        preparedExport = null;
        preparedExport = await opManager.PrepareExportAsync(SelectedAccount, SelectedVault, selectedItemObjects);
        
        if (preparedExport.Success == false)
        {
            IsLoading = false;
            var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"{preparedExport.ErrorMessage}.\n{preparedExport.ErrorMessageDetails}.", ButtonEnum.Ok, Icon.Error);
            await errorMessageBox.ShowWindowDialogAsync(mainWindow);

            IsError = true;
            ErrorText = preparedExport.ErrorMessage;
            return;
        }
        
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("IMPORTANT: 1Password SSH Assistant will append the below configs even if the new changes are already present in their respective config files. If you run an export on the same items multiple times you may need to manually repair your config files.");
        stringBuilder.AppendLine();
        if (preparedExport.PublicKeysToExport.Any())
        {
            stringBuilder.AppendLine("The following public keys need to be exported:");
            foreach (var selectedItemObject in preparedExport.PublicKeysToExport)
            {
                stringBuilder.AppendLine($"- {selectedItemObject.Title} as {Path.GetFileName(selectedItemObject.PublicKeyPath)}");
            }
            stringBuilder.AppendLine();
        }
        else
        {
            stringBuilder.AppendLine("No public keys need to be exported.");
        }

        stringBuilder.AppendLine("\n");

        if (preparedExport.SSHConfigToBeCreated)
        {
            stringBuilder.AppendLine("The following config will be created:");
        }
        else
        {
            stringBuilder.AppendLine("The following config will be appended to:");
        }
        stringBuilder.AppendLine(opManager.GetSSHConfigPath());
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("IMPORTANT: Update UPDATE_HOST_NAME_HERE to be the host name you want to connect to for that ssh key.");
        stringBuilder.AppendLine("IMPORTANT: Update UPDATE_USERNAME_HERE to be the username you want to connect with for that ssh key.");
        stringBuilder.AppendLine("\n");
        stringBuilder.AppendLine(preparedExport.SSHConfigToAppend);
        stringBuilder.AppendLine("\n");

        if (preparedExport.AgentTomlToBeCreated)
        {
            stringBuilder.AppendLine("The following config will be created:");
        }
        else
        {
            stringBuilder.AppendLine("The following config will be appended to:");
        }
        stringBuilder.AppendLine(opManager.GetAgentTomlPath());
        stringBuilder.AppendLine("\n");
        stringBuilder.AppendLine(preparedExport.AgentTomlToAppend);
        
        IsLoading = false;
        ExportPreviewText = stringBuilder.ToString();
    }
    
    async Task GoToStage5()
    {
        if (preparedExport is null)
        {
            return;
        }
        
        if (preparedExport.Success == false)
        {
            return;
        }
        
        UpdateStageButtons(5);
        ExportSummaryText = string.Empty;
        ResetError();
        LoadingText = "Exporting...";
        IsLoading = true;
        var exportResult = await opManager.PerformExportAsync(preparedExport);

        var stringBuilder = new StringBuilder();
        
        if (exportResult.PublicKeyGenerationSuccess)
        {
            stringBuilder.AppendLine("Public keys exported successfully.");
        }
        else
        {
            stringBuilder.AppendLine("Public keys failed to export.");
            stringBuilder.AppendLine(exportResult.PublicKeyGenerationSummary);
        }
        
        stringBuilder.AppendLine();
        
        if (exportResult.AppendSSHConfigSuccess)
        {
            stringBuilder.AppendLine("SSH config appended successfully.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("IMPORTANT: Update UPDATE_HOST_NAME_HERE to be the host name you want to connect to for that ssh key.");
            stringBuilder.AppendLine("IMPORTANT: Update UPDATE_USERNAME_HERE to be the username you want to connect with for that ssh key.");
            stringBuilder.AppendLine();
        }
        else
        {
            stringBuilder.AppendLine("SSH config failed to append.");
            stringBuilder.AppendLine(exportResult.SSHConfigSummary);
        }
        
        stringBuilder.AppendLine();
        
        if (exportResult.AppendAgentTomlSuccess)
        {
            stringBuilder.AppendLine("Agent config appended successfully.");
        }
        else
        {
            stringBuilder.AppendLine("Agent config failed to append.");
            stringBuilder.AppendLine(exportResult.AgentTomlSummary);
        }
        
        ExportSummaryText = stringBuilder.ToString();
        
        IsLoading = false;
    }
    
    

    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task GoToPreviewAsync()
    { 
        if (SelectedAccount is null || SelectedVault is null)
        {
            return;
        }
        
        var selectedItemObjects = new List<Item>();
        foreach (var item in Items)
        {
            if (item.IsChecked)
            {
                selectedItemObjects.Add(item.Item);
            }
        }

        if (selectedItemObjects.Count == 0)
        {
            var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Please select one or more SSH keys to continue.", ButtonEnum.Ok, Icon.Error);
            await errorMessageBox.ShowWindowDialogAsync(mainWindow);
            return;
        }

        await GoToStage4();
    }
    
    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task ExportAsync()
    {
        await GoToStage5();
    }
    
    
    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task MenuSelectAccountClickedAsync()
    {
        if (IsLoading)
        {
            return;
        }

        var messageBox = MessageBoxManager.GetMessageBoxStandard("Discard changes?", $"Are you sure you want to discard changes and go back to select account?", ButtonEnum.YesNo, Icon.Question);
        var result = await messageBox.ShowWindowDialogAsync(mainWindow);
        if (result == ButtonResult.Yes)
        {
            await GoToStage1();
        }
    }
    
    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task MenuSelectVaultClickedAsync()
    {
        if (IsLoading)
        {
            return;
        }

        var messageBox = MessageBoxManager.GetMessageBoxStandard("Discard changes?", $"Are you sure you want to discard changes and go back to select vault?", ButtonEnum.YesNo, Icon.Question);
        var result = await messageBox.ShowWindowDialogAsync(mainWindow);
        if (result == ButtonResult.Yes)
        {
            await GoToStage2();
        }
    }
    
    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task MenuSelectItemsClickedAsync()
    {
        if (IsLoading)
        {
            return;
        }

        var messageBox = MessageBoxManager.GetMessageBoxStandard("Discard changes?", $"Are you sure you want to discard changes and go back to select SSH keys?", ButtonEnum.YesNo, Icon.Question);
        var result = await messageBox.ShowWindowDialogAsync(mainWindow);
        if (result == ButtonResult.Yes)
        {
            await GoToStage3();
        }
    }
    
    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task MenuPreviewChangesClickedAsync()
    {
        if (IsLoading)
        {
            return;
        }
        
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Discard changes?", $"Are you sure you want to discard changes and go back to preview output?", ButtonEnum.YesNo, Icon.Question);
        var result = await messageBox.ShowWindowDialogAsync(mainWindow);
        if (result == ButtonResult.Yes)
        {
            await GoToStage4();
        }
    }
    
  }