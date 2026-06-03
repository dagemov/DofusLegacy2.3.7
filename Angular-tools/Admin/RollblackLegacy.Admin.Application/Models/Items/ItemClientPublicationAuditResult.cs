namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record ItemClientPublicationAuditResult(
    bool ClientDataAvailable,
    bool TemplateKnown,
    string? ClientRootPath,
    string? ItemsD2oPath,
    string? FailureReason);
