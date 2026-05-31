# Rollback To Sunshine Risk Register

## R1: Public bind IP hardcoded into local bootstrap

- Severity: High
- Module: Sunshine AuthServer and WorldServer
- Symptom:
  `SocketException 10049` during `Socket.Bind`
- Cause:
  `AuthIp` and `WorldIp` were set to the VPS public IP in the local runtime config
- Mitigation:
  add `AuthBindIp` and `WorldBindIp`, validate them, and fall back to `0.0.0.0`

## R2: Config path ambiguity

- Severity: Medium
- Module: `GameConfig`
- Symptom:
  operator may edit one `Config.xml` while the process reads another
- Cause:
  old code relied on relative path `.\\Config.xml`
- Mitigation:
  resolve config from `AppDomain.CurrentDomain.BaseDirectory`

## R3: Bind address and connect address conflated

- Severity: High
- Module: local bootstrap workflow
- Symptom:
  developers use a public address for local bind and break startup
- Cause:
  no distinction between local socket bind and client-facing address
- Mitigation:
  document and keep separate:
  - local bind: `AuthBindIp`, `WorldBindIp`
  - world connect address: `worlds.Address`
  - auth connect address: Dofus client `connection.host`

## R4: Local bootstrap validated but local client still points remote

- Severity: Medium
- Module: client bootstrap
- Symptom:
  server starts locally but client still tries remote IP
- Evidence:
  - `Client2.3.7/config.xml` has `connection.host=194.99.21.223`
  - `sunshine.worlds.Id=18` still returns `194.99.21.223:3467`
- Mitigation:
  local-only client and DB address override when running local tests

## R5: `--no-console-input` exits immediately after bootstrap

- Severity: Low
- Module: runtime validation
- Symptom:
  ports open briefly and then disappear because `Main()` ends
- Mitigation:
  validate bind either with a normal console window or with a dedicated hold-open test mode if one is added later
