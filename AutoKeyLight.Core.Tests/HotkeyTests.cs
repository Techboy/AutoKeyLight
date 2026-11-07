using AutoKeyLight.Core;

namespace AutoKeyLight.Core.Tests
{
    public class HotkeyTests
    {
        [Fact]
        public void DefaultsRegisterNothing()
        {
            RecordingRegistrar registrar = new RecordingRegistrar();
            HotkeyBook book = new HotkeyBook();
            IReadOnlyList<string> errors = book.BindSaved("", null, "  ", registrar);
            Assert.Empty(errors);
            Assert.Empty(registrar.Registrations);
            Assert.Equal("None", book.Display(LightMode.Auto));
            Assert.Equal("None", book.Display(LightMode.On));
            Assert.Equal("None", book.Display(LightMode.Off));
        }

        [Theory]
        [InlineData("A")]
        [InlineData("Ctrl")]
        [InlineData("Ctrl+Alt")]
        [InlineData("Win+A")]
        [InlineData("Ctrl+C")]
        [InlineData("Alt+F4")]
        [InlineData("Ctrl+Shift+Esc")]
        public void UnsafeShortcutsAreRejected(string text)
        {
            Assert.False(HotkeyGrammar.TryParse(text, out HotkeyChord? chord, out string? error));
            Assert.Null(chord);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void ModifierChordIsAcceptedAndMapped()
        {
            Assert.True(HotkeyGrammar.TryParse("ctrl+alt+shift+f13", out HotkeyChord? chord, out string? error));
            Assert.Null(error);
            Assert.NotNull(chord);
            Assert.Equal("Ctrl+Alt+Shift+F13", HotkeyGrammar.Format(chord!));
            Assert.True(HotkeyGrammar.TryVirtualKey(chord, out uint modifiers, out uint virtualKey));
            Assert.Equal(0x70u + 12, virtualKey);
            Assert.True((modifiers & 0x0001) != 0);
            Assert.True((modifiers & 0x0002) != 0);
            Assert.True((modifiers & 0x0004) != 0);
            Assert.True((modifiers & 0x4000) != 0);
        }

        [Fact]
        public void TheSameChordCannotBeUsedForTwoModes()
        {
            RecordingRegistrar registrar = new RecordingRegistrar();
            HotkeyBook book = new HotkeyBook();
            HotkeyChord chord = new HotkeyChord(true, true, true, "A");
            Assert.True(book.Assign(LightMode.Auto, chord, registrar).Applied);
            HotkeyAssignment second = book.Assign(LightMode.On, chord, registrar);
            Assert.False(second.Applied);
            Assert.Contains("Auto", second.Error);
            Assert.Null(book.Current(LightMode.On));
            Assert.Equal(chord, book.Current(LightMode.Auto));
        }

        [Fact]
        public void RegistrationConflictKeepsThePreviousShortcut()
        {
            RecordingRegistrar registrar = new RecordingRegistrar { Fail = new HotkeyChord(true, true, false, "B") };
            HotkeyBook book = new HotkeyBook();
            HotkeyChord original = new HotkeyChord(true, true, true, "A");
            book.Assign(LightMode.On, original, registrar);
            HotkeyAssignment result = book.Assign(LightMode.On, registrar.Fail, registrar);
            Assert.False(result.Applied);
            Assert.Contains("another program", result.Error);
            Assert.Equal(original, book.Current(LightMode.On));
            Assert.Contains(registrar.Registrations, entry => entry.Id == HotkeyIds.For(LightMode.On) && entry.Chord.Equals(original));
        }

        [Fact]
        public void ReplacingAShortcutUnregistersTheOldIdFirst()
        {
            RecordingRegistrar registrar = new RecordingRegistrar();
            HotkeyBook book = new HotkeyBook();
            book.Assign(LightMode.Off, new HotkeyChord(true, true, true, "O"), registrar);
            registrar.Events.Clear();
            book.Assign(LightMode.Off, new HotkeyChord(true, true, true, "F"), registrar);
            Assert.Equal("unregister", registrar.Events[0].Kind);
            Assert.Equal(HotkeyIds.For(LightMode.Off), registrar.Events[0].Id);
            Assert.Equal("register", registrar.Events[1].Kind);
        }

        [Fact]
        public void ReleaseAllClearsEveryRegistration()
        {
            RecordingRegistrar registrar = new RecordingRegistrar();
            HotkeyBook book = new HotkeyBook();
            book.Assign(LightMode.Auto, new HotkeyChord(true, true, true, "A"), registrar);
            book.ReleaseAll(registrar);
            Assert.Null(book.Current(LightMode.Auto));
            Assert.Contains(HotkeyIds.For(LightMode.Auto), registrar.Unregistered);
            Assert.Contains(HotkeyIds.For(LightMode.On), registrar.Unregistered);
            Assert.Contains(HotkeyIds.For(LightMode.Off), registrar.Unregistered);
        }

        [Fact]
        public void HotkeyIdsAreStableAndDistinct()
        {
            int[] ids = { HotkeyIds.For(LightMode.Auto), HotkeyIds.For(LightMode.On), HotkeyIds.For(LightMode.Off) };
            Assert.Equal(ids.Distinct().Count(), ids.Length);
            Assert.True(HotkeyIds.TryMode(ids[1], out LightMode mode));
            Assert.Equal(LightMode.On, mode);
            Assert.False(HotkeyIds.TryMode(99, out _));
        }

        [Fact]
        public void SavedInvalidShortcutIsReportedAndNotRegistered()
        {
            RecordingRegistrar registrar = new RecordingRegistrar();
            HotkeyBook book = new HotkeyBook();
            IReadOnlyList<string> errors = book.BindSaved("Ctrl+C", "", "", registrar);
            Assert.NotEmpty(errors);
            Assert.Empty(registrar.Registrations);
        }

        private sealed class RecordingRegistrar : IHotkeyRegistrar
        {
            public List<(int Id, HotkeyChord Chord)> Registrations { get; } = new List<(int, HotkeyChord)>();
            public List<int> Unregistered { get; } = new List<int>();
            public List<(string Kind, int Id)> Events { get; } = new List<(string, int)>();
            public HotkeyChord? Fail { get; set; }

            public bool TryRegister(int id, HotkeyChord chord)
            {
                Events.Add(("register", id));
                if (Fail != null && Fail.Equals(chord))
                    return false;

                Registrations.Add((id, chord));
                return true;
            }

            public void Unregister(int id)
            {
                Events.Add(("unregister", id));
                Unregistered.Add(id);
            }
        }
    }
}
