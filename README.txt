CrewChief version 3.

Changelog
---------
Version 3.10.0: Added DRS and push to pass messages. This version requires a new sound pack.

Version 3.9.8: Fixed regression in PCars opponent pit detection. Added missing driver names to bundle version. No new sound pack for this version.

Version 3.9.7: Overhauled PCars mandatory pitstop handling; added Audi TT cup car to car classes (R3E), added some new driver names; some driver last name extraction fixes. No new sound pack for this version

Version 3.9.6: Updated to support shared memory change.

Version 3.9.5: tidied up radio bleep / click sound handling. This version requires a new sound pack.

Version 3.9.4: Added optional between-message delay to slow things down a bit when there's a big pile of queued messages to be played ("pause_between_message" property); added "what's my best lap" / "what's my best lap time" response. No new sound pack for this version.

Version 3.9.3: Added UDP packet ordering checks and data rate estimate; Spotter tweaks to make it function better with the UDP data (don't allow an existing overlap to be called 'clear' if the closing speed is out of range, allow different car / track size params for the UDP spotter); Fixed and edge-case crash bug with the background sound player; Reworked internals to allow the app to assign and listen for button presses inside the UDP data packet (for console controller buttons); Fixes to qualify position reporting; Disable practice / qualify pole time delta report in PCars; Fixed UDP int -> float mapping for damage levels and some others (thanks to Pock for the heads-up); Various minor bug fixes; Added some driver names. No new sound pack for this version.

Verions 3.9.2: Fixes for PCars UDP network data stream. No new sound pack for this version.

Version 3.9.1: Disabled spotter in Raceroom hotlap / leaderboard challenge mode; Fixed issue where race sector reports were being played every sector if practice / qual sector reports were configured to play for each sector. No new sound pack required for this version but a few more messages have been added to existing events (sound pack is now version 3.9b).

Version 3.9.0: Fixed Raceroom engine temp baseline data collection (should fix false positives); Allow gap requests to be read for gaps > 1 minute; Tweaked brake damage levels; Driver name mapping enhancements (can now have multiple lastname spellings map to the same sound file); Added pre-start procedure messages (PCars only for now - some more work to do here and some known issues / limitations); Added overtake and being overtaken messages; Some re-ordering of message priorities; Added optional alternate radio beeps (with no actual beeps) - select the "use_alternate_beeps" to enable this - thanks to Georg Ortner for these; Don't play gap messages if the car they're referring to has been overtaken; Added more driver names. This version requires a new sound pack.

Version 3.8.6: Ignore cars which haven't completed a lap when checking for pit exit clear (prevents car sitting in pit lane at start of a session triggering messages); Tweaked sector time reports in race; Fixed resource leak in UDP connection handler; some internal refactoring; Added options to disable brake, suspension, or all damage reports. New sound pack not required, but recommended for this version (sounds_3.8f or sounds_lo_fi_3.8f).

Version 3.8.5: Updated to use latest UDP network packet structures; Fixed incorrect laps remaining response in last 2 laps; Use Raceroom 'session best lap' data when joining part way through a session. No new sound pack for this version.

Version 3.8.4b: Fixed unknown track causing constant session restarts in the app. No new sound pack for this version.

Version 3.8.4: Reworked approaching car check when exiting pits; A few minor fixes; More driver names. No new sound pack for this version.

Version 3.8.3: Fixes to Raceroom car class handling - should ensure the app gets the right class for the player instead of using the default class sometimes (and giving inappropriate multiclass messages); Fixed some laptime comparisons (including backwards time comparison in qual / practice lap time check that resulted in slow laptimes, rather than fast ones, being reported); Major overhaul of Raceroom opponent data handling - stopped using the slot_id to index opponents as this is very unreliable (caused inaccurate position change message, pitting message, lap times, sector deltas, etc). Opponents are now indexed using their names, and it's important to note that this causes issues when multiple opponents have the same name (e.g. when selecting more AI opponents than the car class has unique names for). The app will only record lap times and stuff for one of these opponents; Use a more sensible wheel size minimum for karts in PCars to prevent inappropriate wheelspin warnings; Don't spam log with best-guess track messages; Don't play 'push-now' messages in practice and qual; In practice and qual sessions, if the fastest lap is set before the player joins we use this historical lap for pace reports (note there will be no sector times for this lap so no sector reports until a faster lap is set) - RaceRoom only; Fixed broken sectors comparison in hotlap mode; Added a few driver names. No new sound pack for this version.

Version 3.8.2: Added info and test harness for adding new driver names and driver name mappings; Added small random delays to some messages; Tweaked R3E engine temperature check; Bug fixes - fixed some array index bugs in opponent lap times & pace reports, removed confusing per-class pace reported in qual, added missing closeChannel call to ambient & track temp request. No new sound pack for this version.

Version 3.8.1: Added conditions monitoring for PCars - will report air and track temperature if there's been a significant change (report frequencies and change thresholds are configurable), and will report rain start / stop; fixed kart wheel sizes for PCars (should make the wheelspin and brake locking warnings behave properly); some fixes for PCars UDP network data; more driver names; A couple of minor fixes. This version requires a new sound pack *if you want to use the conditions monitoring*.

Version 3.8.0: Overhauled sector time reporting to make the sound files neater and the logic for sector reports much more sensible; Fixed session end for PCars timed races; Added 'frequency_of...' options, some new and some replace existing enable / disable options. These give finer control over the behaviour of the app. For these, 0 means 'never play this message' and 10 means 'play this message whenever possible / practical'; Spotter fixes - better opponent speed calculation, don't call 'clear' if the game reports that the player and opponent cars are 'inside' each other. These should stop a lot of the bouncing; Made Raceroom aero damage threshold more sensitive; Added gap messages for gaps which are stable fluctuating a little (less than a second per sector) in addition to the existing messages for increasing / decreasing gaps;  Added some driver names. This version requires a new sound pack.

Version 3.7.2: Fixed broken 'pace' calculation in qualifying and practice - was always using -1 seconds as best opponent laptime; Added additional checks to Raceroom blue flags; Some lap time / pace / deltas reporting tweaks to make it less intrusive; Initial work for PCars console data. No new sound pack for this version.

Version 3.7.1: Fixed incorrect comparison lap for 'best lap' messages (wasn't taking player's laps into account); Some fixes in the sector delta and lap times reporting rules. No new sound pack for this version.

Version 3.7.0: Fixed missing gaps to opponent when asking "where's [driver-name]" in Raceroom; Reduce likelihood of incorrect blue flags in Raceroom; Treat terminal damage as race-end and don't play encouragement messages; Reworked 'push now' event - plays warning / encouragement near end of race if there's a chance of position change; Reworked lap time comparisons to give more helpful information - pace etc is based on recent best lap in race; Added sector deltas reporting (still some rough edges to iron out here - the sound files need some work); Added sector times response ("What are my sector times?" and "What's my last sector time?"); Added some option / prime tyre change stuff for DTM 2014; Added "what tyres is [driver-name / driver position] on?" response (for DTM 2014 and other series with prime / option tyres); Some more driver names; Various fixes. This version requires a new sound pack.

Version 3.6.5: Fixed driver name loading in RaceRoom (restarting a race with a different opponent set sometimes left the old driver names in the cache); Make opponent race laptime announcements optional (enable_opponent_laptime_reporting_in_race property, defaults to true); Added some driver names. No new sound pack for this version.

Version 3.6.4: Raceroom and PCars spotter overhaul (more reliable, less CPU intensive); Added blue flag warnings to Raceroom; Work around noise in Raceroom opponent race place data; Fixed best lap time playing in Raceroom when it shouldn't; Added lots of car class data and Raceroom now uses this (the app knows which car class each driver is using - still some work to be done to make the best use of this info); Fixed app properties not persisting between app updates; Added session lap times to console window at end of session; Added a load of driver names; First cut of DTM 2015 race end rules code (timed race + 1 extra lap at the end). No new sound pack for this version.

Version 3.6.3: Major overhaul of RaceRoom side of the app to use driver names, load driver names mid-session (for Raceroom ADAC GTM driver swaps - not implemented for PCars yet), proper RaceRoom opponent lap / sector times, pit info, wheel-spin / locking etc; Lots more driver names; New RaceRoom spotter implementation (similar to PCars version); No new sound pack for this version (but the latest driver_names pack has lots of new names).

Version 3.6.2: Reset game state when restarting a race (will still fail if you restart during the countdown phase); Added "who's in front / behind on track" response - gives driver name & position of the guy in front / behind regardless of his race position (if he's moving and not in the pits); Fixed missing "who's ahead" response (wasn't wired up in the speech recogniser); Fixed some issues in the spotter handling re-ordered participants array; Spotter now calls "car left" / "car right" after the other side goes clear following a three-wide; Make opponent speed calculation handle noisy data better; Added options to set the minimum required voice recognition confidence for regular requests ("what's my position?" and stuff - defaults to 50%), and a separate confidence level for driver-name requests ("where's Bob?" - defaults to 40%); Fixed app playing messages when monitoring other cars; Fixed app not running in free practice mode; Fixed missing program start arguments when restarting app after altering properties. No new sound pack for this version.

Version 3.6.1: Minor damage and start message tweaks; added missing pit detection for Willow Springs, Dubai and Sonoma; Tweaked Zolder pit detection. No new sound pack for this version.

Version 3.6.0: Added voice recognition to get laps (last and best) by car position as well as name ("what's p4's best lap" type stuff); Added voice recognition to get the car front or behinds best & last laps ("what's the car ahead's last lap"); Fuel remaining estimate fixes; Make the fuel event cope with refuelling; Added 'can't pronounce it' response for when the app hasn't got the voice file for an opponent; Added 'nearly empty' fuel warning; Fixed missing first lap in app's timings (time for first completed lap was being ignored); Don't play opponent best lap messages unless this lap is more than a tenth quicker than their previous best; A few fixes; Some internal rewiring for Raceroom. This version requires a new sound pack.

Version 3.5.2: Added version number to UI; fixed broken opponent lap time tracking (pace estimates were always incorrect); Fixed missing opponent best-lap reporting; More accurate and efficient opponent pitting detection; Added a delay and validation check to 'the next car is...' message; Don't play the 'let's get those places back' message when starting last; Added opponent last / best lap response ("what was bob's last lap" / "what is bob's best lap"). No new sound pack for this version.

Version 3.5.1: Fixed channel getting left open after enabling / disabling 'deltas' mode; use slicks for formula-rookie. No new sound pack for this version.

Version 3.5.0: fixed a couple of missing damage responses; added 'listen start' beep for when you press the radio button (hold mode only) - can be disabled with use_listen_beep property (I still need to find a suitable sound effect for this); Added more driver names; Fixed edge case bug in radio channel handling that could leave the radio channel open; Added "what's my fuel level" speech recognition; The fuel stuff should now work in practice and qualify and if there's not enough data to estimate the time / number of laps of fuel left (when you ask "how's my fuel") the app will simply tell you the fuel level; Added optional CPU core affinity start option - run the app from a shortcut with CPUn where n is 1 to 8, for example:
"C:\CrewChiefV4\CrewChiefV4.exe" cpu4
This will set the process's processor affinity to CPU number 4 (where the first CPU is CPU1). This version requires a new sound pack.

Version 3.4.1: Minor cold tyre temp increase (there were too many warnings); added a couple more voice options for switching off deltas; fixed some ignored enable / disable messges flags; added driver names for RaceRoom; use names, rather than array indexes, to track PCars participant data (should behave better when PCars messes up the participant data array - particularly in online practice and qual sessions); re-worked opponent gap monitoring; make wheelspin and lockup warning thresholds (total seconds per lap) configurable; Added "who's leading" voice recognition; Added "Repeat" / "say again" voice recognition. some cold tyre warning temp tweaks. No new sound pack for this version.

Version 3.4.0: Fixed some pit detection issues; added option to disable pcars opponent pit detection; disable cut track warnings on out lap / pit exit; tidied up the way numbers are read for time gaps; removed some crap green-light sounds; disable tyre wear info on pit entry for now; added a few more speech recognition bits and bobs; fixed race start message playing while in parmc ferme; don't process data when in replay, pause, or menus; fixed ignored 'enable_brake_locking_warning' and 'enable_wheel_spin_warning' options. This version requires a new sound pack.

Version 3.3.2: Another potential crash-bug in the spotter fixed, make the pit limiter detection stuff work when opponent data arrays get jumbled up in the shared memory. No new sound pack for this version.

Version 3.3.1: Added better error checking to spotter. No new sound pack for this version.

Version 3.3.0: Added separate damage thresholds for different car components; fixed fuel usage tracking in RaceRoom; disable pit detection on rolling starts; added first cut of brake locking and wheel spinning detection; added a load of driver names for online races. The version requires a new sound pack.

Version 3.2.2: Spotter additions to disregard cars which aren't moving, or who's closing speed is too high; fixes to rolling starts; added distinct car classes each with their own brake and engine temp thresholds; added temp thresholds for different tyre types; added some more race start sounds.

Version 3.2.1: Fixed missing tyre & brake temp updates; more damage handling improvements; reinstated PCars cut-track warnings (still not working very well); don't play flag messages in the pit lane; some other fixes and tweaks. No new sound pack for this version.

Version 3.2.0: Fixed button mapping for deltas-only mode; added separate sound files for deltas on / off confirmation; disabled pearls-of-wisdom for the last part of the race; fixed session time remaining issue in qual and practice; added a few new pearls of wisdom. This version requires a new sound pack.

Version 3.1.1: Fixed gap code for opponents not directly ahead or behind - now the response to "where's Bob?" should read the time gap correctly. No new sound pack for this version.

Version 3.1.0: Added 'opponent deltas' mode. This can be toggled with a button or voice command and when enabled, the time delta front and behind it read when you cross the line (the one behind is read when the guy behind crosses the line actually). These messages will still play even if you've disabled other updates ('Kimi mode'); reworked damage reporting to include suspension and brake damage (PCars), and missing wheels (PCars); added brake temperature monitoring; enhanced tyre temperature and wear monitoring; additions to the spotter to make it cope better with noisy data. This version requires a new sound pack.

Version 3.0.2: Fixed 32bit Project Cars

Version 3.0.1: Minor spotter tweak to minimise the inappropriate 'clear' calls for PCars; changed prac / qual session end detection method

Version 3.0.0: Major internal rework and rewiring; added project cars; loads and loads of other stuff



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
You need to install .net 4 or above to use the app. Download the CrewChiefV4.0.0_with_sounds.zip file, extract it somewhere (anywhere, the app's not fussy), and run the enclosed CrewChiefV4.exe. Select a game from the list at the top right. Click the "Start Application" button. Then fire up the game and be amazed at my poor voice acting.


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
The app, the voice recognition packs, and the sound pack, and the driver names pack are all available separately. To install a new version of the app, simply download the CrewChiefV4.x.y_no_sounds and unzip it over the top of your existing installation. If the sound pack also needs to be updated, when you run the app you'll get an error in the console window telling you to update the sound pack. To do this, download the latest sound pack and replace the existing one with this new one. If you want to update your driver names, you can download the latest driver_names pack and unzip this into your install directory, overwriting the /sounds/driver_names folder. At the time of writing, I plan to keep adding to the driver_names package, making it a single ever-growing set of names, rather than releasing names in small chunks. But that might change.

the full app with sounds & driver names can be downloaded here 	 : https://drive.google.com/file/d/0B4KQS820QNFbN1VfZHRZMDdSc1k/view?usp=sharing
the application (with no sounds) can be downloaded here    		 : https://drive.google.com/file/d/0B4KQS820QNFbdGF6X2xhZzk3aG8/view?usp=sharing
the sound pack can be downloaded here 	 						 : https://drive.google.com/file/d/0B4KQS820QNFbM0xoWVExOERnR2s/view?usp=sharing
the 'lo-fi' (more radio-like) sound pack can be downloaded here  : https://drive.google.com/file/d/0B4KQS820QNFbS0N3emliWW9ONHc/view?usp=sharing
the latest driver names can be downloaded here                 	 : https://drive.google.com/file/d/0B4KQS820QNFbZHRlUmxfOVV3NHc/view?usp=sharing
the latest 'lo-fi' driver names can be downloaded here           : https://drive.google.com/file/d/0B4KQS820QNFbdEd2b1FFQS1ldFE/view?usp=sharing

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