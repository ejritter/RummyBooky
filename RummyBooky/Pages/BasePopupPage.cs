namespace RummyBooky.Pages
{
    public abstract class BasePopupPage<TViewModel> : ContentView where TViewModel : BasePopupViewModel
    {
        protected BasePopupPage(TViewModel viewModel)
        {
            BindingContext = viewModel;
        }
    }
}
