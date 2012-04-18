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
using System.Threading;

namespace DREAMPioneer
{
    public enum ButtonType { L1, L2, L3, L4, R1, R2, R3, R4 }
    public delegate void ButtonHandler(ButtonType action, bool down);
    /// <summary>
    /// Interaction logic for EButton.xaml
    /// </summary>
    public partial class EButton : UserControl
    {
        public EButton()
        {
            InitializeComponent();
        }

        public int Over = -1;
        public void Down()
        {
            State = true;
        }

        public void Up()
        {

            State = false;
        }

        private bool _state = false;

        public bool State
        {
            get { return _state; }
            set
            {
                if (value != _state)
                {
                    if (value)
                        SetOn();
                    else
                        SetOff();
                }
                _state = value;
            }
        }

        public string Text
        {
            get { return (string)label.Content; }
            set { label.Content = value; }
        }

        public double TextSize
        {
            get { return label.FontSize; }
            set { label.FontSize = value; }
        }

        public ButtonType button;

        private void SetOn()
        {
            label.Foreground = Brushes.Black;
            Box.Fill = Brushes.White;
        }

        private void SetOff()
        {
            label.Foreground = Brushes.White;
            Box.Fill = Brushes.Gray;
        }
    }
}
