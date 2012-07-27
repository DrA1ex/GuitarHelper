using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PianoKeyEmulator
{
    class Guitar
    {
        public Guitar( params Note[] tune )
        {
            for( int i = 0; i < 6; ++i )
            {
                strs.Add( new GuitarString( tune[i] ) );
            }
        }

        public List<KeyValuePair<int, int>> GetFretsForNote( Note note )
        {
            List<KeyValuePair<int, int>> result = new List<KeyValuePair<int, int>>(); // 1-е значение номер стпуны (от 1 до 6), 2-е - номер лада ( 0 - открытая струна)

            byte currentString = 0;
            foreach( var str in strs )
            {
                var fret = str.GetFretForNote( note );

                if( fret != -1 ) // Если на этой струне можно сыграть заданную ноту
                {
                    result.Add( new KeyValuePair<int, int>( currentString, fret ) );
                }

                ++currentString;
            }

            return result;
        }

        public Note GetNote( byte str, byte fret )
        {
            return strs[str].GetNoteForFret( fret );
        }

        public void SetTuning( params Note[] tune ) // звучание открытых струн
        {
            for( int i = 0; i < 6; ++i )
            {
                strs[i].SetTune( tune[i] );
            }
        }

        List<GuitarString> strs = new List<GuitarString>();
    }

    class GuitarString
    {
        public GuitarString( Note note )
        {
            this.open = note;
        }

        public void SetTune( Note note )
        {
            this.open = note;
        }

        public Note GetNoteForFret( byte fret )
        {
            return open + fret;
        }

        public int GetFretForNote( Note note )
        {
            int fret = -1; // -1 означает, что нельзя сыграть ноту на этой струне

            if( open <= note )
            {
                int octDiff = note.octave - open.octave;
                int noteDiff = note.tone - open.tone;

                fret = octDiff * 12 + noteDiff;
            }

            return fret;
        }

        Note open;

    }
}
