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
    /// Interaction logic for DynamicReconfigureStringDropdown.xaml
    /// </summary>
    public partial class DynamicReconfigureStringDropdown : UserControl
    {
        private DynamicReconfigureInterface dynamic;
        private string name;
        private string def,max,min,edit_method;
        private bool ignore = true;

        public DynamicReconfigureStringDropdown(DynamicReconfigureInterface dynamic, ParamDescription pd, string def, string max, string min, string edit_method)
        {
            this.def = def;
            this.max = max;
            this.min = min;
            this.edit_method = edit_method;
            name = pd.name.data;
            this.dynamic = dynamic;
            InitializeComponent();
            description.Content = name;
            JustTheTip.Content = pd.description.data;
            //dynamic.Subscribe(name, changed);
            ignore = false;
        }

        private void changed(string newstate)
        {
            ignore = true;
            //Dispatcher.Invoke(new Action(() => _checkBox.IsChecked = newstate));
            ignore = false;
        }


        private void Enum_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignore) return;
        }
    }
}
