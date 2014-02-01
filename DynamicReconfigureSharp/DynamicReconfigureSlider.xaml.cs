using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;
using System.Windows.Interactivity;

namespace DynamicReconfigureSharp
{
    /// <summary>
    /// Interaction logic for DynamicReconfigureSlider.xaml
    /// </summary>
    public partial class DynamicReconfigureSlider : UserControl, IDynamicReconfigureLayout
    {
        private DynamicReconfigureInterface dynamic;
        private string name;
        private double def;
        private double max;
        private double min;
        private bool isDouble;
        private bool ignore = true;

        public DynamicReconfigureSlider(DynamicReconfigureInterface dynamic, ParamDescription pd, double def, double max, double min, bool isDouble)
        {
            name = pd.name.data;
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
            description.Content = name + ":";
            JustTheTip.Content = pd.description.data;

            if (isDouble)
            {
                textBehavior.RegularExpression = @"^[\-\.0-9]+$";
                value.IsSnapToTickEnabled = false;
                value.TickFrequency = 0;
                minlabel.Content = string.Format("{0:N2}", value.Minimum);
                maxlabel.Content = string.Format("{0:N2}", value.Maximum);
                value.Value = this.def = def;
                dynamic.Subscribe(name, new Action<double>(changed));
                ignore = false;
            }
            else
            {
                textBehavior.RegularExpression = @"^[\-0-9][0-9]*$";
                value.TickFrequency = 1;
                value.IsSnapToTickEnabled = true;
                minlabel.Content = ""+(int)value.Minimum;
                maxlabel.Content = "" + (int)value.Maximum;
                value.Value = this.def = def;
                dynamic.Subscribe(name, new Action<int>(changed));
                ignore = false;
            }
        }

        private void changed(int newstate)
        {
            ignore = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                box.Text = "" + newstate;
                value.Value = newstate;
                if (intchanged != null)
                    intchanged(newstate);
                ignore = false;
            }));
        }
        private void changed(double newstate)
        {
            ignore = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                box.Text = string.Format("{0:N2}", newstate);
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
                    value.Value = d;
                    if (!ignore)
                        dynamic.Set(name, d);
                }
            }
            else
            {
                int i = 0;
                if (int.TryParse(box.Text, out i))
                {
                    value.Value = i;
                    if (!ignore)
                        dynamic.Set(name, i);
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

        private void Value_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isDouble)
            {
                box.Text = string.Format("{0:N2}", value.Value);
                if (!ignore)
                    dynamic.Set(name, value.Value);
            }
            else
            {
                box.Text = "" + (int)value.Value;
                if (!ignore)
                    dynamic.Set(name, (int)value.Value);
            }
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

        private event Action<int> intchanged;
        private event Action<double> doublechanged;
        internal Action<int> Instrument(Action<int> cb)
        {
            intchanged += cb;
            return (d) =>
            {
                ignore = false;
                value.Value = d;
            };
        }
        internal Action<double> Instrument(Action<double> cb)
        {
            doublechanged += cb;
            return (d) =>
            {
                ignore = false;
                value.Value = d;
            };
        }
    }

    public class AllowableCharactersTextBoxBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty RegularExpressionProperty =
             DependencyProperty.Register("RegularExpression", typeof(string), typeof(AllowableCharactersTextBoxBehavior),
             new FrameworkPropertyMetadata("*"));
        public string RegularExpression
        {
            get
            {
                return (string)base.GetValue(RegularExpressionProperty);
            }
            set
            {
                base.SetValue(RegularExpressionProperty, value);
            }
        }

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register("MaxLength", typeof(int), typeof(AllowableCharactersTextBoxBehavior),
            new FrameworkPropertyMetadata(int.MinValue));
        public int MaxLength
        {
            get
            {
                return (int)base.GetValue(MaxLengthProperty);
            }
            set
            {
                base.SetValue(MaxLengthProperty, value);
            }
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

        void OnPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
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
            var countOfSelectedChars = this.AssociatedObject.SelectedText.Length;
            var caretIndex = this.AssociatedObject.CaretIndex;
            string text = this.AssociatedObject.Text;

            if (countOfSelectedChars > 0 || paste)
            {
                text = text.Remove(caretIndex, countOfSelectedChars);
                return text.Length + newText.Length;
            }
            else
            {
                var insert = Keyboard.IsKeyToggled(Key.Insert);

                return insert && caretIndex < text.Length ? text.Length : text.Length + newText.Length;
            }
        }
    }
}
