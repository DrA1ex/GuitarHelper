using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace PianoKeyEmulator
{
    class Player
    {
        int currentLine = -1;
        string[] cmds = null;
        MainWindow parent = null;
        double playSpeed = 1;

        delegate int RunCmdDelegate( string cmd );
        delegate void ToggleNoteDelegate( Note note, System.Windows.Shapes.Shape major );
        delegate void SetCurrentLineDelegate( int line );

        public Player( MainWindow parent )
        {
            if( parent == null )
                throw new Exception( "Родитель не может быть null" );
            this.parent = parent;
        }


        public void playMusic( string[] x )
        {
            if( cmds == null )
            {
                if( currentLine >= x.Length || currentLine < 0 )
                    currentLine = 0;
                cmds = x;

                ThreadPool.QueueUserWorkItem( RunCmds );
            }
        }

        public void Stop()
        {
            if( cmds != null )
            {
                if( currentLine >= cmds.Length )
                {
                    currentLine = 0;
                }
                parent.SetCurrentLine( currentLine );

                cmds = null;
                parent.StopPlayAll();
            }

        }

        public void SetStartPos( int pos )
        {
            if( cmds == null ) // Если не проигрывается
                currentLine = pos;
        }
        public void SetPlaySpeed( double speed )
        {
            playSpeed = speed;
        }

        private void RunCmds( object state )
        {
            while( cmds != null && currentLine >= 0 && currentLine < cmds.Length )
            {
                int sleepTime = (int)((int)parent.Dispatcher.Invoke(
                    new RunCmdDelegate( RunCmd ), new object[] { cmds[currentLine] } 
                    ) / playSpeed);
                Thread.Sleep( sleepTime );
                parent.Dispatcher.Invoke( (SetCurrentLineDelegate)parent.SetCurrentLine, new object[] { ++currentLine } );
            }

            cmds = null;
            currentLine = -1;
            parent.Dispatcher.Invoke( (Action)parent.StopPlayAll, null );

        }
        private int RunCmd( string cmd )
        {
            int newInterval = 0;
            if( cmd.Length > 0 ) // Если не пустая строка
            {
                if( !int.TryParse( cmd, out newInterval ) ) // Если не число (если не интервал)
                {
                    if( cmd.StartsWith( "@" ) ) // Если аккорд
                    {
                        var lst = cmd.Substring( 1 ).Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
                        if( lst.Length > 0 )
                        {
                            try
                            {
                                byte octave = 3;
                                Tones tone;

                                // 0-й элемент содержит базовую ноту аккорда (Например Dd4 - D# 4-й октавы)
                                if( lst[0].Length == 3 ) // Если октава указана
                                {
                                    octave = byte.Parse( lst[0].Last().ToString() ); // Октавой является последний символ в строке
                                    tone = lst[0].Substring( 0, lst[0].Length - 1 ).ConvertToEnum<Tones>(); // Тоном являются строка без последнего символа
                                }
                                else
                                {
                                    tone = lst[0].ConvertToEnum<Tones>(); // Октава не указана. Содержимое тональность
                                }

                                Note baseNote = new Note( octave, tone );

                                string chordName = "";
                                if( lst.Length >= 2 ) // Если через пробел еще что-то было, значит аккорд не мажорный (у мажорного нет никаких суфиксов)
                                {
                                    chordName = lst[1]; // 1-й элемент должен содержать тип аккорда (например sus2)
                                }

                                int type = 0;
                                foreach( var chord in Chords.chordTypes )
                                {
                                    if( chord.name == chordName )
                                        break;
                                    ++type;
                                }

                                if( type < Chords.chordTypes.Length )
                                {
                                    parent.Dispatcher.Invoke( (Action)parent.StopPlayAll, null );
                                    var chord = Chords.chordTypes[type].BuildChord( baseNote );

                                    foreach( var chordNote in chord )
                                    {
                                        parent.Dispatcher.Invoke( (ToggleNoteDelegate)parent.ToggleNote,
                                            new object[] { chordNote } );
                                    }
                                    parent.Dispatcher.Invoke( (Action)parent.PlayToggled, null );
                                }
                            }
                            catch( Exception ) { }
                        }
                    }
                    else // Если набор нот
                    {
                        var lst = cmd.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );

                        if( lst.Length > 0 )
                        {
                            parent.Dispatcher.Invoke( (Action)parent.StopPlayAll, null );
                            foreach( var note in lst )
                            {
                                try
                                {
                                    Note tmp = MainWindow.ParseKeyName( note );
                                    parent.Dispatcher.Invoke( (ToggleNoteDelegate)parent.ToggleNote,
                                            new object[] { tmp, null } );
                                }
                                catch( Exception ) { }
                            }
                            parent.Dispatcher.Invoke( (Action)parent.PlayToggled, null );
                        }
                    }
                }
            }

            return newInterval;
        }
    }
}
