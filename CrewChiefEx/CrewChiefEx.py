
import ac
import acsys
import sys
import math, os.path
import platform
import pickle, threading
# import libraries
if platform.architecture()[0] == "64bit":
    sysdir=os.path.dirname(__file__)+'/stdlib64'
else:
    sysdir=os.path.dirname(__file__)+'/stdlib'
sys.path.insert(0, sysdir)
os.environ['PATH'] = os.environ['PATH'] + ";."

import ctypes
from ctypes import *
from shared_mem import CrewChiefShared
from shared_mem import SPageFileCrewChief
from shared_mem import acsVehicleInfo

sharedMem = CrewChiefShared()

timer = 0
isOnline = -1
trackLenght = 0
logIt = False
driveNameAndPosLabel = ""
def splineToDistanceRoundTrack(tracklen, splinepos):

    return (splinepos * tracklen)


def updateSharedMemory():
    global sharedMem,isOnline,trackLenght
    sharedmem = sharedMem.getsharedmem()
    sharedMem.update()
    isCountDown = 0
    sharedmem.numVehicles = ac.getCarsCount()
    sharedmem.focusVehicle = ac.getFocusedCar()
    trackLenght = sharedMem.static.trackSPlineLength
    #small hack to detect if session is in countdown fase
    
    sessionTimeValue = sharedMem.graphics.sessionTimeLeft
    ValueSeconds = (sessionTimeValue / 1000) % 60
    ValueMinutes = (sessionTimeValue // 1000) // 60
    sessiontype = sharedMem.graphics.session
   
    if isOnline > 0:
        if sessiontype == 2 or sessiontype == 5 or sessiontype == 6: 
            if ValueMinutes >= 0.0 and ValueSeconds >= 0.1:
                isCountDown = 1
    elif int(ValueMinutes) == 30 and ValueSeconds < 9.999:
        isCountDown = 1
    
    sharedmem.isCountdown = isCountDown

    #now we'll build the slots, so we later know every single (possible) car
    carIds = range(0, ac.getCarsCount(), 1) 
    for carId in carIds:
        #first we'll check wether there is a car for this id; as soon it returns -1
        #it's over
        sharedmem.vehicleInfo[carId].carModel = str(ac.getCarName(carId))
        if sharedmem.vehicleInfo[carId].carModel == '-1':
            break
        else:
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
            sharedmem.vehicleInfo[carId].carRealTimeLeaderboardPosition = ac.getCarRealTimeLeaderboardPosition(carId)+1
            sharedmem.vehicleInfo[carId].distanceRoundTrack = splineToDistanceRoundTrack(trackLenght, ac.getCarState(carId, acsys.CS.NormalizedSplinePosition) )
            sharedmem.vehicleInfo[carId].isConnected = ac.isConnected(carId)

            

def acMain(ac_version):
  global appWindow,sharedMem,isOnline
  serverName = ""
  appWindow = ac.newApp("CrewChiefEx")
  ac.setTitle(appWindow, "CrewChiefEx")
  ac.setSize(appWindow, 300, 40)

  ac.log("CrewChief Was Here! damage report ?")
  ac.console("CrewChief Was Here! damage report ?")

  sharedmem = sharedMem.getsharedmem()
  serverName = ac.getServerName()
  isOnline = len(serverName)
  sharedmem.serverName = serverName
  sharedmem.isOnline = isOnline

  return "CrewChiefEx"



def acUpdate(deltaT):
    global timer
    timer += deltaT
    if timer > 0.05:
        updateSharedMemory()
        timer = 0

