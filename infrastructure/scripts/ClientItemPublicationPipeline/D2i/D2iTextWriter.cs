namespace ClientItemPublicationPipeline.D2i;

/// <summary>
/// Alias de escritura staging sobre <see cref="D2iFile"/>.
/// </summary>
internal static class D2iTextWriter
{
    public static D2iFile Load(string path) => D2iFile.Load(path);

    public static void Save(D2iFile file, string outputPath) => file.Save(outputPath);

    public static void CopyToStaging(string sourcePath, string stagingPath) =>
        D2iFile.CopyToStaging(sourcePath, stagingPath);
}
