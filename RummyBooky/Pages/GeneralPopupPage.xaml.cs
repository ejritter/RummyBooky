using System;
using Microsoft.Maui.Controls;
using RummyBooky.Extensions;
using RummyBooky.ViewModels;

namespace RummyBooky.Pages;

public partial class GeneralPopupPage : BasePopupPage<GeneralPopupViewModel>
{
    public GeneralPopupPage(GeneralPopupViewModel vm) : base(vm)
    {
        InitializeComponent();
    }

    private async void OnButtonClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }
}