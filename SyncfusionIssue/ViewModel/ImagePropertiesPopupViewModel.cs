using CommunityToolkit.Mvvm.ComponentModel;

namespace SyncfusionIssue.ViewModel;

public partial class ImagePropertiesPopupViewModel : ObservableObject
{
    [ObservableProperty] private byte[]? _sources;
    
}