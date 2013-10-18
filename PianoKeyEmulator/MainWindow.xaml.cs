using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using NAudio.Midi;

namespace PianoKeyEmulator
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private const byte InactiveFretAlpha = 120;

        private const int FretOffset = 5;

        private const int KeysOffset = 2;
        private const int KeyHeight = 155;
        private const int KeyWidth = 30;
        private const int DiezHeight = 100;
        private const int DiezWiddth = 20;

        private const byte StartOctave = 0;
        private const byte EndOctave = 8;

        private const int FretsCount = 26;

        private const string Format = "{0} - {1}{2}"; // <Описание> - <Нота><Модификация>

        public readonly string[] InstrumentNames = Enum.GetNames(typeof (AudioSintezator.Instruments));


        private readonly SolidColorBrush _diezColor = new SolidColorBrush(Color.FromRgb(25, 25, 25));
        private readonly SolidColorBrush _fretStroke = new SolidColorBrush(Color.FromArgb(120, 0, 74, 225));
        private readonly SolidColorBrush _genericColor = new SolidColorBrush(Color.FromRgb(248, 248, 248));

        private readonly Guitar _guitar = new Guitar(
            new Note(4, Tones.E),
            new Note(3, Tones.B),
            new Note(3, Tones.G),
            new Note(3, Tones.D),
            new Note(2, Tones.A),
            new Note(2, Tones.E)
            );

        private readonly SolidColorBrush _inactiveFret = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        private readonly Player _player;

        private readonly ColorItem[] _selectionColors =
        {
            new ColorItem(true, Color.FromRgb(0, 138, 255)),
            new ColorItem(true, Color.FromRgb(255, 0, 10)),
            new ColorItem(true, Color.FromRgb(255, 231, 0)),
            new ColorItem(true, Color.FromRgb(78, 255, 0)),
            new ColorItem(true, Color.FromRgb(186, 0, 255)),
            new ColorItem(true, Color.FromRgb(255, 177, 0)),
            new ColorItem(true, Color.FromRgb(154, 100, 134)) //Этот цвет используется если предыдущие заняты
        };

        private readonly AudioSintezator _sintezator = new AudioSintezator();

        private readonly Tuple<string, Note[]>[] _tunes =
        {
            new Tuple<string, Note[]>("Стандарт: EADGBE", new[]
                                                          {
                                                              new Note(4, Tones.E), new Note(3, Tones.B), new Note(3, Tones.G),
                                                              new Note(3, Tones.D), new Note(2, Tones.A), new Note(2, Tones.E)
                                                          }),
            new Tuple<string, Note[]>("Drop D: DADGBE", new[]
                                                        {
                                                            new Note(4, Tones.E), new Note(3, Tones.B), new Note(3, Tones.G),
                                                            new Note(3, Tones.D), new Note(2, Tones.A), new Note(2, Tones.D)
                                                        }),
            new Tuple<string, Note[]>("Double Drop D: DADGBD", new[]
                                                               {
                                                                   new Note(4, Tones.D), new Note(3, Tones.B), new Note(3, Tones.G),
                                                                   new Note(3, Tones.D), new Note(2, Tones.A), new Note(2, Tones.D)
                                                               }),
            new Tuple<string, Note[]>("Drop C: CGCFAD", new[]
                                                        {
                                                            new Note(4, Tones.D), new Note(3, Tones.A), new Note(3, Tones.F),
                                                            new Note(3, Tones.C), new Note(2, Tones.G), new Note(2, Tones.C)
                                                        }),
            new Tuple<string, Note[]>("Drop B flat: A#FA#D#GC", new[]
                                                                {
                                                                    new Note(4, Tones.C), new Note(3, Tones.G), new Note(3, Tones.Dd),
                                                                    new Note(2, Tones.Ad), new Note(2, Tones.F), new Note(1, Tones.Ad)
                                                                }),
            new Tuple<string, Note[]>("Open G: DGDGBD", new[]
                                                        {
                                                            new Note(4, Tones.D), new Note(3, Tones.B), new Note(3, Tones.G),
                                                            new Note(3, Tones.D), new Note(2, Tones.G), new Note(2, Tones.D)
                                                        }),
            new Tuple<string, Note[]>("1/2 step Down: EbG#DbF#BbEb", new[]
                                                                     {
                                                                         new Note(4, Tones.Dd), new Note(3, Tones.Ad), new Note(3, Tones.Fd),
                                                                         new Note(3, Tones.Cd), new Note(2, Tones.Gd), new Note(2, Tones.Dd)
                                                                     })
        };

        private bool _fretWasDown;
        private bool _pianoKeyWasDown;
        private List<Note> _toggled = new List<Note>();

        public MainWindow()
        {
            InitializeComponent();

            _player = new Player(this);

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            CInstrument.ItemsSource = InstrumentNames;

            CTunes.ItemsSource = _tunes;

            CChordMods.ItemsSource = Chords.chordMods;

            CChords.ItemsSource = Chords.chordsBases;

            for (double speed = 0.25; speed <= 2.0; speed += 0.25)
            {
                CPlaySpeed.Items.Add(speed);
            }

            int devicesCount = MidiIn.NumberOfDevices;
            for (int i = 0; i < devicesCount; i++)
            {
                MidiInCapabilities device = MidiIn.DeviceInfo(i);
                CSelectedInput.Items.Add(device);
            }

            #region Генерация клавиш пианино

            int keyPosition = KeysOffset;

            for (byte octave = StartOctave; octave <= EndOctave; ++octave)
            {
                for (int i = 0; i < 12; ++i) // 12 полутонов в октаве
                {
                    var currentKey = new TextBlock();

                    var note = new Note(octave, (Tones) i);

                    currentKey.Text = note.ToString();
                    currentKey.Name = GenerateKeyName(note);

                    if (!note.isDiez()) //Если не диез
                    {
                        currentKey.Width = KeyWidth;
                        currentKey.Height = KeyHeight;

                        currentKey.Background = _genericColor;

                        currentKey.Margin = new Thickness(keyPosition, 0, 0, 0);
                        currentKey.Padding = new Thickness(0, 130, 0, 0);

                        currentKey.SetValue(Panel.ZIndexProperty, 0);

                        keyPosition += KeyWidth + KeysOffset;

                        currentKey.Foreground = Brushes.Black;

                        currentKey.FontSize = 14;
                    }
                    else
                    {
                        currentKey.Width = DiezWiddth;
                        currentKey.Height = DiezHeight;

                        currentKey.Background = _diezColor;

                        currentKey.Margin = new Thickness(keyPosition - DiezWiddth/2 + KeysOffset/2,
                            0, 0, 0);
                        currentKey.Padding = new Thickness(0, 75, 0, 0);

                        currentKey.SetValue(Panel.ZIndexProperty, 1);

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

            for (byte guitarString = 0; guitarString < 6; ++guitarString)
            {
                for (byte fret = 0; fret < FretsCount; ++fret)
                {
                    Shape currentFret;
                    if (fret != 0)
                    {
                        currentFret = new Ellipse {Margin = new Thickness(FretOffset)};
                    }
                    else
                    {
                        currentFret = new Rectangle {Margin = new Thickness(0)};
                    }

                    currentFret.Name = GenerateFretName(guitarString, fret);

                    currentFret.Stroke = _fretStroke;
                    currentFret.Fill = _inactiveFret;

                    FretsGrid.Children.Add(currentFret);

                    currentFret.SetValue(Grid.ColumnProperty, (int) fret);
                    currentFret.SetValue(Grid.RowProperty, (int) guitarString);


                    currentFret.PreviewMouseLeftButtonDown += FretMouseDown;
                    currentFret.PreviewMouseLeftButtonUp += FretMouseUp;
                    currentFret.MouseLeave += FretMouseLeave;

                    currentFret.PreviewMouseRightButtonUp += FretToggled;
                }
            }

            #endregion
        }

        private MidiIn MidiDevice { get; set; }

        public int LineCount { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            CInstrument.SelectedItem = "AcousticPiano";
            CTunes.SelectedIndex = 0;
            //cChordMods.Items[0] = "maj";
            CChordMods.SelectedIndex = -1;
            CChords.SelectedIndex = 0;
            CPlaySpeed.SelectedItem = 1.0;

            StopPlayAll();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            StopPlayAll();
            _sintezator.Dispose();
        }

        private void PianoKeyDown(object sender, MouseButtonEventArgs e)
        {
            if (TSong.IsReadOnly)
            {
                return;
            }

            _pianoKeyWasDown = true;

            var obj = (TextBlock) sender;
            Note note = ParseKeyName(obj.Name);

            _sintezator.PlayTone(note.Octave, note.Tone);

            if (!_toggled.Contains(note))
            {
                HighlightNote(note);
            }
        }

        private void PianoKeyUp(object sender, MouseButtonEventArgs e)
        {
            if (TSong.IsReadOnly)
            {
                return;
            }

            _pianoKeyWasDown = false;

            var obj = (TextBlock) sender;
            Note note = ParseKeyName(obj.Name);

            _sintezator.StopPlaying(note.Octave, note.Tone);

            if (!_toggled.Contains(note))
            {
                DehightlightNote(note);
            }
        }

        private void PianoKeyLeave(object sender, MouseEventArgs e)
        {
            if (TSong.IsReadOnly)
            {
                return;
            }

            if (_pianoKeyWasDown)
            {
                PianoKeyUp(sender, null);
            }
        }

        private void CInstrumentSelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            var instrumentSelector = sender as AutoCompleteBox;
            if (instrumentSelector != null && instrumentSelector.SelectedItem != null)
            {
                _sintezator.StopAll();
                _sintezator.SetInstrument(
                    ((string) (instrumentSelector).SelectedItem).ConvertToEnum<AudioSintezator.Instruments>());
            }
        }

        private void FretMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TSong.IsReadOnly)
            {
                return;
            }

            _fretWasDown = true;

            var obj = (Shape) sender;
            Note note = ParseFretName(obj.Name);
            _sintezator.PlayTone(note.Octave, note.Tone);

            if (!_toggled.Contains(note))
            {
                HighlightNote(note, obj);
            }
        }

        private void FretMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (TSong.IsReadOnly)
            {
                return;
            }

            _fretWasDown = false;

            var obj = (Shape) sender;
            Note note = ParseFretName(obj.Name);

            _sintezator.StopPlaying(note.Id);

            if (!_toggled.Contains(note))
            {
                DehightlightNote(note);
            }
        }

        private void FretMouseLeave(object sender, MouseEventArgs e)
        {
            if (TSong.IsReadOnly)
            {
                return;
            }

            if (_fretWasDown)
            {
                FretMouseUp(sender, null);
            }
        }

        private void CTunesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StopPlayAll();
            _guitar.SetTuning(_tunes[CTunes.SelectedIndex].Item2);
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
            if (TSong.IsReadOnly)
            {
                return;
            }

            var obj = (Shape) sender;

            Note note = ParseFretName(obj.Name);

            ToggleNote(note, obj);
        }

        private void KeyToggled(object sender, MouseButtonEventArgs e)
        {
            if (TSong.IsReadOnly)
            {
                return;
            }

            var obj = (TextBlock) sender;

            ToggleNote(ParseKeyName(obj.Name));
        }

        private void ChordChanged(object sender, SelectionChangedEventArgs e)
        {
            ToggleSelectedChord();
        }

        private void BChordPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PlayToggled();
        }

        private void BChordPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _sintezator.StopAll();
        }

        private void BResetPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            CChordMods.SelectedIndex = -1;
            StopPlayAll();
        }

        private void BPlayPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string[] strs = TSong.Text.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            TSong.IsReadOnly = true;

            _player.SetStartPos(TSong.GetLineIndexFromCharacterIndex(TSong.CaretIndex));
            _player.playMusic(strs);
        }

        private void BStopPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _player.Stop();
            TSong.IsReadOnly = false;
        }

        private void CPlaySpeedSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _player.SetPlaySpeed((double) CPlaySpeed.SelectedItem);
        }

        private void SongDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !TSong.IsReadOnly)
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void SongDrop(object sender, DragEventArgs e)
        {
            try
            {
                var data = (string[]) e.Data.GetData("FileDrop", false);

                if (data.Length == 1)
                {
                    string fileName = data[0];
                    if (fileName.EndsWith(".mid"))
                    {
                        TSong.Text = Utils.ParseMIDI(fileName);
                        SetCurrentLine(0);
                    }
                    else if (fileName.EndsWith(".txt"))
                    {
                        TSong.Text = File.ReadAllText(fileName);
                        SetCurrentLine(0);
                    }
                }
            }
            catch (Exception ex)
            {
                ; //It's ok
            }
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
                    LineCount = textbox.Text.Split(new[] {Environment.NewLine}, StringSplitOptions.None).Count();
                }
                OnPropertyChanged("LineCount");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void CInstrumentPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            CInstrument.IsDropDownOpen = true;
        }

        private void CSelectedInput_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int inputIndex = CSelectedInput.SelectedIndex;

            if (MidiDevice != null)
            {
                MidiDevice.Close();
            }

            MidiDevice = new MidiIn(inputIndex);
            MidiDevice.Start();
            MidiDevice.MessageReceived += MidiDeviceOnMessageReceived;
        }

        private void MidiDeviceOnMessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent != null)
            {
                switch (e.MidiEvent.CommandCode)
                {
                    case MidiCommandCode.NoteOn:
                    {
                        var noteOn = (NoteOnEvent) e.MidiEvent;
                        Note note = Note.FromID(noteOn.NoteNumber);
                        _sintezator.PlayTone(note.Octave, note.Tone, noteOn.Velocity);


                        Application.Current.Dispatcher.BeginInvoke(new Action(() => HighlightNote(note)));
                    }
                        break;
                    case MidiCommandCode.NoteOff:
                    {
                        var noteOn = (NoteEvent) e.MidiEvent;
                        Note note = Note.FromID(noteOn.NoteNumber);
                        _sintezator.StopPlaying(note.Octave, note.Tone);


                        Application.Current.Dispatcher.BeginInvoke(new Action(() => DehightlightNote(note)));
                    }
                        break;
                }
            }
        }

        #region Функциональная часть

        private int _assignedColors;

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
            _sintezator.StopAll();

            foreach (Note i in _toggled)
            {
                _sintezator.PlayTone(i.Octave, i.Tone);
            }
        }

        public void StopPlayAll()
        {
            _sintezator.StopAll();
            foreach (Note note in _toggled)
            {
                DehightlightNote(note);
            }

            _toggled.Clear();

            UpdateChord();
        }

        private Color GetColor()
        {
            ++_assignedColors;
            for (int i = 0; i < _selectionColors.Length - 1; ++i)
            {
                if (_selectionColors[i].free)
                {
                    _selectionColors[i].free = false;
                    return _selectionColors[i].color;
                }
            }
            return _selectionColors.Last().color;
        }

        private void FreeColor(Color color)
        {
            --_assignedColors;
            if (_assignedColors < 0)
            {
                throw new Exception("Попытка освободить больше цветов, чем было выделено");
            }

            if (color == _selectionColors.Last().color)
            {
                return;
            }

            bool isSuccess = false;
            for (int i = 0; i < _selectionColors.Length; ++i)
            {
                if (_selectionColors[i].color == color)
                {
                    if (_selectionColors[i].free == false)
                    {
                        _selectionColors[i].free = true;
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
            tmp.A = InactiveFretAlpha;
            var highlighted = new SolidColorBrush(tmp);

            List<Tuple<byte, byte>> lst = _guitar.GetFretsForNote(note);
            foreach (var objName in lst)
            {
                object o = LogicalTreeHelper.FindLogicalNode(FretsGrid,
                    GenerateFretName(objName.Item1, objName.Item2));
                if (o != null)
                {
                    ((Shape) o).Fill = highlighted;
                }
            }

            if (major != null)
            {
                major.Fill = baseColor;
            }

            string str = GenerateKeyName(note);
            TChordName.Text = note.ToString().Replace('#', '♯');

            var bNote = (TextBlock) LogicalTreeHelper.FindLogicalNode(KeysGrid, str); // имя клавиши имеет след. вид: "Тон[d]Октава"
            if (bNote != null)
            {
                bNote.Background = baseColor;
                bNote.Focus();
            }
        }

        private void DehightlightNote(Note note)
        {
            List<Tuple<byte, byte>> lst = _guitar.GetFretsForNote(note);
            foreach (var objName in lst)
            {
                object o = LogicalTreeHelper.FindLogicalNode(FretsGrid,
                    GenerateFretName(objName.Item1, objName.Item2));
                if (o != null)
                {
                    var shape = o as Shape;

                    Brush brushCopy = shape.Fill.Clone();
                    shape.Fill = brushCopy;

                    var fretFadeAnimation = new ColorAnimation(((SolidColorBrush) shape.Fill).Color, _inactiveFret.Color, new Duration(TimeSpan.FromSeconds(2)));
                    var easing = new QuinticEase {EasingMode = EasingMode.EaseOut};

                    fretFadeAnimation.EasingFunction = easing;
                    var fretStoryboard = new Storyboard();
                    fretStoryboard.Children.Add(fretFadeAnimation);

                    string elementName = shape.Name;

                    if (FindName(elementName) == null)
                    {
                        RegisterName(elementName, shape);
                    }
                    Storyboard.SetTargetName(fretFadeAnimation, elementName);
                    Storyboard.SetTargetProperty(fretFadeAnimation, new PropertyPath("Fill.Color"));

                    fretStoryboard.Begin(this);
                }
            }

            TChordName.Text = "";

            string str = GenerateKeyName(note);

            var bNote = (TextBlock) LogicalTreeHelper.FindLogicalNode(KeysGrid, str);
            if (bNote != null)
            {
                FreeColor(((SolidColorBrush) bNote.Background).Color);

                SolidColorBrush targetColor = str.Length == 2 ? _genericColor : _diezColor;

                Brush brushCopy = bNote.Background.Clone();
                bNote.Background = brushCopy;

                var noteFadeAnimation = new ColorAnimation(((SolidColorBrush) bNote.Background).Color, targetColor.Color, new Duration(TimeSpan.FromSeconds(3)));
                var easing = new QuinticEase {EasingMode = EasingMode.EaseOut};

                noteFadeAnimation.EasingFunction = easing;
                var noteStoryboadrd = new Storyboard();
                noteStoryboadrd.Children.Add(noteFadeAnimation);

                if (FindName(bNote.Name) == null)
                {
                    RegisterName(bNote.Name, bNote);
                }
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
            string[] data = str.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries); // лад имеет след. имя: _Струна_НомерЛада

            Note note = _guitar.GetNote(byte.Parse(data[0]), byte.Parse(data[1]));
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
                int index = TSong.GetCharacterIndexFromLineIndex(line);
                TSong.Select(index,
                    TSong.GetCharacterIndexFromLineIndex(line + 1) - index - 1);
                TSong.Focus();
            }
            catch //Передана несуществующая строка, значи воспроизведение остановлено
            {
                TSong.Select(0, 0);
                TSong.IsReadOnly = false;
            }
        }

        private void UpdateChord()
        {
            if (_toggled.Count > 0)
            {
                try
                {
                    ChordType type;
                    Note baseNote;
                    Chords.GetChord(_toggled, out baseNote, out type);

                    TChordName.Text = string.Format(Format,
                        type.description,
                        baseNote.Tone.ToString().Replace('d', '♯'),
                        type.name);
                }
                catch (Exception e)
                {
                    TChordName.Text = e.Message;
                }
            }
            else
            {
                TChordName.Text = "—";
            }
        }

        private void AddToggledToSong()
        {
            if (_toggled.Count > 0 && !TSong.IsReadOnly)
            {
                var str = new StringBuilder("\n");
                foreach (Note note in _toggled)
                {
                    str.Append(note);
                    str.Append(",");
                }
                if (str.Length > 0)
                {
                    str.Remove(str.Length - 1, 1); // Удаляем последнюю запятую
                    str.Append("\n");

                    TSong.SelectedText = str.ToString();
                    TSong.SelectionLength = 0;

                    int lineIndex = TSong.GetLineIndexFromCharacterIndex(TSong.CaretIndex);
                    int index = TSong.GetCharacterIndexFromLineIndex(lineIndex + 2);
                    TSong.Select(index, 0);
                    TSong.Focus();
                }
            }
        }

        private void ToggleSelectedChord()
        {
            if (CChords.SelectedIndex >= 0 && CChordMods.SelectedIndex >= 0)
            {
                StopPlayAll();

                var chord = CChords.SelectedValue as string;
                int mod = CChordMods.SelectedIndex;
                chord = chord.Replace("#", "d");

                var baseNote = new Note(3, chord.ConvertToEnum<Tones>());

                List<Note> lst = Chords.chordTypes[mod].BuildChord(baseNote);

                foreach (Note chordNote in lst)
                {
                    ToggleNote(chordNote);
                }
            }
        }

        #endregion
    }
}