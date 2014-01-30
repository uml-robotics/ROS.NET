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
using Ros_CSharp;

namespace DynamicReconfigureSharp
{
    public enum DYN_RECFG_TYPE
    {
        type_bool,
        type_str,
        type_int,
        type_double
    }

    /// <summary>
    /// Interaction logic for DynamicReconfigureGroup.xaml
    /// </summary>
    public partial class DynamicReconfigureGroup : UserControl
    {
        private string name;
        private Group group;
        private int _id;
        private int _parent;
        private DynamicReconfigureInterface dynamic;

        public int id
        {
            get { return _id; }
        }

        public int parent
        {
            get { return _parent; }
        }

        public DynamicReconfigureGroup()
        {
            InitializeComponent();
        }

        private Dictionary<string, int> minint = new Dictionary<string, int>();
        private Dictionary<string, int> maxint = new Dictionary<string, int>();
        private Dictionary<string, int> defint = new Dictionary<string, int>();
        private Dictionary<string, bool> minbool = new Dictionary<string, bool>();
        private Dictionary<string, bool> maxbool = new Dictionary<string, bool>();
        private Dictionary<string, bool> defbool = new Dictionary<string, bool>();
        private Dictionary<string, string> minstring = new Dictionary<string, string>();
        private Dictionary<string, string> maxstring = new Dictionary<string, string>();
        private Dictionary<string, string> defstring = new Dictionary<string, string>();
        private Dictionary<string, double> mindouble = new Dictionary<string, double>();
        private Dictionary<string, double> maxdouble = new Dictionary<string, double>();
        private Dictionary<string, double> defdouble = new Dictionary<string, double>();

        private static readonly Dictionary<string, DYN_RECFG_TYPE> TYPE_DICT = new Dictionary<string, DYN_RECFG_TYPE>
        {
            {
                "bool",
                DYN_RECFG_TYPE.type_bool
            }
            ,
            {
                "str",
                DYN_RECFG_TYPE.type_str
            }
            ,
            {
                "int",
                DYN_RECFG_TYPE.type_int
            }
            ,
            {
                "double",
                DYN_RECFG_TYPE.type_double
            }
        };

        private Config def, min, max;
        public DynamicReconfigureGroup(Group g, Config def, Config min, Config max, string name, DynamicReconfigureInterface dynamic)
            : this()
        {
            this.dynamic = dynamic;
            this.name = name;
            this.min = min;
            this.max = max;
            this.def = def;
            group = g;
            container.Header = g.name.data;
            _id = g.id;
            _parent = g.parent;
            foreach (ParamDescription s in g.parameters)
            {
                switch (TYPE_DICT[s.type.data])
                {
                    case DYN_RECFG_TYPE.type_bool:
                        HandleBool(s.name.data);
                        paramsHolder.Children.Add(new DynamicReconfigureCheckbox(dynamic, s, defbool[s.name.data]));
                    break;
                    case DYN_RECFG_TYPE.type_double:
                        HandleDouble(s.name.data);
                        paramsHolder.Children.Add(new DynamicReconfigureSlider(dynamic, s, defdouble[s.name.data], maxdouble[s.name.data], mindouble[s.name.data], true));
                    break;
                    case DYN_RECFG_TYPE.type_int:
                        HandleInt(s.name.data);
                        paramsHolder.Children.Add(new DynamicReconfigureSlider(dynamic, s, defint[s.name.data], maxint[s.name.data], minint[s.name.data], false));
                    break;
                    case DYN_RECFG_TYPE.type_str:
                        HandleString(s.name.data);
                        if (s.edit_method.data.Contains("enum_description"))
                            paramsHolder.Children.Add(new DynamicReconfigureStringDropdown(dynamic, s, defstring[s.name.data], maxstring[s.name.data], minstring[s.name.data], s.edit_method.data));
                        else
                            paramsHolder.Children.Add(new DynamicReconfigureStringBox(dynamic, s, defstring[s.name.data]));
                    break;
                }
            }
        }

        private void HandleInt(string n)
        {
            var pmax = max.ints.FirstOrDefault((p) => p.name.data == n);
            var pmin = min.ints.FirstOrDefault((p) => p.name.data == n);
            var pdef = def.ints.FirstOrDefault((p) => p.name.data == n);
            if (pmax != null) maxint[n] = pmax.value; else maxint[n] = int.MaxValue;
            if (pmin != null) minint[n] = pmin.value; else minint[n] = int.MinValue;
            if (pdef != null) defint[n] = pdef.value; else defint[n] = (minint[n] + (maxint[n] - minint[n]) / 2);
        }
        private void HandleBool(string n)
        {
            var pmax = max.bools.FirstOrDefault((p) => p.name.data == n);
            var pmin = min.bools.FirstOrDefault((p) => p.name.data == n);
            var pdef = def.bools.FirstOrDefault((p) => p.name.data == n);
            if (pmax != null) maxbool[n] = pmax.value; else maxbool[n] = true;
            if (pmin != null) minbool[n] = pmin.value; else minbool[n] = false;
            if (pdef != null) defbool[n] = pdef.value; else defbool[n] = false;
        }
        private void HandleString(string n)
        {
            var pmax = max.strs.FirstOrDefault((p) => p.name.data == n);
            var pmin = min.strs.FirstOrDefault((p) => p.name.data == n);
            var pdef = def.strs.FirstOrDefault((p) => p.name.data == n);
            if (pmax != null) maxstring[n] = pmax.value.data;
            if (pmin != null) minstring[n] = pmin.value.data;
            if (pdef != null) defstring[n] = pdef.value.data;
        }
        private void HandleDouble(string n)
        {
            var pmax = max.doubles.FirstOrDefault((p) => p.name.data == n);
            var pdef = def.doubles.FirstOrDefault((p) => p.name.data == n);
            var pmin = min.doubles.FirstOrDefault((p) => p.name.data == n);
            if (pmax != null) maxdouble[n] = pmax.value; else maxdouble[n] = double.MaxValue;
            if (pmin != null) mindouble[n] = pmin.value; else mindouble[n] = double.MinValue;
            if (pdef != null) defdouble[n] = pdef.value; else defdouble[n] = (mindouble[n] + (maxdouble[n] - mindouble[n])/2d);
        }
    }
}
