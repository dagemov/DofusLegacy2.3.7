import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SpellCatalogItemDto, SpellPagedResultDto, SpellSearchRequest } from './spells.models';
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
}
