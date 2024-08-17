using CommunityToolkit.Mvvm.ComponentModel;
using OPSSHAssistant.Core.Data;

namespace OPSSHAssistant.GUI;

public partial class CheckboxItem : ObservableObject
{
    [ObservableProperty]
    bool isChecked = false;

    public Item Item { get; init; }

    public CheckboxItem(Item item)
    {
        Item = item;
    }
}