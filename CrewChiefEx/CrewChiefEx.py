
import ac
import acsys
import sys
import math, os.path
import platform
import mmap
# import libraries
if platform.architecture()[0] == "64bit":
    sysdir=os.path.dirname(__file__)+'/stdlib64'
else:
    sysdir=os.path.dirname(__file__)+'/stdlib'
sys.path.insert(0, sysdir)
os.environ['PATH'] = os.environ['PATH'] + ";."



try:
  import configparser
except ImportError as e:
  ac.log("{}".format(e))


from sim_info import SimInfo
from sim_info import SPageFileCrewChief
from sim_info import acsVehicleInfo

maxcars = 64
drivers = []
player = 0
maxSlotId = 0
siminfo = SimInfo()
driverName = ""

l_lapcount=0
l_driver=0
l_drivers=0
l_flag=0

lapcount=0
state = ''
def splineToDistanceRoundTrack(tracklen, splinepos):

    return (splinepos  * tracklen) / 1


def updateSharedMemory():
    global siminfo,maxSlotId,state
    sharedmem = siminfo.getsharedmem()

    sharedmem.numVehicles = ac.getCarsCount()
    sharedmem.focusVehicle = ac.getFocusedCar()
    tracklenght = siminfo.static.trackSPlineLength
    
    sessionTimeValue = siminfo.graphics.sessionTimeLeft
    ValueSeconds = (sessionTimeValue / 1000) % 60
    ValueMinutes = (sessionTimeValue // 1000) // 60
    iscountdown = 0
    if int(ValueMinutes) == 30 and ValueSeconds < 9.999:
        iscountdown = 1
    sharedmem.isCountdown = iscountdown

    #now we'll build the slots, so we later know every single (possible) car
    carIds = range(0, ac.getCarsCount(), 1) 
    for carId in carIds:
        #first we'll check wether there is a car for this id; as soon it returns -1
        #it's over
        sharedmem.vehicleInfo[carId].carModel = str(ac.getCarName(carId))
        if sharedmem.vehicleInfo[carId].carModel == '-1':
            break
        else:
            splits = ac.getLastSplits(carId)
            split = range(0, len(splits), 1)
            if len(splits) >= 1:
                sharedmem.vehicleInfo[carId].lastSector1T = split[0]
            if len(splits) >= 2:
                sharedmem.vehicleInfo[carId].lastSector2T = split[1]
            if len(splits) >= 3:
                sharedmem.vehicleInfo[carId].lastSector3T = split[2]

            sharedmem.vehicleInfo[carId].carId = carId
            sharedmem.vehicleInfo[carId].driverName = ac.getDriverName(carId)
            sharedmem.vehicleInfo[carId].carModel = ac.getCarName(carId)
            sharedmem.vehicleInfo[carId].speedMS = ac.getCarState(carId, acsys.CS.SpeedMS)
            sharedmem.vehicleInfo[carId].speedMPH = ac.getCarState(carId, acsys.CS.SpeedMPH)
            sharedmem.vehicleInfo[carId].speedKMH = ac.getCarState(carId, acsys.CS.SpeedKMH)
            sharedmem.vehicleInfo[carId].bestLapMS = ac.getCarState(carId, acsys.CS.BestLap)
            sharedmem.vehicleInfo[carId].lapCount = ac.getCarState(carId, acsys.CS.LapCount)
            sharedmem.vehicleInfo[carId].currentLapInvalid = ac.getCarState(carId, acsys.CS.LapInvalidated)
            sharedmem.vehicleInfo[carId].currentLapTimeMS = ac.getCarState(carId, acsys.CS.LapTime)
            sharedmem.vehicleInfo[carId].lastLapTimeMS = ac.getCarState(carId, acsys.CS.LastLap)
            sharedmem.vehicleInfo[carId].localAngularVelocity = ac.getCarState(carId, acsys.CS.LocalAngularVelocity)
            sharedmem.vehicleInfo[carId].localVelocity = ac.getCarState(carId, acsys.CS.LocalVelocity)
            sharedmem.vehicleInfo[carId].speedTotal = ac.getCarState(carId, acsys.CS.SpeedTotal)
            sharedmem.vehicleInfo[carId].velocity = ac.getCarState(carId, acsys.CS.Velocity)
            sharedmem.vehicleInfo[carId].worldPosition = ac.getCarState(carId, acsys.CS.WorldPosition)
            sharedmem.vehicleInfo[carId].isCarInPitline = ac.isCarInPitline(carId)
            sharedmem.vehicleInfo[carId].isCarInPit = ac.isCarInPit(carId)
            sharedmem.vehicleInfo[carId].carLeaderboardPosition = ac.getCarLeaderboardPosition(carId)
            sharedmem.vehicleInfo[carId].carRealTimeLeaderboardPosition = ac.getCarRealTimeLeaderboardPosition(carId)
            sharedmem.vehicleInfo[carId].distanceRoundTrack = splineToDistanceRoundTrack(tracklenght, ac.getCarState(carId, acsys.CS.NormalizedSplinePosition) )
            sharedmem.vehicleInfo[carId].isConnected = ac.isConnected(carId)


def acMain(ac_version):
  global appWindow,l_lapcount,l_driver,l_drivers,l_flag
  appWindow = ac.newApp("CrewChiefEx")
  ac.setTitle(appWindow, "CrewChiefEx")

  ac.setSize(appWindow, 400, 200)

  ac.log("Hello, Assetto Corsa application world!")
  ac.console("Hello, Assetto Corsa console!")

  l_lapcount = ac.addLabel(appWindow, "Driver:");
  l_driver = ac.addLabel(appWindow, "Car:");
  l_drivers = ac.addLabel(appWindow, "Cars Connected:");
  l_flag = ac.addLabel(appWindow, "flag:");
  ac.setPosition(l_lapcount, 3, 30)
  ac.setPosition(l_driver, 3, 42)
  ac.setPosition(l_drivers, 3, 54)
  ac.setPosition(l_flag, 3, 66)

  return "CrewChiefEx"

def acUpdate(deltaT):
    global siminfo
    global l_lapcount, lapcount,l_driver,l_drivers,maxSlotId,l_flag
    updateSharedMemory()
    siminfo.update()
    sharedmem = siminfo.getsharedmem()
    currentSplits = []
	
    #currentSplits = ac.getCurrentSplits(0)
    #cursplit = range(0, len(currentSplits), 1)
	#if len(currentSplits) >= 1:
	#	sharedmem.vehicleInfo[carId].currentSector1T = cursplit[0]
    #if len(currentSplits) >= 2:
	#	sharedmem.vehicleInfo[carId].currentSector2T = cursplit[1]
    #if len(currentSplits) >= 3:
	#	sharedmem.vehicleInfo[carId].currentSector3T = cursplit[2]
    #currentDriver = "Driver: " + sharedmem.vehicleInfo[0].driverName
    #carModel = "Car: " + sharedmem.vehicleInfo[0].carModel
    #sessiontype = siminfo.graphics.session
    #sessiontimeLeft = format(siminfo.graphics.sessionTimeLeft)
    #splits = ac.getLastSplits(0)
    #servername = ac.getServerName()
    #currentLapInvalid = sharedmem.vehicleInfo[0].currentLapInvalid;
    #connected = ""
    #connected = "Valid lap: " + format(currentLapInvalid)
    #connected = "Cars Connected: " + format(sharedmem.numVehicles)
    #connected = ""
    #splits = []
    #splits = siminfo.graphics.split
    #split=0
    #sessionTimeValue = siminfo.graphics.sessionTimeLeft

    #lastLapValueSeconds = (sessionTimeValue / 1000) % 60
    #lastLapValueMinutes = (sessionTimeValue // 1000) // 60

    #iscountdown = 0
    #if int(lastLapValueMinutes) == 30 and lastLapValueSeconds < 9.999:
    #    iscountdown = 1

    #timeleft = "time left:" + format(int(lastLapValueMinutes)) + "{:.0f}" + format(lastLapValueSeconds)
    #for split in splits:
    #    connected += "splits: " + format(split) + " "
    #connected = format(splineToDistanceRoundTrack(siminfo.static.trackSPlineLength, ac.getCarState(0, acsys.CS.NormalizedSplinePosition)) )  
    #connected = "session: " + format(sessiontype)
    #flag = "flag:" + format(siminfo.graphics.flag)
    
    #ac.setText(l_lapcount, currentDriver )
    #ac.setText(l_driver, connected )  
    #ac.setText(l_drivers, currentLapInvalid )
    #ac.setText(l_flag, timeleft )
    
    





