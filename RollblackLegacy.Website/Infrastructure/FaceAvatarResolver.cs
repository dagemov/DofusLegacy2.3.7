using System.Collections.Frozen;
using RollblackLegacy.Website.Application.Abstractions;

namespace RollblackLegacy.Website.Infrastructure;

public sealed class FaceAvatarResolver : IFaceAvatarResolver
{
    private const string FacesRelative = "wwwroot/images/faces/persos/race/heads";
    private const string FacesWebPath = "/images/faces/persos/race/heads";
    private const string FallbackFace = "1_0";

    private readonly IWebHostEnvironment _environment;
    private FrozenSet<string>? _knownFaces;

    public FaceAvatarResolver(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string ResolvePath(string? seed)
    {
        FrozenSet<string> faces = GetKnownFaces();
        if (faces.Count == 0)
            return $"{FacesWebPath}/{FallbackFace}.png";

        string key = string.IsNullOrWhiteSpace(seed) ? "guest" : seed.Trim();
        int index = Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(key)) % faces.Count;
        string face = faces.ElementAt(index);
        return $"{FacesWebPath}/{face}.png";
    }

    private FrozenSet<string> GetKnownFaces()
    {
        if (_knownFaces is not null)
            return _knownFaces;

        string directory = Path.Combine(_environment.ContentRootPath, FacesRelative);
        if (!Directory.Exists(directory))
        {
            _knownFaces = new[] { FallbackFace }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            return _knownFaces;
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.EnumerateFiles(directory, "*.png"))
        {
            string? name = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(name)
                || string.Equals(name, "none", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "0", StringComparison.OrdinalIgnoreCase)
                || !name.Contains('_', StringComparison.Ordinal))
            {
                continue;
            }

            set.Add(name);
        }

        if (set.Count == 0)
            set.Add(FallbackFace);

        _knownFaces = set.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        return _knownFaces;
    }
}
