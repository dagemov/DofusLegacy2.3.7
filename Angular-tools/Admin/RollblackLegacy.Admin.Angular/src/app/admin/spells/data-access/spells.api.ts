import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
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
import { toSpellQueryParams } from './spells.queries';

@Injectable({
  providedIn: 'root'
})
export class SpellsApi {
  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.adminApiBaseUrl;

  getSpells(
    request: SpellSearchRequest
  ): Observable<SpellPagedResultDto<SpellCatalogItemDto>> {
    return this.httpClient.get<SpellPagedResultDto<SpellCatalogItemDto>>(
      `${this.baseUrl}/spells`,
      {
        params: toSpellQueryParams(request)
      }
    );
  }

  getSpell(spellId: number): Observable<SpellDetailDto> {
    return this.httpClient.get<SpellDetailDto>(`${this.baseUrl}/spells/${spellId}`);
  }

  getSpellLevels(spellId: number): Observable<SpellLevelDetailDto[]> {
    return this.httpClient.get<SpellLevelDetailDto[]>(`${this.baseUrl}/spells/${spellId}/levels`);
  }

  getSpellLevelEffects(spellId: number, levelNumber: number): Observable<SpellLevelEffectsDto> {
    return this.httpClient.get<SpellLevelEffectsDto>(
      `${this.baseUrl}/spells/${spellId}/levels/${levelNumber}/effects`
    );
  }

  updateSpellLevel(
    spellId: number,
    levelNumber: number,
    request: SpellLevelUpdateRequest
  ): Observable<SpellLevelUpdateResultDto> {
    return this.httpClient.patch<SpellLevelUpdateResultDto>(
      `${this.baseUrl}/spells/${spellId}/levels/${levelNumber}`,
      request
    );
  }
}
