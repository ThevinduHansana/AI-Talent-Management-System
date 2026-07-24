using System.Text;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Prompts, JSON schemas and profile serialization shared by the Claude-backed AI services.
/// System prompts are constants so the provider can cache them across requests; everything
/// request-specific goes in the user prompt.
/// </summary>
internal static class AiPrompts
{
    // ---------------------------------------------------------------- system prompts

    public const string ResumeAnalystSystem = """
        You are an experienced technical recruiter reviewing candidate resumes for an applicant
        tracking system. Work only from the resume text you are given.

        Rules:
        - Extract skills the resume actually evidences. Prefer the canonical name from the skill
          catalogue when the resume names the same skill differently ("ReactJS" -> "React").
        - Never invent employers, degrees, dates or skills that are not in the text.
        - completeness_score (0-100) reflects how complete and well-structured the resume is as a
          document, not how strong the candidate is.
        - Insights, strengths and gaps are addressed to the candidate, are specific to this resume,
          and are actionable. No generic filler.
        """;

    public const string MatchingSystem = """
        You are a hiring-fit assessor for an applicant tracking system. Score how well one
        candidate matches one job.

        Rules:
        - score is 0-100: 85+ strong match ready to interview, 65-84 solid with some gaps,
          40-64 partial, below 40 weak.
        - Weigh required skills far more heavily than preferred ones, and respect each skill's
          stated weight and minimum proficiency. Account for relevant years of experience and
          seniority level, and treat adjacent/transferable experience as partial credit.
        - Judge only on evidence in the profile. Absence of evidence is a gap, not a negative.
        - Ignore age, gender, nationality, names and any other protected attribute; they must not
          influence the score.
        - Be calibrated and consistent: the same profile against the same job must score the same.
        """;

    public const string FeedbackSystem = """
        You are a career coach writing feedback for a candidate about a job they applied to.
        Address the candidate directly as "you". Be honest, specific and encouraging — never
        dismissive, never falsely reassuring.

        Rules:
        - Ground every point in the candidate's actual profile against the actual job requirements.
        - Gaps describe what the role asks for that the profile does not yet evidence.
        - Recommendations are concrete next steps the candidate can act on.
        - Do not speculate about the hiring decision, other applicants, or why they were rejected.
        - Never reference age, gender, nationality or any other protected attribute.
        """;

    public const string RecommendationSystem = """
        You are a job-matching assistant. Given one candidate profile and a shortlist of open
        roles, re-rank the roles by genuine fit and explain each briefly.

        Rules:
        - Return every job id you were given, best fit first, with no duplicates and no invented ids.
        - score is 0-100 using the same scale as hiring fit: 85+ strong, 65-84 solid, 40-64 partial.
        - rationale is one sentence, addressed to the candidate, naming the concrete reason for the
          fit ("your 4 years of Kubernetes and CI/CD ownership line up with the platform work here").
        - Judge on skills, experience and stated preferences only; ignore protected attributes.
        """;

    // ---------------------------------------------------------------- response schemas

    public const string ResumeAnalysisSchema = """
        {
          "type": "object",
          "properties": {
            "summary": { "type": "string", "description": "Two-sentence professional summary of the candidate." },
            "skills": { "type": "array", "items": { "type": "string" }, "description": "Skills evidenced by the resume." },
            "sections": { "type": "array", "items": { "type": "string" }, "description": "Sections present, e.g. Contact, Summary, Experience, Education, Skills, Projects, Certifications." },
            "completeness_score": { "type": "integer", "description": "0-100 rating of resume completeness and structure." },
            "insights": { "type": "array", "items": { "type": "string" }, "description": "3-6 actionable observations addressed to the candidate." },
            "strengths": { "type": "array", "items": { "type": "string" }, "description": "2-5 genuine strengths this resume demonstrates." },
            "gaps": { "type": "array", "items": { "type": "string" }, "description": "2-5 gaps or weaknesses to address." },
            "suggested_roles": { "type": "array", "items": { "type": "string" }, "description": "2-5 job titles this candidate is well positioned for." }
          },
          "required": ["summary", "skills", "sections", "completeness_score", "insights", "strengths", "gaps", "suggested_roles"],
          "additionalProperties": false
        }
        """;

    public const string MatchScoreSchema = """
        {
          "type": "object",
          "properties": {
            "score": { "type": "number", "description": "Overall fit, 0-100." },
            "reasoning": { "type": "string", "description": "One or two sentences justifying the score." },
            "matched_skills": { "type": "array", "items": { "type": "string" } },
            "missing_skills": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["score", "reasoning", "matched_skills", "missing_skills"],
          "additionalProperties": false
        }
        """;

    public const string FeedbackSchema = """
        {
          "type": "object",
          "properties": {
            "score": { "type": "number", "description": "Fit of this candidate for this role, 0-100." },
            "summary": { "type": "string", "description": "Two to three sentences on how this application lines up with the role." },
            "strengths": { "type": "array", "items": { "type": "string" }, "description": "2-5 aspects of the profile that fit the role." },
            "gaps": { "type": "array", "items": { "type": "string" }, "description": "2-5 requirements the profile does not yet evidence." },
            "recommendations": { "type": "array", "items": { "type": "string" }, "description": "2-5 concrete next steps." }
          },
          "required": ["score", "summary", "strengths", "gaps", "recommendations"],
          "additionalProperties": false
        }
        """;

