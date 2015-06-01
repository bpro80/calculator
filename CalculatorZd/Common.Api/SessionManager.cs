using System.Data;
using System.Web;
using System.Web.SessionState;
using BO;


namespace Common.Api
{
    public static class SessionManager
    

{
    public const string SESSION_FIRM_INFO_KEY = "FirmInfo";
    public const string SESSION_REPORT_FIMR_INFO_KEY = "ReportFirm";
    public const string SESSION_DATATABLE_REPORT_FIMR_INFO_KEY = "ReportDataTableFirm";
    public const string SESSION_CURRENT_IP_KEY = "CURRENT_IP_KEY";
    public const string SESSION_ACCESS_DENIED_KEY = "SESSION_ACCESS_DENIED_KEY";
        
    private static HttpSessionState Session
    {
        get { return HttpContext.Current.Session; }
    }

    /// <summary>
    ///     Stores the FirmInfo in the session.
    /// </summary>
    public static Firm FirmInfo
    {
        get { return Session[SESSION_FIRM_INFO_KEY] as Firm; }
        set { Session[SESSION_FIRM_INFO_KEY] = value; }
    }

    public static string CurrentIP
    {
        get { return Session[SESSION_CURRENT_IP_KEY] as string; }
        set { Session[SESSION_CURRENT_IP_KEY] = value; }
    }
    public static bool? AccessDenied
    {
        get { return (bool?) Session[SESSION_ACCESS_DENIED_KEY]; }
        set { Session[SESSION_ACCESS_DENIED_KEY] = value; }
    }
    public static Firm ReportFirm
    {
        get { return Session[SESSION_REPORT_FIMR_INFO_KEY] as Firm; }
        set { Session[SESSION_REPORT_FIMR_INFO_KEY] = value; }
    }
}
}