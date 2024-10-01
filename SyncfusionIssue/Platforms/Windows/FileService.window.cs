using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SyncfusionIssue
{
    public partial class FileService
    {
        private async Task<bool> PlatformSaveFileAsync(Stream fileStream, string fileName, string fileKey = "", List<string>? extensions = null)
        {
            try
            {
                var savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, Process.GetCurrentProcess().MainWindowHandle);

                savePicker.FileTypeChoices.Add(fileKey, extensions);
                savePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(fileName);

                var file = await savePicker.PickSaveFileAsync();
                if (string.IsNullOrEmpty(file?.Path))
                {
                    return false;
                }

                await WriteStream(fileStream, file.Path).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }

        private async Task<Stream?> PlatformSaveAsAsync(string fileName, Stream stream)
        {
            try
            {
                var savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, Process.GetCurrentProcess().MainWindowHandle);

                var extension = Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(extension))
                {
                    savePicker.FileTypeChoices.Add(extension, new List<string> { extension });
                }

                savePicker.FileTypeChoices.Add("PDF file",[ ".png"]);
                savePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(fileName);

                var file = await savePicker.PickSaveFileAsync();
                if (string.IsNullOrEmpty(file?.Path))
                {
                    return null;
                }

                await WriteStream(stream, file.Path).ConfigureAwait(false);
                return stream;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
        
        private async Task<string> PlatformSaveAsAsync(string fileName)
        {
            try
            {
                var savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, Process.GetCurrentProcess().MainWindowHandle);

                var extension = Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(extension))
                {
                    savePicker.FileTypeChoices.Add(extension, new List<string> { extension });
                }

                savePicker.FileTypeChoices.Add("PDF file", [".pdf"]);
                savePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(fileName);

                var file = await savePicker.PickSaveFileAsync();

                return file?.Path ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

            }
            return string.Empty;
        }

    }
}
