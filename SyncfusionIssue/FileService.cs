using Syncfusion.Maui.PdfViewer;

namespace SyncfusionIssue
{
    public partial class FileService
    {
        public async Task<Stream?> OpenFile(string fileExtension)
        {
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, fileExtension.Split(";") },
                { DevicePlatform.MacCatalyst, fileExtension.Split(";") },
            });
            PickOptions options = new()
            {
                PickerTitle = "Open",
                FileTypes = fileTypes,
            };

            return await PickFile(options, fileExtension);
        }

        private async Task<Stream?> PickFile(PickOptions options, string fileExtension)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {

                    var memoryStream = new MemoryStream();
                    await using var stream = await result.OpenReadAsync();
                    stream.Position = 0;
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream;

                }

                return null;
            }
            catch (System.Exception ex)
            {
                var message = string.IsNullOrEmpty(ex.Message) == false ? ex.Message : "File open failed.";

                Application.Current?.MainPage?.DisplayAlert("Error", message, "OK");
            }

            return null;
        }

        public async Task<bool> SaveFileAsync(Stream fileStream, string defaultFileName, string fileKeyName, List<string> fileExtensions)
        {
            return await PlatformSaveFileAsync(fileStream, defaultFileName, fileKeyName, fileExtensions);
        }

        private async Task WriteStream(Stream stream, string? filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
                fileStream.SetLength(0);
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                await stream.CopyToAsync(fileStream);
            }
        }

        public async Task<Stream?> SaveAsAsync(SfPdfViewer pdfViewer, string fileName)
        {
            var filePath = await PlatformSaveAsAsync(fileName);
            var stream = await SaveAndCreateCachedFile(pdfViewer, filePath);
            return stream;
        }

        private async Task<Stream?> SaveAndCreateCachedFile(SfPdfViewer pdfViewer, string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    await using (FileStream fileStream = File.Create(filePath))
                    {
                        await pdfViewer.SaveDocumentAsync(fileStream);
                    }

                    await using (FileStream readStream = File.OpenRead(filePath))
                    {
                        return await CreateCachedFile(Path.GetFileName(filePath), readStream);
                    }
                }
            }
            catch (Exception e)
            {
                //ignored
            }

            return null;
        }

        private async Task<Stream?> CreateCachedFile(string fileName, Stream stream)
        {
            var cacheFileName = $"{DateTime.Now:yyyyMMddHHmmss_}{fileName}";
            var cacheFile = Path.Combine(FileSystem.Current.CacheDirectory, cacheFileName);

            if (!IsFileLocked(cacheFile))
            {
                await using (var fileStream = File.Create(cacheFile))
                {
                    await stream.CopyToAsync(fileStream);
                }

                return File.OpenRead(cacheFile);
            }

            return null;
        }

        private bool IsFileLocked(string file)
        {
            try
            {
                using var stream = File.Create(file);
                return false;
            }
            catch (IOException ex)
            {
                return true;
            }
        }
    }
}