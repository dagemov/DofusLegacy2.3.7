# Notas de JPEXS / FFDec para Items

## Propósito

Documentar cuándo `JPEXS Free Flash Decompiler` sí aplica a esta migración y cuándo no.

## Fuente oficial

- Repositorio oficial: [jindrapetrik/jpexs-decompiler](https://github.com/jindrapetrik/jpexs-decompiler)
- Wiki oficial de línea de comandos: [Commandline arguments](https://github.com/jindrapetrik/jpexs-decompiler/wiki/Commandline-arguments)

Resumen oficial útil:

- el proyecto se presenta como decompiler/editor de SWF
- permite extraer recursos, editar scripts y reemplazar imágenes, textos, sonidos y fuentes
- soporta exportación por CLI para imágenes, sprites y shapes

## Comandos CLI relevantes

Según la wiki oficial:

```txt
-export <itemtypes> <outdirectory> <infile_or_directory>
-dumpSWF <infile>
```

Subtipos relevantes para esta investigación:

```txt
image
sprite
shape
```

Ejemplos conceptuales compatibles con la ayuda oficial:

```txt
ffdec-cli.exe -export image <outdir> <input.swf>
ffdec-cli.exe -export sprite <outdir> <input.swf>
ffdec-cli.exe -export shape <outdir> <input.swf>
ffdec-cli.exe -dumpSWF <input.swf>
```

## Dónde sí aplica bien

Aplica bien a la referencia legacy basada en SWF:

- `Items0.swf ... Items10.swf`
- `ItemSets0.swf`
- `ItemTypes0.swf`
- `i18n_es/*.swf`
- `i18n_en/*.swf`

Usos razonables:

- inspección puntual de recursos
- verificar si un icono o sprite legacy existe
- estudiar cómo se empaquetaban identidades en el stack anterior

## Dónde no debe ser la herramienta principal

No debe asumirse como lane principal del cliente actual porque el repo oficial actual usa:

- `Items.d2o`
- `ItemTypes.d2o`
- `ItemSets.d2o`
- `Appearances.d2o`
- `i18n_es.d2i`
- `i18n_en.d2i`
- `bitmap*.d2p`
- `vector*.d2p`

Conclusión:

- para el cliente actual, la investigación debe empezar por `D2O/D2I/D2P`
- `JPEXS / FFDec` queda como herramienta de referencia legacy o de investigación puntual, no como extractor principal del pipeline actual

## Uso futuro permitido

Permitido en una fase futura controlada:

- extracción offline puntual
- sobre uno o muy pocos archivos
- con output en `Infrastructure/temporal-artifacts`

No permitido como paso de esta fase:

- correr sobre todo el cliente
- generar dumps masivos trackeados
- usarlo para justificar cambios cliente sin pipeline de publicación

## Regla operativa

Si el objetivo es:

- `icono legacy SWF`: `JPEXS` puede ayudar
- `template visible en cliente actual`: primero `Items.d2o` + `i18n*.d2i`
- `preview de icono actual`: primero catálogo curado + `bitmap*.d2p`
- `look equipado actual`: primero `Appearances.d2o` y mapping offline
