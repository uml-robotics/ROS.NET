# CartesianWaypointMessage.msg
# One waypoint. Pose constraints can be either active ( CONSTRAINT_ACTIVE) or ignore (CONSTRAINT_INACTIVE) 
float64 CONSTRAINT_ACTIVE=1.0
float64 CONSTRAINT_INACTIVE=0.0
#
#
uint8 timestep # Timestep denotes the order to waypoint execution starting at 1...n waypoints
geometry_msgs/Pose waypoint_pose # desired pose for this waypoint
float64[] position_coefficients # vector of 3 components denoting whether a positional constraint is active (1) or not 0 
float64[] rotation_coefficients # vector of 3 components (RPY) denoting whether a rotational constraint is active (1) or not 0 
float64[] link_offset # vector of 3 components denoting an offset from the defined frame to end effector


