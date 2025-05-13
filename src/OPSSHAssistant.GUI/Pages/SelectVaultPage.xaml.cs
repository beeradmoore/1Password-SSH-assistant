using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SelectVaultPage : ContentPage
{
    public SelectVaultPage(MenuMode mode, Account account)
    {
        InitializeComponent();
        BindingContext = new SelectVaultPageModel(this, mode, account);
    }
    
    
    bool _hasAppeared = false;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAppeared == false)
        {
            _hasAppeared = true;

            if (BindingContext is SelectVaultPageModel model)
            {
                model.LoadVaultsAsync().SafeFireAndForget();
            }
        }
    }
}