using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.MemoryMappedFiles;

namespace iRSDKSharp
{
    public enum BroadcastMessageTypes { CamSwitchPos = 0, CamSwitchNum, CamSetState, ReplaySetPlaySpeed, ReplaySetPlayPosition, ReplaySearch, ReplaySetState, ReloadTextures, ChatCommand, PitCommand, TelemCommand };
    public enum CamSwitchModeTypes { FocusAtIncident = -3, FocusAtLeader = -2, FocusAtExciting = -1, FocusAtDriver = 0 };
    public enum CameraStateTypes { None = 0x0000, IsSessionScreen = 0x0001, IsScenicActive = 0x0002, CamToolActive = 0x0004, UIHidden = 0x0008, UseAutoShotSelection = 0x0010, UseTemporaryEdits = 0x0020, UseKeyAcceleration = 0x0040, UseKey10xAcceleration = 0x0080, UseMouseAimMode = 0x0100 };
    public enum ReplayPositionModeTypes { Begin = 0, Current, End };
    public enum ReplaySearchModeTypes { ToStart = 0, ToEnd, PreviousSession, NextSession, PreviousLap, NextLap, PreviousFrame, NextFrame, PreviousIncident, NextIncident };
    public enum ReplayStateModeTypes { Erasetape = 0 };
    public enum ReloadTexturesModeTypes { All = 0, CarIdx };
    public enum ChatCommandModeTypes { Macro = 0, BeginChat, Reply, Cancel };
    
    public enum PitCommandModeTypes
    {
        Clear = 0,
        WS = 1,
        Fuel = 2,
        LF = 3,
        RF = 4,
        LR = 5,
        RR = 6,
        ClearTires = 7,
        FastRepair = 8,
        ClearWS = 9,
        ClearFR = 10,
        ClearFuel = 11
    };

    public enum TelemCommandModeTypes { Stop = 0, Start, Restart };
    public class Defines
    {
        public const string DataValidEventName = "Local\\IRSDKDataValidEvent";
        public const string MemMapFileName = "Local\\IRSDKMemMapFileName";
        public const string BroadcastMessageName = "IRSDK_BROADCASTMSG";
        public const string PadCarNumName = "IRSDK_PADCARNUM";
        public const int MaxString = 32;
        public const int MaxDesc = 64;
        public const int MaxVars = 4096;
        public const int MaxBufs = 4;
        public const int StatusConnected = 1;
    }

    public class iRacingSDK
    {
        //VarHeader offsets
        public const int VarOffsetOffset = 4;
        public const int VarCountOffset = 8;
        public const int VarNameOffset = 16;
        public const int VarDescOffset = 48;
        public const int VarUnitOffset = 112;
        public int VarHeaderSize = 144;

        public bool IsInitialized = false;

        MemoryMappedFile iRacingFile;
        MemoryMappedViewAccessor FileMapView;

        public CiRSDKHeader Header = null;
        public Dictionary<string, CVarHeader> VarHeaders = new Dictionary<string, CVarHeader>();
        //List<CVarHeader> VarHeaders = new List<CVarHeader>();

