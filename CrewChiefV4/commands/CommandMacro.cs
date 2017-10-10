using CrewChiefV4.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV4.commands
{
    public class CommandMacro
    {
        AudioPlayer audioPlayer;
        MacroItem[] items;
        String confirmationMessage;
        public CommandMacro(AudioPlayer audioPlayer, String confirmationMessage, params MacroItem[] items)
        {
            this.audioPlayer = audioPlayer;
            this.confirmationMessage = confirmationMessage;
            this.items = items;
        }
        public void execute()
        {
            // blocking...
            foreach (MacroItem item in items)
            {
                if (item.macroItemType == MacroItem.MacroItemType.KEYPRESS)
                {
                    KeyPresser.SendScanCodeKeyPress(item.keycode, 20);
                }
                else if (item.macroItemType == MacroItem.MacroItemType.PAUSE)
                {
                    Thread.Sleep(item.pauseMillis);
                }
            }
            audioPlayer.playMessageImmediately(new QueuedMessage(confirmationMessage, 0, null));
        }
    }

    public class MacroItem
    {
        public CrewChiefV4.commands.KeyPresser.KeyCode keycode;
        public int pauseMillis;
        public MacroItemType macroItemType;

        public MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode keycode)
        {
            this.keycode = keycode;
            this.macroItemType = MacroItemType.KEYPRESS;
        }

        public MacroItem(int pauseMillis)
        {
            this.pauseMillis = pauseMillis;
            this.macroItemType = MacroItemType.PAUSE;
        }

        public enum MacroItemType
        {
            KEYPRESS, PAUSE
        }
    }
}
