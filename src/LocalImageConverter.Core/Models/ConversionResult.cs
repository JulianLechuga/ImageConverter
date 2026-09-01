namespace LocalImageConverter.Core.Models;

public class ConversionResult
{
    public bool Success { get; set; }
    public required string SourceFilePath { get; set; }
    public string? DestinationFilePath { get; set; }
    public long OriginalBytes { get; set; }
    public long ConvertedBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }

    public static ConversionResult Ok(string source, string dest, long origBytes, long convBytes, long durationMs) =>
        new()
        {
            Success = true,
            SourceFilePath = source,
            DestinationFilePath = dest,
            OriginalBytes = origBytes,
            ConvertedBytes = convBytes,
            DurationMs = durationMs
        };

    public static ConversionResult Fail(string source, string errorMessage, long origBytes = 0) =>
        new()
        {
            Success = false,
            SourceFilePath = source,
            ErrorMessage = errorMessage,
            OriginalBytes = origBytes
        };
}
