using System.Collections.Generic;
using Microsoft.Maui.Controls;
using RummyBooky.Extensions;
using RummyBooky.ViewModels;

namespace RummyBooky.Pages;

public partial class EditGamePage : BasePage<EditGameViewModel>, IQueryAttributable
{
    public EditGamePage(EditGameViewModel vm) : base(vm)
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ViewModel.ApplyQueryAttributes(query);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (!ViewModel.IsSaved)
        {
            ViewModel.RevertToInitialState();
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }
}

