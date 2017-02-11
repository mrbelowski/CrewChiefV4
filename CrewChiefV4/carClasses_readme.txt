The car classes file is json. This doesn't support comments (booo) so they're in here :)

This is still WIP, but the format is pretty straightforward. A car class will inherit the defaults values for things if they're not specified. The valid params are:

{
	"carClassEnum": this is the built in car class group. If this doesn't match, the app will assign this to UNKNOWN_RACE car group, with the params you specify
	"pCarsClassNames": this is an array of pCars classnames. * or ? denote wildcards. The app searches for classes which have full matches before considering wildcards
	"rf2ClassNames": as above, for rFactor2
	"rf1ClassNames": as above, for rFactor1
	"amsClassNames": as above, for Automobilista
	"acClassNames": as above, for Assetto Corsa
	"raceroomClassIds": this is an array of integers or R3E class IDs	
	"brakeType": can be "Carbon", "Ceramic", "Iron_Race" or "Iron_Road"
	"defaultTyreType": can be "Road", "Bias_Ply", "Unknown_Race", and various others that the app doesn't really use (still to be worked on...)
	"maxSafeWaterTemp": water temp above this (celsius) will trigger a warning
	"maxSafeOilTemp": oil temp above this (celsius) will trigger a warning
	"minTyreCircumference": used to override default tyre size settings, used when calculating wheel locking and spinning. Only karts should need to override this
	"maxTyreCircumference": used to override default tyre size settings, used when calculating wheel locking and spinning. Only karts should need to override this
}


the default values are:
{
	"brakeType": "Iron_Race"
	"defaultTyreType": "Unknown_Race"
	"maxSafeWaterTemp": 105
	"maxSafeOilTemp": 125
	"minTyreCircumference": 0.4 * pi (40cm diameter wheel)
	"maxTyreCircumference": 1.2 * pi (120cm diameter wheel) 
}

Car classes can skip an entry if they're using the default - e.g. most cars skip defaultTyreType so they use Unknown_Race - this is only overridden in road cars.

IMPORTANT....

The file in [install_dir]/ will be *overwritten without warning* on update. If you want to define your own car classes or modify the built in ones, create a new file [My Documents]/CrewChiefV4/carClassData.json. This only needs to contain additions and overrides - the app parses the default car classes (the file in install_dir) then classes in the My Documents version override and add to the default classes.