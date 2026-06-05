import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ClientItemIdentityCheckResultDto } from './client-identity.models';

@Injectable({
  providedIn: 'root'
})
export class ClientIdentityApi {
  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = `${environment.adminApiBaseUrl}/client-identity/items`;

  getItem(itemId: number): Observable<ClientItemIdentityCheckResultDto> {
    return this.httpClient.get<ClientItemIdentityCheckResultDto>(`${this.baseUrl}/${itemId}`);
  }

  checkItems(itemIds: readonly number[]): Observable<ClientItemIdentityCheckResultDto[]> {
    const ids = itemIds.filter((id) => Number.isInteger(id) && id > 0).join(',');
    return this.httpClient.get<ClientItemIdentityCheckResultDto[]>(`${this.baseUrl}/check`, {
      params: { ids }
    });
  }
}
