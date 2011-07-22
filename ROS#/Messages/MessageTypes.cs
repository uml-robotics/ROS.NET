using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Messages.std_msgs;
using Messages.geometry_msgs;
using Messages.nav_msgs;

namespace Messages
{
	public static class TypeHelper
	{
		public static Dictionary<MsgTypes, Type> Types = new Dictionary<MsgTypes, Type>()
		{
			{MsgTypes.Unknown, null},
			{MsgTypes.geometry_msgs__Point, typeof(TypedMessage<geometry_msgs.Point>)},
			{MsgTypes.geometry_msgs__Point32, typeof(TypedMessage<geometry_msgs.Point32>)},
			{MsgTypes.geometry_msgs__PointStamped, typeof(TypedMessage<geometry_msgs.PointStamped>)},
			{MsgTypes.geometry_msgs__Polygon, typeof(TypedMessage<geometry_msgs.Polygon>)},
			{MsgTypes.geometry_msgs__PolygonStamped, typeof(TypedMessage<geometry_msgs.PolygonStamped>)},
			{MsgTypes.geometry_msgs__Pose, typeof(TypedMessage<geometry_msgs.Pose>)},
			{MsgTypes.geometry_msgs__Pose2D, typeof(TypedMessage<geometry_msgs.Pose2D>)},
			{MsgTypes.geometry_msgs__PoseArray, typeof(TypedMessage<geometry_msgs.PoseArray>)},
			{MsgTypes.geometry_msgs__PoseStamped, typeof(TypedMessage<geometry_msgs.PoseStamped>)},
			{MsgTypes.geometry_msgs__PoseWithCovariance, typeof(TypedMessage<geometry_msgs.PoseWithCovariance>)},
			{MsgTypes.geometry_msgs__PoseWithCovarianceStamped, typeof(TypedMessage<geometry_msgs.PoseWithCovarianceStamped>)},
			{MsgTypes.geometry_msgs__Quaternion, typeof(TypedMessage<geometry_msgs.Quaternion>)},
			{MsgTypes.geometry_msgs__QuaternionStamped, typeof(TypedMessage<geometry_msgs.QuaternionStamped>)},
			{MsgTypes.geometry_msgs__Transform, typeof(TypedMessage<geometry_msgs.Transform>)},
			{MsgTypes.geometry_msgs__TransformStamped, typeof(TypedMessage<geometry_msgs.TransformStamped>)},
			{MsgTypes.geometry_msgs__Twist, typeof(TypedMessage<geometry_msgs.Twist>)},
			{MsgTypes.geometry_msgs__TwistStamped, typeof(TypedMessage<geometry_msgs.TwistStamped>)},
			{MsgTypes.geometry_msgs__TwistWithCovariance, typeof(TypedMessage<geometry_msgs.TwistWithCovariance>)},
			{MsgTypes.geometry_msgs__TwistWithCovarianceStamped, typeof(TypedMessage<geometry_msgs.TwistWithCovarianceStamped>)},
			{MsgTypes.geometry_msgs__Vector3, typeof(TypedMessage<geometry_msgs.Vector3>)},
			{MsgTypes.geometry_msgs__Vector3Stamped, typeof(TypedMessage<geometry_msgs.Vector3Stamped>)},
			{MsgTypes.geometry_msgs__Wrench, typeof(TypedMessage<geometry_msgs.Wrench>)},
			{MsgTypes.geometry_msgs__WrenchStamped, typeof(TypedMessage<geometry_msgs.WrenchStamped>)},
			{MsgTypes.nav_msgs__GridCells, typeof(TypedMessage<nav_msgs.GridCells>)},
			{MsgTypes.nav_msgs__MapMetaData, typeof(TypedMessage<nav_msgs.MapMetaData>)},
			{MsgTypes.nav_msgs__OccupancyGrid, typeof(TypedMessage<nav_msgs.OccupancyGrid>)},
			{MsgTypes.nav_msgs__Odometry, typeof(TypedMessage<nav_msgs.Odometry>)},
			{MsgTypes.nav_msgs__Path, typeof(TypedMessage<nav_msgs.Path>)},
			{MsgTypes.rosgraph_msgs__Log, typeof(TypedMessage<rosgraph_msgs.Log>)},
			{MsgTypes.std_msgs__Bool, typeof(TypedMessage<std_msgs.Bool>)},
			{MsgTypes.std_msgs__Byte, typeof(TypedMessage<std_msgs.Byte>)},
			{MsgTypes.std_msgs__ByteMultiArray, typeof(TypedMessage<std_msgs.ByteMultiArray>)},
			{MsgTypes.std_msgs__Char, typeof(TypedMessage<std_msgs.Char>)},
			{MsgTypes.std_msgs__ColorRGBA, typeof(TypedMessage<std_msgs.ColorRGBA>)},
			{MsgTypes.std_msgs__ConnectionHeader, typeof(TypedMessage<std_msgs.ConnectionHeader>)},
			{MsgTypes.std_msgs__Duration, typeof(TypedMessage<std_msgs.Duration>)},
			{MsgTypes.std_msgs__Empty, typeof(TypedMessage<std_msgs.Empty>)},
			{MsgTypes.std_msgs__Float32, typeof(TypedMessage<std_msgs.Float32>)},
			{MsgTypes.std_msgs__Float32MultiArray, typeof(TypedMessage<std_msgs.Float32MultiArray>)},
			{MsgTypes.std_msgs__Float64, typeof(TypedMessage<std_msgs.Float64>)},
			{MsgTypes.std_msgs__Float64MultiArray, typeof(TypedMessage<std_msgs.Float64MultiArray>)},
			{MsgTypes.std_msgs__Header, typeof(TypedMessage<std_msgs.Header>)},
			{MsgTypes.std_msgs__Int16, typeof(TypedMessage<std_msgs.Int16>)},
			{MsgTypes.std_msgs__Int16MultiArray, typeof(TypedMessage<std_msgs.Int16MultiArray>)},
			{MsgTypes.std_msgs__Int32, typeof(TypedMessage<std_msgs.Int32>)},
			{MsgTypes.std_msgs__Int32MultiArray, typeof(TypedMessage<std_msgs.Int32MultiArray>)},
			{MsgTypes.std_msgs__Int64, typeof(TypedMessage<std_msgs.Int64>)},
			{MsgTypes.std_msgs__Int64MultiArray, typeof(TypedMessage<std_msgs.Int64MultiArray>)},
			{MsgTypes.std_msgs__Int8, typeof(TypedMessage<std_msgs.Int8>)},
			{MsgTypes.std_msgs__Int8MultiArray, typeof(TypedMessage<std_msgs.Int8MultiArray>)},
			{MsgTypes.std_msgs__MultiArrayDimension, typeof(TypedMessage<std_msgs.MultiArrayDimension>)},
			{MsgTypes.std_msgs__MultiArrayLayout, typeof(TypedMessage<std_msgs.MultiArrayLayout>)},
			{MsgTypes.std_msgs__String, typeof(TypedMessage<std_msgs.String>)},
			{MsgTypes.std_msgs__Time, typeof(TypedMessage<std_msgs.Time>)},
			{MsgTypes.std_msgs__UInt16, typeof(TypedMessage<std_msgs.UInt16>)},
			{MsgTypes.std_msgs__UInt16MultiArray, typeof(TypedMessage<std_msgs.UInt16MultiArray>)},
			{MsgTypes.std_msgs__UInt32, typeof(TypedMessage<std_msgs.UInt32>)},
			{MsgTypes.std_msgs__UInt32MultiArray, typeof(TypedMessage<std_msgs.UInt32MultiArray>)},
			{MsgTypes.std_msgs__UInt64, typeof(TypedMessage<std_msgs.UInt64>)},
			{MsgTypes.std_msgs__UInt64MultiArray, typeof(TypedMessage<std_msgs.UInt64MultiArray>)},
			{MsgTypes.std_msgs__UInt8, typeof(TypedMessage<std_msgs.UInt8>)},
			{MsgTypes.std_msgs__UInt8MultiArray, typeof(TypedMessage<std_msgs.UInt8MultiArray>)},
			{MsgTypes.Messages__DummyMsgThing, typeof(TypedMessage<Messages.DummyMsgThing>)}
		};

