import { execFile } from 'node:child_process';
import { bridgeConfig, sanitizeRunId } from './_bridge-lib.mjs';

function sshEnv() {
  const env = { ...process.env };
  if (!env.PROGRAMDATA) env.PROGRAMDATA = 'C:\\ProgramData';
  if (!env.SYSTEMROOT) env.SYSTEMROOT = 'C:\\Windows';
  return env;
}

function execPromise(cmd, args, timeoutMs = 120000) {
  return new Promise((resolve) => {
    execFile(cmd, args, { timeout: timeoutMs, maxBuffer: 32 * 1024 * 1024, windowsHide: true, env: sshEnv() },
      (err, stdout, stderr) => {
        resolve({
          code: err ? (err.code ?? 1) : 0,
          stdout: stdout ?? '',
          stderr: stderr ?? '',
          timedOut: Boolean(err && err.killed),
        });
      });
  });
}

export function createSSHDockerAdapter(cfg = bridgeConfig()) {
  const { ssh, db } = cfg;
  const sshTarget = `${ssh.user}@${ssh.host}`;
  const commandLog = [];

  function requireSsh() {
    if (!ssh.host || !ssh.key) {
      throw new Error('SSH not configured (SSH_HOST / SSH_KEY or BRIDGE_SSH_*)');
    }
  }

  function sshArgs(remoteCommand) {
    return [
      '-i', ssh.key,
      '-o', 'StrictHostKeyChecking=accept-new',
      '-o', 'ConnectTimeout=15',
      '-o', 'BatchMode=yes',
      sshTarget,
      remoteCommand,
    ];
  }

  async function sshRun(remoteCommand, label = 'ssh') {
    requireSsh();
    const rendered = `ssh ${sshTarget} ${remoteCommand}`;
    commandLog.push({ type: label, command: rendered });
    const res = await execPromise('ssh', sshArgs(remoteCommand));
    return { ...res, ssh_command: rendered };
  }

  async function scpUpload(localPath, remotePath) {
    requireSsh();
    const rendered = `scp ${localPath} ${sshTarget}:${remotePath}`;
    commandLog.push({ type: 'scp', command: rendered });
    const res = await execPromise('scp', [
      '-O',
      '-i', ssh.key,
      '-o', 'StrictHostKeyChecking=accept-new',
      '-o', 'BatchMode=yes',
      localPath,
      `${sshTarget}:${remotePath}`,
    ]);
    return { ...res, ssh_command: rendered };
  }

  async function ensureRemoteDir(remoteDir) {
    const safe = sanitizeRunId(remoteDir.replace(/\//g, '_'));
    return sshRun(`mkdir -p ${remoteDir}`, 'mkdir');
  }

  return {
    getCommandLog: () => [...commandLog],

    async runSQL(container, sql, label = 'runSQL') {
      const remoteFile = `/tmp/mek-exec-${sanitizeRunId(label)}.sql`;
      const localTmp = `${cfg.bridgeDir}/out/.remote-${Date.now()}.sql`;
      const { writeFileSync } = await import('node:fs');
      writeFileSync(localTmp, sql, 'utf8');
      await scpUpload(localTmp, remoteFile);
      const copyCmd = `docker cp ${remoteFile} ${container}:${remoteFile}`;
      await sshRun(copyCmd, 'docker_cp');
      const execCmd = `docker exec ${container} bash -lc 'mariadb -uroot -p"$MYSQL_ROOT_PASSWORD" ${db.database} < ${remoteFile}'`;
      const res = await sshRun(execCmd, label);
      if (res.code !== 0) {
        throw new Error(`runSQL failed: ${res.stderr || res.stdout}`);
      }
      return { stdout: res.stdout, stderr: res.stderr, ssh_command: execCmd };
    },

    async queryRows(container, sql) {
      const b64 = Buffer.from(sql, 'utf8').toString('base64');
      const execCmd = `echo ${b64} | base64 -d | docker exec -i ${container} bash -lc 'mariadb -N -uroot -p"$MYSQL_ROOT_PASSWORD" ${db.database}'`;
      const res = await sshRun(execCmd, 'query');
      if (res.code !== 0) {
        throw new Error(`queryRows failed: ${res.stderr || res.stdout}`);
      }
      return res.stdout.trim();
    },

    async backupDatabase(container, tables, runId) {
      const backupId = `${sanitizeRunId(runId)}-pre`;
      const remotePath = `${db.backupDir}/${backupId}.sql`;
      const tableList = tables.join(' ');
      const script = [
        'set -euo pipefail',
        `mkdir -p ${db.backupDir}`,
        `docker exec ${container} sh -lc 'exec mariadb-dump --single-transaction --quick -uroot -p"$MYSQL_ROOT_PASSWORD" ${db.database} ${tableList}' > ${remotePath}`,
        `test -s ${remotePath}`,
        `echo BACKUP_FILE=${remotePath}`,
      ].join(' && ');
      const res = await sshRun(script, 'backup');
      if (res.code !== 0) {
        throw new Error(`backupDatabase failed: ${res.stderr || res.stdout}`);
      }
      return { backup_id: backupId, remote_path: remotePath, ssh_command: script };
    },

    async restoreBackup(container, backupId) {
      const remotePath = `${db.backupDir}/${sanitizeRunId(backupId)}.sql`;
      const execCmd = `docker exec -i ${container} bash -lc 'mariadb -uroot -p"$MYSQL_ROOT_PASSWORD" ${db.database}' < ${remotePath}`;
      const res = await sshRun(execCmd, 'restore');
      return res.code === 0;
    },

    async uploadAndApplyPatch(container, localPatchPath, runId) {
      const remoteDir = `/tmp/mek/${sanitizeRunId(runId)}`;
      await ensureRemoteDir(remoteDir);
      const remotePatch = `${remoteDir}/patch.sql`;
      await scpUpload(localPatchPath, remotePatch);
      const containerPath = `/tmp/mek-patch-${sanitizeRunId(runId)}.sql`;
      await sshRun(`docker cp ${remotePatch} ${container}:${containerPath}`, 'docker_cp_patch');
      const execCmd = `docker exec ${container} bash -lc 'mariadb -uroot -p"$MYSQL_ROOT_PASSWORD" ${db.database} < ${containerPath}'`;
      const res = await sshRun(execCmd, 'apply_patch');
      if (res.code !== 0) {
        throw new Error(`apply patch failed: ${res.stderr || res.stdout}`);
      }
      return { ssh_command: execCmd };
    },
  };
}
