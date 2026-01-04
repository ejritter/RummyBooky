namespace RummyBooky.Views;

public partial class PlayerCardView : BaseView
{
	public PlayerCardView()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty AssignedPlayerModelProperty =
		BindableProperty.Create(
			propertyName: nameof(AssignedPlayerModel),
			declaringType: typeof(PlayerCardView),
			returnType: typeof(PlayerModel),
			propertyChanged: OnAssignedPlayerModelChanged);

	public PlayerModel AssignedPlayerModel
	{
		get => (PlayerModel)GetValue(AssignedPlayerModelProperty);
		set => SetValue(AssignedPlayerModelProperty, value);
	}

    private static void OnAssignedPlayerModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PlayerCardView view && newValue is PlayerModel player)
        {
            view.BindingContext = player;
        }
    }
}