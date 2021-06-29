## MotionPlannerCartesianMessage.msg
# This message allows the user to send a series of waypoints to the trajectory motion planner
# planning_mode indicates the type of constrained optimization that tnhe planner will use
# 
uint8 p_WAYPOINTS=21 # Move through a number of cartesian waypoints
uint8 p_JOINT_CONFIGURATION=22 
uint8 p_VISIBILITY=23 
uint8 p_DUALARM=24
uint8 p_CONTACT=25
uint8 p_REACTIVE=26
uint8 p_VR=27 # This enum is for VR control, it is not yet defined in code
#


Header header # defines frame to move
CartesianWaypointMessage[] waypoint # A vector of waypoints denoting constraint at each timestep
uint8 planning_mode 


