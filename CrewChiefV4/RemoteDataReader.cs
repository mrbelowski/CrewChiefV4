using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4
{
    public class RemoteDataReader
    {
        // reads data from server or other remote location to augment local game data
        protected Boolean enabled = false;
        protected Boolean active = false;

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
        public void populateRemoteData(Object dataDataWrapper)
        {
            if (active && enabled)
            {
                populateRemoteDataInternal(dataDataWrapper);
            }
        }

        private virtual void populateRemoteDataInternal(Object dataWrapper) {
            // no-op in base class
        }
    }
}
