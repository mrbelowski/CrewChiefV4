using System;
using System.Text;

namespace CrewChiefV4.iRacing
{
    public class CameraState : BitfieldBase<CameraStates>
    {
        public CameraState() : this(0) { }

        public CameraState(int value) : base(value)
        {
        }
    }

    [Flags]
    public enum CameraStates : uint
    {
        IsSessionScreen = 0x0001, // the camera tool can only be activated if viewing the session screen (out of car)
        IsScenicActive = 0x0002, // the scenic camera is active (no focus car)
        CamToolActive = 0x0004,
        UIHidden = 0x0008,
        UseAutoShotSelection = 0x0010,
        UseTemporaryEdits = 0x0020,
        UseKeyAcceleration = 0x0040,
        UseKey10xAcceleration = 0x0080,
        UseMouseAimMode = 0x0100
    }
}
