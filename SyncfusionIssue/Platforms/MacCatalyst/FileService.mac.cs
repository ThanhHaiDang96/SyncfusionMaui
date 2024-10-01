using Foundation;
using System.Diagnostics;
using UIKit;

namespace SyncfusionIssue;

public partial class FileService
{
    private UIDocumentPickerViewController? _documentPickerViewController;
    private TaskCompletionSource<string>? _taskCompetedSource;

    private async Task<Stream?> PlatformSaveAsAsync(string fileName, Stream stream)
    {
        string initialPath = "DCIM";
        CancellationToken cancellationToken = CancellationToken.None;
        var fileManager = NSFileManager.DefaultManager;
        var tempDirectoryPath = fileManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), true);
        var isDirectoryCreated = fileManager.CreateDirectory(tempDirectoryPath, true, null, out var error);
        if (!isDirectoryCreated)
        {
            throw new Exception(error?.LocalizedDescription ?? "Unable to create temp directory.");
        }

        var fileUrl = tempDirectoryPath.Append(fileName, false);

        // Create a copy of the stream to ensure it remains open
        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            await WriteStream(memoryStream, fileUrl.Path);
        }

        cancellationToken.ThrowIfCancellationRequested();
        _taskCompetedSource?.TrySetCanceled(CancellationToken.None);
        var tcs = _taskCompetedSource = new(cancellationToken);

        _documentPickerViewController = new(new[] { fileUrl })
        {
            DirectoryUrl = NSUrl.FromString(initialPath)
        };
        _documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
        _documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;

        var currentViewController = Platform.GetCurrentUIViewController();
        if (currentViewController is not null)
        {
            currentViewController.PresentViewController(_documentPickerViewController, true, null);
        }
        else
        {
            throw new Exception("Unable to get a window where to present the file saver UI.");
        }

        try
        {
            var fullPath = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Open a new stream from the saved file
            var savedStream = File.OpenRead(fullPath);
            return savedStream;
        }
        catch (Exception)
        {
            // If an exception occurs (e.g., user cancels), return null
            return null;
        }
    }

    private void DocumentPickerViewControllerOnWasCancelled(object? sender, EventArgs e)
    {
        _taskCompetedSource?.TrySetCanceled();
    }

    private void DocumentPickerViewControllerOnDidPickDocumentAtUrls(object? sender,
        UIDocumentPickedAtUrlsEventArgs e)
    {
        _taskCompetedSource?.TrySetResult(e.Urls[0].Path ??
                                          throw new Exception("Unable to retrieve the path of the saved file."));
    }

    private async Task<string> PlatformSaveAsAsync(string fileName)
    {
        try
        {
            string initialPath = "DCIM";
            CancellationToken cancellationToken = CancellationToken.None;
            var fileManager = NSFileManager.DefaultManager;
            var tempDirectoryPath = fileManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), true);
            var isDirectoryCreated = fileManager.CreateDirectory(tempDirectoryPath, true, null, out var error);
            if (!isDirectoryCreated)
            {
                throw new Exception(error?.LocalizedDescription ?? "Unable to create temp directory.");
            }

            var fileUrl = tempDirectoryPath.Append(fileName, false);

            cancellationToken.ThrowIfCancellationRequested();
            _taskCompetedSource?.TrySetCanceled(CancellationToken.None);
            var tcs = _taskCompetedSource = new(cancellationToken);

            // Create a temporary file
            var tempFilePath = Path.Combine(tempDirectoryPath.Path, fileName);
            File.Create(tempFilePath).Dispose(); // Create an empty file

            var tempFileUrl = NSUrl.FromFilename(tempFilePath);
            _documentPickerViewController = new(new[] { tempFileUrl }, UIDocumentPickerMode.ExportToService)
            {
                DirectoryUrl = NSUrl.FromString(initialPath)
            };

            _documentPickerViewController.DidPickDocumentAtUrls +=
                DocumentPickerViewControllerOnDidPickDocumentAtUrls;
            _documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;

            var currentViewController = Platform.GetCurrentUIViewController();
            if (currentViewController is not null)
            {
                currentViewController.PresentViewController(_documentPickerViewController, true, null);
            }
            else
            {
                throw new Exception("Unable to get a window where to present the file saver UI.");
            }

            return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return string.Empty;
    }

    private async Task<bool> PlatformSaveFileAsync(Stream fileStream, string fileName, string fileKey = "", List<string>? extensions = null)
    {
        string initialPath = "DCIM";
        CancellationToken cancellationToken = CancellationToken.None;
        var fileManager = NSFileManager.DefaultManager;
        var tempDirectoryPath = fileManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), true);
        var isDirectoryCreated = fileManager.CreateDirectory(tempDirectoryPath, true, null, out var error);
        if (!isDirectoryCreated)
        {
            throw new Exception(error?.LocalizedDescription ?? "Unable to create temp directory.");
        }

        var fileUrl = tempDirectoryPath.Append(fileName, false);

        // Create a copy of the stream to ensure it remains open
        using (var memoryStream = new MemoryStream())
        {
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            await WriteStream(memoryStream, fileUrl.Path);
        }

        cancellationToken.ThrowIfCancellationRequested();
        _taskCompetedSource?.TrySetCanceled(CancellationToken.None);
        var tcs = _taskCompetedSource = new(cancellationToken);

        _documentPickerViewController = new(new[] { fileUrl })
        {
            DirectoryUrl = NSUrl.FromString(initialPath)
        };
        _documentPickerViewController.DidPickDocumentAtUrls += DocumentPickerViewControllerOnDidPickDocumentAtUrls;
        _documentPickerViewController.WasCancelled += DocumentPickerViewControllerOnWasCancelled;

        var currentViewController = Platform.GetCurrentUIViewController();
        if (currentViewController is not null)
        {
            currentViewController.PresentViewController(_documentPickerViewController, true, null);
        }
        else
        {
            throw new Exception("Unable to get a window where to present the file saver UI.");
        }

        try
        {
            var fullPath = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (Exception)
        {
            // If an exception occurs (e.g., user cancels), return null
        }

        return false;
    }
}