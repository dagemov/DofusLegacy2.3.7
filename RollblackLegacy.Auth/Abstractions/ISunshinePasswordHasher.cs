namespace RollblackLegacy.Auth.Abstractions;

public interface ISunshinePasswordHasher
{
    string HashForStorage(string plainPassword);
}
