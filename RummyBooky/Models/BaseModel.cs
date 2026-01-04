namespace RummyBooky.Models;

public abstract partial class BaseModel : ObservableObject
{
    [ObservableProperty]
    public partial AppTheme? CurrentTheme { get; set; } = Application.Current?.RequestedTheme;
}
