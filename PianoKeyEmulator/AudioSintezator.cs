using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;

namespace PianoKeyEmulator
{

    internal struct ChordType
    {
        public ChordType( string desc, string name, params int[] intervals )
        {
            this.description = desc;
            this.name = name;
            this.intervals = intervals;
        }

        public List<Note> BuildChord( Note baseNote )
        {
            List<Note> result = new List<Note>();

            result.Add( baseNote );

            Note current = baseNote;

            foreach( var interval in intervals )
            {
                current = current + interval;
                result.Add( current );
            }

            return result;
        }

        public string description;
        public string name;
        public int[] intervals;
    }

    internal struct Note
    {
        public Note( byte oct, Tones t )
        {
            octave = oct;
            tone = t;

            id = 12 + octave * 12 + (int)tone;
        }

        public byte octave;
        public Tones tone;
        public int id;

        public static Note FromString( string str )
        {
            byte octave = byte.Parse( str.Last().ToString() );
            Tones tone = str.Substring( 0, str.Length - 1 ).Replace('#','d').ConvertToEnum<Tones>();

            return new Note( octave, tone );
        }

        public bool isDiez()
        {
            return tone.ToString().Contains( 'd' );
        }

        #region Operators Defenition

        public static int operator -( Note note1, Note note2 )
        {
            return Math.Abs( note1.id - note2.id );
        }

        public static Note operator +( Note note, int semitons )
        {
            byte octave = (byte)(note.octave + semitons / 12); // 12 полутонов в октаве
            int tmp = (int)note.tone + semitons % 12;
            if( tmp > (int)Tones.B ) // Последняя нота в октаве
            {
                ++octave;
                tmp = tmp % 12;
            }
            Tones tone = (Tones)(tmp);

            return new Note( octave, tone );
        }

        public static bool operator <( Note note1, Note note2 )
        {
            return note1.id < note2.id;
        }

        public static bool operator >( Note note1, Note note2 )
        {
            return note1.id > note2.id;
        }

        public static bool operator >=( Note note1, Note note2 )
        {
            return note1.id >= note2.id;
        }

        public static bool operator <=( Note note1, Note note2 )
        {
            return note1.id <= note2.id;
        }

        public static bool operator ==( Note note1, Note note2 )
        {
            return note1.id == note2.id;
        }

        public static bool operator !=( Note note1, Note note2 )
        {
            return note1.id != note2.id;
        }

        public override string ToString()
        {
            return tone.ToString().Replace('d','#') + octave;
        }
        #endregion
    }

    public enum Tones
    {
        A = 9, Ad = 10, B = 11, C = 0, Cd = 1,
        D = 2, Dd = 3, E = 4, F = 5, Fd = 6, G = 7, Gd = 8
    }

    class AudioSintezator : IDisposable
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

        public int PlayTone( byte octave, Tones tone )
        {
            int note = 12 + octave * 12 + (int)tone; // 12 полутонов в октаве, начинаем считать с 0-й октавы (есть еще и -1-ая)

            if( !playingTones.Contains( note ) )
            {
                midiOut.Send( MidiMessage.StartNote( note, 127, 0 ).RawData ); // воспроизводим ноту с макс. силой нажатия на канале 0
                playingTones.Add( note );
            }

            return note;
        }

        public void StopPlaying( int id )
        {
            if( playingTones.Contains( id ) )
            {
                midiOut.Send( MidiMessage.StopNote( id, 0, 0 ).RawData );
                playingTones.Remove( id );
            }
        }

        public void StopPlaying( byte octave, Tones tone )
        {
            StopPlaying( 12 + octave * 12 + (int)tone );
        }

        public void StopAll()
        {
            while( playingTones.Count > 0 )
            {
                StopPlaying( playingTones.First() );
            }

            playingTones.Clear();
        }

        public void SetInstrument( Instruments instrument )
        {
            midiOut.Send( MidiMessage.ChangePatch( (int)instrument, 0 ).RawData );
        }

        MidiOut midiOut = new MidiOut( 0 );
        List<int> playingTones = new List<int>();

        public bool isNotePlayed( Note note )
        {
            return playingTones.Contains( note.id );
        }

        public void Dispose()
        {
            midiOut.Close();
            midiOut.Dispose();
        }
    }
}