    public const string RecommendationSchema = """
        {
          "type": "object",
          "properties": {
            "recommendations": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "job_id": { "type": "string", "description": "The job id exactly as supplied." },
                  "score": { "type": "number", "description": "Fit score, 0-100." },
                  "rationale": { "type": "string", "description": "One sentence addressed to the candidate." }
                },
                "required": ["job_id", "score", "rationale"],
                "additionalProperties": false
              }
            }
          },
          "required": ["recommendations"],
          "additionalProperties": false
        }
        """;

    // ---------------------------------------------------------------- profile serialization

    /// <summary>
    /// Renders a job as plain text for the model. Requires <c>JobSkills.Skill</c>,
    /// <c>Organization</c> and <c>Department</c> to be loaded.
    /// </summary>
    public static string DescribeJob(Job job, bool includeFullDescription = true)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Title: {job.Title}");
        builder.AppendLine($"Organization: {job.Organization?.Name ?? "n/a"}");
        if (job.Department is not null) builder.AppendLine($"Department: {job.Department.Name}");
        builder.AppendLine($"Seniority: {job.ExperienceLevel} | Employment: {job.EmploymentType}");
        builder.AppendLine($"Location: {(job.IsRemote ? "Remote" : job.Location ?? "n/a")}");

        if (includeFullDescription)
        {
            builder.AppendLine($"Description: {Trim(job.Description, 2500)}");
            if (!string.IsNullOrWhiteSpace(job.Responsibilities)) builder.AppendLine($"Responsibilities: {Trim(job.Responsibilities, 1500)}");
            if (!string.IsNullOrWhiteSpace(job.Requirements)) builder.AppendLine($"Requirements: {Trim(job.Requirements, 1500)}");
        }

        if (job.JobSkills.Count > 0)
        {
            builder.AppendLine("Required/preferred skills (name, required?, minimum proficiency, weight 1-10):");
            foreach (var skill in job.JobSkills.OrderByDescending(s => s.Weight))
            {
                var kind = skill.IsRequired ? "required" : "preferred";
                builder.AppendLine($"  - {skill.Skill?.Name ?? "unknown"} ({kind}, min {skill.MinimumProficiency}, weight {skill.Weight})");
            }
        }
        else
        {
            builder.AppendLine("Required/preferred skills: none specified.");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders a candidate profile as plain text. Requires <c>CandidateSkills.Skill</c>,
    /// <c>Experiences</c> and <c>Educations</c> to be loaded.
    /// </summary>
    public static string DescribeCandidate(Candidate candidate, string? resumeText = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Headline: {candidate.Headline ?? "n/a"}");
        builder.AppendLine($"Current position: {candidate.CurrentPosition ?? "n/a"}");
        builder.AppendLine($"Total years of experience: {candidate.YearsOfExperience}");
        builder.AppendLine($"Location: {candidate.Location ?? "n/a"} | Availability: {candidate.AvailabilityStatus}");
        if (!string.IsNullOrWhiteSpace(candidate.Summary)) builder.AppendLine($"Summary: {Trim(candidate.Summary, 1200)}");

        if (candidate.CandidateSkills.Count > 0)
        {
            builder.AppendLine("Skills (name, proficiency, years):");
            foreach (var skill in candidate.CandidateSkills.OrderByDescending(s => s.ProficiencyLevel))
            {
                builder.AppendLine($"  - {skill.Skill?.Name ?? "unknown"} ({skill.ProficiencyLevel}, {skill.YearsOfExperience}y)");
            }
        }
        else
        {
            builder.AppendLine("Skills: none listed on the profile.");
        }

        if (candidate.Experiences.Count > 0)
        {
            builder.AppendLine("Experience:");
            foreach (var experience in candidate.Experiences.OrderByDescending(e => e.StartDate).Take(8))
            {
                var end = experience.IsCurrent ? "present" : experience.EndDate?.ToString("yyyy-MM") ?? "n/a";
                builder.AppendLine($"  - {experience.Title} at {experience.Company} ({experience.StartDate:yyyy-MM} to {end})");
                if (!string.IsNullOrWhiteSpace(experience.Description))
                {
                    builder.AppendLine($"    {Trim(experience.Description, 400)}");
                }
            }
        }

        if (candidate.Educations.Count > 0)
        {
            builder.AppendLine("Education:");
            foreach (var education in candidate.Educations.OrderByDescending(e => e.StartDate).Take(5))
            {
                builder.AppendLine($"  - {education.Degree}{(string.IsNullOrWhiteSpace(education.FieldOfStudy) ? "" : $" in {education.FieldOfStudy}")}, {education.Institution}");
            }
        }

        if (!string.IsNullOrWhiteSpace(resumeText))
        {
            builder.AppendLine("Resume text:");
            builder.AppendLine(Trim(resumeText, 6000));
        }

        return builder.ToString();
    }

    public static string Trim(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }
}
