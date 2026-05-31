# Sunshine auth login hash debug

## Stored password format

Sunshine stores `accounts.Password` as:

`MD5(passwordPlano)`

One in-repo example is `Sunshine.BaseServer/Commands/BaseCommand.cs`, where account creation hashes the human password before inserting the account.

## Auth comparison flow

During login, Sunshine does not compare the human password directly.

The auth server computes:

`MD5(accounts.Password + ticket)`

and compares that value with `IdentificationMessage.password`.

The ticket is generated in `AuthServer` and sent to the client in `HelloConnectMessage`.

## Safe diagnostics

Enable this in runtime `Config.xml` when you need to trace a real login attempt:

`AuthDebugHashes=true`

With that flag enabled, Sunshine logs lines like:

`[AUTH-DEBUG] flow=Identification username=sebcos1 accountFound=true ticketLength=32 dbPasswordLength=32 dbPasswordMd5Like=true receivedLength=32 expected=abc123...9f0d received=def456...a123 match=false`

These diagnostics intentionally avoid:

- plain-text passwords
- the full stored DB password
- the full expected/received hashes

## Resetting a known password manually

To force a known human password, store the MD5 of that plain password in `accounts.Password`.

Example:

```sql
UPDATE accounts
SET Password = MD5('polondrolo3')
WHERE Username = 'sebcos1';
```

If that stored value matches the expected MD5 and login still fails, the next step is to compare the client-submitted hash with the server-side `MD5(accounts.Password + ticket)` trace.
