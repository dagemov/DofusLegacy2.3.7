using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Rollback.Admin.Services;

public sealed class CombatClientPatchService
{
    private static readonly Regex DataFieldVisibilityRegex = new(
        @"protected\s+var\s+_datas:Array\s*=\s*new Array\(\);",
        RegexOptions.Compiled);
    public const string FightContextPatchDisabledMessage =
        "Parche bloqueado: cualquier mutacion automatica de FightContextFrame para combate/tactico/transicion queda prohibida. " +
        "El baseline limpio entra a combate estable y la prueba aislada con --fightcontext-safe-only reproduce VerifyError #1023.";

    private readonly ClientDataPathResolver _pathResolver;

    public CombatClientPatchService(ClientDataPathResolver pathResolver) =>
        _pathResolver = pathResolver;

    public async Task<CombatClientPatchResult> ApplyAsync(
        bool transitionStability = false,
        bool emoticonGuard = false,
        bool tacticalMode = false,
        bool castHoverFix = false,
        CancellationToken cancellationToken = default)
    {
        if (transitionStability || tacticalMode)
            throw new InvalidOperationException(FightContextPatchDisabledMessage);

        string? backupDirectory = null;
        var changes = new List<string>();

        if (emoticonGuard)
        {
            backupDirectory ??= CreateBackupDirectory();
            var emoticonChanged = await PatchSwfClassAsync(
                _pathResolver.EnsureDofusSwfPath(),
                "RoleplayEmoticonFrame",
                PatchRoleplayEmoticonFrameScript,
                backupDirectory,
                new[] { "RoleplayEmoticonFrame", "com.ankamagames.dofus.logic.game.roleplay.frames.RoleplayEmoticonFrame" },
                cancellationToken);

            changes.Add(emoticonChanged
                ? "Guardas de transicion aplicadas en RoleplayEmoticonFrame."
                : "RoleplayEmoticonFrame ya tenia las guardas de transicion.");
        }

        if (castHoverFix)
        {
            backupDirectory ??= CreateBackupDirectory();
            var changed = await PatchSwfClassAsync(
                _pathResolver.EnsureGameUiCoreSwfPath(),
                "Banner",
                PatchBannerScript,
                backupDirectory,
                new[] { "Banner", "ui.Banner" },
                cancellationToken);

            changes.Add(changed
                ? "Fix de hover/over de cast aplicado en Banner."
                : "Banner ya tenia aplicado el fix de hover/over de cast.");
        }

        if (changes.Count is 0)
            changes.Add("Sin cambios: baseline limpio sin parches de combate. FightContextFrame queda deshabilitado por inseguro.");

        return new CombatClientPatchResult(
            Summary: string.Join(" ", changes),
            Changes: changes,
            BackupDirectory: backupDirectory);
    }

