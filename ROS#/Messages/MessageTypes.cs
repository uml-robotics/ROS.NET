using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Messages;
using Messages.std_msgs;
using Messages.rosgraph_msgs;
using Messages.custom_msgs;
using Messages.geometry_msgs;
using Messages.nav_msgs;
using String=Messages.std_msgs.String;

namespace Messages
{
	public static class TypeHelper
	{
		public static System.Type GetType(string name)
		{
			return System.Type.GetType(name, true, true);
		}

		public static Dictionary<MsgTypes, TypeInfo> TypeInformation = new Dictionary<MsgTypes, TypeInfo>()
		{			{MsgTypes.custom_msgs__arraytest, new TypeInfo(typeof(TypedMessage<custom_msgs.arraytest>), false, false,
@"int32[2] integers
int32[] lengthlessintegers
string teststring
string[2] teststringarray
string[] teststringarraylengthless",
				 new Dictionary<string, MsgFieldInfo>{
					{"integers", new MsgFieldInfo("integers", true, typeof(int), false, "", true, "2", false)},
					{"lengthlessintegers", new MsgFieldInfo("lengthlessintegers", true, typeof(int), false, "", true, "", false)},
					{"teststring", new MsgFieldInfo("teststring", true, typeof(String), false, "", false, "", false)},
					{"teststringarray", new MsgFieldInfo("teststringarray", true, typeof(String), false, "", true, "2", false)},
					{"teststringarraylengthless", new MsgFieldInfo("teststringarraylengthless", true, typeof(String), false, "", true, "", false)}
			})},
			{MsgTypes.custom_msgs__simpleintarray, new TypeInfo(typeof(TypedMessage<custom_msgs.simpleintarray>), false, false,
@"int16[3] knownlengtharray
int16[] unknownlengtharray",
				 new Dictionary<string, MsgFieldInfo>{
					{"knownlengtharray", new MsgFieldInfo("knownlengtharray", true, typeof(short), false, "", true, "3", false)},
					{"unknownlengtharray", new MsgFieldInfo("unknownlengtharray", true, typeof(short), false, "", true, "", false)}
			})},
			{MsgTypes.geometry_msgs__Point, new TypeInfo(typeof(TypedMessage<geometry_msgs.Point>), false, false,
@"float64 x
float64 y
float64 z",
				 new Dictionary<string, MsgFieldInfo>{
					{"x", new MsgFieldInfo("x", true, typeof(double), false, "", false, "", false)},
					{"y", new MsgFieldInfo("y", true, typeof(double), false, "", false, "", false)},
					{"z", new MsgFieldInfo("z", true, typeof(double), false, "", false, "", false)}
			})},
			{MsgTypes.geometry_msgs__Point32, new TypeInfo(typeof(TypedMessage<geometry_msgs.Point32>), false, false,
@"float32 x
float32 y
float32 z",
				 new Dictionary<string, MsgFieldInfo>{
					{"x", new MsgFieldInfo("x", true, typeof(float), false, "", false, "", false)},
					{"y", new MsgFieldInfo("y", true, typeof(float), false, "", false, "", false)},
					{"z", new MsgFieldInfo("z", true, typeof(float), false, "", false, "", false)}
			})},
			{MsgTypes.geometry_msgs__PointStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.PointStamped>), true, true,
@"Header header
Point point",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"point", new MsgFieldInfo("point", false, typeof(TypedMessage<Point>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Polygon, new TypeInfo(typeof(TypedMessage<geometry_msgs.Polygon>), false, true,
@"geometry_msgs/Point32[] points",
				 new Dictionary<string, MsgFieldInfo>{
					{"points", new MsgFieldInfo("points", false, typeof(TypedMessage<Point32>), false, "", true, "", true)}
			})},
			{MsgTypes.geometry_msgs__PolygonStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.PolygonStamped>), true, true,
@"Header header
Polygon polygon",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"polygon", new MsgFieldInfo("polygon", false, typeof(TypedMessage<Polygon>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Pose, new TypeInfo(typeof(TypedMessage<geometry_msgs.Pose>), false, true,
@"Point position
Quaternion orientation",
				 new Dictionary<string, MsgFieldInfo>{
					{"position", new MsgFieldInfo("position", false, typeof(TypedMessage<Point>), false, "", false, "", true)},
					{"orientation", new MsgFieldInfo("orientation", false, typeof(TypedMessage<Quaternion>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Pose2D, new TypeInfo(typeof(TypedMessage<geometry_msgs.Pose2D>), false, false,
@"float64 x
float64 y
float64 theta",
				 new Dictionary<string, MsgFieldInfo>{
					{"x", new MsgFieldInfo("x", true, typeof(double), false, "", false, "", false)},
					{"y", new MsgFieldInfo("y", true, typeof(double), false, "", false, "", false)},
					{"theta", new MsgFieldInfo("theta", true, typeof(double), false, "", false, "", false)}
			})},
			{MsgTypes.geometry_msgs__PoseArray, new TypeInfo(typeof(TypedMessage<geometry_msgs.PoseArray>), true, true,
@"Header header
geometry_msgs/Pose[] poses",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"poses", new MsgFieldInfo("poses", false, typeof(TypedMessage<Pose>), false, "", true, "", true)}
			})},
			{MsgTypes.geometry_msgs__PoseStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.PoseStamped>), true, true,
@"Header header
Pose pose",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"pose", new MsgFieldInfo("pose", false, typeof(TypedMessage<Pose>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__PoseWithCovariance, new TypeInfo(typeof(TypedMessage<geometry_msgs.PoseWithCovariance>), false, true,
@"Pose pose
float64[36] covariance",
				 new Dictionary<string, MsgFieldInfo>{
					{"pose", new MsgFieldInfo("pose", false, typeof(TypedMessage<Pose>), false, "", false, "", true)},
					{"covariance", new MsgFieldInfo("covariance", true, typeof(double), false, "", true, "36", false)}
			})},
			{MsgTypes.geometry_msgs__PoseWithCovarianceStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.PoseWithCovarianceStamped>), true, true,
@"Header header
PoseWithCovariance pose",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"pose", new MsgFieldInfo("pose", false, typeof(TypedMessage<PoseWithCovariance>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Quaternion, new TypeInfo(typeof(TypedMessage<geometry_msgs.Quaternion>), false, false,
@"float64 x
float64 y
float64 z
float64 w",
				 new Dictionary<string, MsgFieldInfo>{
					{"x", new MsgFieldInfo("x", true, typeof(double), false, "", false, "", false)},
					{"y", new MsgFieldInfo("y", true, typeof(double), false, "", false, "", false)},
					{"z", new MsgFieldInfo("z", true, typeof(double), false, "", false, "", false)},
					{"w", new MsgFieldInfo("w", true, typeof(double), false, "", false, "", false)}
			})},
			{MsgTypes.geometry_msgs__QuaternionStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.QuaternionStamped>), true, true,
