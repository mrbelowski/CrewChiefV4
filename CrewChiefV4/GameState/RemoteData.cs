using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.GameState
{
    // value object to hold all remote data gather from the various remote readers
    public class RemoteData
    {
        public ACKMRData acKMRData = new ACKMRData();
        public RestData restData = new RestData();
    }

    public class ACKMRData
    {

    }

    public class RestData
    {
        public String pacenotesSet;
        public Boolean pacenotesEnabled = false;
    }
}
