namespace RollblackLegacy.Website.Application.Abstractions;

public interface ISunshinePasswordHasher
{
    string HashForStorage(string plainPassword);
}
