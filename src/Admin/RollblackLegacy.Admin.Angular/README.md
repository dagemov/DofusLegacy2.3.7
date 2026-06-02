# RollblackLegacy Admin Angular

Angular workspace for the in-repo admin UI.

Current implemented slice:

- `/admin/items`
- `/admin/items/:itemId`

Local commands:

```bash
npm install
npm start
npm run build
```

The dev server proxies `/api/*` to the local Admin API through `proxy.conf.json`.
