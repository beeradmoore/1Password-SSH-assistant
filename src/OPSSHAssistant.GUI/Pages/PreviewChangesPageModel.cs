using CommunityToolkit.Mvvm.ComponentModel;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class PreviewChangesPageModel : ObservableObject
{
    readonly WeakReference<PreviewChangesPage> _page;
    readonly MenuMode _mode;
    readonly Account _account;
    readonly Vault _vault;
    readonly List<Item> _items;
    
    public PreviewChangesPageModel(PreviewChangesPage page, MenuMode mode, Account account, Vault vault, List<Item> items)
    {
        _page = new WeakReference<PreviewChangesPage>(page);
        _mode = mode;
        _account = account;
        _vault = vault;
        _items = items;
    }
}