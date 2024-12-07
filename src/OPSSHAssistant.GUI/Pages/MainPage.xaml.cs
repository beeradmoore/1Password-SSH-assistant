using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;

namespace OPSSHAssistant.GUI.Pages;

public partial class MainPage : ContentPage
{
    bool _hasAppeared = false;
    
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainPageModel(this);
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAppeared == false)
        {
            _hasAppeared = true;

            if (BindingContext is MainPageModel model)
            {
                model.CheckFor1PasswordAsync().SafeFireAndForget();
            }
        }
    }
}