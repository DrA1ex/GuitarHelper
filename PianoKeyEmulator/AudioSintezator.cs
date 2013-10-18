using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;

namespace PianoKeyEmulator
{
    internal struct ChordType
    {
        public string description;
        public int[] intervals;
        public string name;

        public ChordType(string desc, string name, params int[] intervals)
        {
            description = desc;
            this.name = name;
            this.intervals = intervals;
        }

        public List<Note> BuildChord(Note baseNote)
        {
            var result = new List<Note>();

            result.Add(baseNote);

            Note current = baseNote;

            foreach (int interval in intervals)
            {
                current = current + interval;
                result.Add(current);
            }

            return result;
        }
    }

    internal struct Note
    {
        public int Id;
        public byte Octave;
        public Tones Tone;

        public Note(byte oct, Tones t)
        {
            Octave = oct;
            Tone = t;

            Id = 12 + Octave*12 + (int) Tone;
        }

        public static Note FromString(string str)
        {
            byte octave = byte.Parse(str.Last().ToString());
            var tone = str.Substring(0, str.Length - 1).Replace('#', 'd').ConvertToEnum<Tones>();

            return new Note(octave, tone);
        }

        public static Note FromID(int id)
        {
            return new Note((byte) (id/12 - 1), (Tones) (id%12));
        }

        public bool isDiez()
        {
            return Tone.ToString().Contains('d');
        }

        #region Operators Defenition

        public static int operator -(Note note1, Note note2)
        {
            return Math.Abs(note1.Id - note2.Id);
        }

        public static Note operator +(Note note, int semitons)
        {
            var octave = (byte) (note.Octave + semitons/12); // 12 полутонов в октаве
            int tmp = (int) note.Tone + semitons%12;
            if (tmp > (int) Tones.B) // Последняя нота в октаве
            {
                ++octave;
                tmp = tmp%12;
            }
            var tone = (Tones) (tmp);

            return new Note(octave, tone);
        }

        public static bool operator <(Note note1, Note note2)
        {
            return note1.Id < note2.Id;
        }

        public static bool operator >(Note note1, Note note2)
        {
            return note1.Id > note2.Id;
        }

        public static bool operator >=(Note note1, Note note2)
        {
            return note1.Id >= note2.Id;
        }

        public static bool operator <=(Note note1, Note note2)
        {
            return note1.Id <= note2.Id;
        }

        public static bool operator ==(Note note1, Note note2)
        {
            return note1.Id == note2.Id;
        }

        public static bool operator !=(Note note1, Note note2)
        {
            return note1.Id != note2.Id;
        }

        public override string ToString()
        {
            return Tone.ToString().Replace('d', '#') + Octave;
        }

        #endregion
    }

    public enum Tones
    {
        A = 9,
        Ad = 10,
        B = 11,
        C = 0,
        Cd = 1,
        D = 2,
        Dd = 3,
        E = 4,
        F = 5,
        Fd = 6,
        G = 7,
        Gd = 8
    }

    internal class AudioSintezator : IDisposable
    {
        public enum Instruments
        {
            AcousticPiano,
            BriteAcouPiano,
            ElectricGrandPiano,
            HonkyTonkPiano,
            ElecPiano1,
            ElecPiano2,
            Harsichord,
            Clavichord,
            Celesta,
            Glockenspiel,
            MusicBox,
            Vibraphone,
            Marimba,
            Xylophone,
            TubularBells,
            Dulcimer,
            DrawbarOrgan,
            PercOrgan,
            RockOrgan,
            ChurchOrgan,
            ReedOrgan,
            Accordian,
            Harmonica,
            TangoAccordian,
            AcousticGuitar,
            SteelAcousGuitar,
            ElJazzGuitar,
            ElectricGuitar,
            ElMutedGuitar,
            OverdrivenGuitar,
            DistortionGuitar,
            GuitarHarmonic,
            AcousticBass,
            ElBassFinger,
            ElBassPick,
            FretlessBass,
            SlapBass1,
            SlapBass2,
            SynthBass1,
            SynthBass2,
            Violin,
            Viola,
            Cello,
            ContraBass,
            TremeloStrings,
            PizzStrings,
            OrchStrings,
            Timpani,
            StringEns1,
            StringEns2,
            SynthStrings1,
            SynthStrings2,
            ChoirAahs,
            VoiceOohs,
            SynthVoice,
            OrchestraHit,
            Trumpet,
            Trombone,
            Tuba,
            MutedTrumpet,
            FrenchHorn,
            BrassSection,
            SynthBrass1,
            SynthBrass2,
            SopranoSax,
            AltoSax,
            TenorSax,
            BaritoneSax,
            Oboe,
            EnglishHorn,
            Bassoon,
            Clarinet,
            Piccolo,
            Flute,
            Recorder,
            PanFlute,
            BlownBottle,
            Shakuhachi,
            Whistle,
            Ocarina,
            Lead1Square,
            Lead2Sawtooth,
            Lead3Calliope,
            Lead4Chiff,
            Lead5Charang,
            Lead6Voice,
            Lead7Fifths,
            Lead8BassLd,
            Pad1NewAge,
            Pad2Warm,
            Pad3Polysynth,
            Pad4Choir,
            Pad5Bowed,
            Pad6Metallic,
            Pad7Halo,
            Pad8Sweep,
            FX1Rain,
            FX2Soundtrack,
            FX3Crystal,
            FX4Atmosphere,
            FX5Brightness,
            FX6Goblins,
            FX7Echoes,
            FX8SciFi,
            Sitar,
            Banjo,
            Shamisen,
            Koto,
            Kalimba,
            Bagpipe,
            Fiddle,
            Shanai,
            TinkerBell,
            Agogo,
            SteelDrums,
            Woodblock,
            TaikoDrum,
            MelodicTom,
            SynthDrum,
            ReverseCymbal,
            GuitarFretNoise,
            BreathNoise,
            Seashore,
            BirdTweet,
            Telephone,
            Helicopter,
            Applause,
            Gunshot
        }

        private const int Chanel = 1;

        private readonly MidiOut _midiOut = new MidiOut(0);
        private readonly List<int> _playingTones = new List<int>();

        public void Dispose()
        {
            _midiOut.Close();
            _midiOut.Dispose();
        }

        public int PlayTone(byte octave, Tones tone, int strength = 127)
        {
            int note = 12 + octave*12 + (int) tone; // 12 полутонов в октаве, начинаем считать с 0-й октавы (есть еще и -1-ая)

            if (!_playingTones.Contains(note))
            {
                _midiOut.Send(MidiMessage.StartNote(note, strength, Chanel).RawData); // воспроизводим ноту на канале 0
                _playingTones.Add(note);
            }

            return note;
        }

        public void StopPlaying(int id)
        {
            if (_playingTones.Contains(id))
            {
                _midiOut.Send(MidiMessage.StopNote(id, 0, Chanel).RawData);
                _playingTones.Remove(id);
            }
        }

        public void StopPlaying(byte octave, Tones tone)
        {
            StopPlaying(12 + octave*12 + (int) tone);
        }

        public void StopAll()
        {
            while (_playingTones.Count > 0)
            {
                StopPlaying(_playingTones.First());
            }

            _playingTones.Clear();
        }

        public void SetInstrument(Instruments instrument)
        {
            _midiOut.Send(MidiMessage.ChangePatch((int) instrument, Chanel).RawData);
        }

        public bool IsNotePlayed(Note note)
        {
            return _playingTones.Contains(note.Id);
        }
    }
}