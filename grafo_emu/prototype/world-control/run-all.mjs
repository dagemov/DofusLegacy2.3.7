#!/usr/bin/env node
/** Run all MCP World Control v1 validation tests */
import { writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { fullReport } from './mcp-readiness.mjs';

const json = JSON.stringify(fullReport, null, 2);
console.log(json);

const outPath = join(dirname(fileURLToPath(import.meta.url)), 'last-run.json');
writeFileSync(outPath, json, 'utf8');

process.exit(0);
