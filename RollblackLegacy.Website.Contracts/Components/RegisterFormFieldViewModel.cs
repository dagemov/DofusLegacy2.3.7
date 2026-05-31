namespace RollblackLegacy.Website.Contracts.Components;

public sealed class RegisterFormFieldViewModel
{
    public required string FieldName { get; init; }

    public required string FieldId { get; init; }

    public required string Label { get; init; }

    public required string Type { get; init; }

    public string? Value { get; init; }

    public string? Placeholder { get; init; }

    public string? AutoComplete { get; init; }

    public string? HelpText { get; init; }

    public string? HxGet { get; init; }

    public string? HxTrigger { get; init; }

    public string? HxTarget { get; init; }
}
