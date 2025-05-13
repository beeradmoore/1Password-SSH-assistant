using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using HealthKit;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SelectVaultPageModel : ObservableObject
{
    readonly WeakReference<SelectVaultPage> _page;
    readonly MenuMode _mode;
    readonly Account _account;

    readonly List<Vault> _vaults = new List<Vault>();
    public ObservableCollection<Vault> Vaults { get; } = new ObservableCollection<Vault>();
    
    [ObservableProperty]
    Vault? _selectedVault = null;
    
    [ObservableProperty]
    bool _isLoading = false;
    
    [ObservableProperty]
    bool _isError = false;

    [ObservableProperty]
    string _errorText = "An unknown error occured";

    [ObservableProperty]
    bool _vaultsLoaded = false;

    [ObservableProperty]
    string _searchText = string.Empty;
    
    public SelectVaultPageModel(SelectVaultPage page, MenuMode mode, Account account)
    {
        _page = new WeakReference<SelectVaultPage>(page);
        _mode = mode;
        _account = account;
    }
    
    internal async Task LoadVaultsAsync()
    {
        _vaults.Clear();
        Vaults.Clear();
        
        SelectedVault = null;

        IsError = false;
        IsLoading = true;

        var loadedVaults = await App.OPManager.LoadVaultsAsync(_account).ConfigureAwait(false);

        IsLoading = false;

        if (loadedVaults is null || loadedVaults.Count == 0)
        {
            if (_page.TryGetTarget(out SelectVaultPage? selectedVaultPage))
            {
                await selectedVaultPage.Dispatcher.DispatchAsync(async () =>
                {
                    await selectedVaultPage.DisplayAlert("Error", $"Could not list vaults.\n{App.OPManager.LastError}", "Okay");
                });

                IsError = true;
                ErrorText = "Could not list vaults";
            }
            return;
        }

        _vaults.AddRange(loadedVaults);

        FilterVaults(string.Empty);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SearchText))
        {
            FilterVaults(SearchText);
        }
        else if (e.PropertyName == nameof(SelectedVault))
        {
            if (SelectedVault is null)
            {
                return;
            }

            var selectedVault = SelectedVault;
            SelectedVault = null;

            if (_page.TryGetTarget(out SelectVaultPage? page))
            {
                page.Navigation.PushAsync(new SelectItemsPage(_mode, _account, selectedVault));
            }
        }
    }

    void FilterVaults(string searchText)
    {
        if (_page.TryGetTarget(out SelectVaultPage? page))
        {
            page.Dispatcher.Dispatch(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    Vaults.Clear();
                    foreach (var vault in _vaults)
                    {
                        Vaults.Add(vault);
                    }

                    return;
                }

                foreach (var vault in _vaults)
                {
                    if (vault.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        if (Vaults.Contains(vault))
                        {
                            // NO-OP
                        }
                        else
                        {
                            for (var i = 0; i < Vaults.Count; ++i)
                            {
                                if (Vaults[i].Name.CompareTo(vault.Name) < 0)
                                {
                                    Vaults.Insert(i, vault);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Vaults.Remove(vault);
                    }
                }
            });
        }
    }
}