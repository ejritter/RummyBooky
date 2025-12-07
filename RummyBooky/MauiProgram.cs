using FilePath = System.IO.Path;
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
        IServiceCollection services = builder.Services;
        services.AddSerilog(
            new LoggerConfiguration()
                .WriteTo.File(FilePath.Combine(FileSystem.Current.AppDataDirectory, "RummyBookyLog.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger());

        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<IAppAudioService, AppAudioService>();

        builder.Services.AddSingleton<GameService>();

        builder.Services.AddTransientPopup<GeneralPopupPage, GeneralPopupViewModel>();
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<NewGameViewModel>();
        builder.Services.AddSingleton<NewGamePage>();
        builder.Services.AddTransient<CurrentGamePage>();
        builder.Services.AddTransient<CurrentGameViewModel>();

        return builder.Build();
    }
}
