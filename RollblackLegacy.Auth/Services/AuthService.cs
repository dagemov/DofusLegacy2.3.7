using RollblackLegacy.Auth.Abstractions;
using RollblackLegacy.Auth.Contracts;
using RollblackLegacy.Auth.Domain;

namespace RollblackLegacy.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly ILegacyAccountRepository _repository;
    private readonly ISunshinePasswordHasher _passwordHasher;

    public AuthService(ILegacyAccountRepository repository, ISunshinePasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthOperationResult> RegisterAsync(
        AuthRegisterRequest request,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        string normalizedUsername = (request.Username ?? string.Empty).Trim();
        string normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedUsername)
            || normalizedUsername.Length < 3
            || normalizedUsername.Length > 24)
        {
            return Failure("Datos invalidos", "El nombre de cuenta debe tener entre 3 y 24 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail) || !normalizedEmail.Contains('@'))
        {
            return Failure("Datos invalidos", "Introduce un correo valido.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return Failure("Datos invalidos", "La contrasena debe tener al menos 6 caracteres.");
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Failure("Datos invalidos", "La confirmacion de contrasena no coincide.");
        }

        if (await _repository.UsernameExistsAsync(normalizedUsername, cancellationToken))
        {
            return Failure("Nombre de cuenta en uso", "Ese nombre de cuenta ya existe. Prueba con una variante diferente.");
        }

        if (await _repository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Failure("Correo ya registrado", "Ese correo ya esta asociado a una cuenta existente.");
        }

        string passwordHash = _passwordHasher.HashForStorage(request.Password);
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

            return new AuthOperationResult
            {
                Success = true,
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
            return Failure(
                "Registro no disponible",
                "No pudimos completar el alta de cuenta en este momento. Revisa la conexion a la base de datos e intentalo de nuevo.");
        }
    }

    public async Task<AuthOperationResult> LoginAsync(
        AuthLoginRequest request,
        CancellationToken cancellationToken)
    {
        string normalizedUsername = (request.Username ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Failure("Credenciales invalidas", "Introduce usuario y contrasena.");
        }

        AuthAccountRecord? account;

        try
        {
            account = await _repository.GetByUsernameAsync(normalizedUsername, cancellationToken);
        }
        catch
        {
            return Failure("Login no disponible", "No pudimos validar las credenciales en este momento.");
        }

        if (account is null)
        {
            return Failure("Credenciales invalidas", "Usuario o contrasena incorrectos.");
        }

        if (account.IsBanned)
        {
            return Failure("Cuenta suspendida", "Esta cuenta esta baneada y no puede iniciar sesion.");
        }

        string passwordHash = _passwordHasher.HashForStorage(request.Password);

        if (!string.Equals(account.PasswordHash, passwordHash, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("Credenciales invalidas", "Usuario o contrasena incorrectos.");
        }

        return new AuthOperationResult
        {
            Success = true,
            Title = "Sesion iniciada",
            Message = "Credenciales validadas correctamente.",
            AccountId = account.Id,
            Username = account.Username,
            Nickname = account.Nickname,
        };
    }

    public async Task<UsernameAvailabilityResult> CheckUsernameAvailabilityAsync(
        string? username,
        CancellationToken cancellationToken)
    {
        string normalizedUsername = (username ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return new UsernameAvailabilityResult
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
            return new UsernameAvailabilityResult
            {
                HasValue = true,
                IsAvailable = false,
                Message = "No se pudo validar ahora mismo.",
                Tone = "warning",
            };
        }

        return new UsernameAvailabilityResult
        {
            HasValue = true,
            IsAvailable = !exists,
            Message = exists
                ? "Ese nombre ya esta ocupado."
                : "Ese nombre esta disponible.",
            Tone = exists ? "danger" : "success",
        };
    }

    private static AuthOperationResult Failure(string title, string message) =>
        new()
        {
            Success = false,
            Title = title,
            Message = message,
        };
}
