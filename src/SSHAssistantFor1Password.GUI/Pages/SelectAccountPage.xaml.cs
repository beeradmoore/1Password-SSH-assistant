using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using SSHAssistantFor1Password.Core.Enums;

namespace SSHAssistantFor1Password.GUI.Pages;

public partial class SelectAccountPage : ContentPage
{
    public SelectAccountPage(MenuMode mode)
    {
        InitializeComponent();
        BindingContext = new SelectAccountPageModel(this, mode);
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        if (BindingContext is SelectAccountPageModel model)
        {
            model.Cancel();
        }
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
