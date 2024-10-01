using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.Maui.ImageEditor;
using System.Windows.Input;

namespace SyncfusionIssue.ViewModel;

public partial class ImagePropertiesPopupViewModel : ObservableObject
{
    public TaskCompletionSource<byte[]?> ImageDataSource { get; set; } = new TaskCompletionSource<byte[]?>();

    [ObservableProperty] private byte[]? _sources;

    [RelayCommand]
    private async Task Ok(object obj)
    {
        if (obj is SfImageEditor imageEditor)
        {
            imageEditor.SaveEdits();
            
            using (var memoryStream = new MemoryStream())
            {
                await using var stream = await imageEditor.GetImageStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                ImageDataSource?.SetResult(memoryStream.ToArray());
            }

            await Shell.Current.Navigation.PopModalAsync(true);
        }
    }

    [RelayCommand]
    private async Task Cancel(object obj)
    {
        ImageDataSource?.SetResult(null);
        await Shell.Current.Navigation.PopModalAsync();
    }
}