using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SelectItemsPageModel : ObservableObject
{
    readonly WeakReference<SelectItemsPage> _page;
    readonly MenuMode _mode;
    readonly Account _account;
    readonly Vault _vault;
    readonly List<CheckboxItem> _items = new List<CheckboxItem>();
    
    public ObservableCollection<CheckboxItem> Items { get; } = new ObservableCollection<CheckboxItem>();

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
    
    [ObservableProperty]
    CheckboxItem? _selectedItem = null;

    [ObservableProperty]
    bool _goToNextPageEnabled = false;
    
    public string NextButtonText { get; private set; } = string.Empty;
    
    public SelectItemsPageModel(SelectItemsPage page, MenuMode mode, Account account, Vault vault)
    {
        _page = new WeakReference<SelectItemsPage>(page);
        _mode = mode;
        _account = account;
        _vault = vault;

        NextButtonText = _mode switch
        {
            MenuMode.ExportPPK => "Export ppk",
            MenuMode.ExportPubAppendSSHConfigAndAgentToml => "Set Details",
            _ => throw new Exception("Unknown mode for SelectItemsPageModel"),
        };
    }
    
    internal async Task LoadItemsAsync()
    {
        _items.Clear();
        Items.Clear();
        
        SelectedItem = null;

        IsError = false;
        IsLoading = true;

        var loadedItems = await App.OPManager.LoadItemsAsync(_account, _vault).ConfigureAwait(false);

        IsLoading = false;

        if (loadedItems is null || loadedItems.Count == 0)
        {
            if (_page.TryGetTarget(out SelectItemsPage? selectItemsPage))
            {
                await selectItemsPage.Dispatcher.DispatchAsync(async () =>
                {
                    await selectItemsPage.DisplayAlert("Error", $"Could not list items.\n{App.OPManager.LastError}", "Okay");
                });

                IsError = true;
                ErrorText = "Could not list items";
            }
            return;
        }

        foreach (var item in loadedItems)
        {
            _items.Add(new CheckboxItem(item));
        }

        FilterItems(string.Empty);
    }
    
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SearchText))
        {
            FilterItems(SearchText);
        }
        else if (e.PropertyName == nameof(SelectedItem))
        {
            if (SelectedItem is null)
            {
                return;
            }

            SelectedItem.IsChecked = !SelectedItem.IsChecked;
            SelectedItem = null;
            
            GoToNextPageEnabled = Items.Any(i => i.IsChecked);
        }
    }

    void FilterItems(string searchText)
    {
        if (_page.TryGetTarget(out SelectItemsPage? page))
        {
            page.Dispatcher.Dispatch(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    Items.Clear();
                    foreach (var item in _items)
                    {
                        Items.Add(item);
                    }

                    return;
                }

                foreach (var item in _items)
                {
                    if (item.Item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        if (Items.Contains(item))
                        {
                            // NO-OP
                        }
                        else
                        {
                            for (var i = 0; i < Items.Count; ++i)
                            {
                                if (Items[i].Item.Title.CompareTo(item.Item.Title) < 0)
                                {
                                    Items.Insert(i, item);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Items.Remove(item);
                    }
                }
            });
        }
    }

    [RelayCommand]
    async Task GoToNextPageAsync()
    {
        if (_page.TryGetTarget(out SelectItemsPage? page))
        {
            await page.Dispatcher.DispatchAsync(async () =>
            {
                var selectedItems = _items.Where(x => x.IsChecked).Select(x => x.Item).ToList();
                if (selectedItems.Count == 0)
                {
                    await page.DisplayAlert("Error", "No items selected.", "Okay");
                    return;
                }

                if (_mode == MenuMode.ExportPPK)
                {
                    // TODO: Implement PPK export
                }
                else if (_mode == MenuMode.ExportPubAppendSSHConfigAndAgentToml)
                {
                    await page.Navigation.PushAsync(new SetDetailsPage(_mode, _account, _vault, selectedItems));
                }
                else
                {
                    throw new Exception("Unknown mode for SelectItemsPageModel");    
                }
            });
        }
    }
}