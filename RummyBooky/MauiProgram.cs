namespace RummyBooky;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });



        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<IAppAudioService, AppAudioService>();
        builder.Services.AddSingleton<GameService>();

        builder.Services.AddTransientPopup<GeneralPopupPage, GeneralPopupViewModel>();

        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<NewGameViewModel>();
        builder.Services.AddTransient<NewGamePage>();
        builder.Services.AddTransient<LeaderboardViewModel>();
        builder.Services.AddTransient<LeaderboardPage>();
        
        builder.Services.AddTransient<EditPlayerViewModel>();
        builder.Services.AddTransient<EditPlayerPage>();
        builder.Services.AddTransient<CurrentGamePage>();
        builder.Services.AddTransient<CurrentGameViewModel>();
        builder.Services.AddTransient<EditGameViewModel>();
        builder.Services.AddTransient<EditGamePage>();
        
        return builder.Build();
    }
}
