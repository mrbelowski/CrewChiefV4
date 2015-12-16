using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace CrewChiefV4.PCars
{
    //public class eGameState {public eGameState eGameState { get; set; }}

    public enum eGameState
    {
        [Description("Waiting for game to start...")]
        GAME_EXITED = 0,
        [Description("In Menus")]
        GAME_FRONT_END,
        [Description("In Session")]
        GAME_INGAME_PLAYING,
        [Description("Game Paused")]
        GAME_INGAME_PAUSED,
        [Description("Undefined")]
        GAME_UNDEFINED,
        [Description("Viewing replay")]
        GAME_VIEWING_REPLAY,
        //-------------
        GAME_MAX
    }
}
