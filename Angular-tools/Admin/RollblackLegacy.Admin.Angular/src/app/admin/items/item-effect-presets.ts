import { AdminEffectOptionDto } from './data-access/items.models';

/** Una línea de preset: effectId auditado o labelMatch contra catálogo 7D.2. */
export interface ItemEffectPresetEntry {
  effectId?: number;
  /** Subcadena del label ES del catálogo (p. ej. "+ PA", "+ Vitalidad"). */
  labelMatch?: string;
  value: number;
  /** Nota para preview / docs si la línea es ambigua. */
  note?: string;
}

export interface ItemEffectPresetDefinition {
  id: string;
  name: string;
  description: string;
  entries: ItemEffectPresetEntry[];
}

export interface ResolvedPresetLine {
  entry: ItemEffectPresetEntry;
  option: AdminEffectOptionDto | null;
  status: 'ok' | 'missing';
}

/**
 * EffectIds auditados en dofus-tester-item-creation.md (Sunshine EffectsEnum completo).
 * Líneas solo con labelMatch se resuelven en runtime contra GET item-effects/options.
 */
export const ITEM_EFFECT_PRESETS: ItemEffectPresetDefinition[] = [
  {
    id: 'dofus-hielos-ux',
    name: 'Dofus de los Hielos (UX ejemplo)',
    description:
      'Solo referencia UX Phase 5 — +40 daños, +80 prospección, +50 sabiduría, +10 críticos. No publica automáticamente.',
    entries: [
      { effectId: 112, value: 40, note: '+ Daños' },
      { effectId: 176, value: 80, note: '+ Prospección' },
      { effectId: 124, value: 50, note: '+ Sabiduría' },
      { effectId: 115, value: 10, note: '+ Golpes críticos' }
    ]
  },
  {
    id: 'dofus-tester-qa',
    name: 'Dofus Tester QA',
    description:
      'Bundle de stats para items de prueba (12616/12617). EffectIds alineados con dofus-tester-item-creation.md.',
    entries: [
      { effectId: 111, value: 6, note: '+ PA' },
      { effectId: 128, value: 6, note: '+ PM' },
      { effectId: 117, value: 3, note: '+ Alcance' },
      { effectId: 182, value: 3, note: '+ Invocaciones' },
      { effectId: 125, value: 500, note: '+ Vitalidad' },
      { effectId: 176, value: 200, note: '+ Prospección' },
      { effectId: 138, value: 400, note: '+ Potencia (% daños)' },
      { effectId: 112, value: 50, note: '+ Daños' },
      { effectId: 124, value: 200, note: '+ Sabiduría' },
      { effectId: 410, value: 40, note: 'Retiro PA (APAttack)' },
      { effectId: 412, value: 40, note: 'Retiro PM' },
      { effectId: 753, value: 50, note: '+ Placaje' },
      { effectId: 752, value: 50, note: '+ Esquiva (Huida)' }
    ]
  },
  {
    id: 'dofus-basico',
    name: 'Dofus básico',
    description: 'Stats típicos de dofús custom (valores sugeridos, editables tras aplicar).',
    entries: [
      { labelMatch: '+ Vitalidad', value: 100 },
      { labelMatch: '+ Sabiduria', value: 50 },
      { labelMatch: '+ Prospeccion', value: 100 },
      { labelMatch: '+ Potencia', value: 200 },
      { labelMatch: '+ Danos', value: 30 }
    ]
  },
  {
    id: 'amuleto-basico',
    name: 'Amuleto básico',
    description: 'PA + stats de amuleto (valores sugeridos).',
    entries: [
      { labelMatch: '+ PA', value: 1 },
      { labelMatch: '+ Vitalidad', value: 80 },
      { labelMatch: '+ Sabiduria', value: 40 },
      { labelMatch: '+ Danos', value: 20 },
      { labelMatch: '+ Potencia', value: 30 }
    ]
  },
  {
    id: 'botas-basicas',
    name: 'Botas básicas',
    description: 'PM + vitalidad + placaje/esquiva (valores sugeridos).',
    entries: [
      { labelMatch: '+ PM', value: 1 },
      { labelMatch: '+ Vitalidad', value: 60 },
      { labelMatch: '+ Placaje', value: 30 },
      { labelMatch: '+ Huida', value: 30 }
    ]
  },
  {
    id: 'capa-sombrero-basico',
    name: 'Capa/Sombrero básico',
    description: 'Vitalidad + sabiduría + potencia/daños (valores sugeridos).',
    entries: [
      { labelMatch: '+ Vitalidad', value: 70 },
      { labelMatch: '+ Sabiduria', value: 35 },
      { labelMatch: '+ Potencia', value: 25 },
      { labelMatch: '+ Danos', value: 15 }
    ]
  }
];

export function resolvePresetLines(
  preset: ItemEffectPresetDefinition,
  optionsById: Map<number, AdminEffectOptionDto>,
  allOptions: AdminEffectOptionDto[]
): ResolvedPresetLine[] {
  return preset.entries.map((entry) => {
    let option: AdminEffectOptionDto | null = null;

    if (entry.effectId !== undefined) {
      option = optionsById.get(entry.effectId) ?? null;
    } else if (entry.labelMatch) {
      option = findOptionByLabel(entry.labelMatch, allOptions);
    }

    return {
      entry,
      option,
      status: option ? 'ok' : 'missing'
    };
  });
}

function findOptionByLabel(labelMatch: string, allOptions: AdminEffectOptionDto[]): AdminEffectOptionDto | null {
  const needle = labelMatch.trim().toLowerCase();
  const matches = allOptions.filter((option) => option.label.toLowerCase().includes(needle));

  if (matches.length === 0) {
    return null;
  }

  const exact = matches.find((option) => option.label.toLowerCase() === needle);
  if (exact) {
    return exact;
  }

  return matches.sort((left, right) => left.label.length - right.label.length)[0];
}

export function formatPresetPreviewLine(line: ResolvedPresetLine): string {
  if (line.status === 'missing') {
    const ref = line.entry.effectId ?? line.entry.labelMatch ?? '?';
    return `PENDIENTE — no encontrado en catálogo (${ref})`;
  }

  const note = line.entry.note ? ` · ${line.entry.note}` : '';
  return `${line.option!.label} (${line.option!.effectId}) = ${line.entry.value}${note}`;
}
