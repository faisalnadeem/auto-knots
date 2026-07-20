namespace AutoKnots.Models;

public enum InspectionStatus
{
    NotInspected = 0,
    Pending = 1,
    Passed = 2,
    PassedWithIssues = 3,
    Failed = 4
}
