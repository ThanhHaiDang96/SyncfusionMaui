using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.Drawing;
using Syncfusion.Maui.PdfViewer;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Redaction;
using PointF = Syncfusion.Drawing.PointF;
using SizeF = Microsoft.Maui.Graphics.SizeF;

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
            await using var redactStream = RedactAnnotation(PdfStream);
            if (redactStream != null)
            {
                _pdfViewer.LoadDocument(redactStream);
            }
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

        [RelayCommand]
        private void PdfViewerAnnotationAdded(object obj)
        {
            if (obj is (SfPdfViewer pdfViewer, AnnotationEventArgs { Annotation: not null } eventArgs))
            {
                _selectedAnnotation = eventArgs.Annotation;
                if (eventArgs.Annotation is SquareAnnotation squareAnnotation)
                {
                    squareAnnotation.Name = $"Redaction{Guid.NewGuid()}";
                    squareAnnotation.FillColor = Colors.White;
                }
                
            }
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

        [RelayCommand]
        private async Task ShowCustomImage()
        {
            try
            {
                if (_selectedAnnotation is StampAnnotation stampAnnotation)
                {
                    await using var imageStream = GetImageStreamFromAnnotation(stampAnnotation);
                    if (imageStream == null)
                    {
                        return;
                    }
                    using var memoryStream = new MemoryStream();
                    imageStream.Position = 0;
                    await imageStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    var imagePropertiesPopupViewModel = new ImagePropertiesPopupViewModel()
                    {
                        Sources = memoryStream.ToArray(),
                        ImageDataSource = new TaskCompletionSource<byte[]?>(memoryStream.ToArray()),
                    };

                    await Shell.Current.Navigation.PushModalAsync(new ImagePropertiesPopup()
                    {
                        BindingContext = imagePropertiesPopupViewModel,
                    });

                    var data = await imagePropertiesPopupViewModel.ImageDataSource.Task;
                    if (data != null)
                    {
                        UpdateImageAnnotation(data, stampAnnotation);
                    }

                }
               
            }
            catch (Exception )
            {
                //
            }
        }

        private void UpdateImageAnnotation(byte[] data, StampAnnotation stampAnnotation)
        {
            var stream = new MemoryStream(data);
            var newAnnotation = new StampAnnotation(stream, stampAnnotation.PageNumber, stampAnnotation.Bounds)
            {
                Name = stampAnnotation.Name,
            };
            _pdfViewer?.AddAnnotation(newAnnotation);
            _pdfViewer?.RemoveAnnotation(stampAnnotation);

            _selectedAnnotation = newAnnotation;
        }

        private Stream? RedactAnnotation(Stream? documentStream)
        {
            if (_pdfViewer == null)
            {
                return null;
            }

            var annotations = _pdfViewer.Annotations.Where(x => x.Name?.Contains("Redaction") == true).ToList();
            if (annotations.Count != 0)
            {
                using var document = new PdfLoadedDocument(documentStream);

                foreach (var annotation in annotations)
                {
                    // Type cast the annotation to get the annotation specific properties.
                    if (annotation is SquareAnnotation squareAnnotation)
                    {
                        //Get the first page from the document
                        if (document.Pages[annotation.PageNumber - 1] is PdfLoadedPage page)
                        {
                            var redaction = new PdfRedaction(
                                bounds: new RectangleF(squareAnnotation.Bounds.X, 
                                    squareAnnotation.Bounds.Y, 
                                    squareAnnotation.Bounds.Width, 
                                    squareAnnotation.Bounds.Height))
                            {
                                //Set fill color for the redaction bounds 
                                FillColor =  Syncfusion.Drawing.Color.MediumPurple,
                            };
                            //Add a redaction object into the redaction collection of loaded page
                            page.AddRedaction(redaction);
                            //Redact the contents from the PDF document
                            document.Redact();
                        }
                    }
                    
                }

                //Creating the stream object
                MemoryStream stream = new MemoryStream();
                //Save the document
                document.Save(stream);

                //Close the document
                document.Close(true);

                return stream;
            }

            return null;
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

        private Stream? GetImageStreamFromAnnotation(StampAnnotation? stampAnnotation)
        {
            if (stampAnnotation == null || _pdfViewer == null)
            {
                return null;
            }

            try
            {
                using Stream mainStream = new MemoryStream();
                _pdfViewer.SaveDocument(outputStream: mainStream);
                using var pdfLoadedDocument = new PdfLoadedDocument(mainStream);
                var pageNumber = stampAnnotation.PageNumber;
                if (pdfLoadedDocument.Pages[pageNumber - 1] is PdfLoadedPage page)
                {
                    foreach (var annotation in page.Annotations)
                    {
                        if (annotation is PdfLoadedRubberStampAnnotation pdfLoadedRubberStampAnnotation && (stampAnnotation.Name == pdfLoadedRubberStampAnnotation.Name))
                        {
                            return pdfLoadedRubberStampAnnotation.GetImages()?.FirstOrDefault();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //ignore
            }


            return null;
        }
    }
}