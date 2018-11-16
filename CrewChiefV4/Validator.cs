using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrewChiefV4
{
    class Validator
    {
        // TODO: add more undeserving users to this list as and when they crawl out the woodwork
        // These users are persistent wreckers, trolls, rude, ungrateful, moaners, or any combination.
         private static HashSet<String> wnkers = new HashSet<String>(StringComparer.InvariantCultureIgnoreCase) { "mr.sisterfister", "bigsilverhotdog", 
             "paul hance", "aline senna", "giuseppe sangalli", "patrick förster", "chris iwaski", "gazman", "peter koch",
             "andreas christiansen", "greg metcalf", "Aditas H1Z1Cases.com.", "Bruno Bæ",
             "maciej bugno", "patrick schilhan", "tim heinemann", "Josh Cassar", "Jesse Hoppo"};
 
          public static void validate(String str)
          {
             if (wnkers.Contains(str.Trim()))
             {
                 throw new ValidationException();
             }
          }
    }

    /**
     * special exception for special players so they can grumble that the app is spamming an exception. I'd call it something
     * more colourful but my usual charm deserts me.
     */
    class ValidationException : Exception
    {
        public ValidationException()
            : base() { }
    }
}
