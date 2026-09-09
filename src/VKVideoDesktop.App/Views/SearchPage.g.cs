using Microsoft.UI.Xaml.Controls;

namespace VKVideoDesktop.App.Views;

public sealed partial class SearchPage
{
    internal AutoSuggestBox SearchBox = null!;
    internal StackPanel EmptyState = null!;
    internal ListView ResultsList = null!;
    internal ProgressRing LoadingRing = null!;
}
