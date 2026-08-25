namespace BetterDamagePreviews.Preview;

/// <summary>
/// Indicates how accurate the calculated damage is.
/// </summary>
public enum Accuracy
{
    /// <summary>
    /// The actual damage will be exactly as calculated.
    /// </summary>
    Accurate,

    /// <summary>
    /// The actual damage may be more or less than the calculated damage once the card is played.
    /// </summary>
    Approximate,

    /// <summary>
    /// The actual damage will be at least as much as calculated, but may be more.
    /// </summary>
    Minimum
}