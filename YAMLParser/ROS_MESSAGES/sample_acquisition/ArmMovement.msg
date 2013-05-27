# ArmMovement.msg
# [ml] Message type for controlling the arm and reporting arm status.

bool gripper_open

# Each of the following has a range of [-1,1]
float64 pan_motor_velocity
float64 tilt_motor_velocity

