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
    WeakReference<SelectAccountPage> _weakPage;
    MenuMode _mode;

    // Use a different list for account
    readonly List<Account> _accounts = new List<Account>();

    public ObservableCollection<Account> FilteredAccounts { get; } = new ObservableCollection<Account>();

    [ObservableProperty]
    public partial Account? SelectedAccount { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsError { get; set; }

    [ObservableProperty]
    public partial string ErrorText { get; set; } = "An unknown error occured";

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public SelectAccountPageModel(SelectAccountPage page, MenuMode mode)
    {
        _weakPage = new WeakReference<SelectAccountPage>(page);
        _mode = mode;
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    internal async Task LoadAccountsAsync()
    {
        _accounts.Clear();
        FilteredAccounts.Clear();
        SelectedAccount = null;

        IsError = false;
        IsLoading = true;

        var result = await App.OPManager.LoadAccountsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

        IsLoading = false;

        if (result.Cancelled)
        {
            return;
        }

        if (result.Success == false)
        {
            if (_weakPage.TryGetTarget(out var page))
            {
                await page.Dispatcher.DispatchAsync(async () =>
                {
                    await page.DisplayAlert("Error", $"Could not list accounts.\n{result.ErrorMessage}", "Okay");
                });

                IsError = true;
                ErrorText = "Could not list accounts";
            }

            return;
        }

        if (result.Data is null || result.Data.Count == 0)
        {
            if (_weakPage.TryGetTarget(out var page))
            {
                await page.Dispatcher.DispatchAsync(async () =>
                {
                    await page.DisplayAlert("Error", $"Could not list accounts.", "Okay");
                });
            }

            return;
        }

        _accounts.AddRange(result.Data);

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

            if (_weakPage.TryGetTarget(out var page))
            {
                page.Navigation.PushAsync(new SelectVaultPage(_mode, selectedAccount));
            }
        }
    }

    void FilterAccounts(string searchText)
    {
        if (_weakPage.TryGetTarget(out var page))
        {
            page.Dispatcher.Dispatch(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    FilteredAccounts.Clear();
                    foreach (var account in _accounts)
                    {
                        FilteredAccounts.Add(account);
                    }

                    return;
                }

                foreach (var account in _accounts)
                {
                    if (account.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        if (FilteredAccounts.Contains(account))
                        {
                            // NO-OP
                        }
                        else
                        {
                            for (var i = 0; i < FilteredAccounts.Count; ++i)
                            {
                                if (FilteredAccounts[i].Email.CompareTo(account.Email, StringComparison.InvariantCultureIgnoreCase) < 0)
                                {
                                    FilteredAccounts.Insert(i, account);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        FilteredAccounts.Remove(account);
                    }
                }
            });
        }
    }
}
