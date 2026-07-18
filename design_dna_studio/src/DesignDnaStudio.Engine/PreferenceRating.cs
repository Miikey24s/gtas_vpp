namespace DesignDnaStudio.Engine;

public enum PreferenceRating
{
    StrongRight = -2,
    SlightRight = -1,
    Tie = 0,
    SlightLeft = 1,
    StrongLeft = 2
}

public enum TieReason
{
    None = 0,
    BothLiked = 1,
    BothDisliked = 2,
    NoVisibleDifference = 3,
    ContextDependent = 4
}

public enum SkipReason
{
    None = 0,
    NotRelevant = 1,
    CannotEvaluate = 2,
    StimulusProblem = 3
}
