namespace LocalImageConverter.Core.Models;

public enum ItemStatus
{
    Waiting,
    Processing,
    Completed,
    Error,
    Cancelled
}

public enum ResizeMode
{
    KeepOriginal,
    MaxDimension,
    CustomDimensions
}

public enum MetadataOption
{
    KeepAll,
    StripPrivateGpsExif,
    StripAll
}

public enum ConflictResolution
{
    AutoRename,
    Overwrite,
    Skip
}

public enum AlphaBackgroundColor
{
    White,
    Black,
    CustomHex
}

public enum OutputDirectoryMode
{
    ConvertedSubfolder,
    CustomFolder,
    SameFolderAsOriginal
}
