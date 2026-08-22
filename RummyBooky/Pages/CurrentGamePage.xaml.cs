using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using RummyBooky.Extensions;
using RummyBooky.Models;
using RummyBooky.ViewModels;

namespace RummyBooky.Pages;

public partial class CurrentGamePage : BasePage<CurrentGameViewModel>, IQueryAttributable
{
	public CurrentGamePage(CurrentGameViewModel vm) : base(vm)
	{
		InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.Players.CollectionChanged += Players_CollectionChanged;
	}

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentGameViewModel.Players) || e.PropertyName == nameof(CurrentGameViewModel.CurrentGame))
        {
            MainThread.BeginInvokeOnMainThread(PopulatePlayerRows);
        }
    }

    private void Players_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(PopulatePlayerRows);
    }

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		ViewModel.ApplyQueryAttributes(query);
        MainThread.BeginInvokeOnMainThread(PopulatePlayerRows);
	}

	protected override bool OnBackButtonPressed()
    {
        return true;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel?.OnAppearing();
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsEnabled = false,
            IsVisible = false
        });
        MainThread.BeginInvokeOnMainThread(PopulatePlayerRows);
    }

    public void PopulatePlayerRows()
    {
        try
        {
            var players = (ViewModel?.CurrentGame?.Players != null && ViewModel.CurrentGame.Players.Count > 0)
                ? ViewModel.CurrentGame.Players
                : ViewModel?.Players;

            Console.WriteLine($"[DEBUG_CURRENTGAME] PopulatePlayerRows called. Players count: {players?.Count ?? -1}");

            PlayersListStack.Children.Clear();
            if (players == null || !players.Any())
            {
                Console.WriteLine("[DEBUG_CURRENTGAME] Players collection is null or empty.");
                return;
            }

            foreach (var player in players)
            {
                Console.WriteLine($"[DEBUG_CURRENTGAME] Adding row for player: {player.PlayerName}, score: {player.PlayerScore}");
                var rowGrid = new Grid
                {
                    HeightRequest = 66,
                    ColumnSpacing = 0,
                    HorizontalOptions = LayoutOptions.Fill,
                    RowDefinitions =
                    {
                        new RowDefinition { Height = 65 },
                        new RowDefinition { Height = 1 }
                    },
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = 2 },
                        new ColumnDefinition { Width = 95 },
                        new ColumnDefinition { Width = 2 },
                        new ColumnDefinition { Width = 115 }
                    },
                    BindingContext = player
                };

                // Background
                var bgBox = new BoxView { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill, Color = Color.FromArgb("#0F172A") };
                Grid.SetRow(bgBox, 0);
                Grid.SetColumnSpan(bgBox, 5);
                rowGrid.Children.Add(bgBox);

                // Column 0: Dealer badge + Player Name
                var nameGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    Padding = new Thickness(8, 0)
                };

                var dealerImg = new Image
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = 22,
                    HeightRequest = 22,
                    Margin = new Thickness(2, 0)
                };
                dealerImg.SetDynamicResource(VisualElement.StyleProperty, "DealerImage");
                dealerImg.SetBinding(VisualElement.IsVisibleProperty, nameof(PlayerModel.IsDealer));
                Grid.SetColumn(dealerImg, 0);
                nameGrid.Children.Add(dealerImg);

                var nameLabel = new Label
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    TextColor = Colors.White,
                    FontSize = 16
                };
                nameLabel.SetDynamicResource(VisualElement.StyleProperty, "PlayerLabel");
                nameLabel.SetBinding(Label.TextProperty, nameof(PlayerModel.PlayerName));
                Grid.SetColumn(nameLabel, 1);
                nameGrid.Children.Add(nameLabel);

                Grid.SetRow(nameGrid, 0);
                Grid.SetColumn(nameGrid, 0);
                rowGrid.Children.Add(nameGrid);

                // Separator 1
                var sep1 = new BoxView { WidthRequest = 1, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Fill, Color = Color.FromArgb("#334155") };
                Grid.SetRow(sep1, 0);
                Grid.SetColumn(sep1, 1);
                rowGrid.Children.Add(sep1);

                // Column 2: Total Score
                var scoreLabel = new Label
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.White,
                    FontSize = 16
                };
                scoreLabel.SetDynamicResource(VisualElement.StyleProperty, "PlayerLabel");
                scoreLabel.SetBinding(Label.TextProperty, nameof(PlayerModel.PlayerScore));
                Grid.SetRow(scoreLabel, 0);
                Grid.SetColumn(scoreLabel, 2);
                rowGrid.Children.Add(scoreLabel);

                // Separator 2
                var sep2 = new BoxView { WidthRequest = 1, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Fill, Color = Color.FromArgb("#334155") };
                Grid.SetRow(sep2, 0);
                Grid.SetColumn(sep2, 3);
                rowGrid.Children.Add(sep2);

                // Column 4: Round Score Entry
                var entryBorder = new Border
                {
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    WidthRequest = 70,
                    Padding = 0
                };
                entryBorder.SetDynamicResource(VisualElement.StyleProperty, "TagEntryBorder");

                var scoreEntry = new Entry
                {
                    WidthRequest = 60,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    FontSize = 15,
                    Keyboard = Keyboard.Numeric,
                    TextColor = Colors.White
                };
                scoreEntry.SetDynamicResource(VisualElement.StyleProperty, "TagEntry");
                scoreEntry.SetBinding(Entry.TextProperty, new Binding(nameof(PlayerModel.PlayerScoreText), BindingMode.TwoWay));
                entryBorder.Content = scoreEntry;

                Grid.SetRow(entryBorder, 0);
                Grid.SetColumn(entryBorder, 4);
                rowGrid.Children.Add(entryBorder);

                // Bottom Divider
                var div = new BoxView { HeightRequest = 1, VerticalOptions = LayoutOptions.End, HorizontalOptions = LayoutOptions.Fill, Color = Color.FromArgb("#334155") };
                Grid.SetRow(div, 1);
                Grid.SetColumnSpan(div, 5);
                rowGrid.Children.Add(div);

                PlayersListStack.Children.Add(rowGrid);
                Console.WriteLine($"[DEBUG_CURRENTGAME] Successfully added row for: {player.PlayerName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG_CURRENTGAME] EXCEPTION in PopulatePlayerRows: {ex}");
        }
    }

    private async void OnMainPageClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnEditGameClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnPreviousRoundClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnNextRoundClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnReturnToActiveRoundClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnQuitGameClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnCalculateScoresClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }
}

