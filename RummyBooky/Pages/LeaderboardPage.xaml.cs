using System;
using Microsoft.Maui.Controls;
using RummyBooky.Extensions;
using RummyBooky.ViewModels;

namespace RummyBooky.Pages;

public partial class LeaderboardPage : BasePage<LeaderboardViewModel>
{
    public LeaderboardPage(LeaderboardViewModel vm) : base(vm)
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LeaderboardViewModel vm && vm.AppearingCommand.CanExecute(null))
        {
            await vm.AppearingCommand.ExecuteAsync(null);
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnRankItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }
}