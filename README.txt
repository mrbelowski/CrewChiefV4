CrewChief version 4.

Changelog
---------
Version 4.1.4.3: Fixed 'box this lap' calls being made when there is no mandatory stop, when running PCars in UDP mode.

Version 4.1.4.2: Fixed some speech recogniser / button handling issues - "Toggle" mode is now renamed "Press and release button" and actually works; Read the sector times response as a single message per sector, to allow interrupting and fix an issue with the Italian number reader.

Version 4.1.4.1: Fixed missing sector 3 time being read as "zero tenths off the pace".

Version 4.1.4.0: Reworked sector delta reporting to provide actual deltas, rather than approximations; Some changes to the Italian number reader (still work in progress); Some bug fixes.

Version 4.1.3.2: Removed some debug code that shouldn't have made it into the release.

Version 4.1.3.1: A couple of internal fixes.
	
Version 4.1.3.0: Added language-specific sound pack stuff; Better support for language specific number and time speech generation; Some internal bug fixing; Don't play wheel locking warnings if the player has a missing wheel or puncture; Don't play laptime improving / worsening messages if the conditions have significantly changed (rain or track temp); Don't play a message twice in succession if a player asks for something that the app was going to tell them anyway; Don't play good / OK start messages if the player has picked up a penalty (i.e. false start);Insert a short pause between some messages;Reduce the likelihood of multiple sweary messages being played in quick succession;Some better error trapping when the app is closed.

Version 4.1.2.2: Fixed radio channel (hold) button function for PCars network data.

Version 4.1.2.1: Added some car class data and pit detection points for the PCars Lotus DLC; Fixed some pit detection issues in PCars; Added option to enable spotter in hot lap (time trial) mode for PCars; Don't play lap time messages when we're in the pit lane;Don't complain about worsening lap times if the player has made a pass on this lap

Version 4.1.2.0: Major speech recognizer overhaul to allow customisation; Externalised all UI text; Added some options to number reading; Fixes to Hot Lap (timetrial) mode in PCars; Don't trigger flags event when stationary; A couple of internal bug fixes

Version 4.1.1.4: Added some car classes and Bannockbrae track for PCars; Remove stale opponents in PCars; Some internal error handling

Version 4.1.1.3: Allow messages with optional prefixes / suffixes to play without their prefixes or suffixes; Tidied up String encoding handling; Reverted console logging change (after a couple of attempts - hence the version number jump)

