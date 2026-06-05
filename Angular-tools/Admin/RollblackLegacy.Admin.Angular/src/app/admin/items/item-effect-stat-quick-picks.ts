import { AdminEffectOptionDto } from './data-access/items.models';

export function resolveStatIconAssetPath(iconAsset?: string): string | null {
  if (!iconAsset?.trim()) {
    return null;
  }

  const normalized = iconAsset.trim().replace(/\\/g, '/');
  return normalized.startsWith('/assets/') ? normalized : `/assets/${normalized}`;
}

export interface StatQuickPickDefinition {
  id: string;
  emoji: string;
  /** Ruta bajo src/assets (p. ej. icons/fire.png). */
  iconAsset?: string;
  title: string;
  subtitle: string;
  effectId?: number;
  labelMatches?: string[];
  searchAliases: string[];
  defaultValue: number;
}

/** Capa visual prioritaria — los EffectIds se resuelven contra GET item-effects/options. */
export const STAT_QUICK_PICKS: StatQuickPickDefinition[] = [
  {
    id: 'intelligence',
    emoji: '🔥',
    iconAsset: 'icons/fire.png',
    title: 'Inteligencia',
    subtitle: 'Fuego',
    effectId: 119,
    searchAliases: ['inteligencia', 'intelligence', 'fuego', 'fire'],
    defaultValue: 50
  },
  {
    id: 'wisdom',
    emoji: '🌙',
    iconAsset: 'icons/widsom.png',
    title: 'Sabiduría',
    subtitle: 'Wisdom',
    effectId: 124,
    searchAliases: ['sabiduria', 'sabiduría', 'wisdom'],
    defaultValue: 50
  },
  {
    id: 'strength',
    emoji: '🌱',
    iconAsset: 'icons/force.png',
    title: 'Fuerza',
    subtitle: 'Tierra',
    effectId: 118,
    searchAliases: ['fuerza', 'strength', 'tierra', 'earth'],
    defaultValue: 50
  },
  {
    id: 'chance',
    emoji: '💧',
    iconAsset: 'icons/water.png',
    title: 'Suerte',
    subtitle: 'Agua',
    effectId: 123,
    searchAliases: ['suerte', 'chance', 'agua', 'water'],
    defaultValue: 50
  },
  {
    id: 'agility',
    emoji: '🌪️',
    iconAsset: 'icons/Air.png',
    title: 'Agilidad',
    subtitle: 'Aire',
    effectId: 122,
    searchAliases: ['agilidad', 'agility', 'aire', 'air'],
    defaultValue: 50
  },
  {
    id: 'vitality',
    emoji: '❤️',
    iconAsset: 'icons/hp.png',
    title: 'Vitalidad',
    subtitle: 'HP',
    effectId: 125,
    searchAliases: ['vitalidad', 'vitality', 'hp', 'vida'],
    defaultValue: 100
  },
  {
    id: 'damage',
    emoji: '⚔️',
    iconAsset: 'icons/neutral.png',
    title: 'Daños',
    subtitle: 'Flat damage',
    effectId: 112,
    searchAliases: ['danos', 'daños', 'damage', 'dano'],
    defaultValue: 40
  },
  {
    id: 'range',
    emoji: '🎯',
    iconAsset: 'icons/Range.png',
    title: 'Alcance',
    subtitle: 'Range',
    effectId: 117,
    searchAliases: ['alcance', 'range'],
    defaultValue: 1
  },
  {
    id: 'mp',
    emoji: '👣',
    iconAsset: 'icons/PM.png',
    title: 'PM',
    subtitle: 'Movement',
    effectId: 128,
    searchAliases: ['pm', 'movement', 'movimiento'],
    defaultValue: 1
  },
  {
    id: 'ap',
    emoji: '⭐',
    iconAsset: 'icons/PA.png',
    title: 'PA',
    subtitle: 'Action points',
    effectId: 111,
    searchAliases: ['pa', 'action', 'puntos de accion'],
    defaultValue: 1
  },
  {
    id: 'prospecting',
    emoji: '👁️',
    iconAsset: 'icons/Prospeccion.png',
    title: 'Prospección',
    subtitle: 'Prospecting',
    effectId: 176,
    searchAliases: ['prospeccion', 'prospección', 'prospecting'],
    defaultValue: 80
  },
  {
    id: 'lock',
    emoji: '🧲',
    title: 'Placaje',
    subtitle: 'Lock',
    effectId: 753,
    searchAliases: ['placaje', 'lock'],
    defaultValue: 20
  },
  {
    id: 'dodge',
    emoji: '🌀',
    title: 'Esquiva',
    subtitle: 'Dodge',
    effectId: 752,
    searchAliases: ['esquiva', 'dodge', 'huida'],
    defaultValue: 20
  },
  {
    id: 'ap-attack',
    emoji: '🧿',
    title: 'Retiro PA',
    subtitle: 'AP reduction',
    effectId: 410,
    searchAliases: ['retiro pa', 'ap attack', 'retirada pa'],
    defaultValue: 10
  },
  {
    id: 'mp-attack',
    emoji: '🧊',
    title: 'Retiro PM',
    subtitle: 'MP reduction',
    effectId: 412,
    searchAliases: ['retiro pm', 'mp attack'],
    defaultValue: 10
  },
  {
    id: 'critical',
    emoji: '💥',
    iconAsset: 'icons/iniciative.png',
    title: 'Golpes críticos',
    subtitle: 'Critical hits',
    effectId: 115,
    searchAliases: ['golpes criticos', 'golpes críticos', 'critical', 'critico'],
    defaultValue: 10
  },
  {
    id: 'summons',
    emoji: '🧬',
    iconAsset: 'icons/summon.png',
    title: 'Invocaciones',
    subtitle: 'Summons',
    effectId: 182,
    searchAliases: ['invocaciones', 'summons', 'invoc'],
    defaultValue: 1
  },
  {
    id: 'air-damage',
    emoji: '🌪️',
    iconAsset: 'icons/Air.png',
    title: 'Daño aire',
    subtitle: 'Air damage %',
    labelMatches: ['Danos aire', 'Daños aire', 'damage per air', 'air damage'],
    searchAliases: ['daño aire', 'daños aire', 'air damage', 'damage per air', 'danos aire'],
    defaultValue: 5
  }
];

