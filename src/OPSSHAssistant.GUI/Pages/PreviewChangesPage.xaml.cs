using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class PreviewChangesPage : ContentPage
{
    public PreviewChangesPage()
    {
    }

    public PreviewChangesPage(MenuMode mode, Account account, Vault vault, List<Item> items)
    {
        InitializeComponent();

        throw new NotImplementedException();
    }
}