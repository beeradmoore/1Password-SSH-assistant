using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
    WeakReference<MainWindow> weakMainWindow;
    OPManager opManager;

    public ObservableCollection<Account> Accounts { get; set; } = new ObservableCollection<Account>();
    public ObservableCollection<Vault> Vaults { get; set; } = new ObservableCollection<Vault>();
    public ObservableCollection<CheckboxItem> Items { get; set; } = new ObservableCollection<CheckboxItem>();

    [ObservableProperty]
    Account? selectedAccount = null;

    [ObservableProperty]
    Vault? selectedVault = null;

    [ObservableProperty]
    string accountComboBoxPlaceholder = string.Empty;

    [ObservableProperty]
    string vaultComboBoxPlaceholder = string.Empty;

    [ObservableProperty]
    bool isLoadingItems = false;
    
    public MainWindowModel(MainWindow mainWindow)
    {
        weakMainWindow = new WeakReference<MainWindow>(mainWindow);
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
                    ContentMessage = "This tool will use the 1Password CLI to list accounts, vaults, and items.\nYou will be prompted to authorise access multiple times in this process.\n\nAre you sure you want to continue?",
                    Icon = Icon.Question,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    MaxWidth = 500,
                    MaxHeight = 800,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInCenter = true,
                    Topmost = false,
                });
            if (weakMainWindow.TryGetTarget(out MainWindow? mainWindow))
            {
                var messageBoxResult = await messageBox.ShowWindowDialogAsync(mainWindow);
                if (messageBoxResult == "Quit")
                {
                    Environment.Exit(1);
                    return;
                }

                if (opManager.SSHConfigPathExists == false)
                {
                    var warningMessageBox = MessageBoxManager.GetMessageBoxStandard("Warning", $"SSH directory ({opManager.SSHConfigPath}) does not exist. SSH pathing needs to be configured for public key generation to work.", ButtonEnum.Ok, Icon.Warning);
                    await warningMessageBox.ShowWindowDialogAsync(mainWindow);
                }

                AccountComboBoxPlaceholder = "Loading accounts...";
                mainWindow.IsEnabled = false;
                var loadedAccounts = await opManager.LoadAccountsAsync();
                mainWindow.IsEnabled = true;
                AccountComboBoxPlaceholder = "Select an account";
                
                if (loadedAccounts is null || loadedAccounts.Count == 0)
                {
                    var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not list accounts.\n{opManager.LastError}", ButtonEnum.Ok, Icon.Error);
                    await errorMessageBox.ShowWindowDialogAsync(mainWindow);
                    return;
                }
                
                foreach (var account in loadedAccounts)
                {
                    Accounts.Add(account);
                }
            }
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
            if (weakMainWindow.TryGetTarget(out MainWindow? mainWindow))
            {
                await messageBox.ShowWindowDialogAsync(mainWindow);
                Environment.Exit(1);
            }
        }
    }

    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedAccount))
        {
            SelectedVault = null;
            Vaults.Clear();
            
            if (SelectedAccount is not null)
            {
                if (weakMainWindow.TryGetTarget(out MainWindow? mainWindow))
                {
                    VaultComboBoxPlaceholder = "Loading vaults...";
                    mainWindow.IsEnabled = false;
                    var loadedVaults = await opManager.LoadVaultsAsync(SelectedAccount);
                    mainWindow.IsEnabled = true;
                    VaultComboBoxPlaceholder = "Select a vault";
                
                    if (loadedVaults is null || loadedVaults.Count == 0)
                    {
                        var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not list vaults.\n{opManager.LastError}", ButtonEnum.Ok, Icon.Error);
                        await errorMessageBox.ShowWindowDialogAsync(mainWindow);
                        return;
                    }

                    foreach (var vault in loadedVaults)
                    {
                        Vaults.Add(vault);
                    }
                }
            }
        }
        else if (e.PropertyName == nameof(SelectedVault))
        {
            if (SelectedVault is null)
            {
                Items.Clear();
            }
            else
            {
                if (SelectedAccount is null)
                {
                    return;
                }
                
                if (weakMainWindow.TryGetTarget(out MainWindow? mainWindow))
                {
                    mainWindow.IsEnabled = false;
                    IsLoadingItems = true;
                    var loadedItems = await opManager.LoadItemsAsync(SelectedAccount, SelectedVault);
                    IsLoadingItems = false;
                    mainWindow.IsEnabled = true;

                    if (loadedItems is null)
                    {
                        var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not list items.\n{opManager.LastError}", ButtonEnum.Ok, Icon.Error);
                        await errorMessageBox.ShowWindowDialogAsync(mainWindow);
                        return;
                    }

                    if (loadedItems.Count == 0)
                    {
                        var errorMessageBox = MessageBoxManager.GetMessageBoxStandard("Error", $"No SSH keys found.", ButtonEnum.Ok, Icon.Error);
                        await errorMessageBox.ShowWindowDialogAsync(mainWindow);
                        return;
                    }
                    
                    Items.Clear();

                    foreach (var loadedItem in loadedItems)
                    {
                        Items.Add(new CheckboxItem(loadedItem));
                    }
                    
                    
                }
            }
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task GenerateAsync()
    {
        if (weakMainWindow.TryGetTarget(out MainWindow? mainWindow))
        {
            var selectedItemObjects = new List<Item>();
            foreach (var item in Items)
            {
                if (item.IsChecked)
                {
                    selectedItemObjects.Add(item.Item);
                }
            }
            var anyPublicKeysNeedExport = await opManager.LoadPublicKeysToExportAsync(SelectedAccount, SelectedVault, selectedItemObjects);

            if (anyPublicKeysNeedExport is null)
            {
                //AnsiConsole.MarkupLine("[red]Error: Could determine public keys to export.[/]");
                //AnsiConsole.WriteLine(opManager.LastError);
                //Environment.Exit(1);
            }

            if (anyPublicKeysNeedExport == true)
            {
                var publicKeystoExportStringBuilder = new StringBuilder();
                
                foreach (var selectedItemObject in selectedItemObjects)
                {
                    if (selectedItemObject.NeedsExport)
                    {
                        publicKeystoExportStringBuilder.AppendLine($"{selectedItemObject.Title} as {Path.GetFileName(selectedItemObject.PublicKeyPath)}");
                    }
                }
                
                
                var exportPublicKeysMessageBox = MessageBoxManager.GetMessageBoxStandard("Export public keys?", publicKeystoExportStringBuilder.ToString(), ButtonEnum.YesNo, Icon.Question);
                var exportPubilcKeysResponse = await exportPublicKeysMessageBox.ShowWindowDialogAsync(mainWindow);

                if (exportPubilcKeysResponse == ButtonResult.Yes)
                {
                    foreach (var selectedItemObject in selectedItemObjects)
                    {
                        if (selectedItemObject.NeedsExport)
                        {
                            try
                            {
                                File.WriteAllText(selectedItemObject.PublicKeyPath, selectedItemObject.PublicKey);
                            }
                            catch (Exception err)
                            {
                                Debugger.Break();
                                //AnsiConsole.MarkupLine($"[red]Error: Could not export {selectedItemObject.PublicKeyPath}. ({err.Message})[/]");
                            }
                        }
                    }
                }
            }
            else
            {
                //AnsiConsole.MarkupLine($"[green]No public keys needed exporting. Skipping.[/]");
            }
            
            
            var sshConfig = opManager.GenerateUpdatedSSHConfig(selectedAccount, selectedVault, selectedItemObjects);

            var outputStringBuilder = new StringBuilder();
            outputStringBuilder.AppendLine("\n\n");
            outputStringBuilder.AppendLine("The following config needs to appended to:");
            outputStringBuilder.AppendLine(opManager.GetSSHConfigPath());
            outputStringBuilder.AppendLine();
            outputStringBuilder.AppendLine("IMPORTANT: Update UPDATE_HOST_NAME_HERE to be the host name you want to connect to for that ssh key.");
            outputStringBuilder.AppendLine("IMPORTANT: Update UPDATE_USERNAME_HERE to be the username you want to connect with for that ssh key.");
            outputStringBuilder.AppendLine("\n");
            outputStringBuilder.AppendLine(sshConfig);
            outputStringBuilder.AppendLine("\n\n");

            
            var agentToml = opManager.GenerateUpdatedAgentToml(selectedAccount, selectedVault, selectedItemObjects);

            outputStringBuilder.AppendLine();
            outputStringBuilder.AppendLine("The following config needs to appended to:");
            outputStringBuilder.AppendLine(opManager.GetAgentTomlPath());
            outputStringBuilder.AppendLine("\n\n");
            outputStringBuilder.AppendLine(agentToml);
            outputStringBuilder.AppendLine("\n\n");


        }
    }
}