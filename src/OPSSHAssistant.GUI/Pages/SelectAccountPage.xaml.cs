using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.GUI.Pages;

public partial class SelectAccountPage : ContentPage
{
    public SelectAccountPage(MenuMode mode)
    {
        InitializeComponent();
        BindingContext = new SelectAccountPageModel(this, mode);
    }

    bool _hasAppeared = false;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAppeared == false)
        {
            _hasAppeared = true;

            if (BindingContext is SelectAccountPageModel model)
            {
                model.LoadAccountsAsync().SafeFireAndForget();
            }
        }
    }
}