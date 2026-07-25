namespace SobekCM.Engine_Library
{
    /// <summary> Previously an IHttpModule that rewrote /engine/... paths to sobekcm.svc?urlrelative=...
    /// In ASP.NET Core, this URL mapping is handled by routing middleware in the web host project.
    /// This class is retained as a placeholder and can be removed once the web project is converted. </summary>
    public class MicroserviceRewriter
    {
    }
}
