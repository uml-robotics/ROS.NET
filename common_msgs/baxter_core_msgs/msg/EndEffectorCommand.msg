## Command to be sent to an end effector
uint32 id       # target end effector id
string command  # operation to perform
# Well known commands:
string   CMD_NO_OP           = no_op
string   CMD_SET             = set
string   CMD_CONFIGURE       = configure
string   CMD_REBOOT          = reboot
string   CMD_RESET           = reset
string   CMD_CALIBRATE       = calibrate
string   CMD_CLEAR_CALIBRATION = clear_calibration
string   CMD_PREPARE_TO_GRIP = prepare_to_grip
string   CMD_GRIP            = grip
string   CMD_RELEASE         = release
string   CMD_GO              = go
string   CMD_STOP            = stop
#
string args     # JSON arguments to the command
#
string sender   # optional identifier, returned in state when the command is handled
uint32 sequence # optional sequence number, return in state when the command is handled

