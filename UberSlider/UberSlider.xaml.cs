using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UberSlider
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UberSlider : UserControl
    {
        #region PROPERTIES
        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
           "Max",
           typeof(double),
           typeof(UberSlider),
           new FrameworkPropertyMetadata(100.0d,
               FrameworkPropertyMetadataOptions.None, (obj, args) =>
               {
                   try
                   {
                       if (obj is UberSlider)
                       {
                           UberSlider target = obj as UberSlider;
                           target.Max = (double)args.NewValue;
                       }
                   }
                   catch (Exception e) { Console.WriteLine(e); }
               }));
        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
           "Min",
           typeof(double),
           typeof(UberSlider),
           new FrameworkPropertyMetadata(0.0d,
               FrameworkPropertyMetadataOptions.None, (obj, args) =>
               {
                   try
                   {
                       if (obj is UberSlider)
                       {
                           UberSlider target = obj as UberSlider;
                           target.Min = (double)args.NewValue;
                       }
                   }
                   catch (Exception e) { Console.WriteLine(e); }
               }));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
           "Label",
           typeof(string),
           typeof(UberSlider),
           new FrameworkPropertyMetadata("Label",
               FrameworkPropertyMetadataOptions.None, (obj, args) =>
               {
                   try
                   {
                       if (obj is UberSlider)
                       {
                           UberSlider target = obj as UberSlider;
                           target.Label = (string)args.NewValue;
                       }
                   }
                   catch (Exception e) { Console.WriteLine(e); }
               }));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
           "Value",
           typeof(double),
           typeof(UberSlider),
           new FrameworkPropertyMetadata(0.0d,
               FrameworkPropertyMetadataOptions.None, (obj, args) =>
               {
                   try
                   {
                       if (obj is UberSlider)
                       {
                           UberSlider target = obj as UberSlider;
                           target.Value = (double)args.NewValue;
                       }
                   }
                   catch (Exception e) { Console.WriteLine(e); }
               }));
        #endregion

        public Slider slider { get { return _slider; } }

        private double __min, __max, __current;

        public enum DPadDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        public double Value
        {
            get
            {
                return __current;
            }
            set
            {
                __current = value;
                _slider.Value = value;
                _current.Content = value;
            }
        }

        public string Label
        {
            get { return _label.Text; }
            set { _label.Text = value; }
        }

        public double Max
        {
            get { return __max;  }
            set { __max = value; _max.Content = "" + value; _slider.Maximum = value; }
        }

        public double Min
        {
            get { return __min; }
            set { __min = value; _min.Content = "" + value; _slider.Minimum = value; }
        }

        public UberSlider()
        {
            InitializeComponent();
        }

        private void _slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            __current = Math.Round(e.NewValue, 3);
            _current.Content = "" + __current;
        }

        // dpad up functions
        public void DPadButton(DPadDirection dir, bool state)
        {
            // if up is pressed
            if (state)
            {
                switch (dir)
                {
                    case DPadDirection.Left:
                        {
                            if (slider.Value - .1 > Min)
                                slider.Value -= .1;
                            else
                                slider.Value = Min;
                        }
                        break;
                    case DPadDirection.Right:
                        {
                            if (slider.Value + .1 < Max)
                                slider.Value += .1;
                            else
                                slider.Value = Max;
                        }
                        break;
                }
            }
        }
    }
}
