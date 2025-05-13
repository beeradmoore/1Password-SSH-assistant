using CommunityToolkit.Mvvm.ComponentModel;
using OPSSHAssistant.Core.Data;

namespace OPSSHAssistant.GUI;

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