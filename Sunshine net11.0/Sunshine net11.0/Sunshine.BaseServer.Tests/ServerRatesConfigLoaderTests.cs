using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sunshine.BaseServer.Configuration;
using Xunit;

namespace Sunshine.BaseServer.Tests
{
    public class ServerRatesConfigLoaderTests
    {
        private readonly ServerRatesConfigLoader _loader = new ServerRatesConfigLoader();
        private readonly List<string> _infoLogs = new List<string>();
        private readonly List<string> _warningLogs = new List<string>();

        [Fact]
        public void Load_ValidFile_AppliesConfiguredValues()
        {
            using (var directory = new TempDirectory())
            {
                var filePath = Path.Combine(directory.Path, ServerRatesConfig.FileName);
                File.WriteAllLines(filePath, new[]
                {
                    "XP_RATE=3.5",
                    "DROP_RATE=2",
                    "KAMAS_RATE=4",
                    "PP_RATE=1.5",
                    "WEAPON_USES_PER_TURN=3",
                    "WEAPON_USES_PER_FIGHT=5",
                    "SPELL_USES_DEFAULT=2"
                });

                var result = Load(filePath);

                Assert.Equal(3.5d, result.Config.XpRate);
                Assert.Equal(2d, result.Config.DropRate);
                Assert.Equal(4d, result.Config.KamasRate);
                Assert.Equal(1.5d, result.Config.PpRate);
                Assert.Equal(3, result.Config.WeaponUsesPerTurn);
                Assert.Equal(5, result.Config.WeaponUsesPerFight);
                Assert.Equal(2, result.Config.SpellUsesDefault);
                Assert.Empty(result.Warnings);
            }
        }

        [Fact]
        public void Load_MissingFile_CreatesDefaults()
        {
            using (var directory = new TempDirectory())
            {
                var filePath = Path.Combine(directory.Path, ServerRatesConfig.FileName);
                Assert.False(File.Exists(filePath));

                var result = Load(filePath);

                Assert.True(result.CreatedFile);
                Assert.True(File.Exists(filePath));
                Assert.Equal(2d, result.Config.XpRate);
                Assert.Equal(1d, result.Config.DropRate);
                Assert.Equal(1d, result.Config.KamasRate);
                Assert.Equal(1d, result.Config.PpRate);
                Assert.Equal(2, result.Config.WeaponUsesPerTurn);
                Assert.Equal(0, result.Config.WeaponUsesPerFight);
                Assert.Equal(0, result.Config.SpellUsesDefault);
            }
        }

        [Fact]
        public void Load_InvalidValues_UseDefaultsAndWarn()
        {
            using (var directory = new TempDirectory())
            {
                var filePath = Path.Combine(directory.Path, ServerRatesConfig.FileName);
                File.WriteAllLines(filePath, new[]
                {
                    "XP_RATE=not-a-number",
                    "WEAPON_USES_PER_TURN=-3",
                    "BAD_LINE_WITHOUT_EQUALS"
                });

                var result = Load(filePath);

                Assert.Equal(2d, result.Config.XpRate);
                Assert.Equal(2, result.Config.WeaponUsesPerTurn);
                Assert.True(result.Warnings.Count >= 2);
                Assert.Contains(result.Warnings, warning => warning.Contains("XP_RATE"));
                Assert.Contains(result.Warnings, warning => warning.Contains("WEAPON_USES_PER_TURN"));
            }
        }

        [Theory]
        [InlineData(0, int.MaxValue)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        public void WeaponUsesPerTurn_AffectsCombatLimit(int configuredUses, int expectedLimit)
        {
            var config = new ServerRatesConfig { WeaponUsesPerTurn = configuredUses };

            Assert.Equal(expectedLimit, config.ResolveMaxWeaponUsesPerTurn());
        }

        [Theory]
        [InlineData(0u, 0, 0u)]
        [InlineData(0u, 3, 3u)]
        [InlineData(2u, 0, 2u)]
        [InlineData(2u, 5, 2u)]
        public void SpellUsesDefault_RespectsTemplateOverride(uint templateMax, int serverDefault, uint expected)
        {
            var config = new ServerRatesConfig { SpellUsesDefault = serverDefault };

            Assert.Equal(expected, config.ResolveMaxSpellUsesPerTurn(templateMax));
        }

        private ServerRatesLoadResult Load(string filePath)
        {
            _infoLogs.Clear();
            _warningLogs.Clear();
            return _loader.Load(filePath, _infoLogs.Add, _warningLogs.Add);
        }

        private sealed class TempDirectory : IDisposable
        {
            public TempDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sunshine-rates-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch
                {
                }
            }
        }
    }
}
