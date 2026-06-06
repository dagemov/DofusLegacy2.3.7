import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { PublicationBackupStatusDto } from './publication.models';

@Injectable({ providedIn: 'root' })
export class PublicationApi {
  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.adminApiBaseUrl;

  getBackupStatus(): Observable<PublicationBackupStatusDto> {
    return this.httpClient.get<PublicationBackupStatusDto>(`${this.baseUrl}/publication/backup-status`);
  }
}
