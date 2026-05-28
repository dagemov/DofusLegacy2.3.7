# Rollback To Sunshine Module Plan

## Scope completed

1. Locate the real runtime config source for the external Sunshine copy.
2. Confirm the exact `AuthIp`, `AuthPort`, `WorldIp`, and `WorldPort`.
3. Inspect Auth and World socket bind logic.
4. Confirm the bind address does not exist on the local Windows host.
5. Patch local bootstrap safely without touching DB, client, or gameplay rules.
6. Validate successful bind on `446` and `3467`.

## Applied approach

Chosen option: Config plus code.

Reason:

- config-only would fix the current machine but not protect future local bootstrap copies from the same mistake
- code-only would still leave confusing local config
- config plus code keeps local behavior safe and gives explicit diagnostic logs

## Current external Sunshine status

- External path:
  `C:\Users\Hombr\Downloads\RollBackShushine`
- Auth bind:
  `0.0.0.0:446`
- World bind:
  `0.0.0.0:3467`
- Local validation:
  successful

## Remaining local bootstrap tasks

1. Keep the bind fallback in the clean Sunshine repo copy as the canonical implementation.
2. Decide how local client tests should override:
   - `Client2.3.7/config.xml`
   - `sunshine.worlds.Address`
3. Optionally add a dedicated local profile such as:
   - `AuthBindIp=0.0.0.0`
   - `WorldBindIp=0.0.0.0`
   - `AuthIp=127.0.0.1`
   - `WorldIp=127.0.0.1`
4. Optionally add a development flag to keep the process alive without requiring an interactive console window.

## Non-goals for this module

- no DB schema changes
- no client binary changes
- no gameplay changes
- no public port changes
- no VPS deployment changes inside this task
