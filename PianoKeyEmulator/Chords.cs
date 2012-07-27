using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PianoKeyEmulator
{
    static class Chords
    {
        public static ChordType[] chordTypes = new ChordType[]{
            new ChordType("мажорное трезвучие", "", 4,3),
            new ChordType("минорное трезвучие", "m", 3,4),
            new ChordType("увеличенное трезвучие", "5+", 4,4),
            new ChordType("уменьшенное трезвучие", "m-5", 3,3),
            new ChordType("большой мажорный септаккорд", "maj7", 4,3,4),
            new ChordType("большой минорный септаккорд", "m+7", 3,4,4),
            new ChordType("доминантсептаккорд", "7", 4,3,3),
            new ChordType("малый минорный септаккорд", "m7", 3,4,3),
            new ChordType("полуувеличенный септаккорд", "maj5+", 4,4,3),
            new ChordType("полууменьшенный септаккорд", "m7-5", 3,3,4),
            new ChordType("уменьшенный септаккорд", "dim", 3,3,3),
            new ChordType("трезвучие с задержанием (IV)", "sus2", 2,5),
            new ChordType("трезвучие с задержанием (II)", "sus4", 5,2),
            new ChordType("секстмажор","6", 4,3,2),
            new ChordType("секстминор", "m6", 3,4,2),
            new ChordType("большой нонмажор", "9", 4,3,3,4),
            new ChordType("большой нонминор", "m9", 3,4,3,4),
            new ChordType("малый нонмажор", "-9", 4,3,3,3),
            new ChordType("малый нонминор", "m-9", 3,4,3,3),
            new ChordType("нота",""),
            new ChordType("малая секунда", " - М2", 1),
            new ChordType("большая секунда", " - Б2", 2),
            new ChordType("малая терция", " - М3", 3),
            new ChordType("большая терция", " - Б3", 4),
            new ChordType("чистая кварта", " - Ч4", 5),
            new ChordType("увеличенная кварта", " - УВ4", 6),
            new ChordType("чистая квинта", " - Ч5", 7),
            new ChordType("малая секста", " - М6", 8),
            new ChordType("большая секста", " - Б6", 9),
            new ChordType("малая септима", " - М7", 10),
            new ChordType("большая септима", " - Б7", 11),
            new ChordType("октава", " - О", 12),
            new ChordType("малая нона", " - М9", 13),
            new ChordType("большая нона", " - Б9", 14) 
    };

        public static string[] chordsBases = new string[] {
            "A","A#","B","C","C#","D","D#","E",
            "F","F#","G","G#"
        };

        public static string[] chordMods = new string[] {
            "","m","5+","m-5","maj7","m+7","7",
            "m7","maj5+","m7-5","dim","sus2","sus4",
            "6","m6","9","m9","-9","m-9"
        };

        private static int GetChordType( List<Note> tmp )
        {
            int[] intervals = new int[tmp.Count - 1];
            for( int i = 0; i < tmp.Count - 1; ++i )
            {
                intervals[i] = tmp[i] - tmp[i + 1];
            }

            int type = 0;
            foreach( var chordType in Chords.chordTypes )
            {
                if( Utils.CompareArrays( intervals, chordType.intervals ) )
                    break;

                ++type;
            }
            return type;
        }

        public static void GetChord( List<Note> chordNotes, out Note BaseNote, out ChordType type )
        {
            List<Note> notes = PrepareNotes( chordNotes );
            int typeIndex = GetChordType( notes );

            if( typeIndex < chordTypes.Length )
            {
                BaseNote = notes[0];
                type = chordTypes[typeIndex];
            }
            else
            {
                bool unknown = true;
                var possibleChord = new List<Note>( notes );

                foreach( List<Note> perm in Utils.GeneratePermutation( possibleChord ) )
                {
                    for( int k = 1; k < perm.Count; ++k ) // Убираем промежутки между нотами ( > 12 полутонов )
                    {
                        if( perm[k].tone > perm[k - 1].tone )
                        {
                            perm[k] = new Note( perm[k - 1].octave, perm[k].tone );
                        }
                        else
                        {
                            perm[k] = new Note( (byte)(perm[k - 1].octave + 1), perm[k].tone );
                        }
                    }

                    typeIndex = GetChordType( possibleChord );

                    if( typeIndex < Chords.chordTypes.Length )
                    {
                        unknown = false;
                        break; // Мы нашли что нужно, выходим
                    }
                }

                if( unknown )
                {
                    throw new Exception( "неизвестный аккорд" );
                }
                else
                {
                    BaseNote = possibleChord[0];
                    type = chordTypes[typeIndex];
                }
            }
        }

        private static List<Note> PrepareNotes( List<Note> notes )
        {
            List<Note> tmp = new List<Note>();

            bool finded = false;
            for( int i = 0; i < notes.Count; ++i )
            {
                finded = false;
                var note = notes[i];
                for( int j = 0; j < tmp.Count; ++j )
                {
                    if( note.tone == tmp[j].tone )
                    {
                        finded = true;
                        break;
                    }
                }

                if( !finded )
                {
                    tmp.Add( note );
                }
            }

            if( tmp.Count == 1 && notes.Count > 1 )
                return notes;

            byte lowest = tmp[0].octave;
            var lowesTone = tmp[0].tone;
            for( int i = 0; i < tmp.Count; ++i )
            {
                if( tmp[i].octave > lowest )
                {
                    if( Utils.CountOfTones( tmp[i].tone, notes ) > 1 )
                    {
                        if( tmp[i].tone > lowesTone )
                        {
                            tmp[i] = new Note( lowest, tmp[i].tone );
                        }
                        else
                        {
                            tmp[i] = new Note( (byte)(lowest + 1), tmp[i].tone );
                        }
                    }
                }
            }

            tmp = tmp.OrderBy( x => x.id ).ToList();
            return tmp;
        }
    }
}
