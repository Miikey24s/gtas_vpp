namespace DesignDnaStudio.Web.Data.Entities;

public enum StudyStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public enum StimulusKind
{
    Atomic = 0,
    Holistic = 1,
    Contextual = 2,
    UploadedReference = 3
}

public enum StimulusPreviewType
{
    Dashboard = 0,
    Login = 1,
    DataGrid = 2,
    Form = 3,
    Moodboard = 4
}

public enum AssessmentStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Abandoned = 3
}

public enum AssessmentPhase
{
    Calibration = 0,
    AdaptiveComparison = 1,
    Completed = 2
}
