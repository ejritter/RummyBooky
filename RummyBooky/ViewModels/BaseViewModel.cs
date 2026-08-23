

namespace RummyBooky.ViewModels;

public abstract class BaseViewModel(IPopupService popupService, GameService gameService) : ObservableObject
{


    protected readonly IPopupService _popupService = popupService;
    protected readonly GameService _gameService = gameService;
    protected static AppTheme CurrentTheme => Application.Current?.RequestedTheme switch
    {
        AppTheme.Light => AppTheme.Light,
        AppTheme.Dark => AppTheme.Dark,
        _ => AppTheme.Dark
    };
    public virtual async Task<PopupResultsModel> ShowPopupAsync(
        string title, 
        string message, 
        bool isDismissable = true, 
        List<PlayerModel>? players = null, 
        GameStatus? gameStatus = GameStatus.Unknown,
        bool? showOkay = null,
        bool? showCancel = null,
        bool? showQuit = null,
        string? okayText = null,
        string? cancelText = null,
        string? confirmText = null)
    {
        var queryAttributes = new Dictionary<string, object>
        {
            [nameof(BasePopupViewModel.Title)] = title,
            [nameof(BasePopupViewModel.Message)] = message
        };
        if (players != null)
            queryAttributes["players"] = players;
        if (gameStatus != null)
            queryAttributes["gameStatus"] = gameStatus;
        if (showOkay.HasValue)
            queryAttributes["showOkay"] = showOkay.Value;
        if (showCancel.HasValue)
            queryAttributes["showCancel"] = showCancel.Value;
        if (showQuit.HasValue)
            queryAttributes["showQuit"] = showQuit.Value;
        if (!string.IsNullOrEmpty(okayText))
            queryAttributes["okayText"] = okayText;
        if (!string.IsNullOrEmpty(cancelText))
            queryAttributes["cancelText"] = cancelText;
        if (!string.IsNullOrEmpty(confirmText))
            queryAttributes["confirmText"] = confirmText;

        var results = await _popupService
            .ShowPopupAsync<GeneralPopupViewModel>(
                shell: Shell.Current,
                options: new PopupOptions
                {
                    CanBeDismissedByTappingOutsideOfPopup = isDismissable,
                    PageOverlayColor = GetPageOverlayColor()
                },
                shellParameters: queryAttributes);
        if (results is not null &&
            results is IPopupResult<PopupResultsModel> userResults)
        {
            return userResults.Result;
        }
        else
        {
            return new PopupResultsModel();
        }
    }

    private static Color GetPageOverlayColor()
    {
        return Color.FromRgba(0, 0, 0, 150);
    }
}
