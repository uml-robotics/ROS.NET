using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;

namespace DynamicReconfigureSharp
{
    [DataContract]
    public class EnumDescription
    {
        public string enum_description;
        public EnumValue[] Enum;
    }

    [DataContract]
    public class EnumValue
    {
        public int srcline;
        public string description;
        public string srcfile;
        public string cconsttype;
        public string value;
        public string ctype;
        public string type;
        public string name;
    }

    public static class EnumParser
    {
        public static Dictionary<string, string> Parse(string s)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();
            Stack<char> stack = new Stack<char>();
            string currentname = "";
            string currentvalue = null;
            bool insidearray = false;
            int index = 0;
            for(int i=0;i<s.Length;i++)
            {
                char c = s[i];

                if (insidearray)
                {
                    if (c == ':') continue;
                    if (c == ']')
                    {
                        stack.Pop();
                        insidearray = false;
                        fields[currentname.Trim()] = currentvalue.Trim();
                        currentname = "";
                        currentvalue = null;
                        continue;
                    }
                    currentvalue += c;
                    continue;
                }
                if (c == '[')
                {
                    stack.Push(c);
                    insidearray = true;
                    continue;
                }
                if (c == ':') continue;
                if ((c == "'"[0] || c == '"' || c == '}') && stack.Count > 0 && stack.Peek() == c)
                {
                    //close
                    stack.Pop();
                    if (currentvalue != null)
                    {
                        fields[currentname.Trim()] = currentvalue.Trim();
                        currentname = "";
                        currentvalue = null;
                    }
                    else
                    {
                        currentvalue = "";
                    }
                    continue;
                }
                else if (c == "'"[0] || c == '"' || c == '{')
                {
                    //open
                    stack.Push(c);
                    continue;
                }
                if (c == ',') continue;
                if (currentvalue == null)
                {
                    currentname = currentname + c;
                }
                else
                {
                    currentvalue = currentvalue + c;
                }
            }
            return fields;
        }
    }


    /// <summary>
    /// Interaction logic for DynamicReconfigureStringDropdown.xaml
    /// </summary>
    public partial class DynamicReconfigureStringDropdown : UserControl
    {
        private DynamicReconfigureInterface dynamic;
        private string name;
        private object def, max, min;
        private string edit_method;
        private bool ignore = true;

        public DynamicReconfigureStringDropdown(DynamicReconfigureInterface dynamic, ParamDescription pd, object def, object max, object min, string edit_method)
        {
            this.def = def;
            this.max = max;
            this.min = min;
            this.edit_method = edit_method.Replace("'enum'","'Enum'");
            Dictionary<string, string> parsed = EnumParser.Parse(this.edit_method);
            string[] vals = parsed["Enum"].Split(new []{'}'}, StringSplitOptions.RemoveEmptyEntries);
            List<Dictionary<string, string>> descs = vals.Select(s => EnumParser.Parse(s+"}")).ToList();
            descs = descs.Except(descs.Where((d) => d.Count == 0)).ToList();
            EnumDescription ed = new EnumDescription();
            ed.Enum = new EnumValue[descs.Count];
            ed.enum_description = parsed["enum_description"];
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
