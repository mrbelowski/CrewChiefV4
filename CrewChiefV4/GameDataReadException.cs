using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    [Serializable]
    class GameDataReadException : Exception
    {
        public Exception cause;
        public String message;
        public GameDataReadException(String message, Exception cause)
        {
            this.message = message;
            this.cause = cause;
        }
        public GameDataReadException(String message)
        {
            this.message = message;
        }
    }
}
