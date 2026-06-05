# VPS Restart Safety Checklist

Use this checklist before any restart tied to item rollout, vendor changes, or client-publication-dependent runtime validation.

Primary reference:

- [vps-controlled-restart.md](./vps-controlled-restart.md)

## Pre-window

- [ ] Confirm SSH access works from the current workstation
- [ ] Confirm the target service is `sunshine-server`
- [ ] Confirm no unrelated deployment is piggybacking on the same restart

## Save and backup

- [ ] Announce the maintenance window in game
- [ ] Run `.save` while World is still reachable
- [ ] Use `.stop <seconds>` if a graceful shutdown window is required
- [ ] Run the focused backup script
- [ ] Record the backup path and file size
- [ ] Confirm the dump is non-empty

## Apply window

- [ ] Apply only the intended SQL or operational change
- [ ] Do not restart MySQL unless the task explicitly requires it
- [ ] Do not run `docker compose down -v`
- [ ] Do not delete containers or volumes

## Restart

- [ ] Restart only `sunshine-server`
- [ ] Watch logs until the service is ready
- [ ] Confirm public ports recover

## Post-restart validation

- [ ] Confirm login path is healthy
- [ ] Confirm the exact target scenario is healthy
- [ ] Record the validation result in docs before calling the task complete