Version 4.1.1.0: Better selection of sound files from those available for each message - should give less repetition;Made the console logging a bit more efficient; Some String encoding rework for PCars. PS4 users should use UTF-8 for the pcars_character_encoding property, XBox and PC should use windows-1252; Added PCars V8 Supercar to car classes (more to come here); Fixed last-lap message for R3E timed races (should now work when you're not leading)

Version 4.1.0.3: Fixed possible bug in pit detection that could cause repeated messages; Added 'can you hear me' speech recognition to check it's working (should respond with 'yes, I can hear you'); Take start position into account when generating race end message; A couple of internal bug fixes; A few sound pack tweaks to make the personalisation sounds work a little better

Version 4.1.0.2: Renamed UDP network button data option to make it clearer that this takes button presses from the UDP stream, rather than from the device directly

Version 4.1.0.1: More internal fixes to the radio channel handling logic to handle a couple of edge-cases where it wasn't closing the channel promptly; Spotter performance and latency improvements; Spotter logic fixes for cases where a '3 wide' turns into a 'car left' / 'car right'; Don't attempt to update and load a new driver name for an existing player if the new name isn't valid / usable; Tyre temp range tweaks; Check messages for validity and timeout just before playing them; Use separate class for each PCars Road car class; Handle broken PCars string data which had null characters in the middle of the String; PCars car class handling improvements

Version 4.1.0.0: Internal audio handling overhaul - better queue handling, smarter caching of sound objects, more reliable radio channel state management (should prevent channel being left open); Added support for personalised message prefixes and suffixes; Spotter fix - reinstated missing width separation check to prevent spotter calls being made when a car is directly in front / behind but within the car length parameter; Internal audio handling overhaul - better queue handling, more reliable radio channel state management (should prevent channel being left open); Fixed number reading for some numbers; Fixed DTM 2014 tyre compound error in the 'box now' message; Validate overtake messages to ensure they're not out of date by the time they're played

Version 4.0.3.5: Fixed major regression for Project Cars - hold all internal Strings as raw byte arrays (which may or may not have a null first character) and decode them when we need them

Version 4.0.3.4: Internal rework for Project Cars to handle String data which occasionally starts with a null character. Should fix 'missing' opponents and incorrect car classes

Version 4.0.3.3: Major spotter overhaul - changed the way app calculates opponent speeds, much more accurate. Should make a difference to the ghost calls

Version 4.0.3.2: Overtaking messages tweak - make these a bit more likely; Increased some brake temp thresholds: Fixed "what's my best lap time" response; Stop the autoupdater running when the app starts listening for data; Added packet rate estimate to console output for PCars Network data

Version 4.0.3.1: Fixed startup bug on initial install; Some fuel useage warning rework

Version 4.0.3.0: Fixed overtaking messages in PCars (caused by noise in the opponent speed data - this is now based on a sliding average); Fixed baseline engine temperature calculations for RaceRoom; Corrected brake temp thresholds and engine damage thresholds; Some internal bug fixes in the spotter and numeric message handling; Do auto update checks in a background Thread; Fixed session time left reporting

Version 4.0.2.0: Added optional default sound pack installation location override property ('override_default_sound_pack_location'); Fixed RaceRoom spotter ghost calls at some tracks; reworked laptime comparisons for practice and qual sessions; fixed "where's p X" response.

Version 4.0.1.0: Fixed sound pack installation location - this now uses /Users/[username]/AppData/Local/CrewChiefV4/sounds

Version 4.0.0.0: Initial release of version 4. The app now comes packaged as a single auto-updating .msi installer and includes integrated sound and driver names pack updating. The spotter has been overhauled, brake temp messages fixed, and car class and driver names for RaceRoom Formula 2 drivers have been added.


Known Issues Which Aren't Fixable
---------------------------------

Project Cars doesn't send opponent laptime data, so the app has to time their laps. In practice and qual sessions this is fairly reliable (because the app can use the time remaining in the session, sent by the game, for its 'clock' when timing). In race sessions with a fixed number of laps the app has nothing it can use as a clock to time the laps, so times them itself. This can lead to opponent lap / sector time inaccuracies if the player pauses the game (the app's clock is still running).


Joining a session part way through (practice or qualify session online) will result in the app having an incomplete set of data for opponent lap and sector times. In such cases the best opponent lap and sector data is inaccurate. For Project Cars, there's nothing I can do about this. The opponent lap and sector times aren't in the shared memory (the app has to time their laps), so the pace and sector delta reports may be inaccurate (they use the fastest lap completed while the app is running). For Raceroom we can get the fastest opponent lap time, but if this lap was completed before the app was running, the sectors within that lap aren't accessible. In this case the pace report will include the lap time delta, but there'll be no sector delta reports.

In both cases as soon as an opponent sets a faster lap, the app will have up to date best lap data so the pace and sector reports will be accurate and complete.


Project Cars doesn't send opponent car class data, so the app has to assume that all drivers in the race are in the same car class. For multiclass races, all pace and other reports will be relative to the overall leader / fastest car.


RaceRoom uses a 'slot_id' field to uniquely identify drivers in a race. However, this field doesn't really work properly (there are lots of issues with it), so the app has to use the driver's names. Driver names for AI driver are not unique. All the lap time and other data held for each driver is indexed by driver name so if a race has 2 or more drivers with the same name, the app will get things like lap and sector times wrong. This is only a problem racing the AI - be aware that if you have a car class with a limited number of unique AI drivers (Daytona Prototypes / German Nationals / Americal Nationals / Hill Climb Legends / etc), but select a field size greater than this, the app will do weird things.


RaceRoom doesn't have a pre-start procedure phase for offline races, and in the pre-start phase online ("Gridwalk") very little valid and accurate data is available.


Project Cars doesn't have a distinct pre-start procedure phase. I've added some more messages before the 'get ready' but there's a risk here that they might delay the 'get ready' message.


Detecting 'good' passes isn't really feasible. I've tried to limit the 'good pass' messages to overtakes that are reasonably 'secure', don't result in the other car slowing excessively, and don't involve the player going off-track. I can't, for example, tell the difference between a clean pass and a bump-and-run punt, so you might get congratulated for driving like a berk.


