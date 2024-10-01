using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.Maui.PdfViewer;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using PointF = Syncfusion.Drawing.PointF;

namespace SyncfusionIssue.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        private SfPdfViewer? _pdfViewer;

        private Annotation? _selectedAnnotation;

        private readonly FileService _fileService;

        [ObservableProperty] private bool _isRedactionMode;

        [ObservableProperty] private bool _isAddImageMode;

        [ObservableProperty] private Stream? _pdfStream = null;

        private Stream? _imageStream = null;

        public MainViewModel()
        {
            _fileService = new FileService();
        }

        public virtual void OnPageAppearing(object obj)
        {
            if (obj is MainPage page)
            {
                _pdfViewer = page.PdfViewer;
            }
        }

        [RelayCommand]
        private async Task Open()
        {
            var stream = await _fileService.OpenFile(".pdf");
            if (stream?.CanRead == true)
            {
                PdfStream = stream;
                // _pdfViewer?.LoadDocument(stream);
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (_pdfViewer == null)
            {
                return;
            }

            _pdfViewer.AnnotationMode = AnnotationMode.None;
            var stream = await _fileService.SaveAsAsync(_pdfViewer, "SavedFile.pdf");
            if (stream == null)
            {
                Shell.Current?.DisplayAlert("Error", "Save file failed!", "OK");
            }
        }

        [RelayCommand]
        private void AddRedactionAnnotation()
        {
            ClearTemporary();
            IsAddImageMode = false;
            if (IsRedactionMode)
            {
                if (_pdfViewer != null)
                {
                    SetupAddRedaction();
                }
                else
                {
                    TurnOffAnnotation();
                }
            }
        }

        [RelayCommand]
        private async Task AddImage(object? obj)
        {
            IsRedactionMode = false;
            var stream = await _fileService.OpenFile(".png;.jpg;.jpeg");
            if (stream != null)
            {
                _imageStream = stream;
                if (obj is Label label)
                {
                    // Fade the label in (from opacity 0 to 1) over 500ms
                    await label.FadeTo(1, 500);

                    // Wait for 5 seconds (5000ms)
                    await Task.Delay(5000);

                    // Fade the label out (from opacity 1 to 0) over 500ms
                    await label.FadeTo(0, 500);
                }
            }
        }

        private void SetupAddRedaction()
        {
            if (_pdfViewer != null)
            {
                _pdfViewer.AnnotationMode = AnnotationMode.Square;
                _pdfViewer.AnnotationSettings.Square.Color = Microsoft.Maui.Graphics.Color.FromRgba("#4597F8FF");
                _pdfViewer.AnnotationSettings.Square.FillColor = Colors.Transparent;
                _pdfViewer.AnnotationSettings.Square.Opacity = 1.0f;
                _pdfViewer.AnnotationSettings.Square.BorderWidth = 1.0f;
            }
        }

        private void TurnOffAnnotation()
        {
            if (_pdfViewer != null)
            {
                _pdfViewer.AnnotationMode = AnnotationMode.None;
            }
        }

        [RelayCommand]
        private void PdfViewerAnnotationAdded(object obj)
        {
        }

        [RelayCommand]
        private void PdfViewerAnnotationSelected(object? obj)
        {
            if (obj is (SfPdfViewer pdfViewer, AnnotationEventArgs { Annotation: not null } eventArgs))
            {
                _selectedAnnotation = eventArgs.Annotation;
                if (eventArgs.Annotation is SquareAnnotation squareAnnotation)
                {
                }

                if (eventArgs.Annotation is StampAnnotation stampAnnotation)
                {
                }
            }
        }

        [RelayCommand]
        private void PdfViewerAnnotationDeselected(object? obj)
        {
            if (obj is (SfPdfViewer pdfViewer, AnnotationEventArgs eventArgs))
            {
                _selectedAnnotation = null;

                if (eventArgs.Annotation is SquareAnnotation squareAnnotation)
                {
                }

                if (eventArgs.Annotation is StampAnnotation stampAnnotation)
                {
                    // On image selected   
                }
            }
        }

        [RelayCommand]
        private void PdfViewerTapped(object obj)
        {
            if (obj is (SfPdfViewer pdfViewer, GestureEventArgs eventArgs) && _imageStream != null)
            {
                AddImageAnnotation(_imageStream, eventArgs.PagePosition, eventArgs.PageNumber);
                ClearTemporary();
            }
        }

        private void RedactAnnotation()
        {
            
        }

        private void ClearTemporary()
        {
            _imageStream?.Close();
            _imageStream = null;
        }

        private void AddImageAnnotation(Stream? imageStream, PointF pagePosition, int pageNumber)
        {
            if (imageStream == null)
            {
                return;
            }

            if (_pdfViewer == null || PdfStream == null)
            {
                return;
            }

            var fileStream = new MemoryStream();
            imageStream.Position = 0;
            imageStream.CopyTo(fileStream);
            var image = Syncfusion.Drawing.Image.FromStream(fileStream);
            var size = GetPdfPageSize(PdfStream, pageNumber, PdfGraphicsUnit.Pixel);
            float height = image.Height;
            float width = image.Width;

            if (height > size.Height)
            {
                height = size.Height - pagePosition.Y;
                width = height * image.Width / image.Height;
            }

            if (width > size.Width)
            {
                width = size.Width - pagePosition.X;
                height = width * image.Height / image.Width;
            }

            var stampAnnotation = new StampAnnotation(fileStream, pageNumber, new RectF(pagePosition.X, pagePosition.Y, width, height))
            {
                Name = $"Image{Guid.NewGuid()}",
            };

            _pdfViewer.AddAnnotation(stampAnnotation);
        }

        private SizeF GetPdfPageSize(Stream pdfStream, int pageNumber, PdfGraphicsUnit unit = PdfGraphicsUnit.Inch)
        {
            var width = 0f;
            var height = 0f;
            try
            {
                using var documentLoaded = new PdfLoadedDocument(pdfStream);
                var converter = new PdfUnitConverter();
                if (pageNumber > 0 || pageNumber <= documentLoaded.PageCount)
                {
                    width = converter.ConvertFromPixels(documentLoaded.Pages[pageNumber - 1].Size.Width, unit);
                    height = converter.ConvertFromPixels(documentLoaded.Pages[pageNumber - 1].Size.Height, unit);
                }

                width = converter.ConvertFromPixels(documentLoaded.Pages[0].Size.Width, unit);
                height = converter.ConvertFromPixels(documentLoaded.Pages[0].Size.Height, unit);
            }
            catch (Exception)
            {
                // ignored
            }

            return new SizeF(width, height);
        }
    }
}