        public bool Startup()
        {
            try
            {
                iRacingFile = MemoryMappedFile.OpenExisting(Defines.MemMapFileName);
                FileMapView = iRacingFile.CreateViewAccessor();
                VarHeaderSize = Marshal.SizeOf(typeof(VarHeader));

                Header = new CiRSDKHeader(FileMapView);
                GetVarHeaders();

                IsInitialized = true;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private void GetVarHeaders()
        {
            VarHeaders.Clear();
            for (int i = 0; i < Header.VarCount; i++)
            {
                int type = FileMapView.ReadInt32(Header.VarHeaderOffset + ((i * VarHeaderSize)));
                int offset = FileMapView.ReadInt32(Header.VarHeaderOffset + ((i * VarHeaderSize) + VarOffsetOffset));
                int count = FileMapView.ReadInt32(Header.VarHeaderOffset + ((i * VarHeaderSize) + VarCountOffset));
                byte[] name = new byte[Defines.MaxString];
                byte[] desc = new byte[Defines.MaxDesc];
                byte[] unit = new byte[Defines.MaxString];
                FileMapView.ReadArray<byte>(Header.VarHeaderOffset + ((i * VarHeaderSize) + VarNameOffset), name, 0, Defines.MaxString);
                FileMapView.ReadArray<byte>(Header.VarHeaderOffset + ((i * VarHeaderSize) + VarDescOffset), desc, 0, Defines.MaxDesc);
                FileMapView.ReadArray<byte>(Header.VarHeaderOffset + ((i * VarHeaderSize) + VarUnitOffset), unit, 0, Defines.MaxString);
                string nameStr = System.Text.Encoding.Default.GetString(name).TrimEnd(new char[] { '\0' });
                string descStr = System.Text.Encoding.Default.GetString(desc).TrimEnd(new char[] { '\0' });
                string unitStr = System.Text.Encoding.Default.GetString(unit).TrimEnd(new char[] { '\0' });
                VarHeaders[nameStr] = new CVarHeader(type, offset, count, nameStr, descStr, unitStr);
            }
        }

        public object GetData(string name)
        {
            if(IsInitialized && Header != null)
            {
                CVarHeader header = null;
                if (VarHeaders.TryGetValue(name, out header))
                {
                    int varOffset = header.Offset;
                    int count = header.Count;
                    if (header.Type == CVarHeader.VarType.irChar)
                    {
                        byte[] data = new byte[count];
                        FileMapView.ReadArray<byte>(Header.Buffer + varOffset, data, 0, count);
                        return System.Text.Encoding.Default.GetString(data).TrimEnd(new char[] { '\0' });
                    }
                    else if (header.Type == CVarHeader.VarType.irBool)
                    {
                        if (count > 1)
                        {
                            bool[] data = new bool[count];
                            FileMapView.ReadArray<bool>(Header.Buffer + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return FileMapView.ReadBoolean(Header.Buffer + varOffset);
                        }
                    }
                    else if (header.Type == CVarHeader.VarType.irInt || header.Type == CVarHeader.VarType.irBitField)
                    {
                        if (count > 1)
                        {
                            int[] data = new int[count];
                            FileMapView.ReadArray<int>(Header.Buffer + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return FileMapView.ReadInt32(Header.Buffer + varOffset);
                        }
                    }
                    else if (header.Type == CVarHeader.VarType.irFloat)
                    {
                        if (count > 1)
                        {
                            float[] data = new float[count];
                            FileMapView.ReadArray<float>(Header.Buffer + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return FileMapView.ReadSingle(Header.Buffer + varOffset);
                        }
                    }
                    else if (header.Type == CVarHeader.VarType.irDouble)
                    {
                        if (count > 1)
                        {
                            double[] data = new double[count];
                            FileMapView.ReadArray<double>(Header.Buffer + varOffset, data, 0, count);
                            return data;
                        }
                        else
                        {
                            return FileMapView.ReadDouble(Header.Buffer + varOffset);
                        }
                    }
                }
            }
            return null;
        }

        public string GetSessionInfoString()
        {
            if(IsInitialized && Header != null)
            {
                byte[] data = new byte[Header.SessionInfoLength];
                FileMapView.ReadArray<byte>(Header.SessionInfoOffset, data, 0, Header.SessionInfoLength);
                return System.Text.Encoding.Default.GetString(data).TrimEnd(new char[] { '\0' });
            }
            return null;
        }
        public byte [] GetSessionInfo()
        {
            if (IsInitialized && Header != null)
            {
                byte[] data = new byte[Header.SessionInfoLength];
                FileMapView.ReadArray<byte>(Header.SessionInfoOffset, data, 0, Header.SessionInfoLength);
                return data;
            }
            return null;
        }
        public bool IsConnected()
        {
            if (IsInitialized && Header != null)
            {
                return (Header.Status & 1) > 0;
            }
            return false;
        }

        public void Shutdown()
        {
            IsInitialized = false;
            Header = null;
            if (FileMapView != null)
            {
                FileMapView.Dispose();
            }
            if (iRacingFile != null)
            {
                iRacingFile.Dispose();
            }
            
        }
        IntPtr GetPadCarNumID()
        {
            return RegisterWindowMessage(Defines.PadCarNumName);
        }
        static IntPtr GetBroadcastMessageID()
        {
            return RegisterWindowMessage(Defines.BroadcastMessageName);
        }

        public static void BroadcastMessage(BroadcastMessageTypes msg, int var1, int var2, int var3)
        {
            BroadcastMessage(msg, var1, MakeLong((short)var2, (short)var3));
        }

        public static int BroadcastMessage(BroadcastMessageTypes msg, int var1, int var2)
        {
            IntPtr msgId = GetBroadcastMessageID();
            IntPtr hwndBroadcast = IntPtr.Add(IntPtr.Zero, 0xffff);
            IntPtr result = IntPtr.Zero;
            if (msgId != IntPtr.Zero)
            {
                result = PostMessage(hwndBroadcast, msgId.ToInt32(), MakeLong((short)msg, (short)var1), var2);
            }
            return result.ToInt32();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr RegisterWindowMessage(string lpProcName);

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public static int MakeLong(short lowPart, short highPart)
        {
            return (int)(((ushort)lowPart) | (uint)(highPart << 16));
        }

        public static short HiWord(int dword)
        {
            return (short)(dword >> 16);
        }

        public static short LoWord(int dword)
        {
            return (short)dword;
        }
    }

    


    //144 bytes
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VarHeader
    {
        //16 bytes: offset = 0
        public int type;
        //offset = 4
        public int offset;
        //offset = 8
        public int count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public int[] pad;

        //32 bytes: offset = 16
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Defines.MaxString)]
        public string name;
        //64 bytes: offset = 48
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Defines.MaxDesc)]
        public string desc;
        //32 bytes: offset = 112
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Defines.MaxString)]
        public string unit;
    }

    //32 bytes
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VarBuf
    {
        public int tickCount;
        public int bufOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] pad;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct iRSDKHeader
    {
        //12 bytes: offset = 0
        public int ver;
        public int status;
        public int tickRate;

        //12 bytes: offset = 12
        public int sessionInfoUpdate;
        public int sessionInfoLen;
        public int sessionInfoOffset;

        //8 bytes: offset = 24
        public int numVars;
        public int varHeaderOffset;
        
        //16 bytes: offset = 32
        public int numBuf;
        public int bufLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] pad1;

        //128 bytes: offset = 48
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Defines.MaxBufs)]
        public VarBuf[] varBuf;
    }
}