Quick start
-----------
You need to install .net 4.5 or above to use the app. Download the CrewChiefV4.msi installer and run it. Start the app. Click the "Download sound pack" button and the "Download driver names" button to get the latest sounds and driver names. Select a game from the list at the top right. When the sounds and driver names have finished downloading, click the "Start Application" button. Then fire up the game. Note that the app comes with swearing 'off' by default - if you want to be sworn at you need to enable this in the Properties UI.


Running with voice recognition
------------------------------
If you want to use voice recognition, download the correct speech recognition installers for your system (speech_recognition_32bit.zip or speech_recognition_64bit.zip). Run SpeechPlatformRuntime.msi (this is the MS speech recognition engine), then run MSSpeech_SR_en-GB_TELE.msi or MSSpeech_SR_en-US_TELE.msi depending on your preferred accent (these are the 'cultural info' installers). If you want to use US speech recognition (MSSpeech_SR_en-US_TELE.msi) you must modify the "speech_recognition_location" property to "en-US". This can be done by editing CrewChiefV4.exe.config, or by modifying the property value in the application's Properties area. If you're happy with en-GB you don't need to do anything other than run the 2 speech recognition installers.

For speech recognition, you need a microphone configured as the default "Recording" device in Windows.

To get started, run CrewChiefV4.exe and choose a "Voice recognition mode". There are 3 modes (the radio buttons at the bottom right). "Disabled" means that the app won't attempt any speech recognition. "Hold button" means you have to hold down a button while you speak, and release the button when you're finished. "Toggle button" means you press a button once to start the speech recognition, and the app will continue to listen and process your spoken requests until you press the button again to switch it off (while the app is listening you can make as many voice requests as you like, you don't need to toggle speech recognition off and back on again if you want to ask another question). "Always on" means the app is always listening for and processing speech commands. Selecting "Disabled" or "Always on" from this list makes the app ignore the button assigned to "Talk to crew chief".

If you want to use Hold button or Toggle button mode, select a controller device ("Available controllers" list, bottom left), choose "Talk to crew chief" in the "Available actions" list and click "Assign control". Then press the button you want to assign to your radio button. 

You need to speak clearly and your mic needs to be properly set up - you might need to experiment with levels and gain (Microphone boost) in the Windows control panel. If he understood he'll respond - perhaps with helpful info, perhaps with "we don't have that data". If he doesn't quite understand he'll ask you to repeat yourself. If he can't even tell if you've said something he'll remain silent. There's some debug logging in the main window that might be useful.

I've not finished implementing this but currently the app understands and responds to the following commands:

"how's my [fuel / tyre wear / body work / aero / engine / transmission / suspension / pace ]"
"how are my [tyre temps / tyre temperatures / brakes / brake temps / brake temperatures / engine temps / engine temperatures]"
"what's my [gap in front / gap ahead / gap behind / last lap / last lap time / lap time / position / fuel level / best lap / best lap time]"
"keep quiet / I know what I'm doing / leave me alone" (switches off messages)
"keep me informed / keep me posted / keep me updated" (switches messages back on)
"how long's left / how many laps are left / how many laps to go"
"spot / don't spot" (switches the spotter on and off - note even in "leave me alone" mode the spotter still operates unless you explicitly switch it off)
"do I still have a penalty / do I have a penalty / have I served my penalty"
"do I have to pit / do I need to pit / do I have a mandatory pit stop / do I have a mandatory stop / do I have to make a pit stop"
"where's [opponent driver last name]"
"where's P [opponent position]"
"what's [opponent driver last name]'s last lap"
"what's [opponent driver last name]'s best lap"
"what's [opponent race position]'s last lap" (for example, "what's p 4's best lap", or "what's position 4's last lap")
"what's [opponent race position]'s best lap"
"what's [the car in front / the guy in front / the car ahead / the guy ahead]'s last lap"
"what's [the car in front / the guy in front / the car ahead / the guy ahead]'s best lap"
"what's [the car behind / the guy behind]'s last lap"
"what's [the car behind / the guy behind]'s best lap"
"what tyre(s) is [opponent driver last name / opponent race position] on" (DTM 2014 only - reports "options" or "primes")
"what are my sector times"
"what's my last sector time"
"who's leading" (this one only works if you have the driver name recording for the lead car)
"who's [ahead / ahead in the race / in front / in front in the race / behind / behind in the race]" (gives the name of the car in front / behind in the race or on the timing sheet for qual / practice. This one only works if you have the driver name recording for that driver)
"who's [ahead on track / in front on track / behind on track]" (gives the name of the car in front / behind in on track, regardless of his race / qual position. This one only works if you have the driver name recording for that driver)
"tell me the gaps / give me the gaps / tell me the deltas / give me the deltas" (switch on 'deltas' mode where the time deltas in front and behind get read out on each lap. Note that these messages will play even if you have disabled messages)
"don't tell me the gaps / don't tell me the deltas / no more gaps / no more deltas" (switch off deltas mode)
"repeat last message / say again" (replays the last message)
"What's the air temp / what's the air temperature / what's the track temp / what's the track temperature" (current air / track temps in celsius)
"What are my [brake / tyre] [temperatures / temps]"



Speech recognition customisation
--------------------------------
If you want to change the phrases the app listens for (e.g. instead of asking "how's my tyre wear", perhaps you want to as "how's my boots looking"), create a file called "speech_recognition_config.txt" in [user]\AppData\Local\CrewChiefV4 and use this to override the defaults found in [installDir]\speech_recognition_config.txt



Other button assignments
------------------------
You can assign the 'toggle spotter on/off', 'toggle race updates on/off', 'toggle opponent deltas' and 'repeat last message' to separate buttons if you want to be able to toggle the spotter function and toggle the crew chief's updates on or off during the race. This doesn't require voice recognition to be installed - simply run the app, assign a button to one or both of these functions, and when in-race pressing that button will toggle the spotter / crew chief / opponent deltas on and off.


Properties
----------
When you first run the app it will create a user configuration folder in /Users/[username]/AppData/local/CrewChiefV4 (for example, on my system this is in C:\Users\Jim\AppData\Local\CrewChiefV4). This folder holds your application settings. The settings can be accessed by clicking the "Properties" button in the app. This displays a popup window where you can tweak stuff if you want to. This interface is a bit rubbish but should let you tweak settings if you want to, although the properties are all (currently) undocumented. If you do change something in this interface, the app needs to restart to pick up the change - the "Save and restart" button should do this.

Each property has a "reset to default" button, or if you get completely stuck you can close the app and delete the user configuration folder and it should reset everything.


Custom controllers
------------------
This is untested. If your controller doesn't show up in the list of available controllers you can set the "custom_controller_guid" property to the GUID of your controller device. If this is a valid controller GUID the app will attempt to initialise it an add it to the list of available controllers.


Program start arguments
-----------------------
If you want to have the game pre-selected, start the app like this for PCars: [full path]\CrewChiefV4.exe PCARS_64BIT. Or use R3E or PCARS_32BIT.
This can be used in conjunction with the launch_pcars / launch_raceroom / [game]_launch_exe / [game]_launch_params and run_immediately options to set crew chief up to start the game selected in the app launch argument, and start its own process. I'll provide examples of this approach soon. 


Updating the app
----------------
If a new version of the app is available the auto updater will prompt you to download it. This will download and run a new .msi installer - just point it at the existing install location and it'll update your old installation. It won't remove your existing sound pack or your settings.

If a new sound pack or driver names pack is available the appropriate Download button(s) will be enabled - these will download and unpack the updated sounds / driver names, then restart the application.

the 64bit speech recognition installers can be downloaded here 	 : https://drive.google.com/file/d/0B4KQS820QNFbY05tVnhiNVFnYkU/view?usp=sharing
the 32bit speech recognition installers can be downloaded here   : https://drive.google.com/file/d/0B4KQS820QNFbRVJrVjU4X1NxSEU/view?usp=sharing


Donations
---------
I made this because I wanted to make it and I enjoy making stuff. Working with the various quirks, errors and omissions in the shared data which the games provide hasn't been much fun, but it's all part of the challenge. Having said that, there are many many hours of hard work invested in this.
If you use it and like it and it becomes a regular and positive part of your sim racing, I'd be grateful if you would consider making a small donation. If only to stop my wife from complaining at me.

My paypal address is jim.britton@yahoo.co.uk

Or you can use this to donate directly:

https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=LW33XFXP4DPZE

Would be great to recoup some of the investment in making this, but the most important thing is that the app is used 'in anger' and enjoyed as part of the sim racing experience. To this end, I'm always on the lookout for bug reports and feature suggestions.

One final point. If the app says "Jim is faster than you", let him through :)