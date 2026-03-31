namespace SfidNet.EntityFramework;

/// <summary>
/// Defines how typed Snowfake identifiers should be stored in the database.
/// </summary>
public enum SfidStorageKind
{
    /// <summary>
    /// Store the identifier as a 64-bit integer.
    /// </summary>
    Int64,

    /// <summary>
    /// Store the identifier as a string for providers that do not support 64-bit integers well.
    /// </summary>
    String,
}