    private async Task<bool> PatchSwfClassAsync(
        string swfPath,
        string className,
        Func<string, string> transform,
        string backupDirectory,
        IReadOnlyCollection<string> classSelectors,
        CancellationToken cancellationToken)
    {
        var workspace = _pathResolver.CreateTempWorkspace($"combat-{className.ToLowerInvariant()}");
        try
        {
            var (scriptsDirectory, scriptPath) = TryExportClassScript(
                swfPath,
                className,
                classSelectors,
                workspace);

            if (scriptPath is null || scriptsDirectory is null)
                throw new InvalidOperationException($"FFDec no exporto {className}.as desde {Path.GetFileName(swfPath)}.");

            var original = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8, cancellationToken);
            var transformed = EnsureImportableScript(transform(original));
            if (string.Equals(original, transformed, StringComparison.Ordinal))
                return false;

            await File.WriteAllTextAsync(scriptPath, transformed, Encoding.UTF8, cancellationToken);

            var patchedPath = Path.Combine(workspace, Path.GetFileName(swfPath));
            RunFfdec("-importScript", swfPath, patchedPath, scriptsDirectory);

            BackupFileIfNeeded(swfPath, backupDirectory);
            File.Copy(patchedPath, swfPath, overwrite: true);
            return true;
        }
        finally
        {
            try
            {
                if (Directory.Exists(workspace))
                    Directory.Delete(workspace, recursive: true);
            }
            catch
            {
            }
        }
    }

    private (string? ScriptsDirectory, string? ScriptPath) TryExportClassScript(
        string swfPath,
        string className,
        IReadOnlyCollection<string> classSelectors,
        string workspace)
    {
        foreach (var selector in classSelectors.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
        {
            var exportDirectory = Path.Combine(workspace, $"src-{SanitizeSegment(selector)}");
            Directory.CreateDirectory(exportDirectory);

            try
            {
                RunFfdec("-selectclass", selector, "-export", "script", exportDirectory, swfPath);
            }
            catch
            {
                continue;
            }

            var scriptsDirectory = Path.Combine(exportDirectory, "scripts");
            if (!Directory.Exists(scriptsDirectory))
                continue;

            var scriptPath = Directory
                .EnumerateFiles(scriptsDirectory, $"{className}.as", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (scriptPath is not null)
                return (scriptsDirectory, scriptPath);
        }

        var fullExportDirectory = Path.Combine(workspace, "src-full");
        Directory.CreateDirectory(fullExportDirectory);
        RunFfdec("-export", "script", fullExportDirectory, swfPath);

        var fullScriptsDirectory = Path.Combine(fullExportDirectory, "scripts");
        if (!Directory.Exists(fullScriptsDirectory))
            return (null, null);

        var fullScriptPath = Directory
            .EnumerateFiles(fullScriptsDirectory, $"{className}.as", SearchOption.AllDirectories)
            .FirstOrDefault();

        return (fullScriptsDirectory, fullScriptPath);
    }

    private static string PatchFightContextFrameScript(string script, bool tacticalMode)
    {
        var newline = GetNewline(script);

        if (!script.Contains("_combatTacticalModeApplied:Boolean", StringComparison.Ordinal))
        {
            script = InsertAfter(
                script,
                "      private var _fightType:uint;",
                $"{newline}      private var _combatTacticalModeApplied:Boolean = false;{newline}      {newline}      private var _previousAlwaysShowGrid:Boolean = false;{newline}      {newline}      private var _previousTransparentOverlayMode:Boolean = false;{newline}      {newline}      private var _previousGroundOnly:Boolean = false;{newline}      {newline}      private var _previousHideForeground:Boolean = false;");
        }

        script = ReplaceOnce(
            script,
            "             Atouin.getInstance().displayGrid(true);",
            "             this.applyCombatTacticalMode();");

        script = ReplaceOnce(
            script,
            "         Atouin.getInstance().displayGrid(false);",
            "         this.restoreCombatTacticalMode();");

        var applyMethod = tacticalMode
            ? $"      private function applyCombatTacticalMode() : void{newline}" +
              $"      {{{newline}" +
              $"         var atouin:Atouin = Atouin.getInstance();{newline}" +
              $"         if(!atouin || !atouin.options || this._combatTacticalModeApplied){newline}" +
              $"         {{{newline}" +
              $"            return;{newline}" +
              $"         }}{newline}" +
              $"         this._combatTacticalModeApplied = true;{newline}" +
              $"         this._previousAlwaysShowGrid = Boolean(atouin.options.alwaysShowGrid);{newline}" +
              $"         atouin.options.alwaysShowGrid = true;{newline}" +
              $"         atouin.displayGrid(true);{newline}" +
              $"      }}{newline}" +
              $"      {newline}"
            : $"      private function applyCombatTacticalMode() : void{newline}" +
              $"      {{{newline}" +
              $"         Atouin.getInstance().displayGrid(true);{newline}" +
              $"      }}{newline}" +
              $"      {newline}";

        var restoreMethod = tacticalMode
            ? $"      private function restoreCombatTacticalMode() : void{newline}" +
              $"      {{{newline}" +
              $"         var atouin:Atouin = Atouin.getInstance();{newline}" +
              $"         if(!atouin || !atouin.options){newline}" +
              $"         {{{newline}" +
              $"            return;{newline}" +
              $"         }}{newline}" +
              $"         if(this._combatTacticalModeApplied){newline}" +
              $"         {{{newline}" +
              $"            atouin.options.alwaysShowGrid = this._previousAlwaysShowGrid;{newline}" +
              $"            this._combatTacticalModeApplied = false;{newline}" +
              $"         }}{newline}" +
              $"         atouin.displayGrid(atouin.options.alwaysShowGrid);{newline}" +
              $"      }}{newline}" +
              $"      {newline}"
            : $"      private function restoreCombatTacticalMode() : void{newline}" +
              $"      {{{newline}" +
              $"         Atouin.getInstance().displayGrid(false);{newline}" +
              $"      }}{newline}" +
              $"      {newline}";

        var reloadMethod =
            $"      private function reloadCombatMapDisplay() : void{newline}" +
            $"      {{{newline}" +
            $"      }}{newline}" +
            $"      {newline}";

        if (script.Contains("private function applyCombatTacticalMode() : void", StringComparison.Ordinal))
            script = ReplaceMethod(script, "private function applyCombatTacticalMode() : void", applyMethod);

        if (script.Contains("private function restoreCombatTacticalMode() : void", StringComparison.Ordinal))
            script = ReplaceMethod(script, "private function restoreCombatTacticalMode() : void", restoreMethod);

        if (script.Contains("private function reloadCombatMapDisplay() : void", StringComparison.Ordinal))
            script = ReplaceMethod(script, "private function reloadCombatMapDisplay() : void", reloadMethod);

        if (!script.Contains("private function applyCombatTacticalMode() : void", StringComparison.Ordinal))
        {
            script = InsertBefore(
                script,
                "      public function getFighterName(fighterId:int) : String",
                applyMethod + restoreMethod + reloadMethod);
        }

        return script;
    }

    private static string PatchRoleplayEmoticonFrameScript(string script)
    {
        var newline = GetNewline(script);

        var intervalMethod =
            $"      public function interval() : void{newline}" +
            $"      {{{newline}" +
            $"         var playedCharacterManager:PlayedCharacterManager = PlayedCharacterManager.getInstance();{newline}" +
            $"         if(!playedCharacterManager || !playedCharacterManager.characteristics){newline}" +
            $"         {{{newline}" +
            $"            if(Boolean(this._interval)){newline}" +
            $"            {{{newline}" +
            $"               clearInterval(this._interval);{newline}" +
            $"               this._interval = 0;{newline}" +
            $"            }}{newline}" +
            $"            return;{newline}" +
            $"         }}{newline}" +
            $"         playedCharacterManager.characteristics.lifePoints = playedCharacterManager.characteristics.lifePoints + 1;{newline}" +
            $"         if(playedCharacterManager.characteristics.lifePoints >= playedCharacterManager.characteristics.maxLifePoints){newline}" +
            $"         {{{newline}" +
            $"            if(Boolean(this._interval)){newline}" +
            $"            {{{newline}" +
            $"               clearInterval(this._interval);{newline}" +
            $"               this._interval = 0;{newline}" +
            $"            }}{newline}" +
            $"            playedCharacterManager.characteristics.lifePoints = playedCharacterManager.characteristics.maxLifePoints;{newline}" +
            $"         }}{newline}" +
            $"         KernelEventsManager.getInstance().processCallback(HookList.CharacterStatsList);{newline}" +
            $"      }}{newline}";

        var pulledMethod =
            $"      public function pulled() : Boolean{newline}" +
            $"      {{{newline}" +
            $"         if(Boolean(this._interval)){newline}" +
            $"         {{{newline}" +
            $"            clearInterval(this._interval);{newline}" +
            $"            this._interval = 0;{newline}" +
            $"         }}{newline}" +
            $"         return true;{newline}" +
            $"      }}{newline}";

        if (script.Contains("public function interval() : void", StringComparison.Ordinal))
            script = ReplaceMethod(script, "public function interval() : void", intervalMethod);

        if (script.Contains("public function pulled() : Boolean", StringComparison.Ordinal))
            script = ReplaceMethod(script, "public function pulled() : Boolean", pulledMethod);

        script = ReplaceOnce(
            script,
            $"               this._interval = setInterval(this.interval,lprbmsg.regenRate * 100);",
            $"               if(Boolean(this._interval)){newline}               {{{newline}                  clearInterval(this._interval);{newline}               }}{newline}               this._interval = setInterval(this.interval,lprbmsg.regenRate * 100);");

        script = ReplaceOnce(
            script,
            $"               clearInterval(this._interval);{newline}               PlayedCharacterManager.getInstance().characteristics.lifePoints = lpremsg.lifePoints;{newline}               PlayedCharacterManager.getInstance().characteristics.maxLifePoints = lpremsg.maxLifePoints;{newline}               KernelEventsManager.getInstance().processCallback(HookList.CharacterStatsList);",
            $"               if(Boolean(this._interval)){newline}               {{{newline}                  clearInterval(this._interval);{newline}                  this._interval = 0;{newline}               }}{newline}               if(Boolean(PlayedCharacterManager.getInstance()) && Boolean(PlayedCharacterManager.getInstance().characteristics)){newline}               {{{newline}                  PlayedCharacterManager.getInstance().characteristics.lifePoints = lpremsg.lifePoints;{newline}                  PlayedCharacterManager.getInstance().characteristics.maxLifePoints = lpremsg.maxLifePoints;{newline}                  KernelEventsManager.getInstance().processCallback(HookList.CharacterStatsList);{newline}               }}");

        return script;
    }

    private static string PatchBannerScript(string script)
    {
        var newline = GetNewline(script);

        if (!script.Contains("_isSpellCastMode:Boolean", StringComparison.Ordinal))
        {
            script = InsertAfter(
                script,
                "      private var _waitingObjectPosition:uint;",
                $"{newline}      {newline}      private var _isSpellCastMode:Boolean = false;{newline}      {newline}      private var _castModeUiTargets:Array;");
        }

        script = ReplaceOnce(
            script,
            $"      public function onCancelCastSpell(spellWrapper:Object) : void{newline}      {{{newline}         this.uiApi.setFollowCursorUri(null);{newline}      }}",
            $"      public function onCancelCastSpell(spellWrapper:Object) : void{newline}      {{{newline}         this._isSpellCastMode = false;{newline}         this.clearCastModeUiState();{newline}         this.setCastModeUiInteractions(true);{newline}         this.uiApi.setFollowCursorUri(null);{newline}      }}");

        script = ReplaceOnce(
            script,
            $"      public function onCastSpellMode(spellWrapper:Object) : void{newline}      {{{newline}         this.uiApi.setFollowCursorUri(spellWrapper.iconUri,false,false,-15,-15,0.75);{newline}      }}",
            $"      public function onCastSpellMode(spellWrapper:Object) : void{newline}      {{{newline}         this._isSpellCastMode = true;{newline}         this.clearCastModeUiState();{newline}         this.setCastModeUiInteractions(false);{newline}         this.uiApi.setFollowCursorUri(spellWrapper.iconUri,false,false,-15,-15,0.75);{newline}      }}");

        script = ReplaceOnce(
            script,
            $"      public function unload() : void{newline}      {{{newline}         this.uiApi.setFollowCursorUri(null);",
            $"      public function unload() : void{newline}      {{{newline}         this._isSpellCastMode = false;{newline}         this.clearCastModeUiState();{newline}         this.setCastModeUiInteractions(true);{newline}         this.uiApi.setFollowCursorUri(null);");

        script = ReplaceOnce(
            script,
            $"      public function onRollOver(target:Object) : void{newline}      {{{newline}         var tooltipText:String = null;",
            $"      public function onRollOver(target:Object) : void{newline}      {{{newline}         if(Boolean(this._isSpellCastMode)){newline}         {{{newline}            this.uiApi.hideTooltip();{newline}            return;{newline}         }}{newline}         var tooltipText:String = null;");

        script = ReplaceOnce(
            script,
            $"      public function onItemRollOver(target:Object, item:Object) : void{newline}      {{{newline}         var data:* = undefined;",
            $"      public function onItemRollOver(target:Object, item:Object) : void{newline}      {{{newline}         if(Boolean(this._isSpellCastMode)){newline}         {{{newline}            this.uiApi.hideTooltip();{newline}            return;{newline}         }}{newline}         var data:* = undefined;");

        if (!script.Contains("private function clearCastModeUiState()", StringComparison.Ordinal))
        {
            var methodBlock =
                $"      private function clearCastModeUiState() : void{newline}" +
                $"      {{{newline}" +
                $"         this.uiApi.hideTooltip();{newline}" +
                $"      }}{newline}" +
                $"      {newline}" +
                $"      private function setCastModeUiInteractions(enabled:Boolean) : void{newline}" +
                $"      {{{newline}" +
                $"         var target:Object = null;{newline}" +
                $"         if(!this._castModeUiTargets){newline}" +
                $"         {{{newline}" +
                $"            this._castModeUiTargets = [this.btn_items,this.btn_character,this.btn_quests,this.btn_map,this.btn_friends,this.btn_mount,this.btn_mainMenu,this.btn_tabSpells,this.btn_tabItems,this.btn_tabEmotes,this.btn_upArrow,this.btn_downArrow,this.gd_spellitemotes,this.miniMap];{newline}" +
                $"         }}{newline}" +
                $"         for each(target in this._castModeUiTargets){newline}" +
                $"         {{{newline}" +
                $"            if(Boolean(target)){newline}" +
                $"            {{{newline}" +
                $"               if(target.hasOwnProperty(\"mouseEnabled\")){newline}" +
                $"               {{{newline}" +
                $"                  target.mouseEnabled = enabled;{newline}" +
                $"               }}{newline}" +
                $"               if(target.hasOwnProperty(\"mouseChildren\")){newline}" +
                $"               {{{newline}" +
                $"                  target.mouseChildren = enabled;{newline}" +
                $"               }}{newline}" +
                $"            }}{newline}" +
                $"         }}{newline}" +
                $"      }}{newline}" +
                $"      {newline}";

            script = InsertBefore(
                script,
                "      public function onSpellMovement(spellId:uint, position:uint) : void",
                methodBlock);
        }

        return script;
    }

    private void RunFfdec(params string[] arguments)
    {
        var ffdecPath = _pathResolver.EnsureFfdecCliPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = ffdecPath,
            Arguments = BuildArguments(arguments),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo iniciar FFDec.");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
            return;

        var details = string.Join(
            Environment.NewLine,
            new[] { standardOutput, standardError }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()));

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(details)
                ? $"FFDec fallo con codigo {process.ExitCode}."
                : $"FFDec fallo con codigo {process.ExitCode}:{Environment.NewLine}{details}");
    }

    private string CreateBackupDirectory()
    {
        var repoRoot = _pathResolver.EnsureRepoRoot();
        var backupDirectory = Path.Combine(
            repoRoot,
            "runtime",
            "client-state-backups",
            "combat-client-patch",
            DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        Directory.CreateDirectory(backupDirectory);
        return backupDirectory;
    }

    private static string InsertAfter(string script, string marker, string content)
    {
        var index = script.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
            throw new InvalidOperationException($"No se encontro el marcador '{marker}' para insertar el parche cliente.");

        return script.Insert(index + marker.Length, content);
    }

    private static string InsertBefore(string script, string marker, string content)
    {
        var index = script.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
            throw new InvalidOperationException($"No se encontro el marcador '{marker}' para insertar el parche cliente.");

        return script.Insert(index, content);
    }

    private static string ReplaceOnce(string script, string oldValue, string newValue)
    {
        var index = script.IndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0)
            return script;

        return script.Remove(index, oldValue.Length).Insert(index, newValue);
    }

    private static string ReplaceMethod(string script, string signature, string replacement)
    {
        var signatureIndex = script.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
            return script;

        var bodyStart = script.IndexOf('{', signatureIndex);
        if (bodyStart < 0)
            return script;

        var bodyEnd = FindMatchingBrace(script, bodyStart);
        if (bodyEnd < 0)
            return script;

        return script.Remove(signatureIndex, bodyEnd - signatureIndex + 1).Insert(signatureIndex, replacement);
    }

    private static string GetNewline(string script) =>
        script.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static string EnsureImportableScript(string script)
    {
        if (DataFieldVisibilityRegex.IsMatch(script))
            return DataFieldVisibilityRegex.Replace(script, "public var _datas:Array = new Array();", 1);

        return script;
    }

    private static int FindMatchingBrace(string script, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < script.Length; i++)
        {
            switch (script[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                        return i;
                    break;
            }
        }

        return -1;
    }

    private static void BackupFileIfNeeded(string filePath, string backupDirectory)
    {
        if (!File.Exists(filePath))
            return;

        var backupPath = Path.Combine(backupDirectory, Path.GetFileName(filePath));
        if (File.Exists(backupPath))
            return;

        File.Copy(filePath, backupPath, overwrite: false);
    }

    private static string BuildArguments(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(QuoteArgument));

    private static string QuoteArgument(string value) =>
        value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;

    private static string SanitizeSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
    }
}

public sealed record CombatClientPatchResult(string Summary, IReadOnlyCollection<string> Changes, string? BackupDirectory);
