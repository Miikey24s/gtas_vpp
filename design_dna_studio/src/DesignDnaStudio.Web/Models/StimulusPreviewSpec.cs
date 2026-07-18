namespace DesignDnaStudio.Web.Models;

public sealed record StimulusPreviewSpec(
    string Palette,
    string Accent,
    string Surface,
    string Layout,
    string Radius,
    string Density,
    string Typography,
    string Texture,
    string Elevation,
    string Imagery,
    string Motion,
    string LabelMode,
    string Disclosure,
    string Context,
    int ExpressionLevel = 2);
