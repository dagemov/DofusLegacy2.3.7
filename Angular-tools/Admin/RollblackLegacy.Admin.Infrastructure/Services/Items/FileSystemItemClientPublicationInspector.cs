using System.Buffers.Binary;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Infrastructure.Configuration;
using RollblackLegacy.Admin.Infrastructure.Services.ClientIdentity;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class FileSystemItemClientPublicationInspector : IItemClientPublicationInspector
{
    private readonly string _contentRootPath;
    private readonly AdminClientIdentityOptions _identityOptions;
    private readonly object _syncRoot = new();
    private HashSet<int>? _knownItemTemplateIds;
    private HashSet<int>? _knownItemTypeIds;
    private string? _knownItemsD2oPath;
    private string? _knownItemTypesD2oPath;
    private string? _knownClientRootPath;
    private string? _knownFailureReason;

    public FileSystemItemClientPublicationInspector(
        IHostEnvironment hostEnvironment,
        IOptions<AdminClientIdentityOptions> identityOptions)
    {
        _contentRootPath = hostEnvironment.ContentRootPath;
        _identityOptions = identityOptions.Value;
    }

    public Task<ItemClientPublicationAuditResult> InspectAsync(
        int itemId,
        int typeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            EnsureCacheLoaded();

            if (_knownItemTemplateIds is null)
            {
                return Task.FromResult(new ItemClientPublicationAuditResult(
                    ClientDataAvailable: false,
                    TemplateKnown: false,
                    TypeKnown: false,
                    ClientRootPath: _knownClientRootPath,
                    ItemsD2oPath: _knownItemsD2oPath,
                    ItemTypesD2oPath: _knownItemTypesD2oPath,
                    FailureReason: _knownFailureReason));
            }

            return Task.FromResult(new ItemClientPublicationAuditResult(
                ClientDataAvailable: true,
                TemplateKnown: _knownItemTemplateIds.Contains(itemId),
                TypeKnown: _knownItemTypeIds?.Contains(typeId) == true,
                ClientRootPath: _knownClientRootPath,
                ItemsD2oPath: _knownItemsD2oPath,
                ItemTypesD2oPath: _knownItemTypesD2oPath,
                FailureReason: null));
        }
    }

    private void EnsureCacheLoaded()
    {
        if (_knownItemTemplateIds is not null || _knownFailureReason is not null)
        {
            return;
        }

        var paths = ClientIdentityRepositoryPathResolver.Resolve(_contentRootPath, _identityOptions);
        _knownClientRootPath = paths.ClientRootPath;
        _knownItemsD2oPath = paths.ItemsD2oPath;
        _knownItemTypesD2oPath = paths.ItemTypesD2oPath;

        if (!File.Exists(_knownItemsD2oPath))
        {
            _knownFailureReason = $"Items.d2o was not found at '{_knownItemsD2oPath}'.";
            return;
        }

        try
        {
            _knownItemTemplateIds = ReadD2oIndexIds(_knownItemsD2oPath);
            _knownItemTypeIds = File.Exists(_knownItemTypesD2oPath)
                ? ReadD2oIndexIds(_knownItemTypesD2oPath)
                : new HashSet<int>();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _knownFailureReason = exception.Message;
        }
    }

    private static HashSet<int> ReadD2oIndexIds(string itemsD2oPath)
    {
        using var stream = File.OpenRead(itemsD2oPath);
        var header = new byte[3];
        stream.ReadExactly(header);

        var headerText = System.Text.Encoding.ASCII.GetString(header);
        if (!string.Equals(headerText, "D2O", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unexpected D2O header '{headerText}' in '{itemsD2oPath}'.");
        }

        var buffer = new byte[4];
        stream.ReadExactly(buffer);
        var headerOffset = BinaryPrimitives.ReadInt32BigEndian(buffer);
        stream.Position = headerOffset;

        stream.ReadExactly(buffer);
        var indexLength = BinaryPrimitives.ReadInt32BigEndian(buffer);
        if (indexLength < 0 || indexLength % 8 != 0)
        {
            throw new InvalidDataException($"The Items.d2o index length '{indexLength}' is invalid.");
        }

        var count = indexLength / 8;
        var templateIds = new HashSet<int>(count);
        for (var index = 0; index < count; index++)
        {
            stream.ReadExactly(buffer);
            var templateId = BinaryPrimitives.ReadInt32BigEndian(buffer);
            stream.ReadExactly(buffer);
            templateIds.Add(templateId);
        }

        return templateIds;
    }
}
