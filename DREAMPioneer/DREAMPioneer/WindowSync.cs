#region USINGZ

using GenericTypes_Surface_Adapter;
using Messages.whosonfirst;
using SpeechLib;
using System.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;
#if SURFACEWINDOW
using GenericTypes_Surface_Adapter;
using Microsoft.Surface;
using Microsoft.Surface.Core;
using ContactEventArgs = Microsoft.Surface.Core.ContactEventArgs;
using Contact = Microsoft.Surface.Core.Contact;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
#else
using EM3MTouchLib;
#endif
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DREAMController;
using GenericTouchTypes;
using System.IO;
using System.Threading;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using d = System.Drawing;
using Touch = GenericTouchTypes.Touch;
using cm = Messages.custom_msgs;
using tf = Messages.tf;
using System.Text;
using System.ComponentModel;
using otherTimer = System.Timers;

#endregion
namespace DREAMPioneer
{
    public class WindowSync
    {
    }
}
