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
using DynamicReconfigure;
using Messages.dynamic_reconfigure;

namespace DynamicReconfigureSharp
{
    /// <summary>
    /// Interaction logic for DynamicReconfigureCheckbox.xaml
    /// </summary>
    public partial class DynamicReconfigureCheckbox : UserControl, IDynamicReconfigureLayout
    {
        private DynamicReconfigureInterface dynamic;
        private string name;
        private bool def;
        private bool ignore = true;

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
            Dispatcher.BeginInvoke(new Action(() => { 
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
            return (d) =>
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
