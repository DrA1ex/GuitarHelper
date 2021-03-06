﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Shapes;

namespace PianoKeyEmulator
{
    internal class Player
    {
        private readonly MainWindow parent;
        private string[] cmds;
        private int currentLine = -1;
        private double playSpeed = 1;

        public Player(MainWindow parent)
        {
            if (parent == null)
            {
                throw new Exception("Родитель не может быть null");
            }
            this.parent = parent;
        }

        public void playMusic(string[] x)
        {
            if (cmds == null)
            {
                if (currentLine >= x.Length || currentLine < 0)
                {
                    currentLine = 0;
                }
                cmds = x;

                ThreadPool.QueueUserWorkItem(RunCmds);
            }
        }

        public void Stop()
        {
            if (cmds != null)
            {
                if (currentLine >= cmds.Length)
                {
                    currentLine = 0;
                }
                parent.SetCurrentLine(currentLine);

                cmds = null;
                parent.StopPlayAll();
            }
        }

        public void SetStartPos(int pos)
        {
            if (cmds == null) // Если не проигрывается
            {
                currentLine = pos;
            }
        }

        public void SetPlaySpeed(double speed)
        {
            playSpeed = speed;
        }

        private void RunCmds(object state)
        {
            while (cmds != null && currentLine >= 0 && currentLine < cmds.Length)
            {
                var sleepTime = (int) ((int) parent.Dispatcher.Invoke(
                    new RunCmdDelegate(RunCmd), new object[] {cmds[currentLine]}
                    )/playSpeed);
                Thread.Sleep(sleepTime);
                parent.Dispatcher.Invoke((SetCurrentLineDelegate) parent.SetCurrentLine, new object[] {++currentLine});
            }

            cmds = null;
            currentLine = -1;
            parent.Dispatcher.Invoke((Action) parent.StopPlayAll, null);
        }

        private int RunCmd(string cmd)
        {
            int newInterval = 0;
            if (cmd.Length > 0) // Если не пустая строка
            {
                if (!int.TryParse(cmd, out newInterval)) // Если не число (если не интервал)
                {
                    if (cmd.StartsWith("@")) // Если аккорд
                    {
                        string[] lst = cmd.Substring(1).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        if (lst.Length > 0)
                        {
                            try
                            {
                                Note baseNote;

                                // 0-й элемент содержит базовую ноту аккорда (Например D#4 - D# 4-й октавы)
                                try
                                {
                                    baseNote = Note.FromString(lst[0]);
                                }
                                catch
                                {
                                    baseNote = new Note(3, lst[0].Replace("#", "d").ConvertToEnum<Tones>());
                                    //Возможно не указана октава
                                }


                                string chordName = "";
                                if (lst.Length >= 2)
                                    // Если через пробел еще что-то было, значит аккорд не мажорный (у мажорного нет никаких суфиксов)
                                {
                                    chordName = lst[1]; // 1-й элемент должен содержать тип аккорда (например sus2)
                                }

                                int type = 0;
                                foreach (ChordType chord in Chords.chordTypes)
                                {
                                    if (chord.name == chordName)
                                    {
                                        break;
                                    }
                                    ++type;
                                }

                                if (type < Chords.chordTypes.Length)
                                {
                                    parent.Dispatcher.Invoke((Action) parent.StopPlayAll, null);
                                    List<Note> chord = Chords.chordTypes[type].BuildChord(baseNote);

                                    foreach (Note chordNote in chord)
                                    {
                                        parent.Dispatcher.Invoke((ToggleNoteDelegate) parent.ToggleNote,
                                            new object[] {chordNote, null});
                                    }
                                    parent.Dispatcher.Invoke((Action) parent.PlayToggled, null);
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine(e.Message);
                            }
                        }
                    }
                    else // Если набор нот
                    {
                        string[] lst = cmd.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

                        if (lst.Length > 0)
                        {
                            parent.Dispatcher.Invoke((Action) parent.StopPlayAll, null);
                            foreach (string note in lst.Distinct())
                            {
                                try
                                {
                                    Note tmp = Note.FromString(note);
                                    parent.Dispatcher.Invoke((ToggleNoteDelegate) parent.ToggleNote,
                                        new object[] {tmp, null});
                                }
                                catch (Exception)
                                {
                                }
                            }
                            parent.Dispatcher.Invoke((Action) parent.PlayToggled, null);
                        }
                    }
                }
            }

            return newInterval;
        }

        private delegate int RunCmdDelegate(string cmd);

        private delegate void SetCurrentLineDelegate(int line);

        private delegate void ToggleNoteDelegate(Note note, Shape major);
    }
}