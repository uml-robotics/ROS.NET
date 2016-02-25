#region USINGZ

using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;

#endregion

namespace DynamicReconfigureSharp
{
    /// <summary>
    ///     Interaction logic for DynamicReconfigureSlider.xaml
    /// </summary>
    public partial class DynamicReconfigureSlider : UserControl, IDynamicReconfigureLayout
    {
        private double def;
        private DynamicReconfigureInterface dynamic;
        private bool ignore = true;
        private bool isDouble;
        private double max;
        private double min;
        private string name;
        private bool dragStarted = false;

        private string Format<T>(T o) where T : struct
        {
            var d = o as double?;
            string fmat = string.Format("{{0:G{0}}}{1}", textBehavior.MaxLength, d != null && isDouble && (int)d.Value == d.Value ? ".0" : "");
            string ret = string.Format(fmat, o);
            if (o is int || ret.Length < textBehavior.MaxLength)
                return ret;
            //if we have a double that's longer than maxlength,
            //  see if trimming least significant digits to fit would
            //    remove the decimal point OR
            //    cause the number to end with the decimal point
            //  if neither occurs, truncate the string
            string shorter = ret.Substring(0, textBehavior.MaxLength);
            if (shorter.Contains(".") && shorter[textBehavior.MaxLength - 1] != '.')
                return shorter;
            return ret;
        }


        public DynamicReconfigureSlider(DynamicReconfigureInterface dynamic, ParamDescription pd, double def, double max, double min, bool isDouble)
        {
            name = pd.name;
            this.dynamic = dynamic;
            this.isDouble = isDouble;
            InitializeComponent();
            this.min = min;
            if (double.IsInfinity(this.min))
                value.Minimum = -1000.0;
            else
                value.Minimum = min;
            this.max = max;
            if (double.IsInfinity(this.max))
                value.Maximum = 1000.0;
            else
                value.Maximum = max;
            description.Text = name + ":";
            JustTheTip.Content = pd.description;

            double range = value.Maximum - value.Minimum;
            minlabel.Content = Format(value.Minimum);
            maxlabel.Content = Format(value.Maximum);
            value.Value = this.def = def;
            if (isDouble)
            {
                textBehavior.RegularExpression = @"^[\-\.0-9]+$";
                value.IsSnapToTickEnabled = false;
                value.TickFrequency = range / 10.0;
                value.LargeChange = range / 10.0;
                value.SmallChange = range / 100.0;
                dynamic.Subscribe(name, new Action<double>(changed));
                ignore = false;
            }
            else
            {
                textBehavior.RegularExpression = @"^[\-0-9][0-9]*$";
                value.IsSnapToTickEnabled = true;
                value.TickFrequency = Math.Max(1, (int) Math.Floor(range  /10.0));
                value.LargeChange = Math.Max(1, (int)Math.Floor(range / 10.0));
                value.SmallChange = 1;
                dynamic.Subscribe(name, new Action<int>(changed));
                ignore = false;
            }
        }

        private void changed(int newstate)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ignore = true;
                box.Text = "" + newstate;
                value.Value = newstate;
                if (intchanged != null)
                    intchanged(newstate);
                ignore = false;
            }));
        }

        private void changed(double newstate)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ignore = true;
                box.Text = Format(newstate);
                value.Value = newstate;
                if (doublechanged != null)
                    doublechanged(newstate);
                ignore = false;
            }));
        }

        private void commit()
        {
            if (isDouble)
            {
                double d = 0;
                if (double.TryParse(box.Text, out d))
                {
                    if (value.Value != d)
                    {
                        value.Value = d;                  
                    }
                }
            }
            else
            {
                int i = 0;
                if (int.TryParse(box.Text, out i))
                {
                    if (value.Value != i)
                    {
                        value.Value = i;
                    }
                }
            }
        }

        private void Box_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                commit();
            }
        }

        private void Box_OnLostFocus(object sender, RoutedEventArgs e)
        {
            commit();
        }

        #region Slider Value Changed
        private void Slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            dragStarted = true;
        }
        private void Slider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            box.Text = Format(value.Value);
        }
        private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            dragStarted = false;
            Value_OnValueChanged(null, null);
        }
        private void Value_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dragStarted) return;
            box.Text = Format(value.Value);
            if (!ignore)
                dynamic.Set(name, (int)value.Value);
        }
        #endregion

        private event Action<int> intchanged;
        private event Action<double> doublechanged;

        internal Action<int> Instrument(Action<int> cb)
        {
            intchanged += cb;
            return d =>
            {
                ignore = false;
                value.Value = d;
            };
        }

        internal Action<double> Instrument(Action<double> cb)
        {
            doublechanged += cb;
            return d =>
            {
                ignore = false;
                value.Value = d;
            };
        }

        #region IDynamicReconfigureLayout Members

        public double getDescriptionWidth()
        {
            return (Content as Grid).ColumnDefinitions[0].ActualWidth;
        }

        public void setDescriptionWidth(double w)
        {
            (Content as Grid).ColumnDefinitions[0].Width = new GridLength(w);
        }

        #endregion

    }

    public class AllowableCharactersTextBoxBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty RegularExpressionProperty =
            DependencyProperty.Register("RegularExpression", typeof (string), typeof (AllowableCharactersTextBoxBehavior),
                new FrameworkPropertyMetadata("*"));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register("MaxLength", typeof (int), typeof (AllowableCharactersTextBoxBehavior),
                new FrameworkPropertyMetadata(int.MinValue));

        public string RegularExpression
        {
            get { return (string) base.GetValue(RegularExpressionProperty); }
            set { base.SetValue(RegularExpressionProperty, value); }
        }

        public int MaxLength
        {
            get { return (int) base.GetValue(MaxLengthProperty); }
            set { base.SetValue(MaxLengthProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(AssociatedObject, OnPaste);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = Convert.ToString(e.DataObject.GetData(DataFormats.Text));

                if (!IsValid(text, true))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValid(e.Text, false);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(AssociatedObject, OnPaste);
        }

        private bool IsValid(string newText, bool paste)
        {
            return !ExceedsMaxLength(newText, paste) && Regex.IsMatch(newText, RegularExpression);
        }

        private bool ExceedsMaxLength(string newText, bool paste)
        {
            if (MaxLength == 0) return false;

            return LengthOfModifiedText(newText, paste) > MaxLength;
        }

        private int LengthOfModifiedText(string newText, bool paste)
        {
            var countOfSelectedChars = AssociatedObject.SelectedText.Length;
            var caretIndex = AssociatedObject.CaretIndex;
            string text = AssociatedObject.Text;

            if (countOfSelectedChars > 0 || paste)
            {
                text = text.Remove(caretIndex, countOfSelectedChars);
                return text.Length + newText.Length;
            }
            var insert = Keyboard.IsKeyToggled(Key.Insert);

            return insert && caretIndex < text.Length ? text.Length : text.Length + newText.Length;
        }
    }
}