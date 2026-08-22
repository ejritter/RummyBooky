using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using RummyBooky.Extensions;

namespace RummyBooky.Views;

public partial class CardBoxView : BaseView
{
	private bool _isExpanded;

	public CardBoxView()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	public static readonly BindableProperty CurrentGameProperty = BindableProperty.Create(
		propertyName: nameof(CurrentGame),
		declaringType: typeof(CardBoxView),
		returnType: typeof(CurrentGameModel),
		defaultValue: null,
		propertyChanged: OnCurrentGameChanged);

	public CurrentGameModel? CurrentGame
	{
		get => (CurrentGameModel?)GetValue(CurrentGameProperty);
		set => SetValue(CurrentGameProperty, value);
	}

	public static readonly BindableProperty PlayersProperty = BindableProperty.Create(
		propertyName: nameof(Players),
		declaringType: typeof(CardBoxView),
		returnType: typeof(IEnumerable<PlayerModel>),
		defaultValue: Array.Empty<PlayerModel>(),
		propertyChanged: OnPlayersChanged);

	public IEnumerable<PlayerModel> Players
	{
		get => (IEnumerable<PlayerModel>)GetValue(PlayersProperty);
		set => SetValue(PlayersProperty, value);
	}

	private static void OnPlayersChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is CardBoxView view)
		{
			view.RefreshView();
		}
	}

	private static void OnCurrentGameChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is CardBoxView view)
		{
			view.RefreshView();
		}
	}

	private void OnLoaded(object? sender, EventArgs e)
	{
		DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;
		RefreshView();
	}

	private void OnUnloaded(object? sender, EventArgs e)
	{
		DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
	}

	private void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			RefreshView();
		});
	}

	private void RefreshView()
	{
		SetBoxImagesForTheme();
		UpdateDimensions();
		BindExpandedPlayers();
		RenderCollapsedCards();
		_ = ApplyExpandedStateAsync(animate: false);
	}

	private IReadOnlyList<PlayerModel> GetOrderedPlayers()
	{
		if (Players is null)
		{
			return Array.Empty<PlayerModel>();
		}

		return Players
			.OrderBy(player => player.PlayerScore)
			.ThenBy(player => player.PlayerName)
			.ToList();
	}

	private void UpdateDimensions()
	{
		var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);

		double imageWidth = desiredWidth;
		double imageHeight = desiredHeight;
		double cardWidth = Math.Max(240d, imageWidth * 0.86d);
		double cardHeight = Math.Max(260d, imageHeight * 0.70d);

		var orderedPlayers = GetOrderedPlayers();
		int count = orderedPlayers.Count;
		double step = 0.22d * cardHeight;

		double boxY = count > 0 ? (count * step) : 0d;
		double totalHeight = boxY + imageHeight;

		CardBoxLayout.WidthRequest = imageWidth;
		CardBoxLayout.HeightRequest = totalHeight;

		CardBoxImage.WidthRequest = imageWidth;
		CardBoxImage.HeightRequest = imageHeight;
		AbsoluteLayout.SetLayoutBounds(CardBoxImage, new Rect(0d, boxY, imageWidth, imageHeight));
		AbsoluteLayout.SetLayoutFlags(CardBoxImage, AbsoluteLayoutFlags.None);

		double viewportX = Math.Max(0d, (imageWidth - cardWidth) / 2d);
		double viewportY = 0d;
		double canvasHeight = count > 0 ? ((count - 1) * step + cardHeight) : 0d;

		CollapsedCardsViewport.WidthRequest = cardWidth;
		CollapsedCardsViewport.HeightRequest = canvasHeight;
		AbsoluteLayout.SetLayoutBounds(CollapsedCardsViewport, new Rect(viewportX, viewportY, cardWidth, canvasHeight));
		AbsoluteLayout.SetLayoutFlags(CollapsedCardsViewport, AbsoluteLayoutFlags.None);

		CollapsedCardsCanvas.WidthRequest = cardWidth;
		CollapsedCardsCanvas.HeightRequest = canvasHeight;
		AbsoluteLayout.SetLayoutBounds(CollapsedCardsCanvas, new Rect(0d, 0d, cardWidth, canvasHeight));
		AbsoluteLayout.SetLayoutFlags(CollapsedCardsCanvas, AbsoluteLayoutFlags.None);

		double labelX = Math.Max(0d, imageWidth * 0.34d);
		double labelY = boxY + Math.Max(0d, imageHeight * 0.53d);
		double labelWidth = Math.Max(0d, imageWidth * 0.34d);

		AbsoluteLayout.SetLayoutBounds(GameStartedLabel, new Rect(labelX, labelY, labelWidth, 24d));
		AbsoluteLayout.SetLayoutFlags(GameStartedLabel, AbsoluteLayoutFlags.None);

		EmptyCardBoxImage.WidthRequest = 100d;
		EmptyCardBoxImage.HeightRequest = Math.Min(imageHeight, 200d);

		ExpandedPlayersList.ClearValue(VisualElement.HeightRequestProperty);
		ExpandedPlayersList.ClearValue(VisualElement.WidthRequestProperty);
	}

	private void SetBoxImagesForTheme()
	{
		if (Application.Current?.RequestedTheme == AppTheme.Dark)
		{
			CardBoxImage.Source = "card_box_dark.png";
			EmptyCardBoxImage.Source = "card_box_empty_dark.png";
			return;
		}

		CardBoxImage.Source = "card_box_light.png";
		EmptyCardBoxImage.Source = "card_box_empty_light.png";
	}

	private void BindExpandedPlayers()
	{
		BindableLayout.SetItemsSource(ExpandedPlayersList, GetOrderedPlayers());
	}

	private void RenderCollapsedCards()
	{
		CollapsedCardsCanvas.Children.Clear();
		var orderedPlayers = GetOrderedPlayers();
		if (orderedPlayers.Count == 0)
		{
			return;
		}

		var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);
		double imageWidth = desiredWidth;
		double imageHeight = desiredHeight;
		double cardWidth = Math.Max(240d, imageWidth * 0.86d);
		double cardHeight = Math.Max(260d, imageHeight * 0.70d);
		double step = 0.22d * cardHeight;

		for (int i = 0; i < orderedPlayers.Count; i++)
		{
			double top = i * step;

			var playerCardView = new PlayerCardView
			{
				AssignedPlayerModel = orderedPlayers[i],
				IsInCardBox = true,
				WidthRequest = cardWidth,
				HeightRequest = cardHeight,
				InputTransparent = true
			};
			playerCardView.ConfigureForCardBox(orderedPlayers[i], cardWidth, cardHeight);

			AbsoluteLayout.SetLayoutBounds(playerCardView, new Rect(0d, top, cardWidth, cardHeight));
			AbsoluteLayout.SetLayoutFlags(playerCardView, AbsoluteLayoutFlags.None);
			CollapsedCardsCanvas.Children.Add(playerCardView);
		}
	}

	private async Task ApplyExpandedStateAsync(bool animate = true)
	{
		if (animate)
		{
			await CollapsedContainer.TransitionCardBoxAsync(ExpandedContainer, _isExpanded);
		}
		else
		{
			CollapsedContainer.IsVisible = !_isExpanded;
			ExpandedContainer.IsVisible = _isExpanded;
			CollapsedContainer.Opacity = !_isExpanded ? 1 : 0;
			ExpandedContainer.Opacity = _isExpanded ? 1 : 0;
		}
	}

	private async void OnCardBoxTapped(object? sender, TappedEventArgs e)
	{
		if (_isExpanded) return;
		_isExpanded = true;
		await ApplyExpandedStateAsync(animate: true);
	}

	private async void OnEmptyCardBoxTapped(object? sender, TappedEventArgs e)
	{
		if (!_isExpanded) return;
		_isExpanded = false;
		await ApplyExpandedStateAsync(animate: true);
	}
}