#!/usr/bin/env node
/** Phase A — C# code flow inventory */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { scanCsharpRoleFiles, writeJson } from './_model-lib.mjs';

const scan = scanCsharpRoleFiles();

const byRole = {};
for (const c of scan.classes) {
  byRole[c.role] = (byRole[c.role] || 0) + 1;
}

const flows = scan.classes.map((c) => {
  const verbs = {};
  for (const m of c.methods) {
    verbs[m.verb] = (verbs[m.verb] || 0) + 1;
  }
  const dominantVerb = Object.entries(verbs).sort((a, b) => b[1] - a[1])[0]?.[0] || 'other';
  return {
    class_name: c.class_name,
    file: c.file,
    role: c.role,
    namespace: c.namespace,
    tables_manipulated: c.tables_referenced,
    table_count: c.tables_referenced.length,
    method_count: c.methods.length,
    methods: c.methods.map((m) => ({ name: m.name, verb: m.verb })),
    methods_sample: c.methods.slice(0, 15).map((m) => ({ name: m.name, verb: m.verb })),
    verb_distribution: verbs,
    dominant_flow: dominantVerb,
    dependencies: c.dependencies,
    entities_inferred: [...new Set([
      ...c.tables_referenced,
      ...c.dependencies.filter((d) => !d.endsWith('Manager')),
    ])],
  };
});

flows.sort((a, b) => b.table_count - a.table_count);

export const result = {
  phase: 'A_CODE_INVENTORY',
  source: 'Sunshine net11.0/Sunshine net11.0',
  total_cs_files: scan.total_cs_files,
  role_files_scanned: scan.role_files_scanned,
  by_role: byRole,
  classes: flows,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'code_flow_inventory.json'), result);

if (process.argv[1]?.includes('code-inventory')) {
  console.log(JSON.stringify({
    role_files_scanned: result.role_files_scanned,
    total_cs_files: result.total_cs_files,
    by_role: result.by_role,
  }, null, 2));
}
