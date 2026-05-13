namespace RummyBooky.Views;

public partial class CardBoxView : BaseView
{
	public CardBoxView()
	{
		InitializeComponent();
		UpdateDimensions();
		UpdateCardLayouts();
	}

	public static readonly BindableProperty CurrentGameProperty = BindableProperty.Create(
		propertyName: nameof(CurrentGame),
		declaringType: typeof(CardBoxView),
		returnType: typeof(CurrentGameModel),
		defaultValue: null);

	public CurrentGameModel? CurrentGame
	{
		get => (CurrentGameModel?)GetValue(CurrentGameProperty);
		set => SetValue(CurrentGameProperty, value);
	}

	public static readonly BindableProperty PlayersProperty = BindableProperty.Create(
		propertyName: nameof(Players),
		declaringType: typeof(CardBoxView),
		returnType: typeof(IEnumerable<PlayerModel>),
		defaultValue: Array.Empty<PlayerModel>());

	public IEnumerable<PlayerModel> Players
	{
		get => (IEnumerable<PlayerModel>)GetValue(PlayersProperty);
		set => SetValue(PlayersProperty, value);
	}

	private void UpdateDimensions()
	{
		var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);
		thisCardBoxView.WidthRequest = desiredWidth;
		thisCardBoxView.HeightRequest = desiredHeight;
	}

	private void UpdateCardLayouts()
	{
		PlayersInCardBox.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
		{
			ItemSpacing = 0
		};

		foreach (PlayerModel player in Players)
		{
			var playerCardView = new PlayerCardView
			{
				AssignedPlayerModel = player,
				IsInCardBox = true
			};

			/*What we want to do here, create another playerCardViewNew, then set its height
			 * property to half of playerCardView. Then add it to a collection.
			 * vertically so only names are showing.*/
			

		}
    }
}