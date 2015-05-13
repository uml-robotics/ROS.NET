#region USINGZ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;

#endregion

namespace DynamicReconfigureSharp
{
    [DataContract]
    public class EnumDescription
    {
        public EnumValue[] Enum;
        public string enum_description;
    }

    [DataContract]
    public class EnumValue
    {
        public string cconsttype;
        public string ctype;
        public string description;
        public string name;
        public string srcfile;
        public int srcline;
        public string type;
        public string value;
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
            for (int i = 0; i < s.Length; i++)
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
                if (c == "'"[0] || c == '"' || c == '{')
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

        public static Dictionary<string, string> SubParse(string s)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();
            string[] namevalue = s.Trim("'"[0], '"', '{', '}').Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < namevalue.Length; i++)
            {
                string[] spaced = namevalue[i].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                string[] pair = new string[2];
                pair[1] = "";
                pair[0] = spaced[0].Trim("'"[0], '{');
                for (int j = 1; j < spaced.Length; j++)
                    pair[1] += " " + spaced[j].Trim("'"[0], '{');
                pair[1] = pair[1].Trim();
                fields.Add(pair[0], pair[1]);
            }
            return fields;
        }
    }

    internal enum DROPDOWN_TYPE
    {
        STR,
        INT
    }

    /// <summary>
    ///     Interaction logic for DynamicReconfigureStringDropdown.xaml
    /// </summary>
    public partial class DynamicReconfigureStringDropdown : UserControl, IDynamicReconfigureLayout
    {
        private static readonly Dictionary<string, DROPDOWN_TYPE> types = new Dictionary<string, DROPDOWN_TYPE>
        {
            {"str", DROPDOWN_TYPE.STR},
            {"int", DROPDOWN_TYPE.INT}
        };

        private object def;
        private DynamicReconfigureInterface dynamic;
        private string edit_method;
        private EnumDescription enumdescription;
        private bool ignore = true;
        private object max, min;
        private string name;

        public DynamicReconfigureStringDropdown(DynamicReconfigureInterface dynamic, ParamDescription pd, object def, object max, object min, string edit_method)
        {
            this.def = def;
            this.max = max;
            this.min = min;
            this.edit_method = edit_method.Replace("'enum'", "'Enum'");
            Dictionary<string, string> parsed = EnumParser.Parse(this.edit_method);
            string[] vals = parsed["Enum"].Split(new[] {'}'}, StringSplitOptions.RemoveEmptyEntries);
            List<Dictionary<string, string>> descs = vals.Select(s => EnumParser.SubParse(s + "}")).ToList();
            descs = descs.Except(descs.Where(d => d.Count == 0)).ToList();
            enumdescription = new EnumDescription();
            enumdescription.Enum = new EnumValue[descs.Count];
            enumdescription.enum_description = parsed["enum_description"];
            Type tdesc = typeof (EnumValue);

            for (int i = 0; i < descs.Count; i++)
            {
                Dictionary<string, string> desc = descs[i];
                EnumValue newval = new EnumValue();
                foreach (string s in desc.Keys)
                {
                    FieldInfo fi = tdesc.GetField(s);
                    if (fi.FieldType == typeof (int))
                        fi.SetValue(newval, int.Parse(desc[s]));
                    else
                        fi.SetValue(newval, desc[s]);
                }
                enumdescription.Enum[i] = newval;
            }
            name = pd.name.data;
            this.dynamic = dynamic;
            InitializeComponent();
            for (int i = 0; i < enumdescription.Enum.Length; i++)
            {
                if (!types.ContainsKey(enumdescription.Enum[i].type))
                {
                    throw new Exception("HANDLE " + enumdescription.Enum[i].type);
                }
                switch (types[enumdescription.Enum[i].type])
                {
                    case DROPDOWN_TYPE.INT:
                    {
                        ComboBoxItem cbi = new ComboBoxItem {Tag = int.Parse(enumdescription.Enum[i].value), Content = enumdescription.Enum[i].name, ToolTip = new ToolTip {Content = enumdescription.Enum[i].description + " (" + enumdescription.Enum[i].value + ")"}};
                        @enum.Items.Add(cbi);
                        if (i == 0)
                        {
                            @enum.SelectedValue = this.def;
                            dynamic.Subscribe(name, (Action<int>) changed);
                        }
                        else if (enumdescription.Enum[i].type != enumdescription.Enum[i - 1].type)
                            throw new Exception("NO CHANGSIES MINDSIES");
                    }
                        break;
                    case DROPDOWN_TYPE.STR:
                    {
                        ComboBoxItem cbi = new ComboBoxItem {Tag = enumdescription.Enum[i].value, Content = enumdescription.Enum[i].name, ToolTip = new ToolTip {Content = enumdescription.Enum[i].description}};
                        @enum.Items.Add(cbi);
                        if (i == 0)
                        {
                            @enum.SelectedValue = this.def;
                            dynamic.Subscribe(name, (Action<string>) changed);
                        }
                        else if (enumdescription.Enum[i].type != enumdescription.Enum[i - 1].type)
                            throw new Exception("NO CHANGSIES MINDSIES");
                    }
                        break;
                }
            }
            description.Content = name + ":";
            JustTheTip.Content = pd.description.data;
            ignore = false;
        }

        private void changed(string newstate)
        {
            ignore = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                @enum.SelectedValue = newstate;
                if (stringchanged != null)
                    stringchanged(newstate);
                ignore = false;
            }));
        }

        private void changed(int newstate)
        {
            ignore = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                @enum.SelectedValue = newstate;
                if (intchanged != null)
                    intchanged(newstate);
                ignore = false;
            }));
        }


        private void Enum_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignore) return;
            switch (types[enumdescription.Enum[0].type])
            {
                case DROPDOWN_TYPE.INT:
                    dynamic.Set(name, (int) (@enum.Items[@enum.SelectedIndex] as ComboBoxItem).Tag);
                    break;
                case DROPDOWN_TYPE.STR:
                    dynamic.Set(name, (string) (@enum.Items[@enum.SelectedIndex] as ComboBoxItem).Tag);
                    break;
            }
        }


        private event Action<int> intchanged;
        private event Action<string> stringchanged;

        internal Action<int> Instrument(Action<int> cb)
        {
            intchanged += cb;
            return d =>
            {
                foreach (ComboBoxItem i in @enum.Items)
                {
                    if ((int) i.Tag == d)
                    {
                        ignore = false;
                        i.IsSelected = true;
                        break;
                    }
                }
            };
        }

        internal Action<string> Instrument(Action<string> cb)
        {
            stringchanged += cb;
            return d =>
            {
                foreach (ComboBoxItem i in @enum.Items)
                {
                    if ((string) i.Tag == d)
                    {
                        ignore = false;
                        i.IsSelected = true;
                        break;
                    }
                }
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