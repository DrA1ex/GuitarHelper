using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections;

namespace PianoKeyEmulator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        AudioSintezator sintezator = new AudioSintezator();
        Guitar guitar = new Guitar(
            new Note(4, Tones.E),
            new Note(3, Tones.B),
            new Note(3, Tones.G),
            new Note(3, Tones.D),
            new Note(2, Tones.A),
            new Note(2, Tones.E)
            );

        Player player = null;

        Tuple<string, Note[]>[] Tunes = new Tuple<string, Note[]>[]
        {
            new Tuple<string, Note[]>("Стандарт: EADGBE", new Note[] { new Note(4, Tones.E), new Note(3, Tones.B), new Note(3, Tones.G),
                new Note(3, Tones.D), new Note(2, Tones.A),new Note(2, Tones.E)}),

            new Tuple<string, Note[]>("Drop D: DADGBE", new Note[] { new Note(4, Tones.E), new Note(3, Tones.B), new Note(3, Tones.G),
                new Note(3, Tones.D), new Note(2, Tones.A),new Note(2, Tones.D)}),

            new Tuple<string, Note[]>("Double Drop D: DADGBD", new Note[] { new Note(4, Tones.D), new Note(3, Tones.B), new Note(3, Tones.G),
                new Note(3, Tones.D), new Note(2, Tones.A),new Note(2, Tones.D)}),

            new Tuple<string, Note[]>("Drop C: CGCFAD", new Note[] { new Note(4, Tones.D), new Note(3, Tones.A), new Note(3, Tones.F),
                new Note(3, Tones.C), new Note(2, Tones.G),new Note(2, Tones.C)}),

            new Tuple<string, Note[]>("Drop B flat: A#FA#D#GC", new Note[] { new Note(4, Tones.C), new Note(3, Tones.G), new Note(3, Tones.Dd),
                new Note(2, Tones.Ad), new Note(2, Tones.F),new Note(1, Tones.Ad)}),

            new Tuple<string, Note[]>("Open G: DGDGBD", new Note[] { new Note(4, Tones.D), new Note(3, Tones.B), new Note(3, Tones.G),
                new Note(3, Tones.D), new Note(2, Tones.G),new Note(2, Tones.D)}),

            new Tuple<string, Note[]>("1/2 step Down: EbG#DbF#BbEb", new Note[] { new Note(4, Tones.Dd), new Note(3, Tones.Ad), new Note(3, Tones.Fd),
                new Note(3, Tones.Cd), new Note(2, Tones.Gd),new Note(2, Tones.Dd)})
            
        };



        SolidColorBrush diezColor = new SolidColorBrush(Color.FromRgb(25, 25, 25));
        SolidColorBrush genericColor = new SolidColorBrush(Color.FromRgb(248, 248, 248));

        SolidColorBrush inactiveFret = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        SolidColorBrush fretStroke = new SolidColorBrush(Color.FromArgb(120, 0, 74, 225));

        const byte inactiveFretAlpha = 120;

        ColorItem[] selectionColors = new ColorItem[]
        {
            new ColorItem(true, Color.FromRgb(0,138,255)),
            new ColorItem(true, Color.FromRgb(255,0,10)),
            new ColorItem(true, Color.FromRgb(255,231,0)),
            new ColorItem(true, Color.FromRgb(78,255,0)),
            new ColorItem(true, Color.FromRgb(186,0,255)),
            new ColorItem(true, Color.FromRgb(255,177,0)),
            new ColorItem(true, Color.FromRgb(154,100,134)) //Этот цвет используется если предыдущие заняты
        };

        const int FretOffset = 5;

        const int KeysOffset = 2;
        const int KeyHeight = 155;
        const int KeyWidth = 30;
        const int DiezHeight = 100;
        const int DiezWiddth = 20;

        const byte StartOctave = 0;
        const byte EndOctave = 8;

        const int FretsCount = 26;

        const string Format = "{0} - {1}{2}"; // <Описание> - <Нота><Модификация>

        List<Note> _toggled = new List<Note>();

        public readonly string[] InstrumentNames = Enum.GetNames(typeof(AudioSintezator.Instruments));

        public MainWindow()
        {
            InitializeComponent();

            player = new Player(this);

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            cInstrument.ItemsSource = InstrumentNames;

            cTunes.ItemsSource = Tunes;

            cChordMods.ItemsSource = Chords.chordMods;

            cChords.ItemsSource = Chords.chordsBases;

            for (double speed = 0.25; speed <= 2.0; speed += 0.25)
            {
                cPlaySpeed.Items.Add(speed);
            }


            #region Генерация клавиш пианино
            TextBlock currentKey;
            int keyPosition = KeysOffset;

            for (byte octave = StartOctave; octave <= EndOctave; ++octave)
            {
                for (var i = 0; i < 12; ++i) // 12 полутонов в октаве
                {
                    currentKey = new TextBlock();

                    var note = new Note(octave, (Tones)i);

                    currentKey.Text = note.ToString();
                    currentKey.Name = GenerateKeyName(note);

                    if (!note.isDiez()) //Если не диез
                    {
                        currentKey.Width = KeyWidth;
                        currentKey.Height = KeyHeight;

                        currentKey.Background = genericColor;

                        currentKey.Margin = new Thickness(keyPosition, 0, 0, 0);
                        currentKey.Padding = new Thickness(0, 130, 0, 0);

                        currentKey.SetValue(Grid.ZIndexProperty, 0);

                        keyPosition += KeyWidth + KeysOffset;

                        currentKey.Foreground = Brushes.Black;

                        currentKey.FontSize = 14;
                    }
                    else
                    {
                        currentKey.Width = DiezWiddth;
                        currentKey.Height = DiezHeight;

                        currentKey.Background = diezColor;

                        currentKey.Margin = new Thickness(keyPosition - DiezWiddth / 2 + KeysOffset / 2,
                            0, 0, 0);
                        currentKey.Padding = new Thickness(0, 75, 0, 0);

                        currentKey.SetValue(Grid.ZIndexProperty, 1);

                        currentKey.Foreground = Brushes.White;

                        currentKey.FontSize = 10;
                    }

                    currentKey.TextAlignment = TextAlignment.Center;

                    currentKey.FontWeight = FontWeights.Bold;
                    currentKey.Focusable = true;


                    currentKey.HorizontalAlignment = HorizontalAlignment.Left;
                    currentKey.VerticalAlignment = VerticalAlignment.Top;

                    currentKey.PreviewMouseLeftButtonDown += PianoKeyDown;
                    currentKey.PreviewMouseLeftButtonUp += PianoKeyUp;
                    currentKey.MouseLeave += PianoKeyLeave;

                    currentKey.PreviewMouseRightButtonUp += KeyToggled;

                    KeysGrid.Children.Add(currentKey);
                }
            }
            #endregion

            #region Генерация ладов для гитары

            Shape currentFret;

            for (byte guitarString = 0; guitarString < 6; ++guitarString)
            {
                for (byte fret = 0; fret < FretsCount; ++fret)
                {
                    if (fret != 0)
                    {
                        currentFret = new Ellipse();
                        currentFret.Margin = new Thickness(FretOffset);
                    }
                    else
                    {
                        currentFret = new Rectangle();
                        currentFret.Margin = new Thickness(0);
                    }

                    currentFret.Name = GenerateFretName(guitarString, fret);

                    currentFret.Stroke = fretStroke;
                    currentFret.Fill = inactiveFret;

                    FretsGrid.Children.Add(currentFret);

                    currentFret.SetValue(Grid.ColumnProperty, (int)fret);
                    currentFret.SetValue(Grid.RowProperty, (int)guitarString);



                    currentFret.PreviewMouseLeftButtonDown += FretMouseDown;
                    currentFret.PreviewMouseLeftButtonUp += FretMouseUp;
                    currentFret.MouseLeave += FretMouseLeave;

                    currentFret.PreviewMouseRightButtonUp += FretToggled;
                }
            }
            #endregion
        }

        public int LineCount { get; private set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cInstrument.SelectedItem = "AcousticPiano";
            cTunes.SelectedIndex = 0;
            //cChordMods.Items[0] = "maj";
            cChordMods.SelectedIndex = -1;
            cChords.SelectedIndex = 0;
            cPlaySpeed.SelectedItem = 1.0;

            StopPlayAll();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StopPlayAll();
            sintezator.Dispose();
        }

        bool pianoKeyWasDown = false;
        private void PianoKeyDown(object sender, MouseButtonEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            pianoKeyWasDown = true;

            var obj = (TextBlock)sender;
            var note = ParseKeyName(obj.Name);

            sintezator.PlayTone(note.Octave, note.Tone);

            if (!_toggled.Contains(note))
                HighlightNote(note);
        }

        private void PianoKeyUp(object sender, MouseButtonEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            pianoKeyWasDown = false;

            var obj = (TextBlock)sender;
            var note = ParseKeyName(obj.Name);

            sintezator.StopPlaying(note.Octave, note.Tone);

            if (!_toggled.Contains(note))
                DehightlightNote(note);
        }

        private void PianoKeyLeave(object sender, MouseEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            if (pianoKeyWasDown)
                PianoKeyUp(sender, null);
        }

        private void cInstrument_SelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            var instrumentSelector = sender as AutoCompleteBox;
            if (instrumentSelector != null && instrumentSelector.SelectedItem != null)
            {
                sintezator.StopAll();
                sintezator.SetInstrument(
                    ((string)(instrumentSelector).SelectedItem).ConvertToEnum<AudioSintezator.Instruments>());
            }
        }

        bool FretWasDown = false;
        private void FretMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            FretWasDown = true;

            var obj = (Shape)sender;
            var note = ParseFretName(obj.Name);
            sintezator.PlayTone(note.Octave, note.Tone);

            if (!_toggled.Contains(note))
            {
                HighlightNote(note, obj);
            }
        }

        private void FretMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            FretWasDown = false;

            var obj = (Shape)sender;
            var note = ParseFretName(obj.Name);

            sintezator.StopPlaying(note.Id);

            if (!_toggled.Contains(note))
            {
                DehightlightNote(note);
            }
        }

        private void FretMouseLeave(object sender, MouseEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            if (FretWasDown)
                FretMouseUp(sender, null);
        }

        private void cTunes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StopPlayAll();
            guitar.SetTuning(Tunes[cTunes.SelectedIndex].Item2);
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                StopPlayAll();
            }
            else if (e.Key == Key.F1)
            {
                AddToggledToSong();
            }
        }

        private void FretToggled(object sender, MouseButtonEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            var obj = (Shape)sender;

            Note note = ParseFretName(obj.Name);

            ToggleNote(note, obj);
        }

        private void KeyToggled(object sender, MouseButtonEventArgs e)
        {
            if (tSong.IsReadOnly)
                return;

            var obj = (TextBlock)sender;

            ToggleNote(ParseKeyName(obj.Name));
        }

        private void ChordChanged(object sender, SelectionChangedEventArgs e)
        {
            ToggleSelectedChord();
        }

        private void bChord_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PlayToggled();
        }

        private void bChord_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            sintezator.StopAll();
        }

        private void bReset_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            cChordMods.SelectedIndex = -1;
            StopPlayAll();
        }

        private void bPlay_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var strs = tSong.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            tSong.IsReadOnly = true;

            player.SetStartPos(tSong.GetLineIndexFromCharacterIndex(tSong.CaretIndex));
            player.playMusic(strs);
        }

        private void bStop_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            player.Stop();
            tSong.IsReadOnly = false;
        }

        private void cPlaySpeed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            player.SetPlaySpeed((double)cPlaySpeed.SelectedItem);
        }

        #region Функциональная часть
        internal void ToggleNote(Note note, Shape major = null)
        {
            if (_toggled.Contains(note))
            {
                DehightlightNote(note);
                _toggled.Remove(note);
            }
            else
            {
                HighlightNote(note, major);
                _toggled.Add(note);
            }

            _toggled = _toggled.OrderBy(x => x.Id).ToList();
            UpdateChord();
        }

        public void PlayToggled()
        {
            sintezator.StopAll();

            foreach (var i in _toggled)
            {
                sintezator.PlayTone(i.Octave, i.Tone);
            }
        }

        public void StopPlayAll()
        {
            sintezator.StopAll();
            foreach (var note in _toggled)
            {
                DehightlightNote(note);
            }

            _toggled.Clear();

            UpdateChord();
        }

        private int _assignedColors = 0;

        private Color GetColor()
        {
            ++_assignedColors;
            for (int i = 0; i < selectionColors.Length - 1; ++i)
            {
                if (selectionColors[i].free)
                {
                    selectionColors[i].free = false;
                    return selectionColors[i].color;
                }
            }
            return selectionColors.Last().color;
        }

        private void FreeColor(Color color)
        {
            --_assignedColors;
            if (_assignedColors < 0)
                throw new Exception("Попытка освободить больше цветов, чем было выделено");

            if (color == selectionColors.Last().color)
            {
                return;
            }

            bool isSuccess = false;
            for (int i = 0; i < selectionColors.Length; ++i)
            {
                if (selectionColors[i].color == color)
                {
                    if (selectionColors[i].free == false)
                    {
                        selectionColors[i].free = true;
                    }
                    else
                    {
                        throw new Exception("Цвет уже освобожден");
                    }
                    isSuccess = true;
                }
            }

            if (!isSuccess)
            {
                throw new Exception("Попытка освободить несуществующий цвет");
            }
        }

        private void HighlightNote(Note note, Shape major = null) // major - объект лада, который необходимо подсветить как основной
        {
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            Color newColor = (note.Octave >= StartOctave && note.Octave <= EndOctave) ? GetColor() : Colors.Black;
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            var baseColor = new SolidColorBrush(newColor);
            Color tmp = newColor;
            tmp.A = inactiveFretAlpha;
            var highlighted = new SolidColorBrush(tmp);

            var lst = guitar.GetFretsForNote(note);
            foreach (var objName in lst)
            {
                object o = LogicalTreeHelper.FindLogicalNode(FretsGrid,
                    GenerateFretName(objName.Item1, objName.Item2));
                if (o != null)
                {
                    ((Shape)o).Fill = highlighted;
                }
            }

            if (major != null)
            {
                major.Fill = baseColor;
            }

            string str = GenerateKeyName(note);
            tChordName.Text = note.ToString().Replace('#', '♯');

            var bNote = (TextBlock)LogicalTreeHelper.FindLogicalNode(KeysGrid, str); // имя клавиши имеет след. вид: "Тон[d]Октава"
            if (bNote != null)
            {
                bNote.Background = baseColor;
                bNote.Focus();
            }
        }

        private void DehightlightNote(Note note)
        {
            var lst = guitar.GetFretsForNote(note);
            foreach (var objName in lst)
            {
                object o = LogicalTreeHelper.FindLogicalNode(FretsGrid,
                    GenerateFretName(objName.Item1, objName.Item2));
                if (o != null)
                {
                    var shape = o as Shape;

                    var brushCopy = shape.Fill.Clone();
                    shape.Fill = brushCopy;

                    var fretFadeAnimation = new ColorAnimation(((SolidColorBrush)shape.Fill).Color, inactiveFret.Color, new Duration(TimeSpan.FromSeconds(2)));
                    var easing = new QuinticEase();
                    easing.EasingMode = EasingMode.EaseOut;

                    fretFadeAnimation.EasingFunction = easing;
                    var fretStoryboard = new Storyboard();
                    fretStoryboard.Children.Add(fretFadeAnimation);

                    var elementName = shape.Name;

                    if (FindName(elementName) == null)
                        RegisterName(elementName, shape);
                    Storyboard.SetTargetName(fretFadeAnimation, elementName);
                    Storyboard.SetTargetProperty(fretFadeAnimation, new PropertyPath("Fill.Color"));

                    fretStoryboard.Begin(this);
                }
            }

            tChordName.Text = "";

            string str = GenerateKeyName(note);

            var bNote = (TextBlock)LogicalTreeHelper.FindLogicalNode(KeysGrid, str);
            if (bNote != null)
            {
                FreeColor(((SolidColorBrush)bNote.Background).Color);

                var targetColor = str.Length == 2 ? genericColor : diezColor;

                var brushCopy = bNote.Background.Clone();
                bNote.Background = brushCopy;

                var noteFadeAnimation = new ColorAnimation(((SolidColorBrush)bNote.Background).Color, targetColor.Color, new Duration(TimeSpan.FromSeconds(3)));
                var easing = new QuinticEase();
                easing.EasingMode = EasingMode.EaseOut;

                noteFadeAnimation.EasingFunction = easing;
                var noteStoryboadrd = new Storyboard();
                noteStoryboadrd.Children.Add(noteFadeAnimation);

                if (FindName(bNote.Name) == null)
                    RegisterName(bNote.Name, bNote);
                Storyboard.SetTargetName(noteFadeAnimation, bNote.Name);
                Storyboard.SetTargetProperty(noteFadeAnimation, new PropertyPath("Background.Color"));

                noteStoryboadrd.Begin(this);
            }
        }

        internal static Note ParseKeyName(string data)
        {
            return Note.FromString(data.Replace('#', 'd'));
        }
        private string GenerateKeyName(Note note)
        {
            return note.ToString().Replace('#', 'd');
        }

        internal Note ParseFretName(string str)
        {
            var data = str.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries); // лад имеет след. имя: _Струна_НомерЛада

            var note = guitar.GetNote(byte.Parse(data[0]), byte.Parse(data[1]));
            return note;
        }
        private string GenerateFretName(byte stringNumber, byte fret)
        {
            return "_" + stringNumber + "_" + fret;
        }

        internal void SetCurrentLine(int line)
        {
            try
            {
                int index = tSong.GetCharacterIndexFromLineIndex(line);
                tSong.Select(index,
                    tSong.GetCharacterIndexFromLineIndex(line + 1) - index - 1);
                tSong.Focus();
            }
            catch //Передана несуществующая строка, значи воспроизведение остановлено
            {
                tSong.Select(0, 0);
                tSong.IsReadOnly = false;
            }
        }

        private void UpdateChord()
        {
            if (_toggled.Count > 0)
            {
                ChordType type;
                Note baseNote;

                try
                {
                    Chords.GetChord(_toggled, out baseNote, out type);

                    tChordName.Text = string.Format(Format,
                        type.description,
                        baseNote.Tone.ToString().Replace('d', '♯'),
                        type.name);
                }
                catch (Exception e)
                {
                    tChordName.Text = e.Message;
                }
            }
            else
            {
                tChordName.Text = "—";
            }

        }

        private void AddToggledToSong()
        {
            if (_toggled.Count > 0 && !tSong.IsReadOnly)
            {
                StringBuilder str = new StringBuilder("\n");
                foreach (var note in _toggled)
                {
                    str.Append(note);
                    str.Append(",");
                }
                if (str.Length > 0)
                {
                    str.Remove(str.Length - 1, 1); // Удаляем последнюю запятую
                    str.Append("\n");

                    tSong.SelectedText = str.ToString();
                    tSong.SelectionLength = 0;

                    int lineIndex = tSong.GetLineIndexFromCharacterIndex(tSong.CaretIndex);
                    int index = tSong.GetCharacterIndexFromLineIndex(lineIndex + 2);
                    tSong.Select(index, 0);
                    tSong.Focus();
                }


            }
        }

        void ToggleSelectedChord()
        {
            if (cChords.SelectedIndex >= 0 && cChordMods.SelectedIndex >= 0)
            {
                StopPlayAll();

                string chord = cChords.SelectedValue as string;
                int mod = cChordMods.SelectedIndex;
                chord = chord.Replace("#", "d");

                Note baseNote = new Note(3, chord.ConvertToEnum<Tones>());

                var lst = Chords.chordTypes[mod].BuildChord(baseNote);

                foreach (var chordNote in lst)
                {
                    ToggleNote(chordNote);
                }
            }
        }
        #endregion

        private void tSong_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !tSong.IsReadOnly)
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void tSong_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string[] data = (string[])e.Data.GetData("FileDrop", false);

                if (data.Length == 1)
                {
                    string fileName = data[0];
                    if (fileName.EndsWith(".mid"))
                    {
                        tSong.Text = Utils.ParseMIDI(fileName);
                        SetCurrentLine(0);
                    }
                    else if (fileName.EndsWith(".txt"))
                    {
                        tSong.Text = File.ReadAllText(fileName);
                        SetCurrentLine(0);
                    }
                }
            }
            catch (Exception) { }
        }

        private void SongTextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = sender as TextBox;
            if (textbox != null)
            {
                textbox.UpdateLayout();
                textbox.InvalidateMeasure();

                LineCount = textbox.LineCount;
                if (LineCount == -1)
                {
                    LineCount = textbox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Count();
                }
                OnPropertyChanged("LineCount");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void cInstrument_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            cInstrument.IsDropDownOpen = true;
        }
    }
}
