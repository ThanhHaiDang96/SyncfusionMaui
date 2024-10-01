using Syncfusion.Maui.ImageEditor;
using Syncfusion.Maui.Sliders;

namespace SyncfusionIssue;

public partial class ImagePropertiesPopup : ContentPage
{
	public ImagePropertiesPopup()
	{
		InitializeComponent();
	}
	private void RotateImageOnClicked(object? sender, EventArgs e)
	{
		try
		{
			ImageEditor.Rotate();
		}
		catch (Exception)
		{
			// ignore
		}

	}

	private void OnBrightnessValueChanged(object? sender, SliderValueChangedEventArgs e)
	{
		try
		{
			ImageEditor.ImageEffect(ImageEffect.Brightness, e.NewValue / 100);
		}
		catch (Exception)
		{
			// ignore
		}

	}

	private void OnContrastValueChanged(object? sender, SliderValueChangedEventArgs e)
	{
		try
		{
			ImageEditor.ImageEffect(ImageEffect.Contrast, e.NewValue / 100);
		}
		catch (Exception)
		{
			// ignore
		}       

	}

	private void FlipHorizontal(object? sender, EventArgs e)
	{
		try
		{
			ImageEditor.Flip(ImageFlipDirection.Horizontal);
		}
		catch (Exception)
		{
			// ignore
		}
       
	}

	private void FlipVertical(object? sender, EventArgs e)
	{
		try
		{
			ImageEditor.Flip(ImageFlipDirection.Vertical);
		}
		catch (Exception)
		{
			// ignore
		}


	}
}