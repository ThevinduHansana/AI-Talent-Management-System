namespace RecruitmentPlatform.Domain.Enums;

public enum Gender
{
    NotSpecified = 0,
    Male = 1,
    Female = 2,
    Other = 3
}

public enum EmploymentType
{
    FullTime = 0,
    PartTime = 1,
    Contract = 2,
    Internship = 3,
    Temporary = 4,
    Freelance = 5
}

public enum ExperienceLevel
{
    Entry = 0,
    Junior = 1,
    Mid = 2,
    Senior = 3,
    Lead = 4,
    Executive = 5
}

public enum JobStatus
{
    Draft = 0,
    Open = 1,
    OnHold = 2,
    Closed = 3
}

public enum ApplicationStatus
{
    Applied = 0,
    UnderReview = 1,
    Shortlisted = 2,
    InterviewScheduled = 3,
    Interviewed = 4,
    Offered = 5,
    Hired = 6,
    Rejected = 7,
    Withdrawn = 8
}

public enum ProficiencyLevel
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
    Expert = 3
}

public enum AvailabilityStatus
{
    Available = 0,
    OpenToOffers = 1,
    Employed = 2,
    NotAvailable = 3
}

public enum InterviewMode
{
    Onsite = 0,
    Video = 1,
    Phone = 2
}

public enum InterviewStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    Rescheduled = 3,
    NoShow = 4
}

public enum HiringRecommendation
{
    StrongNo = 0,
    No = 1,
    Yes = 2,
    StrongYes = 3
}

public enum EvaluationDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum NotificationType
{
    General = 0,
    Application = 1,
    Interview = 2,
    Job = 3,
    System = 4,
    Message = 5
}
