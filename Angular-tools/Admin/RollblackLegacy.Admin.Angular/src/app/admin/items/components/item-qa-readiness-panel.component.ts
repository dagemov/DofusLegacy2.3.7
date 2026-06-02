import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ApiProblemPanelComponent } from '../../../shared/components/api-problem-panel.component';
import {
  AdminApiProblem,
  ItemDetailDto,
  ItemQaSummaryDto
} from '../data-access/items.models';
import {
  ClipboardCopyStatus,
  copyTextToClipboard,
  getClipboardSupportInfo
} from '../../../shared/utils/copy-text';

@Component({
  selector: 'app-item-qa-readiness-panel',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './item-qa-readiness-panel.component.html',
  styleUrl: './item-qa-readiness-panel.component.scss'
})
export class ItemQaReadinessPanelComponent {
  @Input() itemId: number | null = null;
  @Input() detail: ItemDetailDto | null = null;
  @Input() qaSummary: ItemQaSummaryDto | null = null;
  @Input() problem: AdminApiProblem | null = null;
  @Input() isLoading = false;

  protected readonly clipboardSupport = getClipboardSupportInfo();
  protected checklistCopyState: ClipboardCopyStatus | null = null;

  protected get workflowStateLabel(): string {
    return (this.qaSummary?.workflowState || 'UNKNOWN').replace(/_/g, ' ');
  }

  protected get workflowBadgeClass(): string {
    switch ((this.qaSummary?.workflowState || '').toUpperCase()) {
      case 'READY_FOR_QA':
        return 'text-bg-success';
      case 'BLOCKED':
        return 'text-bg-warning';
      default:
        return 'text-bg-secondary';
    }
  }

  protected get previewReady(): boolean {
    const state = (this.qaSummary?.previewState?.state || '').toUpperCase();
    return state === 'FOUND' || state === 'MANUAL';
  }

  protected get identityReady(): boolean {
    const identity = this.detail?.clientIdentity;
    return !!identity?.clientName?.trim() && !!identity.iconId && !!identity.appearanceId;
  }

  protected get warningCount(): number {
    return this.qaSummary?.warnings?.length ?? 0;
  }

  protected get hasChecklist(): boolean {
    return this.buildChecklistText().length > 0;
  }

  protected get clipboardHint(): string | null {
    return this.clipboardSupport.helpText;
  }

  protected get copyChecklistLabel(): string {
    switch (this.checklistCopyState) {
      case 'copied':
        return 'Checklist copied';
      case 'manual':
      case 'unavailable':
        return 'Manual copy';
      default:
        return 'Copy QA checklist';
    }
  }

  protected get showManualChecklist(): boolean {
    return this.checklistCopyState === 'manual' || this.checklistCopyState === 'unavailable';
  }

  protected get manualChecklistText(): string {
    return this.buildChecklistText();
  }

  protected async copyChecklist(): Promise<void> {
    const checklist = this.buildChecklistText();
    if (!checklist) {
      return;
    }

    const status = await copyTextToClipboard(checklist);
    this.checklistCopyState = status;

    if (status === 'copied') {
      window.setTimeout(() => {
        if (this.checklistCopyState === 'copied') {
          this.checklistCopyState = null;
        }
      }, 1800);
    }
  }

  private buildChecklistText(): string {
    if (!this.qaSummary) {
      return '';
    }

    const checks = this.qaSummary.recommendedChecks
      .map((entry, index) => `${index + 1}. ${entry}`)
      .join('\n');

    const blockers = this.qaSummary.blockingReasons.length > 0
      ? this.qaSummary.blockingReasons.map((entry, index) => `- ${entry}`).join('\n')
      : '- No derived blockers were found for QA.';

    return [
      `QA / Publish Readiness`,
      `ItemId: ${this.qaSummary.itemId}`,
      `ResolvedName: ${this.qaSummary.resolvedName || 'Unavailable'}`,
      `WorkflowState: ${this.qaSummary.workflowState}`,
      `CanQa: ${this.qaSummary.canQa ? 'YES' : 'NO'}`,
      `CanPublish: ${this.qaSummary.canPublish ? 'YES' : 'NO'}`,
      `PreviewState: ${this.qaSummary.previewState.state}`,
      `IconId: ${this.qaSummary.iconId}`,
      `AppearanceId: ${this.qaSummary.appearanceId}`,
      '',
      'Blocking reasons:',
      blockers,
      '',
      'Recommended checks:',
      checks
    ].join('\n');
  }
}