@"Header header
Quaternion quaternion",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"quaternion", new MsgFieldInfo("quaternion", false, typeof(TypedMessage<Quaternion>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Transform, new TypeInfo(typeof(TypedMessage<geometry_msgs.Transform>), false, true,
@"Vector3 translation
Quaternion rotation",
				 new Dictionary<string, MsgFieldInfo>{
					{"translation", new MsgFieldInfo("translation", false, typeof(TypedMessage<Vector3>), false, "", false, "", true)},
					{"rotation", new MsgFieldInfo("rotation", false, typeof(TypedMessage<Quaternion>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__TransformStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.TransformStamped>), true, true,
@"Header header
string child_frame_id
Transform transform",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"child_frame_id", new MsgFieldInfo("child_frame_id", true, typeof(String), false, "", false, "", false)},
					{"transform", new MsgFieldInfo("transform", false, typeof(TypedMessage<Transform>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Twist, new TypeInfo(typeof(TypedMessage<geometry_msgs.Twist>), false, true,
@"Vector3  linear
Vector3  angular",
				 new Dictionary<string, MsgFieldInfo>{
					{"linear", new MsgFieldInfo("linear", false, typeof(TypedMessage<Vector3>), false, "", false, "", true)},
					{"angular", new MsgFieldInfo("angular", false, typeof(TypedMessage<Vector3>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__TwistStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.TwistStamped>), true, true,
@"Header header
Twist twist",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"twist", new MsgFieldInfo("twist", false, typeof(TypedMessage<Twist>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__TwistWithCovariance, new TypeInfo(typeof(TypedMessage<geometry_msgs.TwistWithCovariance>), false, true,
@"Twist twist
float64[36] covariance",
				 new Dictionary<string, MsgFieldInfo>{
					{"twist", new MsgFieldInfo("twist", false, typeof(TypedMessage<Twist>), false, "", false, "", true)},
					{"covariance", new MsgFieldInfo("covariance", true, typeof(double), false, "", true, "36", false)}
			})},
			{MsgTypes.geometry_msgs__TwistWithCovarianceStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.TwistWithCovarianceStamped>), true, true,
@"Header header
TwistWithCovariance twist",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"twist", new MsgFieldInfo("twist", false, typeof(TypedMessage<TwistWithCovariance>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Vector3, new TypeInfo(typeof(TypedMessage<geometry_msgs.Vector3>), false, false,
@"float64 x
float64 y
float64 z",
				 new Dictionary<string, MsgFieldInfo>{
					{"x", new MsgFieldInfo("x", true, typeof(double), false, "", false, "", false)},
					{"y", new MsgFieldInfo("y", true, typeof(double), false, "", false, "", false)},
					{"z", new MsgFieldInfo("z", true, typeof(double), false, "", false, "", false)}
			})},
			{MsgTypes.geometry_msgs__Vector3Stamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.Vector3Stamped>), true, true,
@"Header header
Vector3 vector",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"vector", new MsgFieldInfo("vector", false, typeof(TypedMessage<Vector3>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__Wrench, new TypeInfo(typeof(TypedMessage<geometry_msgs.Wrench>), false, true,
@"Vector3  force
Vector3  torque",
				 new Dictionary<string, MsgFieldInfo>{
					{"force", new MsgFieldInfo("force", false, typeof(TypedMessage<Vector3>), false, "", false, "", true)},
					{"torque", new MsgFieldInfo("torque", false, typeof(TypedMessage<Vector3>), false, "", false, "", true)}
			})},
			{MsgTypes.geometry_msgs__WrenchStamped, new TypeInfo(typeof(TypedMessage<geometry_msgs.WrenchStamped>), true, true,
@"Header header
Wrench wrench",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"wrench", new MsgFieldInfo("wrench", false, typeof(TypedMessage<Wrench>), false, "", false, "", true)}
			})},
			{MsgTypes.nav_msgs__GridCells, new TypeInfo(typeof(TypedMessage<nav_msgs.GridCells>), true, true,
@"Header header
float32 cell_width
float32 cell_height
geometry_msgs/Point[] cells",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"cell_width", new MsgFieldInfo("cell_width", true, typeof(float), false, "", false, "", false)},
					{"cell_height", new MsgFieldInfo("cell_height", true, typeof(float), false, "", false, "", false)},
					{"cells", new MsgFieldInfo("cells", false, typeof(TypedMessage<Point>), false, "", true, "", true)}
			})},
			{MsgTypes.nav_msgs__MapMetaData, new TypeInfo(typeof(TypedMessage<nav_msgs.MapMetaData>), false, true,
@"time map_load_time
float32 resolution
uint32 width
uint32 height
geometry_msgs/Pose origin",
				 new Dictionary<string, MsgFieldInfo>{
					{"map_load_time", new MsgFieldInfo("map_load_time", true, typeof(Time), false, "", false, "", false)},
					{"resolution", new MsgFieldInfo("resolution", true, typeof(float), false, "", false, "", false)},
					{"width", new MsgFieldInfo("width", true, typeof(uint), false, "", false, "", false)},
					{"height", new MsgFieldInfo("height", true, typeof(uint), false, "", false, "", false)},
					{"origin", new MsgFieldInfo("origin", false, typeof(TypedMessage<Pose>), false, "", false, "", true)}
			})},
			{MsgTypes.nav_msgs__OccupancyGrid, new TypeInfo(typeof(TypedMessage<nav_msgs.OccupancyGrid>), true, true,
@"Header header
MapMetaData info
int8[] data",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"info", new MsgFieldInfo("info", false, typeof(TypedMessage<MapMetaData>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(sbyte), false, "", true, "", false)}
			})},
			{MsgTypes.nav_msgs__Odometry, new TypeInfo(typeof(TypedMessage<nav_msgs.Odometry>), true, true,
@"Header header
string child_frame_id
geometry_msgs/PoseWithCovariance pose
geometry_msgs/TwistWithCovariance twist",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"child_frame_id", new MsgFieldInfo("child_frame_id", true, typeof(String), false, "", false, "", false)},
					{"pose", new MsgFieldInfo("pose", false, typeof(TypedMessage<PoseWithCovariance>), false, "", false, "", true)},
					{"twist", new MsgFieldInfo("twist", false, typeof(TypedMessage<TwistWithCovariance>), false, "", false, "", true)}
			})},
			{MsgTypes.nav_msgs__Path, new TypeInfo(typeof(TypedMessage<nav_msgs.Path>), true, true,
@"Header header
geometry_msgs/PoseStamped[] poses",
				 new Dictionary<string, MsgFieldInfo>{
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"poses", new MsgFieldInfo("poses", false, typeof(TypedMessage<PoseStamped>), false, "", true, "", true)}
			})},
			{MsgTypes.rosgraph_msgs__Log, new TypeInfo(typeof(TypedMessage<rosgraph_msgs.Log>), true, true,
@"byte DEBUG=1
byte INFO=2
byte WARN=4
byte ERROR=8
byte FATAL=16
Header header
byte level
string name
string msg
string file
string function
uint32 line
string[] topics",
				 new Dictionary<string, MsgFieldInfo>{
					{"DEBUG", new MsgFieldInfo("DEBUG", true, typeof(byte), true, "1", false, "", false)},
					{"INFO", new MsgFieldInfo("INFO", true, typeof(byte), true, "2", false, "", false)},
					{"WARN", new MsgFieldInfo("WARN", true, typeof(byte), true, "4", false, "", false)},
					{"ERROR", new MsgFieldInfo("ERROR", true, typeof(byte), true, "8", false, "", false)},
					{"FATAL", new MsgFieldInfo("FATAL", true, typeof(byte), true, "16", false, "", false)},
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"level", new MsgFieldInfo("level", true, typeof(byte), false, "", false, "", false)},
					{"name", new MsgFieldInfo("name", true, typeof(String), false, "", false, "", false)},
					{"msg", new MsgFieldInfo("msg", true, typeof(String), false, "", false, "", false)},
					{"file", new MsgFieldInfo("file", true, typeof(String), false, "", false, "", false)},
					{"function", new MsgFieldInfo("function", true, typeof(String), false, "", false, "", false)},
					{"line", new MsgFieldInfo("line", true, typeof(uint), false, "", false, "", false)},
					{"topics", new MsgFieldInfo("topics", true, typeof(String), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Bool, new TypeInfo(typeof(TypedMessage<std_msgs.Bool>), false, false,
@"bool data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(bool), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Byte, new TypeInfo(typeof(TypedMessage<std_msgs.Byte>), false, false,
@"byte data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(byte), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__ByteMultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.ByteMultiArray>), false, true,
@"MultiArrayLayout  layout
byte[]            data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(byte), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Char, new TypeInfo(typeof(TypedMessage<std_msgs.Char>), false, false,
@"char data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(char), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__ColorRGBA, new TypeInfo(typeof(TypedMessage<std_msgs.ColorRGBA>), false, false,
@"float32 r
float32 g
float32 b
float32 a",
				 new Dictionary<string, MsgFieldInfo>{
					{"r", new MsgFieldInfo("r", true, typeof(float), false, "", false, "", false)},
					{"g", new MsgFieldInfo("g", true, typeof(float), false, "", false, "", false)},
					{"b", new MsgFieldInfo("b", true, typeof(float), false, "", false, "", false)},
					{"a", new MsgFieldInfo("a", true, typeof(float), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__ConnectionHeader, new TypeInfo(typeof(TypedMessage<std_msgs.ConnectionHeader>), true, true,
@"byte DEBUG=1
byte INFO=2
byte WARN=4
byte ERROR=8
byte FATAL=16
Header header
byte level
string name
string msg
string file
string function
uint32 line
string[] topics",
				 new Dictionary<string, MsgFieldInfo>{
					{"DEBUG", new MsgFieldInfo("DEBUG", true, typeof(byte), true, "1", false, "", false)},
					{"INFO", new MsgFieldInfo("INFO", true, typeof(byte), true, "2", false, "", false)},
					{"WARN", new MsgFieldInfo("WARN", true, typeof(byte), true, "4", false, "", false)},
					{"ERROR", new MsgFieldInfo("ERROR", true, typeof(byte), true, "8", false, "", false)},
					{"FATAL", new MsgFieldInfo("FATAL", true, typeof(byte), true, "16", false, "", false)},
					{"header", new MsgFieldInfo("header", false, typeof(TypedMessage<Header>), false, "", false, "", true)},
					{"level", new MsgFieldInfo("level", true, typeof(byte), false, "", false, "", false)},
					{"name", new MsgFieldInfo("name", true, typeof(String), false, "", false, "", false)},
					{"msg", new MsgFieldInfo("msg", true, typeof(String), false, "", false, "", false)},
					{"file", new MsgFieldInfo("file", true, typeof(String), false, "", false, "", false)},
					{"function", new MsgFieldInfo("function", true, typeof(String), false, "", false, "", false)},
					{"line", new MsgFieldInfo("line", true, typeof(uint), false, "", false, "", false)},
					{"topics", new MsgFieldInfo("topics", true, typeof(String), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Duration, new TypeInfo(typeof(TypedMessage<std_msgs.Duration>), false, false,
@"duration data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(TimeData), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Empty, new TypeInfo(typeof(TypedMessage<std_msgs.Empty>), false, false,
@"",
				 new Dictionary<string, MsgFieldInfo>{

			})},
			{MsgTypes.std_msgs__Float32, new TypeInfo(typeof(TypedMessage<std_msgs.Float32>), false, false,
@"float32 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(float), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Float32MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.Float32MultiArray>), false, true,
@"MultiArrayLayout  layout
float32[]         data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(float), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Float64, new TypeInfo(typeof(TypedMessage<std_msgs.Float64>), false, false,
@"float64 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(double), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Float64MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.Float64MultiArray>), false, true,
@"MultiArrayLayout  layout
float64[]         data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(double), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Header, new TypeInfo(typeof(TypedMessage<std_msgs.Header>), false, false,
@"uint32 seq
time stamp
string frame_id",
				 new Dictionary<string, MsgFieldInfo>{
					{"seq", new MsgFieldInfo("seq", true, typeof(uint), false, "", false, "", false)},
					{"stamp", new MsgFieldInfo("stamp", true, typeof(Time), false, "", false, "", false)},
					{"frame_id", new MsgFieldInfo("frame_id", true, typeof(String), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Int16, new TypeInfo(typeof(TypedMessage<std_msgs.Int16>), false, false,
@"int16 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(short), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Int16MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.Int16MultiArray>), false, true,
@"MultiArrayLayout  layout
int16[]           data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(short), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Int32, new TypeInfo(typeof(TypedMessage<std_msgs.Int32>), false, false,
@"int32 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(int), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Int32MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.Int32MultiArray>), false, true,
@"MultiArrayLayout  layout
int32[]           data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(int), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Int64, new TypeInfo(typeof(TypedMessage<std_msgs.Int64>), false, false,
@"int64 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(long), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Int64MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.Int64MultiArray>), false, true,
@"MultiArrayLayout  layout
int64[]           data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(long), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__Int8, new TypeInfo(typeof(TypedMessage<std_msgs.Int8>), false, false,
@"int8 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(sbyte), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Int8MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.Int8MultiArray>), false, true,
@"MultiArrayLayout  layout
int8[]            data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(sbyte), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__MultiArrayDimension, new TypeInfo(typeof(TypedMessage<std_msgs.MultiArrayDimension>), false, false,
@"string label
uint32 size
uint32 stride",
				 new Dictionary<string, MsgFieldInfo>{
					{"label", new MsgFieldInfo("label", true, typeof(String), false, "", false, "", false)},
					{"size", new MsgFieldInfo("size", true, typeof(uint), false, "", false, "", false)},
					{"stride", new MsgFieldInfo("stride", true, typeof(uint), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__MultiArrayLayout, new TypeInfo(typeof(TypedMessage<std_msgs.MultiArrayLayout>), false, true,
@"MultiArrayDimension[] dim
uint32 data_offset",
				 new Dictionary<string, MsgFieldInfo>{
					{"dim", new MsgFieldInfo("dim", false, typeof(TypedMessage<MultiArrayDimension>), false, "", true, "", true)},
					{"data_offset", new MsgFieldInfo("data_offset", true, typeof(uint), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__String, new TypeInfo(typeof(TypedMessage<std_msgs.String>), false, false,
@"string data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(string), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__Time, new TypeInfo(typeof(TypedMessage<std_msgs.Time>), false, false,
@"time data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(TimeData), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__UInt16, new TypeInfo(typeof(TypedMessage<std_msgs.UInt16>), false, false,
@"uint16 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(ushort), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__UInt16MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.UInt16MultiArray>), false, true,
@"MultiArrayLayout  layout
uint16[]            data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(ushort), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__UInt32, new TypeInfo(typeof(TypedMessage<std_msgs.UInt32>), false, false,
@"uint32 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(uint), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__UInt32MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.UInt32MultiArray>), false, true,
@"MultiArrayLayout  layout
uint32[]          data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(uint), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__UInt64, new TypeInfo(typeof(TypedMessage<std_msgs.UInt64>), false, false,
@"uint64 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(ulong), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__UInt64MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.UInt64MultiArray>), false, true,
@"MultiArrayLayout  layout
uint64[]          data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(ulong), false, "", true, "", false)}
			})},
			{MsgTypes.std_msgs__UInt8, new TypeInfo(typeof(TypedMessage<std_msgs.UInt8>), false, false,
@"uint8 data",
				 new Dictionary<string, MsgFieldInfo>{
					{"data", new MsgFieldInfo("data", true, typeof(byte), false, "", false, "", false)}
			})},
			{MsgTypes.std_msgs__UInt8MultiArray, new TypeInfo(typeof(TypedMessage<std_msgs.UInt8MultiArray>), false, true,
@"MultiArrayLayout  layout
uint8[]           data",
				 new Dictionary<string, MsgFieldInfo>{
					{"layout", new MsgFieldInfo("layout", false, typeof(TypedMessage<MultiArrayLayout>), false, "", false, "", true)},
					{"data", new MsgFieldInfo("data", true, typeof(byte), false, "", true, "", false)}
			})}
		};	}

	public enum MsgTypes
	{
		Unknown,
		custom_msgs__arraytest,
		custom_msgs__simpleintarray,
		geometry_msgs__Point,
		geometry_msgs__Point32,
		geometry_msgs__PointStamped,
		geometry_msgs__Polygon,
		geometry_msgs__PolygonStamped,
		geometry_msgs__Pose,
		geometry_msgs__Pose2D,
		geometry_msgs__PoseArray,
		geometry_msgs__PoseStamped,
		geometry_msgs__PoseWithCovariance,
		geometry_msgs__PoseWithCovarianceStamped,
		geometry_msgs__Quaternion,
		geometry_msgs__QuaternionStamped,
		geometry_msgs__Transform,
		geometry_msgs__TransformStamped,
		geometry_msgs__Twist,
		geometry_msgs__TwistStamped,
		geometry_msgs__TwistWithCovariance,
		geometry_msgs__TwistWithCovarianceStamped,
		geometry_msgs__Vector3,
		geometry_msgs__Vector3Stamped,
		geometry_msgs__Wrench,
		geometry_msgs__WrenchStamped,
		nav_msgs__GridCells,
		nav_msgs__MapMetaData,
		nav_msgs__OccupancyGrid,
		nav_msgs__Odometry,
		nav_msgs__Path,
		rosgraph_msgs__Log,
		std_msgs__Bool,
		std_msgs__Byte,
		std_msgs__ByteMultiArray,
		std_msgs__Char,
		std_msgs__ColorRGBA,
		std_msgs__ConnectionHeader,
		std_msgs__Duration,
		std_msgs__Empty,
		std_msgs__Float32,
		std_msgs__Float32MultiArray,
		std_msgs__Float64,
		std_msgs__Float64MultiArray,
		std_msgs__Header,
		std_msgs__Int16,
		std_msgs__Int16MultiArray,
		std_msgs__Int32,
		std_msgs__Int32MultiArray,
		std_msgs__Int64,
		std_msgs__Int64MultiArray,
		std_msgs__Int8,
		std_msgs__Int8MultiArray,
		std_msgs__MultiArrayDimension,
		std_msgs__MultiArrayLayout,
		std_msgs__String,
		std_msgs__Time,
		std_msgs__UInt16,
		std_msgs__UInt16MultiArray,
		std_msgs__UInt32,
		std_msgs__UInt32MultiArray,
		std_msgs__UInt64,
		std_msgs__UInt64MultiArray,
		std_msgs__UInt8,
		std_msgs__UInt8MultiArray
	}
}