			public static Dictionary<MsgTypes, string> MessageDefinitions = new Dictionary<MsgTypes, string>
		{
			{MsgTypes.Unknown, "IDFK"},
			{MsgTypes.geometry_msgs__Point, 
			@"
float64 x
float64 y
float64 z
			"},
			{MsgTypes.geometry_msgs__Point32, 
			@"
float32 x
float32 y
float32 z
			"},
			{MsgTypes.geometry_msgs__PointStamped, 
			@"
Header header
Point point
			"},
			{MsgTypes.geometry_msgs__Polygon, 
			@"
geometry_msgs/Point32[] points
			"},
			{MsgTypes.geometry_msgs__PolygonStamped, 
			@"
Header header
Polygon polygon
			"},
			{MsgTypes.geometry_msgs__Pose, 
			@"
Point position
Quaternion orientation
			"},
			{MsgTypes.geometry_msgs__Pose2D, 
			@"
float64 x
float64 y
float64 theta
			"},
			{MsgTypes.geometry_msgs__PoseArray, 
			@"
Header header
geometry_msgs/Pose[] poses
			"},
			{MsgTypes.geometry_msgs__PoseStamped, 
			@"
Header header
Pose pose
			"},
			{MsgTypes.geometry_msgs__PoseWithCovariance, 
			@"
Pose pose
float64[36] covariance
			"},
			{MsgTypes.geometry_msgs__PoseWithCovarianceStamped, 
			@"
Header header
PoseWithCovariance pose
			"},
			{MsgTypes.geometry_msgs__Quaternion, 
			@"
float64 x
float64 y
float64 z
float64 w
			"},
			{MsgTypes.geometry_msgs__QuaternionStamped, 
			@"
Header header
Quaternion quaternion
			"},
			{MsgTypes.geometry_msgs__Transform, 
			@"
Vector3 translation
Quaternion rotation
			"},
			{MsgTypes.geometry_msgs__TransformStamped, 
			@"
Header header
string child_frame_id
Transform transform
			"},
			{MsgTypes.geometry_msgs__Twist, 
			@"
Vector3  linear
Vector3  angular
			"},
			{MsgTypes.geometry_msgs__TwistStamped, 
			@"
Header header
Twist twist
			"},
			{MsgTypes.geometry_msgs__TwistWithCovariance, 
			@"
Twist twist
float64[36] covariance
			"},
			{MsgTypes.geometry_msgs__TwistWithCovarianceStamped, 
			@"
Header header
TwistWithCovariance twist
			"},
			{MsgTypes.geometry_msgs__Vector3, 
			@"
float64 x
float64 y
float64 z
			"},
			{MsgTypes.geometry_msgs__Vector3Stamped, 
			@"
Header header
Vector3 vector
			"},
			{MsgTypes.geometry_msgs__Wrench, 
			@"
Vector3  force
Vector3  torque
			"},
			{MsgTypes.geometry_msgs__WrenchStamped, 
			@"
Header header
Wrench wrench
			"},
			{MsgTypes.nav_msgs__GridCells, 
			@"
Header header
float32 cell_width
float32 cell_height
geometry_msgs/Point[] cells
			"},
			{MsgTypes.nav_msgs__MapMetaData, 
			@"
time map_load_time
float32 resolution
uint32 width
uint32 height
geometry_msgs/Pose origin
			"},
			{MsgTypes.nav_msgs__OccupancyGrid, 
			@"
Header header
MapMetaData info
int8[] data
			"},
			{MsgTypes.nav_msgs__Odometry, 
			@"
Header header
string child_frame_id
geometry_msgs/PoseWithCovariance pose
geometry_msgs/TwistWithCovariance twist
			"},
			{MsgTypes.nav_msgs__Path, 
			@"
Header header
geometry_msgs/PoseStamped[] poses
			"},
			{MsgTypes.rosgraph_msgs__Log, 
			@"
byte DEBUG=1
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
string[] topics
			"},
			{MsgTypes.std_msgs__Bool, 
			@"
bool data
			"},
			{MsgTypes.std_msgs__Byte, 
			@"
byte data
			"},
			{MsgTypes.std_msgs__ByteMultiArray, 
			@"
MultiArrayLayout  layout
byte[]            data
			"},
			{MsgTypes.std_msgs__Char, 
			@"
char data
			"},
			{MsgTypes.std_msgs__ColorRGBA, 
			@"
float32 r
float32 g
float32 b
float32 a
			"},
			{MsgTypes.std_msgs__ConnectionHeader, 
			@"
byte DEBUG=1
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
string[] topics
			"},
			{MsgTypes.std_msgs__Duration, 
			@"
uint64 data
			"},
			{MsgTypes.std_msgs__Empty, 
			@"
			"},
			{MsgTypes.std_msgs__Float32, 
			@"
float32 data
			"},
			{MsgTypes.std_msgs__Float32MultiArray, 
			@"
MultiArrayLayout  layout
float32[]         data
			"},
			{MsgTypes.std_msgs__Float64, 
			@"
float64 data
			"},
			{MsgTypes.std_msgs__Float64MultiArray, 
			@"
MultiArrayLayout  layout
float64[]         data
			"},
			{MsgTypes.std_msgs__Header, 
			@"
uint32 seq
time stamp
string frame_id
			"},
			{MsgTypes.std_msgs__Int16, 
			@"
int16 data
			"},
			{MsgTypes.std_msgs__Int16MultiArray, 
			@"
MultiArrayLayout  layout
int16[]           data
			"},
			{MsgTypes.std_msgs__Int32, 
			@"
int32 data
			"},
			{MsgTypes.std_msgs__Int32MultiArray, 
			@"
MultiArrayLayout  layout
int32[]           data
			"},
			{MsgTypes.std_msgs__Int64, 
			@"
int64 data
			"},
			{MsgTypes.std_msgs__Int64MultiArray, 
			@"
MultiArrayLayout  layout
int64[]           data
			"},
			{MsgTypes.std_msgs__Int8, 
			@"
int8 data
			"},
			{MsgTypes.std_msgs__Int8MultiArray, 
			@"
MultiArrayLayout  layout
int8[]            data
			"},
			{MsgTypes.std_msgs__MultiArrayDimension, 
			@"
string label
uint32 size
uint32 stride
			"},
			{MsgTypes.std_msgs__MultiArrayLayout, 
			@"
MultiArrayDimension[] dim
uint32 data_offset
			"},
			{MsgTypes.std_msgs__String, 
			@"
string data
			"},
			{MsgTypes.std_msgs__Time, 
			@"
uint64 data
			"},
			{MsgTypes.std_msgs__UInt16, 
			@"
uint16 data
			"},
			{MsgTypes.std_msgs__UInt16MultiArray, 
			@"
MultiArrayLayout  layout
uint16[]            data
			"},
			{MsgTypes.std_msgs__UInt32, 
			@"
uint32 data
			"},
			{MsgTypes.std_msgs__UInt32MultiArray, 
			@"
MultiArrayLayout  layout
uint32[]          data
			"},
			{MsgTypes.std_msgs__UInt64, 
			@"
uint64 data
			"},
			{MsgTypes.std_msgs__UInt64MultiArray, 
			@"
MultiArrayLayout  layout
uint64[]          data
			"},
			{MsgTypes.std_msgs__UInt8, 
			@"
uint8 data
			"},
			{MsgTypes.std_msgs__UInt8MultiArray, 
			@"
MultiArrayLayout  layout
uint8[]           data
			"},
			{MsgTypes.Messages__DummyMsgThing, 
			@"
geometry_msgs/Twist leftnipple
geometry_msgs/Twist rightnipple
			"}};

		public static Dictionary<MsgTypes, bool> IsMetaType = new Dictionary<MsgTypes, bool>()
		{
			{MsgTypes.Unknown, false},
			{MsgTypes.geometry_msgs__Point, false},
			{MsgTypes.geometry_msgs__Point32, false},
			{MsgTypes.geometry_msgs__PointStamped, true},
			{MsgTypes.geometry_msgs__Polygon, true},
			{MsgTypes.geometry_msgs__PolygonStamped, true},
			{MsgTypes.geometry_msgs__Pose, true},
			{MsgTypes.geometry_msgs__Pose2D, false},
			{MsgTypes.geometry_msgs__PoseArray, true},
			{MsgTypes.geometry_msgs__PoseStamped, true},
			{MsgTypes.geometry_msgs__PoseWithCovariance, true},
			{MsgTypes.geometry_msgs__PoseWithCovarianceStamped, true},
			{MsgTypes.geometry_msgs__Quaternion, false},
			{MsgTypes.geometry_msgs__QuaternionStamped, true},
			{MsgTypes.geometry_msgs__Transform, true},
			{MsgTypes.geometry_msgs__TransformStamped, true},
			{MsgTypes.geometry_msgs__Twist, true},
			{MsgTypes.geometry_msgs__TwistStamped, true},
			{MsgTypes.geometry_msgs__TwistWithCovariance, true},
			{MsgTypes.geometry_msgs__TwistWithCovarianceStamped, true},
			{MsgTypes.geometry_msgs__Vector3, false},
			{MsgTypes.geometry_msgs__Vector3Stamped, true},
			{MsgTypes.geometry_msgs__Wrench, true},
			{MsgTypes.geometry_msgs__WrenchStamped, true},
			{MsgTypes.nav_msgs__GridCells, true},
			{MsgTypes.nav_msgs__MapMetaData, true},
			{MsgTypes.nav_msgs__OccupancyGrid, true},
			{MsgTypes.nav_msgs__Odometry, true},
			{MsgTypes.nav_msgs__Path, true},
			{MsgTypes.rosgraph_msgs__Log, true},
			{MsgTypes.std_msgs__Bool, false},
			{MsgTypes.std_msgs__Byte, false},
			{MsgTypes.std_msgs__ByteMultiArray, true},
			{MsgTypes.std_msgs__Char, false},
			{MsgTypes.std_msgs__ColorRGBA, false},
			{MsgTypes.std_msgs__ConnectionHeader, true},
			{MsgTypes.std_msgs__Duration, false},
			{MsgTypes.std_msgs__Empty, false},
			{MsgTypes.std_msgs__Float32, false},
			{MsgTypes.std_msgs__Float32MultiArray, true},
			{MsgTypes.std_msgs__Float64, false},
			{MsgTypes.std_msgs__Float64MultiArray, true},
			{MsgTypes.std_msgs__Header, false},
			{MsgTypes.std_msgs__Int16, false},
			{MsgTypes.std_msgs__Int16MultiArray, true},
			{MsgTypes.std_msgs__Int32, false},
			{MsgTypes.std_msgs__Int32MultiArray, true},
			{MsgTypes.std_msgs__Int64, false},
			{MsgTypes.std_msgs__Int64MultiArray, true},
			{MsgTypes.std_msgs__Int8, false},
			{MsgTypes.std_msgs__Int8MultiArray, true},
			{MsgTypes.std_msgs__MultiArrayDimension, false},
			{MsgTypes.std_msgs__MultiArrayLayout, true},
			{MsgTypes.std_msgs__String, false},
			{MsgTypes.std_msgs__Time, false},
			{MsgTypes.std_msgs__UInt16, false},
			{MsgTypes.std_msgs__UInt16MultiArray, true},
			{MsgTypes.std_msgs__UInt32, false},
			{MsgTypes.std_msgs__UInt32MultiArray, true},
			{MsgTypes.std_msgs__UInt64, false},
			{MsgTypes.std_msgs__UInt64MultiArray, true},
			{MsgTypes.std_msgs__UInt8, false},
			{MsgTypes.std_msgs__UInt8MultiArray, true},
			{MsgTypes.Messages__DummyMsgThing, true}
		};	}

	public enum MsgTypes
	{
		Unknown,
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
		std_msgs__UInt8MultiArray,
		Messages__DummyMsgThing
	}
}
