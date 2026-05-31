using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Contracts.Account;
using RollblackLegacy.Website.Domain.Accounts;

namespace RollblackLegacy.Website.Application.Services;

public sealed class AccountRegistrationService : IAccountRegistrationService
{
    private readonly ILegacyAccountRepository _repository;
    private readonly ISunshinePasswordHasher _passwordHasher;

    public AccountRegistrationService(
        ILegacyAccountRepository repository,
        ISunshinePasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterAccountResultViewModel> RegisterAsync(
        RegisterAccountInputModel input,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        string normalizedUsername = (input.Username ?? string.Empty).Trim();
        string normalizedEmail = (input.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (await _repository.UsernameExistsAsync(normalizedUsername, cancellationToken))
        {
            return new RegisterAccountResultViewModel
            {
                Succeeded = false,
                Title = "Nombre de cuenta en uso",
                Message = "Ese nombre de cuenta ya existe. Prueba con una variante diferente.",
            };
        }

        if (await _repository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return new RegisterAccountResultViewModel
            {
                Succeeded = false,
                Title = "Correo ya registrado",
                Message = "Ese correo ya esta asociado a una cuenta existente.",
            };
        }

        string passwordHash = _passwordHasher.HashForStorage(input.Password);
        var registration = LegacyAccountRegistration.Create(
            normalizedUsername,
            normalizedEmail,
            passwordHash,
            remoteIp);

        try
        {
            LegacyAccountSchemaCapabilities capabilities = await _repository.CreateAsync(
                registration,
                cancellationToken);

            return new RegisterAccountResultViewModel
            {
                Succeeded = true,
                Title = "Cuenta creada",
                Message = capabilities.EmailWasStored
                    ? "Tu cuenta ya fue creada y el correo quedo registrado en la base de datos del servidor."
                    : "Tu cuenta ya fue creada. El esquema actual del auth no expone almacenamiento nativo de correo.",
                Username = registration.Username,
                Email = registration.Email,
                EmailWasStored = capabilities.EmailWasStored,
                UsesWebsiteContactTable = capabilities.UsesWebsiteContactTable,
            };
        }
        catch
        {
            return new RegisterAccountResultViewModel
            {
                Succeeded = false,
                Title = "Registro no disponible",
                Message = "No pudimos completar el alta de cuenta en este momento. Revisa la conexion a la base de datos e intentalo de nuevo.",
            };
        }
    }

    public async Task<UsernameAvailabilityViewModel> CheckUsernameAvailabilityAsync(
        string? username,
        CancellationToken cancellationToken)
    {
        string normalizedUsername = (username ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return new UsernameAvailabilityViewModel
            {
                HasValue = false,
                IsAvailable = false,
                Message = "Escribe un nombre de cuenta para validarlo.",
                Tone = "muted",
            };
        }

        bool exists;

        try
        {
            exists = await _repository.UsernameExistsAsync(normalizedUsername, cancellationToken);
        }
        catch
        {
            return new UsernameAvailabilityViewModel
            {
                HasValue = true,
                IsAvailable = false,
                Message = "No se pudo validar ahora mismo.",
                Tone = "warning",
            };
        }

        return new UsernameAvailabilityViewModel
        {
            HasValue = true,
            IsAvailable = !exists,
            Message = exists
                ? "Ese nombre ya esta ocupado."
                : "Ese nombre esta disponible.",
            Tone = exists ? "danger" : "success",
        };
    }
}
