namespace DesignDnaStudio.Engine;

public sealed record PreferenceObservation(
    DesignVector Left,
    DesignVector Right,
    PreferenceRating? Rating,
    TieReason TieReason = TieReason.None,
    SkipReason SkipReason = SkipReason.None)
{
    public bool IsSkipped => Rating is null;
}
