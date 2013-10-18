using System;
using System.Collections.Generic;
using System.Linq;

namespace PianoKeyEmulator
{
    internal static class Chords
    {
        public static ChordType[] chordTypes =
        {
            new ChordType("мажорное трезвучие", "", 4, 3),
            new ChordType("минорное трезвучие", "m", 3, 4),
            new ChordType("увеличенное трезвучие", "5+", 4, 4),
            new ChordType("уменьшенное трезвучие", "m-5", 3, 3),
            new ChordType("большой мажорный септаккорд", "maj7", 4, 3, 4),
            new ChordType("большой минорный септаккорд", "m+7", 3, 4, 4),
            new ChordType("доминантсептаккорд", "7", 4, 3, 3),
            new ChordType("малый минорный септаккорд", "m7", 3, 4, 3),
            new ChordType("полуувеличенный септаккорд", "maj5+", 4, 4, 3),
            new ChordType("полууменьшенный септаккорд", "m7-5", 3, 3, 4),
            new ChordType("уменьшенный септаккорд", "dim", 3, 3, 3),
            new ChordType("трезвучие с задержанием (IV)", "sus2", 2, 5),
            new ChordType("трезвучие с задержанием (II)", "sus4", 5, 2),
            new ChordType("секстмажор", "6", 4, 3, 2),
            new ChordType("секстминор", "m6", 3, 4, 2),
            new ChordType("большой нонмажор", "9", 4, 3, 3, 4),
            new ChordType("большой нонминор", "m9", 3, 4, 3, 4),
            new ChordType("малый нонмажор", "-9", 4, 3, 3, 3),
            new ChordType("малый нонминор", "m-9", 3, 4, 3, 3),
            new ChordType("Минорный лад", " - Минор", 2, 1, 2, 2, 1, 2),
            new ChordType("Мажорный лад", " - Мажор", 2, 2, 1, 2, 2, 2),
            new ChordType("нота", ""),
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

        public static string[] chordsBases =
        {
            "A", "A#", "B", "C", "C#", "D", "D#", "E",
            "F", "F#", "G", "G#"
        };

        public static string[] chordMods =
        {
            "", "m", "5+", "m-5", "maj7", "m+7", "7",
            "m7", "maj5+", "m7-5", "dim", "sus2", "sus4",
            "6", "m6", "9", "m9", "-9", "m-9", "минор", "мажор"
        };

        private static int GetChordType(List<Note> tmp)
        {
            var intervals = new int[tmp.Count - 1];
            for (int i = 0; i < tmp.Count - 1; ++i)
            {
                intervals[i] = tmp[i] - tmp[i + 1];
            }

            int type = 0;
            foreach (ChordType chordType in chordTypes)
            {
                if (Utils.CompareArrays(intervals, chordType.intervals))
                {
                    break;
                }

                ++type;
            }
            return type;
        }

        public static void GetChord(List<Note> chordNotes, out Note BaseNote, out ChordType type)
        {
            List<Note> notes = PrepareNotes(chordNotes);
            int typeIndex = GetChordType(notes);

            if (typeIndex < chordTypes.Length)
            {
                BaseNote = notes[0];
                type = chordTypes[typeIndex];
            }
            else if (chordNotes.Count <= 7)
            {
                bool unknown = true;
                var possibleChord = new List<Note>(notes);

                foreach (var perm in Utils.GeneratePermutation(possibleChord))
                {
                    for (int k = 1; k < perm.Count; ++k) // Убираем промежутки между нотами ( > 12 полутонов )
                    {
                        if (perm[k].Tone > perm[k - 1].Tone)
                        {
                            perm[k] = new Note(perm[k - 1].Octave, perm[k].Tone);
                        }
                        else
                        {
                            perm[k] = new Note((byte) (perm[k - 1].Octave + 1), perm[k].Tone);
                        }
                    }

                    typeIndex = GetChordType(possibleChord);

                    if (typeIndex < chordTypes.Length)
                    {
                        unknown = false;
                        break; // Мы нашли что нужно, выходим
                    }
                }

                if (!unknown)
                {
                    BaseNote = possibleChord[0];
                    type = chordTypes[typeIndex];
                }
                else
                {
                    throw new Exception("неизвестный аккорд");
                }
            }
            else
            {
                throw new Exception("неизвестный аккорд");
            }
        }

        private static List<Note> PrepareNotes(List<Note> notes)
        {
            var tmp = new List<Note>();

            bool finded = false;
            for (int i = 0; i < notes.Count; ++i)
            {
                finded = false;
                Note note = notes[i];
                for (int j = 0; j < tmp.Count; ++j)
                {
                    if (note.Tone == tmp[j].Tone)
                    {
                        finded = true;
                        break;
                    }
                }

                if (!finded)
                {
                    tmp.Add(note);
                }
            }

            if (tmp.Count == 1 && notes.Count > 1)
            {
                return notes;
            }

            byte lowest = tmp[0].Octave;
            Tones lowesTone = tmp[0].Tone;
            for (int i = 0; i < tmp.Count; ++i)
            {
                if (tmp[i].Octave > lowest)
                {
                    if (Utils.CountOfTones(tmp[i].Tone, notes) > 1)
                    {
                        if (tmp[i].Tone > lowesTone)
                        {
                            tmp[i] = new Note(lowest, tmp[i].Tone);
                        }
                        else
                        {
                            tmp[i] = new Note((byte) (lowest + 1), tmp[i].Tone);
                        }
                    }
                }
            }

            tmp = tmp.OrderBy(x => x.Id).ToList();
            return tmp;
        }
    }
}