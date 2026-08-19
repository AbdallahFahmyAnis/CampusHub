namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>
/// Spec-driven story keys. Keep in sync with specs/jira/README.md and specs/jira/jira-keys.json.
/// </summary>
public static class SddStories
{
    public const string ChS01Tenants = "CH-S01";
    public const string ChS02Invites = "CH-S02";
    public const string ChS03Billing = "CH-S03";
    public const string ChS04OpsCampus = "CH-S04";
    public const string ChS05Notifications = "CH-S05";
    public const string ChS06Certificates = "CH-S06";
    public const string ChS07Analytics = "CH-S07";
    public const string ChS08ChatTutor = "CH-S08";
    public const string ChS09Discovery = "CH-S09";
    public const string ChS10Progress = "CH-S10";
    public const string ChS11Quizzes = "CH-S11";
    public const string ChS12Assignments = "CH-S12";
    public const string ChS13Notes = "CH-S13";
    public const string ChS14Announcements = "CH-S14";
    public const string ChS15Gradebook = "CH-S15";
    public const string ChS16DueDates = "CH-S16";
    public const string ChS17Auth = "CH-S17";
    public const string ChS18Account = "CH-S18";
    public const string ChS19Enroll = "CH-S19";
    public const string ChS20Player = "CH-S20";
    public const string ChS21CoursePass = "CH-S21";

    public static string SpecPath(string storyId) => storyId switch
    {
        ChS01Tenants => "specs/003-tenants-plans",
        ChS02Invites => "specs/004-invites-people",
        ChS03Billing => "specs/005-mock-billing",
        ChS04OpsCampus => "specs/006-ops-campus",
        ChS05Notifications => "specs/007-notifications",
        ChS06Certificates => "specs/008-certificates",
        ChS07Analytics => "specs/009-course-analytics",
        ChS08ChatTutor => "specs/010-chat-ai-tutor",
        ChS09Discovery => "specs/011-catalog-discovery",
        ChS10Progress => "specs/012-progress-dashboard",
        ChS11Quizzes => "specs/013-quizzes",
        ChS12Assignments => "specs/014-assignments",
        ChS13Notes => "specs/015-lecture-notes",
        ChS14Announcements => "specs/016-announcements",
        ChS15Gradebook => "specs/001-course-gradebook",
        ChS16DueDates => "specs/002-assignment-due-dates",
        ChS17Auth => "specs/017-auth-session",
        ChS18Account => "specs/018-account-profile",
        ChS19Enroll => "specs/019-enroll-checkout",
        ChS20Player => "specs/020-course-player",
        ChS21CoursePass => "specs/021-course-pass",
        _ => "specs/000-product",
    };
}
