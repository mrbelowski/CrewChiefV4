import ac
import acsys
import sys
import os.path
import platform
import configparser
import re

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

sharedMem = CrewChiefShared()

pluginVersion = "1.1.0"

timer = 0
lib = None
libInit = 0
loadMemoryModule = 0
connectToKMR = 0
configPath = "apps/python/CrewChiefEx/config.txt"

config = configparser.ConfigParser()
config.read(configPath)

#KMR stuff
kmr_applink_app_id = "CrewChief"
kmr_applink_app_id_encoded = kmr_applink_app_id.encode('utf-8')
kmr_applink_ip = ""
kmr_applink_port = 0
kmr_applink_token = ""
kmr_applink_new_server_data = 0
kmr_applink_chat_fail_counter = 99
kmr_timer = 0
kmr_sent_chat_message = 0

try:
  loadMemoryModule = config.getint("CrewChief", "LoadMemoryModule")
except:
  config.set("CrewChief", "LoadMemoryModule", str(0))
  loadMemoryModule = 0

try:
  connectToKMR = config.getint("CrewChief", "ConnectToKMR")
except:
  config.set("CrewChief", "ConnectToKMR", str(0))
  connectToKMR = 0

if platform.architecture()[0] == "64bit" and loadMemoryModule == 1:
    lib = ctypes.WinDLL(sysdir + '/ACInternalMemoryReader.dll')
    

if lib != None:
    funcInit = lib['Init']
    funcInit.restype = ctypes.c_int32
    libInit = funcInit()

if libInit == 1:
    funcGetSuspensionDamage = lib['GetSuspensionDamage']
    funcGetSuspensionDamage.restype = ctypes.c_float
    funcGetSuspensionDamage.argtypes = [ctypes.c_int32,ctypes.c_int32]

    funcGetEngineLifeLeft = lib['GetEngineLifeLeft']
    funcGetEngineLifeLeft.restype = ctypes.c_float
    funcGetEngineLifeLeft.argtypes = [ctypes.c_int32]

    funcGetTyreInflation = lib['GetTyreInflation']
    funcGetTyreInflation.restype = ctypes.c_float
    funcGetTyreInflation.argtypes = [ctypes.c_int32, ctypes.c_int32]

def updateSharedMemory():
    global sharedMem
    sharedmem = sharedMem.getsharedmem()
    #KMR stuff
    if connectToKMR and kmr_applink_port:
        sharedmem.kmrData.applink_app_id = kmr_applink_app_id_encoded
        sharedmem.kmrData.applink_ip = kmr_applink_ip.encode('utf-8')
        sharedmem.kmrData.applink_port = kmr_applink_port
        sharedmem.kmrData.applink_token = kmr_applink_token.encode('utf-8')

    sharedmem.numVehicles = ac.getCarsCount()
    sharedmem.focusVehicle = ac.getFocusedCar()
    #now we'll build the slots, so we later know every single (possible) car
    carIds = range(0, ac.getCarsCount(), 1)
    for carId in carIds:
        #first we'll check wether there is a car for this id; as soon it returns -1
        #it's over
        if str(ac.getCarName(carId)) == '-1':
            break 			
        else:
            sharedmem.vehicleInfo[carId].carId = carId
            sharedmem.vehicleInfo[carId].driverName = ac.getDriverName(carId).encode('utf-8')
            sharedmem.vehicleInfo[carId].carModel = ac.getCarName(carId).encode('utf-8')
            sharedmem.vehicleInfo[carId].speedMS = ac.getCarState(carId, acsys.CS.SpeedMS)
            sharedmem.vehicleInfo[carId].bestLapMS = ac.getCarState(carId, acsys.CS.BestLap)
            sharedmem.vehicleInfo[carId].lapCount = ac.getCarState(carId, acsys.CS.LapCount)
            sharedmem.vehicleInfo[carId].currentLapInvalid = ac.getCarState(carId, acsys.CS.LapInvalidated)
            sharedmem.vehicleInfo[carId].currentLapTimeMS = ac.getCarState(carId, acsys.CS.LapTime)
            sharedmem.vehicleInfo[carId].lastLapTimeMS = ac.getCarState(carId, acsys.CS.LastLap)
            sharedmem.vehicleInfo[carId].worldPosition = ac.getCarState(carId, acsys.CS.WorldPosition)
            sharedmem.vehicleInfo[carId].isCarInPitline = ac.isCarInPitline(carId)
            sharedmem.vehicleInfo[carId].isCarInPit = ac.isCarInPit(carId)
            sharedmem.vehicleInfo[carId].carLeaderboardPosition = ac.getCarLeaderboardPosition(carId)
            sharedmem.vehicleInfo[carId].carRealTimeLeaderboardPosition = ac.getCarRealTimeLeaderboardPosition(carId)
            sharedmem.vehicleInfo[carId].spLineLength = ac.getCarState(carId, acsys.CS.NormalizedSplinePosition) 
            sharedmem.vehicleInfo[carId].isConnected = ac.isConnected(carId)
            if libInit == 1 and carId == 0:
                sharedmem.vehicleInfo[carId].suspensionDamage[0] = funcGetSuspensionDamage(carId,0)
                sharedmem.vehicleInfo[carId].suspensionDamage[1] = funcGetSuspensionDamage(carId,1)
                sharedmem.vehicleInfo[carId].suspensionDamage[2] = funcGetSuspensionDamage(carId,2)
                sharedmem.vehicleInfo[carId].suspensionDamage[3] = funcGetSuspensionDamage(carId,3)
                sharedmem.vehicleInfo[carId].engineLifeLeft = funcGetEngineLifeLeft(carId)
                sharedmem.vehicleInfo[carId].tyreInflation[0] = funcGetTyreInflation(carId,0)
                sharedmem.vehicleInfo[carId].tyreInflation[1] = funcGetTyreInflation(carId,1)
                sharedmem.vehicleInfo[carId].tyreInflation[2] = funcGetTyreInflation(carId,2)
                sharedmem.vehicleInfo[carId].tyreInflation[3] = funcGetTyreInflation(carId,3)

