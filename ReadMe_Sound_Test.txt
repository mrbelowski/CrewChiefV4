# All lines starting with # will be ignored
# To enable sound testing, create a shortcut and add SMOKE_TEST as startup argument 
# This will enable a text box on the right side of the console output window in witch you can type in what you want played or play a file with the "script" in it like this one. 
# The syntax is as following.
# To play the example file(this one) type in the full path to the file ex. C:\Users\{USERNAME}\Documents\CrewChiefV4\ReadMe_Sound_Test.txt

# To play a sound from a folder (relative to the sound folder) type "folder_name/subfolder_name", ex:
timings/the_gap_to

#To play a opponent drivername type in ex:
name britton

# To play a sound from a folder (relative to the sound folder) type "folder_name/subfolder_name", ex:
timings/ahead_is_increasing

#To play a gap message type in ex:
time gap 5.34

#To start a new message type:
&
timings/the_gap_to
name roslev
timings/ahead_is_increasing
time gap 3.23
&
#To insert a pause type "pause 100" followed by millisec. ex:
pause 150
# Time messages can be played with the following commands
time lap 95.345
&
time sec 12
&
time hun 12.32
&
time ten 12.3
&
#An integer can be played by typing a number
1250 
