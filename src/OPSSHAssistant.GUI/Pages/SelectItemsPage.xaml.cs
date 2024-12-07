using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SelectItemsPage : ContentPage
{
    public SelectItemsPage(MenuMode mode, Account account, Vault vault)
    {
        InitializeComponent();
        BindingContext = new SelectItemsPageModel(this, mode, account, vault);
    }
    
    
    bool _hasAppeared = false;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAppeared == false)
        {
            _hasAppeared = true;

            if (BindingContext is SelectItemsPageModel model)
            {
                model.LoadItemsAsync().SafeFireAndForget();
            }
        }
    }
}