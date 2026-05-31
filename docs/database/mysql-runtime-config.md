# Sunshine MySQL runtime configuration

## Runtime source of truth

Sunshine reads its runtime database settings from:

`Sunshine net11.0/Sunshine net11.0/bin/Debug/net11.0/Database.xml`

The loader is `DatabaseManager.LoadSettings()` in `Sunshine.MySql/Database/DatabaseManager.cs`, which resolves the file from `AppDomain.CurrentDomain.BaseDirectory`.

## Why ItemManager was failing with `using password: NO`

`DatabaseManager` correctly builds a connection string with password and stores it in `DatabaseManager.ConnectionString`.

The broken path was opening a second connection from:

`DatabaseManager.Connection.ConnectionString`

That property no longer included the password, so MySQL saw the login as:

- host: `127.0.0.1`
- user: `sunshine`
- password: missing

This produced:

`Access denied ... (using password: NO)`

## Safe fix

`ItemManager` now opens its secondary connection through:

`DatabaseManager.CreateConnection()`

That keeps the item UID provider aligned with the same runtime connection string that `DatabaseManager` initialized.

## Safe runtime logging

When the item UID provider initializes, Sunshine now logs:

`ItemManager UID provider DB config: Host=... Port=... Database=... User=... PasswordSet=true/false`

The password value itself is never written to logs.
