using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SetDetailsPageModel : ObservableObject
{
    readonly WeakReference<SetDetailsPage> _page;
    readonly MenuMode _mode;
    readonly Account _account;
    readonly Vault _vault;
    
    public List<Item> Items { get; private set; }
    
    

    public SetDetailsPageModel(SetDetailsPage page, MenuMode mode, Account account, Vault vault, List<Item> items)
    {
        _page = new WeakReference<SetDetailsPage>(page);
        _mode = mode;
        _account = account;
        _vault = vault;
        Items = items;
    }

    [RelayCommand]
    async Task GoToPreviewChangesAsync()
    {
        if (_page.TryGetTarget(out SetDetailsPage? page))
        {
            await page.Dispatcher.DispatchAsync(async () =>
            {
                await page.Navigation.PushAsync(new PreviewPemTomlChangesPage(_mode, _account, _vault, Items));
            });
        }
    }
}
