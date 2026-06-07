# Validación VPS — migración `devp` + compile gate Fase 3

| Campo | Valor |
|-------|--------|
| Fecha | 2026-05-30 |
| Rama VPS | `devp` @ `cf69aa1` |
| Compile local | `devp-compile` merge Fase 3 @ `f6c79fc` — **OK** |
| VPS | `174.138.35.107` |
| Path test | `/opt/dofus-2.0.0-build` |

## Procedimiento VPS

```bash
cd /opt/dofus-2.0.0-build
git fetch origin +refs/heads/devp:refs/remotes/origin/devp
git checkout -B devp origin/devp
git reset --hard origin/devp
```

## Procedimiento compile gate (local)

```powershell
git checkout devp-compile
git merge feature/effects-engine-fix-phase3
cd docker
docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```

## Resultado

| Criterio | Estado |
|----------|--------|
| VPS checkout `devp` | **OK** @ `cf69aa1` |
| Build Docker local (Fase 3) | **OK** (~105 s) |
| PRs abiertas → `devp` | #26–#30 |
| Runtime Fase 3 en VPS | **Pendiente** — requiere merge manual PR #28 |

## Notas

- VPS test queda en base `devp` (items builder); fixes efectos Fase 3 disponibles tras merge PR #28.
- `origin/develop` pendiente de eliminación tras verificar PRs abiertas.
