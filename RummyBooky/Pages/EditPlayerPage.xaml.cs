namespace RummyBooky.Pages;

public partial class EditPlayerPage : BasePage<EditPlayerViewModel>
{
	public EditPlayerPage(EditPlayerViewModel vm) :base(vm)
	{
		InitializeComponent();
	}

	private async void Page_loaded(object? sender, EventArgs e)
	{
		if (BindingContext is EditPlayerViewModel vm)
			await vm.PageLoaded();
	}
}