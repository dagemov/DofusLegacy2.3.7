export interface PublicationBackupStatusDto {
  lastClientBackupUtc?: string | null;
  lastClientBackupPath?: string | null;
  lastDbBackupUtc?: string | null;
  lastDbBackupPath?: string | null;
  lastVpsInventoryUtc?: string | null;
  lastVpsInventoryPath?: string | null;
  lastValidationUtc?: string | null;
  lastValidationStatus?: string | null;
  publishLaneStatus: string;
  targetItemId: number;
  stagingPackagePath?: string | null;
  productionPublishBlocked: boolean;
  publishLaneBlockingReasons: string[];
  recoveryReadinessNotes: string[];
  nextManualSteps: string[];
  generatedAtUtc: string;
}
