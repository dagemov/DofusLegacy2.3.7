import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

import { ItemPublicationManifestDto } from '../data-access/items.models';

@Component({
  selector: 'app-publication-manifest-preview',
  imports: [CommonModule],
  templateUrl: './publication-manifest-preview.component.html',
  styleUrl: './publication-manifest-preview.component.scss'
})
export class PublicationManifestPreviewComponent {
  @Input({ required: true }) manifest!: ItemPublicationManifestDto;
  @Input() isLoading = false;
  @Input() problemTitle: string | null = null;

  protected get primaryStateBadgeClass(): string {
    switch ((this.manifest?.primaryState || '').toUpperCase()) {
      case 'READY_TO_STAGE':
      case 'READY_FOR_CONTROLLED_PUBLISH':
        return 'text-bg-success';
      case 'STAGING_PACKAGE_NEEDS_VALIDATION':
        return 'text-bg-info';
      case 'BLOCKED_INVALID_ICON':
      case 'BLOCKED_UNKNOWN_TYPE':
        return 'text-bg-warning';
      default:
        return 'text-bg-danger';
    }
  }
}
