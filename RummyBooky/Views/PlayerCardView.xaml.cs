using System.Windows.Input;
namespace RummyBooky.Views;

public partial class PlayerCardView : BaseView
{
	public PlayerCardView()
	{
		InitializeComponent();
		UpdatePlayerCardDimensions();
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
			propertyChanged: (_, __, ___) => ((PlayerCardView)_).UpdatePlayerCardDimensions());

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
			defaultValue: 14d,
			propertyChanged: (_, __, ___) => ((PlayerCardView)_).UpdatePlayerCardDimensions());

	public double HostWidthInset
	{
		get => (double)GetValue(HostWidthInsetProperty);
		set => SetValue(HostWidthInsetProperty, value);
	}

	private static void OnAssignedPlayerModelChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is PlayerCardView view && newValue is PlayerModel player)
		{
			view.BindingContext = player;
		}

	}



	private void UpdatePlayerCardDimensions()
	{
		var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);

		if (IsInCardBox)
			desiredWidth = Math.Max(0, desiredWidth - HostWidthInset);

		CardBorder.WidthRequest = desiredWidth;
		CardBorder.HeightRequest = desiredHeight;
	}


	
	

}