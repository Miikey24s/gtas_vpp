namespace gtas_vpp_fe.Components.DesignSystem.Composites;

public enum VppWorkflowStepState
{
    Pending,
    Active,
    Complete
}

public sealed record VppWorkflowStep(
    string Key,
    int Number,
    string Label,
    VppWorkflowStepState State,
    bool IsEnabled = true);
