import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { SpellCatalogItemDto, SpellPagedResultDto, SpellSearchRequest } from './spells.models';
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
}
