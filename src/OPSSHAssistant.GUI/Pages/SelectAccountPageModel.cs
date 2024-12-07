using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SelectAccountPageModel : ObservableObject
{
    WeakReference<SelectAccountPage> _page;
    MenuMode _mode;

    readonly List<Account> _accounts = new List<Account>();

    public ObservableCollection<Account> Accounts { get; } = new ObservableCollection<Account>();

    [ObservableProperty]
    Account? _selectedAccount = null;
    
    [ObservableProperty]
    bool _isLoading = false;
    
    [ObservableProperty]
    bool _isError = false;

    [ObservableProperty]
    string _errorText = "An unknown error occured";

    [ObservableProperty]
    string _searchText = string.Empty;
    
    public SelectAccountPageModel(SelectAccountPage page, MenuMode mode)
    {
        _page = new WeakReference<SelectAccountPage>(page);
        _mode = mode;
    }

    internal async Task LoadAccountsAsync()
    {
        _accounts.Clear();
        Accounts.Clear();
        SelectedAccount = null;

        IsError = false;
        IsLoading = true;

        var loadedAccounts = await App.OPManager.LoadAccountsAsync().ConfigureAwait(false);

        IsLoading = false;

        if (loadedAccounts is null || loadedAccounts.Count == 0)
        {
            if (_page.TryGetTarget(out SelectAccountPage? selectAccountPage))
            {
                await selectAccountPage.Dispatcher.DispatchAsync(async () =>
                {
                    await selectAccountPage.DisplayAlert("Error", $"Could not list accounts.\n{App.OPManager.LastError}", "Okay");
                });

                IsError = true;
                ErrorText = "Could not list accounts";
            }
            return;
        }

        _accounts.AddRange(loadedAccounts);
        
        FilterAccounts(SearchText);
    }
    
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SearchText))
        {
            FilterAccounts(SearchText);
        }
        else if (e.PropertyName == nameof(SelectedAccount))
        {
            if (SelectedAccount is null)
            {
                return;
            }

            var selectedAccount = SelectedAccount;
            SelectedAccount = null;
            
            if (_page.TryGetTarget(out SelectAccountPage? selectAccountPage))
            {
                selectAccountPage.Navigation.PushAsync(new SelectVaultPage(_mode, selectedAccount));
            }
        }
    }

    void FilterAccounts(string searchText)
    {
        if (_page.TryGetTarget(out SelectAccountPage? page))
        {
            page.Dispatcher.Dispatch(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    Accounts.Clear();
                    foreach (var account in _accounts)
                    {
                        Accounts.Add(account);
                    }

                    return;
                }

                foreach (var account in _accounts)
                {
                    if (account.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        if (Accounts.Contains(account))
                        {
                            // NO-OP
                        }
                        else
                        {
                            for (var i = 0; i < Accounts.Count; ++i)
                            {
                                if (Accounts[i].Email.CompareTo(account.Email) < 0)
                                {
                                    Accounts.Insert(i, account);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Accounts.Remove(account);
                    }
                }
            });
        }
    }

}