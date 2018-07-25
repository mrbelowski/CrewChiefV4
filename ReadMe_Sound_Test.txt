# All lines starting with # will be ignored
# To enable sound testing, create a shortcut to the app and add SOUND_TEST as startup argument, for example
# "C:\Program Files (x86)\Britton IT Ltd\CrewChiefV4\CrewChiefV4.exe" SOUND_TEST
# This will enable a text box on the right side of the console output window in which you can type in what you want played or play a file with the "script" in it like this one.
#
# To play the sounds in this text box, you first need to start the app (click the 'Start application' button). Then paste in the sounds or script file name, and press the
# 'Test Sounds' button at the bottom of the text box.
#
# The syntax for specifying what to play is as follows.

# To play the example file (such as this one) type in the full path to the file ex. C:\Users\{USERNAME}\Documents\CrewChiefV4\ReadMe_Sound_Test.txt
# Note that the file must be in a folder Crew Chief has permission to read - C:\Users\{USERNAME}\Documents\CrewChiefV4\ is recommended.

# To play a sound from a folder (relative to the sounds/voice folder, which is in C:\Users\{USERNAME}\AppData\Local\CrewChiefV4\sounds\voice) 
# type "folder_name/subfolder_name", for example:
lap_counter/get_ready

# To play an opponent drivername type:
name britton

# To play a message with more than one sound, place them on separate lines:
flags/incident_in_corner_intro
corners/stowe
flags/incident_in_corner_with_driver_intro
name roslev
flags/and
name bakus

# To play a gap message type in e.g.
time gap 5.34

# A single message may consist of multiple sound files. To play multiple messages, each with multiple sound files, separate
# them with an ampersand ('&') character e.g.
timings/the_gap_to
name roslev
timings/ahead_is_increasing
time gap 3.23
&
name leonavicius
timings/is_reeling_you_in
time gap 1.23

# To insert a pause type "pause 100" followed by millisec. e.g.
pause 150

# Time messages can be played with the following commands
time lap 95.345
&
time sec 12
&
time hun 12.32
&
time ten 12.3

# An integer can be played by typing a number
1250 
