namespace RollblackLegacy.Website.Application.Abstractions;

public interface IFaceAvatarResolver
{
    string ResolvePath(string? seed);
}
