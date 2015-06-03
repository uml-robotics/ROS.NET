#region USINGZ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;

#endregion

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
    ///     Interaction logic for DynamicReconfigureGroup.xaml
    /// </summary>
    public partial class DynamicReconfigureGroup : UserControl
    {
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

        private int _id;
        private int _parent;
        private Dictionary<string, DynamicReconfigureStringBox> boxes = new Dictionary<string, DynamicReconfigureStringBox>();
        private Dictionary<string, DynamicReconfigureCheckbox> checkboxes = new Dictionary<string, DynamicReconfigureCheckbox>();
        private Config def;
        private Dictionary<string, DynamicReconfigureStringDropdown> dropdowns = new Dictionary<string, DynamicReconfigureStringDropdown>();
        private DynamicReconfigureInterface dynamic;
        private Group group;
        private Config max;
        private Config min;
        private string name;
        private Dictionary<string, DynamicReconfigureSlider> sliders = new Dictionary<string, DynamicReconfigureSlider>();

        public DynamicReconfigureGroup()
        {
            InitializeComponent();
        }

        public DynamicReconfigureGroup(Group g, Config def, Config min, Config max, string name, DynamicReconfigureInterface dynamic)
            : this()
        {
            this.dynamic = dynamic;
            this.name = name + ":";
            this.min = min;
            this.max = max;
            this.def = def;
            group = g;
            //container.Header = g.name.data;
            _id = g.id;
            _parent = g.parent;
            Loaded += (sender, args) =>
            {
                paramsHolder.Children.Clear();
                dropdowns.Clear();
                checkboxes.Clear();
                sliders.Clear();
                boxes.Clear();

                foreach (ParamDescription s in g.parameters)
                {
                    string _name = s.name.data;
                    switch (TYPE_DICT[s.type.data])
                    {
                        case DYN_RECFG_TYPE.type_bool:
                        {
                            var pdef = def.bools.FirstOrDefault(p => p.name.data == s.name.data);
                            var pmax = max.bools.FirstOrDefault(p => p.name.data == s.name.data);
                            var pmin = min.bools.FirstOrDefault(p => p.name.data == s.name.data);
                            if (s.edit_method.data.Contains("enum_description"))
                            {
                                var d = new DynamicReconfigureStringDropdown(dynamic, s, pdef.value, pmax.value, pmin.value, s.edit_method.data);
                                paramsHolder.Children.Add(d);
                                dropdowns.Add(s.name.data, d);
                                d.Instrument(S =>
                                {
                                    if (IntChanged != null)
                                        IntChanged(_name, S);
                                });
                            }
                            else
                            {
                                var d = new DynamicReconfigureCheckbox(dynamic, s, pdef.value);
                                paramsHolder.Children.Add(d);
                                checkboxes.Add(s.name.data, d);
                                d.Instrument(S =>
                                {
                                    if (BoolChanged != null)
                                        BoolChanged(_name, S);
                                });
                            }
                        }
                            break;
                        case DYN_RECFG_TYPE.type_double:
                        {
                            var pdef = def.doubles.FirstOrDefault(p => p.name.data == s.name.data);
                            var pmax = max.doubles.FirstOrDefault(p => p.name.data == s.name.data);
                            var pmin = min.doubles.FirstOrDefault(p => p.name.data == s.name.data);
                            if (s.edit_method.data.Contains("enum_description"))
                            {
                                var d = new DynamicReconfigureStringDropdown(dynamic, s, pdef.value, pmax.value, pmin.value, s.edit_method.data);
                                paramsHolder.Children.Add(d);
                                dropdowns.Add(s.name.data, d);
                                d.Instrument(S =>
                                {
                                    if (IntChanged != null)
                                        IntChanged(_name, S);
                                });
                            }
                            else
                            {
                                var d = new DynamicReconfigureSlider(dynamic, s, pdef.value, pmax.value, pmin.value, true);
                                paramsHolder.Children.Add(d);
                                sliders.Add(s.name.data, d);
                                d.Instrument(new Action<double>(S =>
                                {
                                    if (DoubleChanged != null)
                                        DoubleChanged(_name, S);
                                }));
                            }
                        }
                            break;
                        case DYN_RECFG_TYPE.type_int:
                        {
                            var pmax = max.ints.FirstOrDefault(p => p.name.data == s.name.data);
                            var pmin = min.ints.FirstOrDefault(p => p.name.data == s.name.data);
                            var pdef = def.ints.FirstOrDefault(p => p.name.data == s.name.data);
                            if (s.edit_method.data.Contains("enum_description"))
                            {
                                var d = new DynamicReconfigureStringDropdown(dynamic, s, pdef.value, pmax.value, pmin.value, s.edit_method.data);
                                paramsHolder.Children.Add(d);
                                dropdowns.Add(s.name.data, d);
                                d.Instrument(S =>
                                {
                                    if (IntChanged != null)
                                        IntChanged(_name, S);
                                });
                            }
                            else
                            {
                                var d = new DynamicReconfigureSlider(dynamic, s, pdef.value, pmax.value, pmin.value, false);
                                paramsHolder.Children.Add(d);
                                sliders.Add(s.name.data, d);
                                d.Instrument(S =>
                                {
                                    if (IntChanged != null)
                                        IntChanged(_name, S);
                                });
                            }
                        }
                            break;
                        case DYN_RECFG_TYPE.type_str:
                        {
                            var pdef = def.strs.FirstOrDefault(p => p.name.data == s.name.data);
                            var pmax = max.strs.FirstOrDefault(p => p.name.data == s.name.data);
                            var pmin = min.strs.FirstOrDefault(p => p.name.data == s.name.data);
                            if (s.edit_method.data.Contains("enum_description"))
                            {
                                var d = new DynamicReconfigureStringDropdown(dynamic, s, pdef.value.data, pmax.value.data, pmin.value.data, s.edit_method.data);
                                paramsHolder.Children.Add(d);
                                dropdowns.Add(s.name.data, d);
                                d.Instrument(S =>
                                {
                                    if (IntChanged != null)
                                        IntChanged(_name, S);
                                });
                            }
                            else
                            {
                                var d = new DynamicReconfigureStringBox(dynamic, s, pdef.value.data);
                                paramsHolder.Children.Add(d);
                                boxes.Add(s.name.data, d);
                                d.Instrument(S =>
                                {
                                    if (StringChanged != null)
                                        StringChanged(_name, S);
                                });
                            }
                        }
                            break;
                    }
                }

                SizeChangedEventHandler saeh = null;
                saeh = (o, a)
                    =>
                {
                    double maxwidth = 0;
                    foreach (DynamicReconfigureCheckbox d in checkboxes.Values) GetMaxWidth(d, ref maxwidth);
                    foreach (DynamicReconfigureSlider d in sliders.Values) GetMaxWidth(d, ref maxwidth);
                    foreach (DynamicReconfigureStringBox d in boxes.Values) GetMaxWidth(d, ref maxwidth);
                    foreach (DynamicReconfigureStringDropdown d in dropdowns.Values) GetMaxWidth(d, ref maxwidth);
                    foreach (IDynamicReconfigureLayout idrl in paramsHolder.Children)
                        idrl.setDescriptionWidth(maxwidth);
                    SizeChanged -= saeh;
                };
                SizeChanged += saeh;
            };
        }

        public DynamicReconfigureInterface ParameterInterface
        {
            get { return dynamic; }
        }

        public int id
        {
            get { return _id; }
        }

        public int parent
        {
            get { return _parent; }
        }

        public string[] GetCheckboxNames()
        {
            return checkboxes.Keys.ToArray();
        }

        public string[] GetDropdownNames()
        {
            return dropdowns.Keys.ToArray();
        }

        public string[] GetBoxNames()
        {
            return boxes.Keys.ToArray();
        }

        public string[] GetSliderNames()
        {
            return sliders.Keys.ToArray();
        }

        public string[] GetNames()
        {
            List<string> names = new List<string>();
            names.AddRange(GetCheckboxNames());
            names.AddRange(GetDropdownNames());
            names.AddRange(GetBoxNames());
            names.AddRange(GetSliderNames());
            return names.ToArray();
        }

        public event Action<string, bool> BoolChanged;
        public event Action<string, string> StringChanged;
        public event Action<string, int> IntChanged;
        public event Action<string, double> DoubleChanged;


        private void GetMaxWidth<T>(T t, ref double w) where T : IDynamicReconfigureLayout
        {
            double W = t.getDescriptionWidth();
            w = W > w ? W : w;
        }

        /// <summary>
        ///     Expose a specific function to call to set a specific named-parameter to an outside caller, and allow them to
        ///     receive a callback when it is changed by somebody else.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="cb">A callback the caller wants called when the named parameter changes</param>
        /// <returns>An action the caller can invoke to change the parameter</returns>
        public Action<double> Instrument(string name, Action<double> cb)
        {
            if (sliders.ContainsKey(name))
            {
                return sliders[name].Instrument(cb);
            }
            throw new Exception("There is no DynamicReconfigure control named " + name + " that controls a parameter that is a double");
        }

        /// <summary>
        ///     Expose a specific function to call to set a specific named-parameter to an outside caller, and allow them to
        ///     receive a callback when it is changed by somebody else.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="cb">A callback the caller wants called when the named parameter changes</param>
        /// <returns>An action the caller can invoke to change the parameter</returns>
        public Action<int> Instrument(string name, Action<int> cb)
        {
            if (sliders.ContainsKey(name))
            {
                return sliders[name].Instrument(cb);
            }
            if (dropdowns.ContainsKey(name))
            {
                return dropdowns[name].Instrument(cb);
            }
            throw new Exception("There is no DynamicReconfigure control named " + name + " that controls a parameter that is an int");
        }

        /// <summary>
        ///     Expose a specific function to call to set a specific named-parameter to an outside caller, and allow them to
        ///     receive a callback when it is changed by somebody else.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="cb">A callback the caller wants called when the named parameter changes</param>
        /// <returns>An action the caller can invoke to change the parameter</returns>
        public Action<bool> Instrument(string name, Action<bool> cb)
        {
            if (checkboxes.ContainsKey(name))
            {
                return checkboxes[name].Instrument(cb);
            }
            throw new Exception("There is no DynamicReconfigure control named " + name + " that controls a parameter that is a bool");
        }

        /// <summary>
        ///     Expose a specific function to call to set a specific named-parameter to an outside caller, and allow them to
        ///     receive a callback when it is changed by somebody else.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="cb">A callback the caller wants called when the named parameter changes</param>
        /// <returns>An action the caller can invoke to change the parameter</returns>
        public Action<string> Instrument(string name, Action<string> cb)
        {
            if (dropdowns.ContainsKey(name))
            {
                return dropdowns[name].Instrument(cb);
            }
            if (boxes.ContainsKey(name))
            {
                return boxes[name].Instrument(cb);
            }
            throw new Exception("There is no DynamicReconfigure control named " + name + " that controls a parameter that is a string");
        }
    }
}