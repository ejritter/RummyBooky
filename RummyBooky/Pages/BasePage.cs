namespace RummyBooky.Pages;

public abstract class BasePage<TViewModel> : ContentPage where TViewModel : BaseViewModel
{
    public TViewModel ViewModel => (TViewModel)BindingContext;

    protected BasePage(TViewModel viewModel)
    {
        BindingContext = viewModel;
    }
}
