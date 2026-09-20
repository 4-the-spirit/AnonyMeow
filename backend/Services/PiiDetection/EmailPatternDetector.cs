using System.Text.RegularExpressions;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.PiiDetection;

public partial class EmailPatternDetector : IPiiDetector
{
    public PiiDetectorType DetectorType => PiiDetectorType.EmailPattern;
    public string Category => "Email";

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailPattern();

    public bool IsMatch(string text) => EmailPattern().IsMatch(text);
}
