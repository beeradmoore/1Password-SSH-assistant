using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using OPSSHAssistant.Core;

namespace OPSSHAssistant.GUI.Pages;

public partial class ExportPemTomlPage : ContentPage
{
    public ExportPemTomlPage(PreparedExport preparedExport)
    {
        InitializeComponent();
        BindingContext = new ExportPemTomlPageModel(this, preparedExport);
    }
    
    bool _hasAppeared = false;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAppeared == false)
        {
            _hasAppeared = true;

            if (BindingContext is ExportPemTomlPageModel model)
            {
                model.ExportDataAsync().SafeFireAndForget();
            }
        }
    }
}