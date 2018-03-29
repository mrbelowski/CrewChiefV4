using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4
{
    public class RemoteDataReader
    {
        // reads data from server or other remote location. This is separate from the game state,
        // but is available to the mappers
        protected Boolean enabled = false;
        protected Boolean active = false;

        public virtual Boolean autoStart() {
            return false;
        }

        public void enable()
        {
            this.enabled = true;
        }
        public void disable()
        {
            this.enabled = false;
        }

        public virtual void activate(Object activationData)
        {
            this.active = true;
        }
        public virtual void deactivate()
        {
            this.active = false;
        }
        // all game-specific readers use their own internal wrapper class, so we can't do this in a generic 
        // way without a substantial refactor. This call in each subclass will probably look quite similar
        public RemoteData getRemoteData(RemoteData remoteData, Object rawGameData)
        {
            try
            {
                if (active && enabled)
                {
                    // if this is the first reader in the chain, initialise the remoteData object.
                    // Subsequent readers in the chain will append their own data to this object.
                    if (remoteData == null)
                    {
                        remoteData = new RemoteData();
                    }
                    return getRemoteDataInternal(remoteData, rawGameData);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                throw new GameDataReadException(e.Message, e);
            }
        }

        public virtual RemoteData getRemoteDataInternal(RemoteData remoteData, Object rawGameData)
        {
            // no-op in base class
            return remoteData;
        }
    }
}
