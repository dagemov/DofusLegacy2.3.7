# P1 Fase 1 — Hallazgos manuales y matriz NPC

**Fecha:** 2026-06-15  
**Fuente:** Validación manual P0 + auditoría DB `sunshine-db`

## Patrón raíz observado

| Patrón | Impacto | Ejemplo |
|---|---|---|
| Mensajes sin replies | Diálogo roto / AutoClose | 889, 890 taberna |
| Un solo LearnJob para menú multi-oficio | Todas las opciones → mismo jobId | NPC 849 Ikul → job 2 |
| CSV replies solo type 1, sin `npcs_replies` tipadas | Navegación sin acción real | 1248 mazmorras, 1223 Risette |
| Action types negativos en `npcs_replies` | Dispatch falla si se activan | NPC 843 (-1/-2/-3) |
| Sunshine no ramifica por reply | Todos los type 1 → siguiente mensaje secuencial | Ikul 3597→3598 |

## Matriz NPCs

| NPC | Nombre | Zona | MapId | Estado | Síntoma | dialogId | replyId | actionType |
|--:|---|---|---|---|---|---:|---:|---:|
| 849 | Contremaître Ikul | Incarnam | 21759491 | **FAIL** | Todas las opciones → leñador (job 2) | 3597 | 3184-3187 | 1→3598→8/3189/2 |
| 863 | Paysan d Incarnam | Incarnam | 21760002 | P0/PENDING | Validar aprender campesino (28) | 3695→3696 | 3182→3189 | 1→8/28 |
| 881 | Bûcheron d Incarnam | Incarnam | 21759493 | P0/PENDING | Validar aprender leñador (2) | 3695→3696 | 3182→3189 | 1→8/2 |
| 882 | Chasseur d Incarnam | Incarnam | 21758977 | P0/PENDING | Validar aprender cazador (41) | 3695→3696 | 3182→3189 | 1→8/41 |
| 883 | Pêcheur d Incarnam | Incarnam | 21760005 | P0/PENDING | Validar aprender pescador (36) | 3695→3696 | 3182→3189 | 1→8/36 |
| 1223 | Fée Risette | Taberna Incarnam | 23592962 | **FAIL** | Diálogo/opciones sin acción tipada | 5452+ | 7261+ | CSV=1 only |
| 889 | Serveuse de Grobid | Taberna Incarnam | 23592962 | **FAIL** | Sin replies (AutoClose) | 3848 | — | — |
| 890 | Habitué de la taverne | Taberna Incarnam | 23592962 | **FAIL** | Sin replies (AutoClose) | 3849 | — | — |
| 872 | Grobid Le Vétéran Kerubim | Taberna Incarnam | 23592962 | **PARTIAL** | CSV nav only, 0 npcs_replies | 3731+ | 3277+ | CSV=1 |
| 1248 | Hugo Frais | Hub mazmorras | 54534173 | **FAIL** | CSV replies sin type 2 Teleport | 7704+ | 8104+ | CSV=1 |
| 1249 | Laurent Gebleuh | Hub mazmorras | 54535193 | **FAIL** | CSV replies sin Teleport | 7704+ | 7731+ | CSV=1 |
| 888 | Avaulé Ganymède | Mazmorra patrón | 21761540 | **PARTIAL** | Tiene type 2 Teleport en DB | 3831 | 3362 | 2 |
| 843 | Struk toer Nhin | Incarnam quest | 21757955 | **OK/FAIL** | Quest type 5 OK; -1/-2/-3 bloquean si dispatch | 3537 | 3120 | 5 |
| TBD | NPC misión genérico | Incarnam | — | **OK** | Aceptar misión funciona | — | — | 5 |

## Lo que funciona

- Recolección + XP oficio (handlers harvest)
- `LearnJobReply` + notify cliente (post-P0)
- AutoClose diálogos sin replies (post-P0)
- Quest reply type 5 en NPCs con `npcs_replies` correctas (ej. 843 reply 3120)

## Lo que no funciona

- Menú multi-oficio con un solo LearnJob (849)
- NPCs con mensajes pero 0 replies (**128** en DB global)
- Hub mazmorras sin `npcs_replies` type 2
- Taberna: Serveuse/Habitué sin ninguna reply
- Fée Risette: solo navegación CSV, sin acciones de quest/item

## Próximo paso

1. Aplicar patch Ikul (Fase 4)
2. Logs clasificados (Fase 2) para capturar replyId/actionType reales en taberna/mazmorras
3. No parchear Risette/taberna sin evidencia de actionType esperado
