uint32 id               # EndEffectorId
uint8  ui_type
    # End Effector type enumeration, for the UI:
    uint8  NO_GRIPPER = 0
    uint8  SUCTION_CUP_GRIPPER = 1
    uint8  ELECTRIC_GRIPPER = 2
    uint8  PASSIVE_GRIPPER = 3
string manufacturer     # Manufacturer name
string product          # Product name
string serial_number    # Serial number, optional
string hardware_rev     # Hardware revision, optional
string firmware_rev     # Firmware revision, optional
string firmware_date    # Firmware date, optional
#
# End Effector Capabilities
bool   has_calibration  # true if the gripper has calibration
bool   controls_grip    # true if the gripper has grip/release control
bool   senses_grip      # true if the gripper has grip sense
bool   reverses_grip    # true if the gripper has reverse-grip mode

bool   controls_force   # true if the gripper has force control
bool   senses_force     # true if the gripper has force sense

bool   controls_position # true if the gripper has position control
bool   senses_position   # true if the gripper has position sense
#
string properties       # JSON; other properties
