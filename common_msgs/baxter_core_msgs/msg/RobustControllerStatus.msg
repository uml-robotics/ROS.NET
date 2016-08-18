# True if the RC is enabled and running, false if not.
bool isEnabled

# The state of the RC with respect to its completion goal.  One of
# NOT_COMPLETE, COMPLETE_W_FAILURE, or COMPLETE_W_SUCCESS
int32 complete
int32 NOT_COMPLETE = 0
int32 COMPLETE_W_FAILURE = 1
int32 COMPLETE_W_SUCCESS = 2

# Identifies the sender of the Enable message that the RC is using for its
# commands.  This should correspond to the "uid" field of a recently published
# RC *Enable message.
string controlUid

# Set to true when the RC self-disables as a result of too much time elapsing
# without receiving an Enable message.
bool timedOut

# A list of relevant error codes.  Error codes are defined by the individual
# robust controllers, consult a robust controller's documentation to see what
# error codes it generates.
string[] errorCodes

# A list of current labels for the RC's current state. For example, "fastapproach",
# "slowapproach", etc. Used primarily for the blended RCs, other RCs can leave this
# blank. This will probably contains just one label, but it could contain multiple labels
# in the future.
string[] labels
