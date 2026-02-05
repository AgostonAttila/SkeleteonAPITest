using System.Text.Encodings.Web;

namespace TestAPI.Services;

public interface IInputSanitizer
{
    string Sanitize(string input);
}

public class InputSanitizer : IInputSanitizer
{
    private readonly HtmlEncoder _encoder;

    public InputSanitizer(HtmlEncoder encoder)
    {
        _encoder = encoder;
    }

    public string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        return _encoder.Encode(input);
    }
}
