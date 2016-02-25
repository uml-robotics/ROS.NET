#region USINGZ

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;

#endregion

namespace DynamicReconfigureSharp
{
    /// <summary>
    ///     Interaction logic for DynamicReconfigureStringDropdown.xaml
    /// </summary>
    public partial class DynamicReconfigureStringBox : UserControl, IDynamicReconfigureLayout
    {
        private string def;
        private DynamicReconfigureInterface dynamic;
        private bool ignore = true;
        private string name;
        private string text;

        public DynamicReconfigureStringBox(DynamicReconfigureInterface dynamic, ParamDescription pd, string def)
        {
            this.def = def;
            name = pd.name;
            this.dynamic = dynamic;
            InitializeComponent();
            description.Text = name + ":";
            JustTheTip.Content = pd.description;
            dynamic.Subscribe(name, changed);
            ignore = false;
            text = Box.Text;
        }

        private void changed(string newstate)
        {
            ignore = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Box.Text = newstate;
                text = newstate;
                if (stringchanged != null)
                    stringchanged(newstate);
                ignore = false;
            }));
        }

        private void commit()
        {
            if (!Box.Text.Equals(text))
            {
                dynamic.Set(name, Box.Text);
                text = Box.Text;
            }
        }

        private void Box_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                commit();
        }

        private void Box_OnLostFocus(object sender, RoutedEventArgs e)
        {
            commit();
        }

        private event Action<string> stringchanged;

        internal Action<string> Instrument(Action<string> cb)
        {
            stringchanged += cb;
            return d =>
            {
                Box.Text = d;
                commit();
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
}