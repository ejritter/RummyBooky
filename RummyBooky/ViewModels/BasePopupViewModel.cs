namespace RummyBooky.ViewModels;

public abstract partial class BasePopupViewModel(IPopupService popupService) : ObservableObject, IQueryAttributable
{

    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Title = (string)query[nameof(BasePopupViewModel.Title)];
        Message = (string)query[nameof(BasePopupViewModel.Message)];

    }

    protected readonly IPopupService _popupService = popupService;

    [ObservableProperty]
    private string? _title = string.Empty;

    [ObservableProperty]
    private string? _message = string.Empty;

    public ObservableCollection<GameStatus> GameStatuses { get; } = new(Enum.GetValues<GameStatus>().Distinct());



}
