## HandPoseTrajectoryRosMessage.msg
# This message allows the user to control the exact finger pose and configuration over ros
# It makes use of the ihmc queue and override system and permits the addition of multiple waypoints
# A third order polynomial function is NOT used to calculate waypoints because that's hard

uint8 robot_side

#J5, J6, J7
ihmc_msgs/OneDoFJointTrajectoryRosMessage[] forearm_joint_trajectory_messages

#Thumb Roll, Thumb Proximal, Thumb Distal, Index Finger, Middle Finger, Pinky Finger
ihmc_msgs/OneDoFJointTrajectoryRosMessage[] hand_joint_trajectory_messages

uint8 execution_mode
uint8 desired_pose
int64 previous_message_id
int64 unique_id

#robot_side options
uint8 LEFT=0
uint8 RIGHT=1
uint8 BOTH=2

#execution_mode options
uint8 OVERRIDE=0
uint8 QUEUE=1
uint8 POSE=2
uint8 DELTA=3

#desired_pose options
uint8 STILL=0
uint8 ROCK_ON=1
uint8 BLADE=2
uint8 FIST=3
uint8 OPEN=4
uint8 REST=5

bool homeAllHandJoints
bool homeAllForearmJoints
