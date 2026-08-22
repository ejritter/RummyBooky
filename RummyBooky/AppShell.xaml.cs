namespace RummyBooky;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(NewGamePage), typeof(NewGamePage));
        Routing.RegisterRoute(nameof(CurrentGamePage), typeof(CurrentGamePage));
        Routing.RegisterRoute(nameof(LeaderboardPage), typeof(LeaderboardPage));
        Routing.RegisterRoute(nameof(EditPlayerPage), typeof(EditPlayerPage));
        Routing.RegisterRoute(nameof(EditGamePage), typeof(EditGamePage));
    }
}
