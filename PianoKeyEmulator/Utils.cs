using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using NAudio.Midi;

namespace PianoKeyEmulator
{
    internal static class Utils
    {
        public static T ConvertToEnum<T>(this string enumString)
        {
            try
            {
                return (T) Enum.Parse(typeof (T), enumString, true);
            }
            catch (Exception ex)
            {
                // Create an instance of T ... we're doing this to that we can peform a GetType() on it to retrieve the name
                //
                T temp = default(T);
                String s = String.Format("'{0}' is not a valid enumeration of '{1}'", enumString, temp.GetType().Name);
                throw new Exception(s, ex);
            }
        }

        public static string ParseMIDI(string fileName)
        {
            var file = new MidiFile(fileName);
            var result = new StringBuilder();

            if (file.Tracks > 0) //Нас не интересуют пустые midi файлы
            {
                IList<MidiEvent> trackEvents = file.Events[0]; // Берем данные только из 1-го трека

                bool firstNote = true;
                long startTime = 0, newTime = 0;

                foreach (MidiEvent midiEvent in trackEvents)
                {
                    switch (midiEvent.CommandCode)
                    {
                        case MidiCommandCode.NoteOn:
                            var e = midiEvent as NoteOnEvent;
                            if (!firstNote)
                            {
                                result.Append(',');
                            }
                            else
                            {
                                startTime = e.AbsoluteTime;

                                long delta = startTime - newTime;
                                if (delta > 0 && newTime > 0) //Если есть пауза, и она не в начале трека
                                {
                                    result.AppendLine("pause");
                                    result.AppendLine(delta.ToString());
                                }
                            }

                            result.Append(Note.FromID(e.NoteNumber));
                            firstNote = false;

                            break;
                        case MidiCommandCode.NoteOff:
                            firstNote = true;
                            newTime = midiEvent.AbsoluteTime;

                            // Если проигрывается несколько нот, то мы получим это сообщение неск. раз
                            // И соответственно получим паузу в 0 мс. А нам такие не нужны
                            if (newTime - startTime > 0)
                            {
                                result.AppendLine(); //Нужен перенос, т.к. ноты вставляются без \n
                                result.AppendLine((newTime - startTime).ToString());
                            }
                            startTime = newTime;
                            break;

                        case MidiCommandCode.MetaEvent:

                            break;
                    }
                }
            }

            return result.ToString();
        }

        public static bool CompareArrays(int[] arr0, int[] arr1)
        {
            if (arr0.Length != arr1.Length)
            {
                return false;
            }
            for (int i = 0; i < arr0.Length; i++)
            {
                if (arr0[i] != arr1[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static int CountOfTones(Tones tone, List<Note> notes)
        {
            int count = 0;

            foreach (Note current in notes)
            {
                if (current.Tone == tone)
                {
                    ++count;
                }
            }

            return count;
        }

        public static void Swap<T>(List<T> lst, int x, int y)
        {
            T tmp = lst[x];
            lst[x] = lst[y];
            lst[y] = tmp;
        }

        public static IEnumerable<List<T>> GeneratePermutation<T>(List<T> list, int k = 0)
        {
            int i;
            if (k == list.Count)
            {
                yield return list;
            }
            else
            {
                for (i = k; i < list.Count; i++)
                {
                    Swap(list, k, i);
                    foreach (var result in GeneratePermutation(list, k + 1))
                    {
                        yield return result;
                    }
                    Swap(list, k, i);
                }
            }
        }
    }

    internal struct ColorItem
    {
        public Color color;
        public bool free;

        public ColorItem(bool free, Color color)
        {
            this.free = free;
            this.color = color;
        }
    }
}