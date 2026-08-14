namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Holds the ASP.NET Core content root (the site's actual source directory), captured once
    /// at startup from IWebHostEnvironment.ContentRootPath. </summary>
    /// <remarks> Under the old WebForms/IIS hosting model, AppDomain.CurrentDomain.BaseDirectory pointed at
    /// the site root, since IIS ran the app directly from source. Under Kestrel it points at wherever the
    /// assemblies actually run from instead (bin/Debug/net10.0 in dev, or the publish folder), which breaks
    /// any code still using it to locate on-disk site content such as design/, default/, or config/. This
    /// gateway is the replacement -- set once in Program.cs right after the app is built, before any request
    /// is served, so no locking is needed for the read-only access afterward. Named "AppRoot" rather than
    /// after ASP.NET Core's own "ContentRoot" term: this codebase already has a Content/ directory holding
    /// digital resource images/metadata (often its own virtual directory or sub-app) plus WebContent static
    /// pages, so "ContentRoot" read as related to those and was misleading. </remarks>
    public static class AppRoot_Gateway
    {
        /// <summary> The site's root directory on disk; set once at startup in Program.cs </summary>
        public static string AppRootPath { get; set; }
    }
}
