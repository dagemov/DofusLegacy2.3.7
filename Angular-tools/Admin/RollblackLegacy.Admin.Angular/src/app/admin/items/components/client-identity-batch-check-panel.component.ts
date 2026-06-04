import { CommonModule } from '@angular/common';
import { Component, DestroyRef, Input, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { ApiProblemPanelComponent } from '../../../shared/components/api-problem-panel.component';
import { ClientIdentityApi } from '../data-access/client-identity.api';
import { ClientItemIdentityCheckResultDto } from '../data-access/client-identity.models';
import {
  CLIENT_IDENTITY_MAX_BATCH_IDS,
  buildClientIdentityCsv,
  buildPrimaryStatusCounts,
  parseClientIdentityItemIds
} from '../data-access/client-identity.batch';
import { presentClientIdentityStatus } from '../data-access/client-identity.status';
import { AdminApiProblem, toAdminApiProblem } from '../data-access/items.models';
import { copyTextToClipboard } from '../../../shared/utils/copy-text';

const QA_SAMPLE_TEXT = '7754,12616,12617,39';

@Component({
  selector: 'app-client-identity-batch-check-panel',
  imports: [CommonModule, FormsModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './client-identity-batch-check-panel.component.html',
  styleUrl: './client-identity-batch-check-panel.component.scss'
})
export class ClientIdentityBatchCheckPanelComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly clientIdentityApi = inject(ClientIdentityApi);

  @Input() collapsedByDefault = true;

  protected expanded = false;
  protected isLoading = false;
  protected idsInput = QA_SAMPLE_TEXT;
  protected results: ClientItemIdentityCheckResultDto[] = [];
  protected problem: AdminApiProblem | null = null;
  protected validationError: string | null = null;
  protected copyStatus: string | null = null;

  protected readonly maxIds = CLIENT_IDENTITY_MAX_BATCH_IDS;

  ngOnInit(): void {
    this.expanded = !this.collapsedByDefault;
  }

  protected get statusCounts() {
    return buildPrimaryStatusCounts(this.results);
  }

  protected get parsedIdCount(): number {
    const parsed = parseClientIdentityItemIds(this.idsInput);
    return parsed.error ? 0 : parsed.ids.length;
  }

  protected toggle(): void {
    this.expanded = !this.expanded;
  }

  protected useSample(): void {
    this.idsInput = QA_SAMPLE_TEXT;
    this.validationError = null;
  }

  protected runAudit(): void {
    const parsed = parseClientIdentityItemIds(this.idsInput);
    this.validationError = parsed.error;
    this.problem = null;
    this.copyStatus = null;

    if (parsed.error || parsed.ids.length === 0) {
      this.results = [];
      return;
    }

    this.isLoading = true;
    this.results = [];

    this.clientIdentityApi
      .checkItems(parsed.ids)
      .pipe(
        catchError((error: unknown) => {
          this.problem = toAdminApiProblem(error);
          return of(null);
        }),
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((results) => {
        if (!results) {
          return;
        }

        this.results = results;
      });
  }

  protected async copyCsv(): Promise<void> {
    if (this.results.length === 0) {
      return;
    }

    const status = await copyTextToClipboard(buildClientIdentityCsv(this.results));
    this.copyStatus = status === 'copied' ? 'CSV copiado al portapapeles.' : 'Copia manual: usa el texto exportado en consola.';
    if (status !== 'copied') {
      console.info(buildClientIdentityCsv(this.results));
    }
  }

  protected statusLabel(code: string): string {
    return presentClientIdentityStatus(code).label;
  }

  protected statusBadgeClass(code: string): string {
    return presentClientIdentityStatus(code).badgeClass;
  }
}
