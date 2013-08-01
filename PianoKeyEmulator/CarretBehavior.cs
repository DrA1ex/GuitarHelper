using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PianoKeyEmulator
{
    public static class CaretBehavior
    {
        public static readonly DependencyProperty ObserveCaretProperty =
            DependencyProperty.RegisterAttached
            (
                "ObserveCaret",
                typeof(bool),
                typeof(CaretBehavior),
                new UIPropertyMetadata(false, OnObserveCaretPropertyChanged)
            );
        public static bool GetObserveCaret(DependencyObject obj)
        {
            return (bool)obj.GetValue(ObserveCaretProperty);
        }
        public static void SetObserveCaret(DependencyObject obj, bool value)
        {
            obj.SetValue(ObserveCaretProperty, value);
        }
        private static void OnObserveCaretPropertyChanged(DependencyObject dpo,
                                                       DependencyPropertyChangedEventArgs e)
        {
            TextBox textBox = dpo as TextBox;
            if (textBox != null)
            {
                if ((bool)e.NewValue == true)
                {
                    textBox.SelectionChanged += textBox_SelectionChanged;
                }
                else
                {
                    textBox.SelectionChanged -= textBox_SelectionChanged;
                }
            }
        }

        static void textBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int caretIndex = textBox.CaretIndex;
            SetCaretIndex(textBox, caretIndex);
            SetLineIndex(textBox, textBox.GetLineIndexFromCharacterIndex(caretIndex));
        }

        private static readonly DependencyProperty CaretIndexProperty =
            DependencyProperty.RegisterAttached("CaretIndex", typeof(int), typeof(CaretBehavior));
        public static void SetCaretIndex(DependencyObject element, int value)
        {
            element.SetValue(CaretIndexProperty, value);
        }
        public static int GetCaretIndex(DependencyObject element)
        {
            return (int)element.GetValue(CaretIndexProperty);
        }

        private static readonly DependencyProperty LineIndexProperty =
            DependencyProperty.RegisterAttached("LineIndex", typeof(int), typeof(CaretBehavior));

        public static void SetLineIndex(DependencyObject element, int value)
        {
            if (value < 0)
                value = 0;
            element.SetValue(LineIndexProperty, value);
        }

        public static int GetLineIndex(DependencyObject element)
        {
            return (int)element.GetValue(LineIndexProperty);
        }
    }
}