def acMain(ac_version):
  global appWindow,sharedMem

  appWindow = ac.newApp("CrewChiefEx")

  ac.setTitle(appWindow, "CrewChiefEx")
  ac.setSize(appWindow, 300, 40)

  ac.log("CrewChief Was Here! damage report ?")
  ac.console("CrewChief Was Here! damage report ?")

  sharedmem = sharedMem.getsharedmem()
  sharedmem.serverName = ac.getServerName().encode('utf-8')
  sharedmem.acInstallPath = os.path.abspath(os.curdir).encode('utf-8')
  sharedmem.isInternalMemoryModuleLoaded = libInit
  sharedmem.pluginVersion = pluginVersion.encode('utf-8')

  # chat listener for KMR
  ac.addOnChatMessageListener(appWindow, onMessage)
  return "CrewChiefEx"

def acUpdate(deltaT):
  global timer, kmr_sent_chat_message, kmr_timer
  timer += deltaT
  kmr_timer += deltaT
  if not kmr_sent_chat_message and connectToKMR:
      if kmr_timer > 6:
          # send the magic KMR message after a few seconds
          kmr_sent_chat_message = 1
          sendKMRChatMessage()
      else:
          kmr_timer += deltaT
  if timer > 0.025:
      updateSharedMemory()
      timer = 0

def onMessage(message, by):
    global kmr_applink_ip, kmr_applink_port, kmr_applink_token, kmr_applink_chat_fail_counter
    if by == "SERVER" and kmr_applink_chat_fail_counter < 9:
        m = re.search("AppLink: ([^:]+):([\d]+) ([^ ]+)$", message)
        if m:
            # this is the message we're looking for. Parse it.
            kmr_applink_ip = m.group(1)
            kmr_applink_port = int(m.group(2))
            kmr_applink_token = m.group(3)
            ac.log("Got KMR response")
            ac.console("Got KMR response")
        else:
            # too bad the message is not the right one, let's increment the fail counter
            kmr_applink_chat_fail_counter += 1

def sendKMRChatMessage():
    global kmr_applink_chat_fail_counter
    # let's reset the fail counter
    kmr_applink_chat_fail_counter = 0
    # and send the initialization chat message
    ac.log("Sending KMR chat message")
    ac.console("Sending KMR chat message")
    ac.sendChatMessage("/kmr applink")