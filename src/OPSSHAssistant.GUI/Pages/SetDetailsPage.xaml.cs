using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SetDetailsPage : ContentPage
{
    public SetDetailsPage(MenuMode mode, Account account, Vault vault, List<Item> selectedItems)
    {
        InitializeComponent();
        BindingContext = new SetDetailsPageModel(this, mode, account, vault, selectedItems);
    }
}