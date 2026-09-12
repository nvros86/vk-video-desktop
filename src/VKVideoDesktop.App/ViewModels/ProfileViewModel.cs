namespace VKVideoDesktop.App.ViewModels;

public sealed class ProfileViewModel
{
    private static string GetDefaultName() => App.GetService<Application.Services.LocalizationService>()["ProfileDefaultName"];
    public string UserName { get; set; } = GetDefaultName();
    public string UserDomain { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
}
