namespace FiscoProjects.SharedKernel;

/// <summary>
/// The product and its brand (ADR-0159). An installation shows this name while nobody changes it, and where the name
/// is the one of the product the logo stands for it (ADR-0160).
/// </summary>
public static class Product
{
    /// <summary>The name of the product, written as the brand writes it.</summary>
    public const string Name = "GerenWare";

    /// <summary>
    /// The horizontal logo for a light background, as the site serves it from its root (web/public, a copy of
    /// docs/marca): a PNG, because e-mail programs do not show SVG.
    /// </summary>
    public const string LogoPath = "/gerenware-horizontal-fundo-claro-64px.png";
}
