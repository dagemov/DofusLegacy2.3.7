using ClientItemPublicationPipeline.D2i;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.Package;

internal sealed class PublicationPackageBuilder
{
    public PublicationPackageBuildResult Build(PublicationPackageBuildRequest request)
    {
        var packageDirectory = request.PackageDirectory;
        Directory.CreateDirectory(packageDirectory);

        var dataCommon = Path.Combine(packageDirectory, "data", "common");
        var dataI18n = Path.Combine(packageDirectory, "data", "i18n");
        Directory.CreateDirectory(dataCommon);
        Directory.CreateDirectory(dataI18n);

        var d2oPublisher = new D2o.D2oStagingPublisher();
        var d2oWork = Path.Combine(packageDirectory, "d2o-work");
        Directory.CreateDirectory(d2oWork);

        var clone = d2oPublisher.CloneItem(
            request.SourceItemsD2oPath,
            d2oWork,
            request.SourceTemplateItemId,
            request.TargetItemId,
            request.TypeId,
            request.IconId,
            request.AppearanceId);

        var stagingItems = clone.StagingItemsPath;
        var reader = new Sunshine.Protocol.Tools.D2o.D2OReader(stagingItems);
        var item = reader.ReadObject<D2oItem>(request.TargetItemId, true)
            ?? throw new InvalidOperationException($"Item {request.TargetItemId} no encontrado tras clone.");
        reader.Close();

        item.nameId = request.I18n.NameId;
        item.descriptionId = request.I18n.DescriptionId;

        var writer = new Sunshine.Protocol.Tools.D2o.D2OWriter(stagingItems);
        writer.StartWriting(backupFile: false);
        writer.Write(item, request.TargetItemId);
        writer.EndWriting();

        var packageItems = Path.Combine(dataCommon, "Items.d2o");
        var packageEs = Path.Combine(dataI18n, "i18n_es.d2i");
        var packageEn = Path.Combine(dataI18n, "i18n_en.d2i");

        WriteFileCopy(stagingItems, packageItems);
        WriteFileCopy(request.I18n.StagingEsPath, packageEs);
        WriteFileCopy(request.I18n.StagingEnPath, packageEn);

        var generatedFiles = new[]
        {
            PublicationPackagePaths.ItemsRelative,
            PublicationPackagePaths.I18nEsRelative,
            PublicationPackagePaths.I18nEnRelative
        };

        var checksums = PublicationPackageChecksumWriter.ComputeChecksums(packageDirectory, generatedFiles);
        PublicationPackageChecksumWriter.WriteChecksumsFile(packageDirectory, checksums);

        var packageId = $"staging-publication-{request.TargetItemId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var manifest = new PublicationPackageManifestDocument
        {
            PackageId = packageId,
            CreatedAt = DateTimeOffset.UtcNow,
            SourceTemplateItemId = request.SourceTemplateItemId,
            TargetItemId = request.TargetItemId,
            NameId = request.I18n.NameId,
            DescriptionId = request.I18n.DescriptionId,
            IdModel = "Mismo textId en i18n_es.d2i e i18n_en.d2i; texto distinto por archivo.",
            GeneratedFiles = generatedFiles,
            Checksums = checksums,
            ValidationStatus = string.Empty,
            IsProductionPackage = false,
            NextManualSteps =
            [
                "Ejecutar --mode validate-publication-package sobre este directorio.",
                "No copiar a Client2.3.7 original hasta Phase 4."
            ]
        };

        PublicationPackageValidator.WriteManifest(packageDirectory, manifest);

        var validator = new PublicationPackageValidator();
        var validation = validator.Validate(
            new PublicationPackageValidationRequest(
                packageDirectory,
                request.RepoRoot,
                request.ClientRootPath,
                request.AdminByIconPreviewDirectory,
                request.ClientItemTypesD2oPath,
                request.TargetItemId,
                request.TypeId,
                request.I18n.NameId,
                request.I18n.DescriptionId,
                request.AppearanceId));

        validator.WriteReports(packageDirectory, validation, manifest with
        {
            ValidationStatus = validation.ValidationStatus,
            BlockingReasons = validation.BlockingReasons,
            Warnings = validation.Warnings,
            NextManualSteps = validation.NextManualSteps,
            Checksums = validation.Checksums
        });

        return new PublicationPackageBuildResult(
            packageDirectory,
            packageId,
            validation.IsValid,
            validation.ValidationStatus,
            manifest.NameId,
            manifest.DescriptionId);
    }

    private static void WriteFileCopy(string sourcePath, string destinationPath) =>
        File.WriteAllBytes(destinationPath, File.ReadAllBytes(sourcePath));
}

internal sealed record PublicationPackageBuildRequest(
    string RepoRoot,
    string PackageDirectory,
    string SourceItemsD2oPath,
    string ClientRootPath,
    string AdminByIconPreviewDirectory,
    string? ClientItemTypesD2oPath,
    int SourceTemplateItemId,
    int TargetItemId,
    int TypeId,
    int IconId,
    int AppearanceId,
    D2iAppendTextResult I18n);

internal sealed record PublicationPackageBuildResult(
    string PackageDirectory,
    string PackageId,
    bool ValidationPassed,
    string ValidationStatus,
    int NameId,
    int DescriptionId);
