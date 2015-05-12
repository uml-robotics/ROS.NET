#region USINGZ

using System;
using System.Windows;
using System.Windows.Controls;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;

#endregion

namespace DynamicReconfigureSharp
{
    /// <summary>
    ///     Interaction logic for DynamicReconfigureCheckbox.xaml
    /// </summary>
    public partial class DynamicReconfigureCheckbox : UserControl, IDynamicReconfigureLayout
    {
        private bool def;
        private DynamicReconfigureInterface dynamic;
        private bool ignore = true;
        private string name;

        public DynamicReconfigureCheckbox(DynamicReconfigureInterface dynamic, ParamDescription pd, bool def)
        {
            this.def = def;
            name = pd.name.data;
            this.dynamic = dynamic;
            InitializeComponent();
            description.Content = name + ":";
            JustTheTip.Content = pd.description.data;
            _checkBox.IsChecked = def;
            dynamic.Subscribe(name, changed);
            ignore = false;
        }

        private void changed(bool newstate)
        {
            ignore = true;
            Dispatcher.Invoke(new Action(() =>
            {
                _checkBox.IsChecked = newstate;
                if (boolchanged != null)
                    boolchanged(newstate);
                ignore = false;
            }));
        }

        private event Action<bool> boolchanged;

        internal Action<bool> Instrument(Action<bool> cb)
        {
            boolchanged += cb;
            return d =>
            {
                ignore = false;
                _checkBox.IsChecked = d;
            };
        }

        private void _checkBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (ignore) return;
            dynamic.Set(name, true);
        }

        private void _checkBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (ignore) return;
            dynamic.Set(name, false);
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