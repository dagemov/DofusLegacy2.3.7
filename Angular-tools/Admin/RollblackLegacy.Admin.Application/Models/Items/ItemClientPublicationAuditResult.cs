namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record ItemClientPublicationAuditResult(
    bool ClientDataAvailable,
    bool TemplateKnown,
    bool TypeKnown,
    string? ClientRootPath,
    string? ItemsD2oPath,
    string? ItemTypesD2oPath,
    string? FailureReason);
