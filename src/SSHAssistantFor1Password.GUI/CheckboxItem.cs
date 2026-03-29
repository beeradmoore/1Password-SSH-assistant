using CommunityToolkit.Mvvm.ComponentModel;
using SSHAssistantFor1Password.Core.Data;

namespace SSHAssistantFor1Password.GUI;

public partial class CheckboxItem : ObservableObject
{
    [ObservableProperty]
    bool _isChecked = false;

    public Item Item { get; init; }

    public CheckboxItem(Item item)
    {
        Item = item;
    }
}