export function resolveQuickPickOption(
  pick: StatQuickPickDefinition,
  optionsById: Map<number, AdminEffectOptionDto>,
  allOptions: AdminEffectOptionDto[]
): { option: AdminEffectOptionDto | null; status: 'ok' | 'unconfirmed' } {
  if (pick.effectId !== undefined) {
    const direct = optionsById.get(pick.effectId);
    return direct ? { option: direct, status: 'ok' } : { option: null, status: 'unconfirmed' };
  }

  const matches = (pick.labelMatches ?? []).map((fragment) => fragment.toLowerCase());
  const found = allOptions.find((option) => {
    const label = option.label.toLowerCase();
    return matches.some((fragment) => label.includes(fragment));
  });

  return found ? { option: found, status: 'ok' } : { option: null, status: 'unconfirmed' };
}

export function optionMatchesHumanSearch(option: AdminEffectOptionDto, term: string): boolean {
  const normalized = term.trim().toLowerCase();
  if (!normalized) {
    return true;
  }

  if (
    option.label.toLowerCase().includes(normalized) ||
    option.protocolName.toLowerCase().includes(normalized) ||
    String(option.effectId).includes(normalized)
  ) {
    return true;
  }

  for (const pick of STAT_QUICK_PICKS) {
    const aliases = pick.searchAliases.map((alias) => alias.toLowerCase());
    if (!aliases.some((alias) => alias.includes(normalized) || normalized.includes(alias))) {
      continue;
    }

    if (pick.effectId !== undefined && pick.effectId === option.effectId) {
      return true;
    }

    if (pick.labelMatches?.some((fragment) => option.label.toLowerCase().includes(fragment.toLowerCase()))) {
      return true;
    }
  }

  return false;
}
