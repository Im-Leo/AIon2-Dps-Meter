using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionDpsMeter.Core.GameData.Services;

// Downloads every skill and buff icon the app can show, resized for the app, into the UI's wwwroot/icons/skills,
// so they ship with the app instead of loading from the CDN on first use. Rerun when the game data adds skills.
// Usage: dotnet run --project tools/SkillIconPacker [output folder]
const int Size = 128;
string output = args.Length > 0 ? args[0] : Path.Combine(FindRepoRoot(), "AionDpsMeter.UI", "wwwroot", "icons", "skills");
Directory.CreateDirectory(output);

// Icons of the known skills, plus every name in skill_icons.json: those also cover skill codes missing from skills.json.
const string CdnBase = "https://assets.playnccdn.com/static-aion2-gamedata/resources";
var mapped = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GameData", "Assets", "skill_icons.json")))!;
var urls = GameDataProvider.Instance.Skills.GetAll()
    .Select(s => s.Icon)
    .Concat(mapped.Values.Select(name => $"{CdnBase}/{name}.png"))
    .Where(u => !string.IsNullOrEmpty(u))
    .Distinct()
    .ToList();
Console.WriteLine($"{urls.Count} icons to pack into {output}");

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:135.0) Gecko/20100101 Firefox/135.0");
int written = 0, missing = 0;
long bytes = 0;
foreach (var url in urls)
{
    var file = Path.Combine(output, Path.GetFileName(new Uri(url!).AbsolutePath));
    byte[] source;
    try { source = await http.GetByteArrayAsync(url); }
    catch (HttpRequestException ex) { Console.WriteLine($"  missing {url}: {(int?)ex.StatusCode}"); missing++; continue; }

    var frame = BitmapFrame.Create(new MemoryStream(source), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
    var scaled = new TransformedBitmap(frame, new ScaleTransform((double)Size / frame.PixelWidth, (double)Size / frame.PixelHeight));
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(scaled));
    using (var stream = File.Create(file)) encoder.Save(stream);
    bytes += new FileInfo(file).Length;
    written++;
}
Console.WriteLine($"written {written}, missing on the CDN {missing}, total {bytes / 1024} KB");

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AionDpsMeter.slnx"))) dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Repository root (AionDpsMeter.slnx) not found; pass the output folder.");
}
