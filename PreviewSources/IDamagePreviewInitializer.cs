using BetterDamagePreviews.PreviewVars;

namespace BetterDamagePreviews.PreviewSources;

/// <summary>
/// Performs some initialization before running the calculations.
/// </summary>
public interface IDamagePreviewInitializer
{
    /// <summary>
    /// Perform any initialization before beginning the calculation. Called only once per calculation.
    /// </summary>
    /// <param name="previewVar">The original <see cref="IDamagePreviewVar"/> on the currently previewed card.</param>
    /// <returns><see langword="true"/> if initialization was successful, otherwise <see langword="false"/> if the calculation should be abandoned.</returns>
    public bool Initialize(IDamagePreviewVar previewVar);
}
