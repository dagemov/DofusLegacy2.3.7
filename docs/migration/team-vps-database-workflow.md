# Team VPS and Database Workflow

## Purpose

Document the validated remote access points and the team rules for using them during the admin migration, without exposing secrets in Git.

## Validated connection points

### Navicat / remote DB

Type: `MySQL` or `MariaDB`

| Field | Value |
| --- | --- |
| Host | `174.138.35.107` |
| Port | `3306` |
| User | `sunshine_remote` |
| Database | `sunshine` |
| Password | `ask team lead / local secret only` |

Rules:

- never commit the real password
- never paste the real password into docs, scripts, or PR comments
- keep the real secret in local-only storage

### SSH

| Field | Value |
| --- | --- |
| User | `root` |
| Host | `174.138.35.107` |
| Hostname | `RollBlackLegacy` |
| Remote working dir | `/root` |
| Platform | `Ubuntu Linux 6.8.0-117-generic x86_64` |

Rules:

- never commit private keys
- never store SSH private keys inside tracked repo folders
- use local key storage only

## Secret handling rules

Allowed places for real secrets:

- local `.env`
- local machine credential storage
- local development config files ignored by Git
- operator password managers

Forbidden places:

- tracked Markdown docs
- committed `appsettings*.json` with real secrets
- PR descriptions
- issue comments
- screenshots

## Working modes

### 1. Docs and analysis only

Use when:

- inventorying legacy tools
- writing plans
- reviewing code

Rules:

- no DB writes
- no SSH write actions
- `git status --short` validation is enough

### 2. Read-only technical audit

Use when:

- checking schema shape
- confirming table names
- validating data presence

Rules:

- read-only queries only
- record the tables inspected
- if any query looks destructive, stop and redesign it

### 3. Controlled admin write

Use when:

- testing future admin API mutations
- performing manual corrections through approved tooling

Rules:

- take a backup first
- document operator, branch, intent, and rollback path
- avoid mixing multiple risky domains in one session

### 4. Production-adjacent client asset publish

Use when:

- publishing item/spell client data
- copying curated PNGs
- patching client SWF payloads

Rules:

- back up the touched client files first
- keep a timestamped backup directory
- note exactly which files changed
- do not perform this from the browser directly; route through guarded backend/infrastructure flows

## Backup rules

Before any risky write phase, create a checkpoint for the relevant data domain.

Minimum required backup checkpoints:

- before item publish experiments
- before spell publish experiments
- before map/group write experiments
- before any production-like DB mutation session

Backup log should capture:

- date/time
- operator
- reason
- branch or ticket
- DB or file scope
- rollback path

## Branch and commit hygiene

Branch rules:

- keep `main` stable
- keep `Yaco` preserved
- new migration work goes through short-lived feature branches

Commit rules:

- commit by intention
- avoid giant mixed commits
- separate docs, architecture, and feature scaffolding where practical

Suggested docs-phase commits:

- `docs: define admin tools migration master plan`
- `docs: inventory legacy blazor admin tools`
- `docs: inventory previous angular admin tools`
- `docs: define dofuslegacy admin target architecture`
- `docs: document team vps database workflow`

## DB workflow checklist

Before connecting:

- confirm whether the task is read-only or write-capable
- confirm whether a backup is required
- confirm where the secret is stored locally

During work:

- avoid ad-hoc destructive SQL
- record affected tables
- keep changes scoped to one module when possible

After work:

- note validation result
- note rollback status
- close local tools and sessions

## SSH workflow checklist

Before remote changes:

- confirm branch and deployment intent
- confirm whether the action is read-only, deploy, or rollback
- confirm logs or files to inspect

During remote changes:

- prefer documented commands
- avoid manual secret exposure in shell history when possible
- do not leave undocumented edits behind

After remote changes:

- record outcome
- capture service status and relevant logs
- update docs if the process changed

## Relationship to the current repo

The current repo already documents general VPS deploy behavior in `docs/vps-deploy.md`.

This document is narrower:

- it exists for the admin migration
- it preserves the validated DB and SSH entry points
- it replaces password disclosure with placeholders
- it adds backup and workflow discipline for future admin writes
