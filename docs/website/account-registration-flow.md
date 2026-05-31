# Account Registration Flow

## Tabla auditada

El emulador autentica contra `accounts` en la base `sunshine`.

Columnas relevantes actuales:

- `Id`
- `Username`
- `Password`
- `Nickname`
- `Role`
- `SecretQuestion`
- `SecretAnswer`
- `IsBanned`
- `Ticket`
- `RegisteredIP`
- `Tokens`
- `NewTokens`

El dump actual no trae columna `Email` en `accounts`.

## Algoritmo de password encontrado

El flujo real esta en:

- `Sunshine.AuthServer/Handlers/Connection/ConnectionHandler.cs`
- `Sunshine.Protocol/Utils/Utils.cs`
- `Sunshine.BaseServer/Commands/BaseCommand.cs`

Compatibilidad encontrada:

1. Al crear una cuenta, Sunshine almacena `MD5(passwordPlano)` en `accounts.Password`.
2. Durante login, el cliente envia `MD5(accounts.Password + ticket)`.
3. Por eso el website debe guardar el hash simple `MD5(passwordPlano)` y no otro formato.

## Implementacion del website

El website:

1. Valida `Username`, `Email`, `Password`, `ConfirmPassword`.
2. Verifica unicidad de username en `accounts`.
3. Verifica unicidad de email.
4. Genera `MD5(passwordPlano)` con `SunshinePasswordHasher`.
5. Inserta la cuenta real en `accounts`.
6. Guarda el correo en `website_account_contacts`.

## Tabla complementaria creada por el website

Para no modificar a ciegas la tabla auth original, el sitio asegura:

```sql
CREATE TABLE IF NOT EXISTS website_account_contacts (
  AccountId INT NOT NULL PRIMARY KEY,
  Email VARCHAR(255) NOT NULL,
  CreatedAtUtc DATETIME NOT NULL,
  UNIQUE KEY UX_website_account_contacts_email (Email),
  CONSTRAINT FK_website_account_contacts_accounts
    FOREIGN KEY (AccountId) REFERENCES accounts (Id)
    ON DELETE CASCADE
);
```

Si algun dia `accounts` incorpora una columna `Email`, el repositorio la detecta y la rellena tambien.

## Decisiones MVP

- `Nickname = Username`
- `SecretQuestion = "registration-email"`
- `SecretAnswer = email normalizado`

Esto mantiene la cuenta utilizable sin pedir mas campos en esta fase.
