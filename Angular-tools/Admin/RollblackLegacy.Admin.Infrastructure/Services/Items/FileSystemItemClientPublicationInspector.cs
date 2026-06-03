using System.Buffers.Binary;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Infrastructure.Configuration;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class FileSystemItemClientPublicationInspector : IItemClientPublicationInspector
{
    private readonly string _contentRootPath;
    private readonly AdminClientPublicationOptions _options;
    private readonly object _syncRoot = new();
    private HashSet<int>? _knownItemTemplateIds;
    private string? _knownItemsD2oPath;
    private string? _knownClientRootPath;
    private string? _knownFailureReason;

    public FileSystemItemClientPublicationInspector(
        IHostEnvironment hostEnvironment,
        IOptions<AdminClientPublicationOptions> options)
    {
        _contentRootPath = hostEnvironment.ContentRootPath;
        _options = options.Value;
    }

    public Task<ItemClientPublicationAuditResult> InspectAsync(int itemId, CancellationToken cancellationToken = default)
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
                    ClientRootPath: _knownClientRootPath,
                    ItemsD2oPath: _knownItemsD2oPath,
                    FailureReason: _knownFailureReason));
            }

            return Task.FromResult(new ItemClientPublicationAuditResult(
                ClientDataAvailable: true,
                TemplateKnown: _knownItemTemplateIds.Contains(itemId),
                ClientRootPath: _knownClientRootPath,
                ItemsD2oPath: _knownItemsD2oPath,
                FailureReason: null));
        }
    }

    private void EnsureCacheLoaded()
    {
        if (_knownItemTemplateIds is not null || _knownFailureReason is not null)
        {
            return;
        }

        var clientRootPath = ResolveClientRootPath();
        var itemsD2oPath = Path.Combine(clientRootPath, "data", "common", "Items.d2o");

        _knownClientRootPath = clientRootPath;
        _knownItemsD2oPath = itemsD2oPath;

        if (!File.Exists(itemsD2oPath))
        {
            _knownFailureReason = $"Items.d2o was not found at '{itemsD2oPath}'.";
            return;
        }

        try
        {
            _knownItemTemplateIds = ReadItemTemplateIds(itemsD2oPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _knownFailureReason = exception.Message;
        }
    }

    private string ResolveClientRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.ClientRootPath))
        {
            return Path.IsPathRooted(_options.ClientRootPath)
                ? _options.ClientRootPath
                : Path.GetFullPath(Path.Combine(_contentRootPath, _options.ClientRootPath));
        }

        return Path.GetFullPath(Path.Combine(_contentRootPath, "..", "..", "..", "Client2.3.7"));
    }

    private static HashSet<int> ReadItemTemplateIds(string itemsD2oPath)
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
