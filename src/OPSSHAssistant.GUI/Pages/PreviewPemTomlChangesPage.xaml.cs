using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using OPSSHAssistant.Core.Data;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class PreviewPemTomlChangesPage : ContentPage
{
    public PreviewPemTomlChangesPage(MenuMode mode, Account account, Vault vault, List<Item> items)
    {
        InitializeComponent();
        BindingContext = new PreviewPemTomlChangesPageModel(this, mode, account, vault, items);
    }
    
    bool _hasAppeared = false;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAppeared == false)
        {
            _hasAppeared = true;

            if (BindingContext is PreviewPemTomlChangesPageModel model)
            {
                model.GenerateOutputAsync().SafeFireAndForget();
            }
        }
    }
}