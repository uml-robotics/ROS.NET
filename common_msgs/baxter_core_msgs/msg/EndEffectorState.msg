#
time timestamp              # time when state was updated
uint32 id                   # EndEffectorId
#
# The following State fields are tristate: 0 = false; 1 = true; 2 = unknown/unsupported
  uint8   STATE_FALSE = 0
  uint8   STATE_TRUE = 1
  uint8   STATE_UNKNOWN = 2
#
uint8   enabled             # true if enabled
uint8   calibrated          # true if calibration has completed
uint8   ready               # true if ready for another command
uint8   moving              # true if moving
uint8   gripping            # true if gripping
uint8   missed              # true if GRIP/GOTO/SET was commanded and the gripper reaches the end of travel
uint8   error               # true if the gripper is in an error state
uint8   reverse             # true if the gripper is in reverse mode
#
float32 position            # position as a percentage of the max position;      0=closed - 100=open
#
  float32 POSITION_CLOSED = 0.0
  float32 POSITION_OPEN   = 100.0
#
float32 force               # force as a percentage of max force;                0=none   - 100=max
#
  float32 FORCE_MIN = 0.0
  float32 FORCE_MAX = 100.0
#
string state                # JSON: other state information
#
string command              # from the last command message
string command_sender
uint32 command_sequence
#
