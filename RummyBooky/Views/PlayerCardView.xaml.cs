using System.Windows.Input;
using RummyBooky.Extensions;
using RummyBooky.Models;
using RummyBooky.Pages;
using RummyBooky.ViewModels;
namespace RummyBooky.Views;

public partial class PlayerCardView : BaseView
{
	public PlayerCardView()
	{
		InitializeComponent();
		Loaded += OnPlayerCardViewLoaded;
		UpdatePlayerCardDimensions();
		ApplyInCardBoxVisualMode();
	}

	public static readonly BindableProperty AssignedPlayerModelProperty =
		BindableProperty.Create(
			propertyName: nameof(AssignedPlayerModel),
			declaringType: typeof(PlayerCardView),
			returnType: typeof(PlayerModel),
			propertyChanged: OnAssignedPlayerModelChanged);


	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(
			propertyName: nameof(Command),
			declaringType: typeof(PlayerCardView),
			returnType: typeof(ICommand));

	public ICommand Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public PlayerModel AssignedPlayerModel
	{
		get => (PlayerModel)GetValue(AssignedPlayerModelProperty);
		set => SetValue(AssignedPlayerModelProperty, value);
	}

	public static readonly BindableProperty IsInCardBoxProperty =
		BindableProperty.Create(
			propertyName: nameof(IsInCardBox),
			declaringType: typeof(PlayerCardView),
			returnType: typeof(bool),
			defaultValue: false,
			propertyChanged: OnIsInCardBoxChanged);

	public bool IsInCardBox
	{
		get => (bool)GetValue(IsInCardBoxProperty);
		set => SetValue(IsInCardBoxProperty, value);
	}

	public static readonly BindableProperty HostWidthInsetProperty =
		BindableProperty.Create(
			propertyName: nameof(HostWidthInset),
			declaringType: typeof(PlayerCardView),
			returnType: typeof(double),
			defaultValue: 16d,
			propertyChanged: OnHostWidthInsetChanged);

	public double HostWidthInset
	{
		get => (double)GetValue(HostWidthInsetProperty);
		set => SetValue(HostWidthInsetProperty, value);
	}

	public void ConfigureForCardBox(PlayerModel playerModel, double cardWidth, double cardHeight)
	{
		AssignedPlayerModel = playerModel;
		IsInCardBox = true;
		WidthRequest = cardWidth;
		HeightRequest = cardHeight;
		ApplyInCardBoxVisualMode();
		UpdatePlayerCardDimensions();
	}

	private static void OnAssignedPlayerModelChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is PlayerCardView view)
		{
			view.UpdateCardBindingContext();
		}
	}

	private static void OnIsInCardBoxChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is PlayerCardView view)
		{
			view.ApplyInCardBoxVisualMode();
			view.UpdatePlayerCardDimensions();
		}
	}

	private static void OnHostWidthInsetChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is PlayerCardView view)
		{
			view.UpdatePlayerCardDimensions();
		}
	}

	private void OnPlayerCardViewLoaded(object? sender, EventArgs e)
	{
		UpdateCardBindingContext();
		ApplyInCardBoxVisualMode();
		UpdatePlayerCardDimensions();
	}

	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();
		UpdateCardBindingContext();
		ApplyInCardBoxVisualMode();
		UpdatePlayerCardDimensions();
	}

	private void UpdateCardBindingContext()
	{
		var effectivePlayer = AssignedPlayerModel ?? (BindingContext as PlayerModel);
		if (CardBorder != null)
		{
			CardBorder.BindingContext = effectivePlayer;
		}
	}

	protected override void OnPropertyChanged(string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);
		if (propertyName == nameof(HeightRequest) || propertyName == nameof(WidthRequest))
		{
			UpdatePlayerCardDimensions();
		}
	}

	private void ApplyInCardBoxVisualMode()
	{
		if (IsInCardBox)
		{
			PlayerStatsGrid.IsVisible = true;
			FooterGrid.IsVisible = false;
			EditPlayerButton.IsVisible = false;
			if (HeaderGrid.Children.Count > 0)
			{
				if (HeaderGrid.Children[0] is VisualElement headerLeadVisual)
				{
					headerLeadVisual.IsVisible = true;
				}
			}
			Grid.SetColumnSpan(HeaderContentLayout, 2);
			CardBorder.Padding = new Thickness(12, 10);
			HeaderGrid.Margin = new Thickness(0, 0, 0, 8);
			HeaderContentLayout.HorizontalOptions = LayoutOptions.Fill;
			HeaderContentLayout.VerticalOptions = LayoutOptions.Fill;
			PlayerNameChip.HorizontalOptions = LayoutOptions.Fill;
			PlayerNameChip.VerticalOptions = LayoutOptions.Fill;
			PlayerNameChip.Margin = new Thickness(4, 0, 0, 0);
			PlayerNameChip.Padding = new Thickness(12, 6);
			PlayerNameLabel.LineBreakMode = LineBreakMode.TailTruncation;
			PlayerNameLabel.MaxLines = 1;
			PlayerNameLabel.FontSize = 14;
			PlayerNameLabel.HorizontalTextAlignment = TextAlignment.Center;
			PlayerNameLabel.VerticalTextAlignment = TextAlignment.Center;
			return;
		}

		PlayerStatsGrid.IsVisible = true;
		FooterGrid.IsVisible = true;
		EditPlayerButton.IsVisible = true;
		if (HeaderGrid.Children.Count > 0)
		{
			if (HeaderGrid.Children[0] is VisualElement headerLeadVisual)
			{
				headerLeadVisual.IsVisible = true;
			}
		}
		Grid.SetColumnSpan(HeaderContentLayout, 1);
		CardBorder.Padding = new Thickness(16);
		HeaderGrid.Margin = new Thickness(8, 0, 16, 16);
		HeaderContentLayout.HorizontalOptions = LayoutOptions.Fill;
		HeaderContentLayout.VerticalOptions = LayoutOptions.Start;
		PlayerNameChip.HorizontalOptions = LayoutOptions.Fill;
		PlayerNameChip.VerticalOptions = LayoutOptions.Fill;
		PlayerNameChip.Margin = new Thickness(8, 0, 0, 0);
		PlayerNameChip.Padding = new Thickness(16, 8);
		PlayerNameLabel.LineBreakMode = LineBreakMode.WordWrap;
		PlayerNameLabel.MaxLines = int.MaxValue;
		PlayerNameLabel.FontSize = 14;
		PlayerNameLabel.HorizontalTextAlignment = TextAlignment.Center;
		PlayerNameLabel.VerticalTextAlignment = TextAlignment.Center;
	}

	private void UpdatePlayerCardDimensions()
	{
		if (IsInCardBox)
		{
			var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);

			if (WidthRequest > 0)
			{
				desiredWidth = WidthRequest;
			}
			else
			{
				desiredWidth = Math.Max(0, desiredWidth - HostWidthInset);
			}

			if (HeightRequest > 0)
			{
				desiredHeight = HeightRequest;
			}
			else
			{
				desiredHeight = 260;
			}

			CardBorder.WidthRequest = desiredWidth;
			CardBorder.HeightRequest = desiredHeight;
			CardBorder.HorizontalOptions = LayoutOptions.Center;
			CardBorder.VerticalOptions = LayoutOptions.Start;
		}
		else
		{
			if (WidthRequest > 0)
			{
				CardBorder.WidthRequest = WidthRequest;
			}
			else
			{
				CardBorder.ClearValue(VisualElement.WidthRequestProperty);
			}

			if (HeightRequest > 0)
			{
				CardBorder.HeightRequest = HeightRequest;
			}
			else
			{
				CardBorder.ClearValue(VisualElement.HeightRequestProperty);
			}

			CardBorder.HorizontalOptions = LayoutOptions.Fill;
			CardBorder.VerticalOptions = LayoutOptions.Fill;
		}
	}

	private bool _isNavigating;

	private async void OnEditPlayerButtonClicked(object? sender, EventArgs e)
	{
		if (_isNavigating)
		{
			return;
		}

		_isNavigating = true;
		try
		{
			await RummyBooky.Extensions.ViewExtensions.AnimatePressAsync(EditPlayerButton);

			var targetPlayer = AssignedPlayerModel ?? CardBorder?.BindingContext as PlayerModel ?? BindingContext as PlayerModel;
			if (targetPlayer is null)
			{
				return;
			}

			if (Command != null && Command.CanExecute(targetPlayer))
			{
				Command.Execute(targetPlayer);
				return;
			}

			if (Shell.Current?.CurrentPage is EditPlayerPage editPage &&
				editPage.BindingContext is EditPlayerViewModel editVm)
			{
				editVm.CurrentPlayer = targetPlayer;
				return;
			}

			if (Shell.Current != null)
			{
				await Shell.Current.GoToAsync(nameof(EditPlayerPage), animate: true, parameters: new Dictionary<string, object>
				{
					[nameof(EditPlayerViewModel.CurrentPlayer)] = targetPlayer
				});
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[PlayerCardView] Navigation error: {ex.Message}");
		}
		finally
		{
			await Task.Delay(500);
			_isNavigating = false;
		}
	}
}