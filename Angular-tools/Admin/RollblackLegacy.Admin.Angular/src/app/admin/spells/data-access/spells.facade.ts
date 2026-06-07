import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  SpellCatalogItemDto,
  SpellDetailDto,
  SpellLevelDetailDto,
  SpellLevelEffectsDto,
  SpellLevelUpdateRequest,
  SpellLevelUpdateResultDto,
  SpellPagedResultDto,
  SpellSearchRequest
} from './spells.models';
import { SpellsApi } from './spells.api';

@Injectable({
  providedIn: 'root'
})
export class SpellsFacade {
  constructor(private readonly spellsApi: SpellsApi) {}

  getSpells(
    request: SpellSearchRequest
  ): Observable<SpellPagedResultDto<SpellCatalogItemDto>> {
    return this.spellsApi.getSpells(request);
  }

  getSpell(spellId: number): Observable<SpellDetailDto> {
    return this.spellsApi.getSpell(spellId);
  }

  getSpellLevels(spellId: number): Observable<SpellLevelDetailDto[]> {
    return this.spellsApi.getSpellLevels(spellId);
  }

  getSpellLevelEffects(spellId: number, levelNumber: number): Observable<SpellLevelEffectsDto> {
    return this.spellsApi.getSpellLevelEffects(spellId, levelNumber);
  }

  updateSpellLevel(
    spellId: number,
    levelNumber: number,
    request: SpellLevelUpdateRequest
  ): Observable<SpellLevelUpdateResultDto> {
    return this.spellsApi.updateSpellLevel(spellId, levelNumber, request);
  }
}
