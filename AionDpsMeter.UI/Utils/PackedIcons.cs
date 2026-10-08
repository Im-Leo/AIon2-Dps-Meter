using System.IO;
using AionDpsMeter.Core;

namespace AionDpsMeter.UI.Utils
{
    /// <summary>
    /// Image source for a skill or buff icon in a Blazor page: the copy shipped with the app when there is one
    /// (served from wwwroot), else the CDN URL.
    /// </summary>
    public static class PackedIcons
    {
        public static string? Src(string? url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
            return File.Exists(Path.Combine(AppPaths.SkillIconPackDirectory, fileName)) ? $"icons/skills/{fileName}" : url;
        }
    }
}
