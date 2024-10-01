using CommunityToolkit.Maui.Converters;
using System.Globalization;

namespace SyncfusionIssue.Converters;

public class ImageSourceConverter : BaseConverter<byte[]?, object?>
{
    public override object? ConvertFrom(byte[]? value, CultureInfo? culture)
    {
        return value == null ? null : ImageSource.FromStream(() => new MemoryStream(value));
    }

    public override byte[]? ConvertBackTo(object? value, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }

    public override object? DefaultConvertReturnValue { get; set; }
    public override byte[]? DefaultConvertBackReturnValue { get; set; }
}