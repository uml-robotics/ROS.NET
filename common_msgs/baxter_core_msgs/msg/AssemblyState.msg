bool enabled             # true if enabled
bool stopped             # true if stopped -- e-stop asserted
bool error               # true if a component of the assembly has an error
#
# The following are specific to the robot top-level assembly:
uint8  estop_button      # One of the following:
  uint8   ESTOP_BUTTON_UNPRESSED = 0   # Robot is not stopped and button is not pressed
  uint8   ESTOP_BUTTON_PRESSED   = 1
  uint8   ESTOP_BUTTON_UNKNOWN   = 2   # STATE_UNKNOWN when estop was asserted by a non-user source
  uint8   ESTOP_BUTTON_RELEASED  = 3   # Was pressed, is now known to be released, but robot is still stopped.
#
uint8  estop_source      # If stopped is true, the source of the e-stop.  One of the following:
  uint8  ESTOP_SOURCE_NONE      = 0   # e-stop is not asserted
  uint8  ESTOP_SOURCE_USER      = 1   # e-stop source is user input (the red button)
  uint8  ESTOP_SOURCE_UNKNOWN   = 2   # e-stop source is unknown
  uint8  ESTOP_SOURCE_FAULT     = 3   # MotorController asserted e-stop in response to a joint fault
  uint8  ESTOP_SOURCE_BRAIN     = 4   # MotorController asserted e-stop in response to a lapse of the brain heartbeat
