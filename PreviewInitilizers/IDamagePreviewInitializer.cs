using BetterDamagePreviews.Preview;

namespace BetterDamagePreviews.PreviewInitilizers;

/// <summary>
/// Performs some initialization before running the calculations.
/// </summary>
public interface IDamagePreviewInitializer
{
    /// <summary>
    /// Perform any initialization before beginning the calculation. Called only once per calculation.
    /// </summary>
    /// <param name="preview">The original <see cref="IDamagePreview"/> on the currently previewed card.</param>
    /// <returns><see langword="true"/> if initialization was successful, otherwise <see langword="false"/> if the calculation should be abandoned.</returns>
    public bool Initialize(IDamagePreview preview);
}
