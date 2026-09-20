using System.Text.RegularExpressions;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.PiiDetection;

public partial class PhoneNumberDetector : IPiiDetector
{
    public PiiDetectorType DetectorType => PiiDetectorType.PhoneNumber;
    public string Category => "Phone Number";

    // Matches common phone formats: optional country code, then 3 groups of digits separated by
    // spaces/dashes/dots/parens — e.g. "555-123-4567", "(555) 123 4567", "+1 555.123.4567".
    [GeneratedRegex(@"(?<!\d)(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]\d{3}[-.\s]\d{4}(?!\d)")]
    private static partial Regex PhonePattern();

    public bool IsMatch(string text) => PhonePattern().IsMatch(text);
}
