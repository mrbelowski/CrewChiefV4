using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrewChiefV4
{
    class NameValidator
    {

        // TODO: add more undeserving shitbags to this list as and when they crawl out the woodwork
         private static String[] wankers = new String[] { "Mr.SisterFister", "BigSilverHotdog", 
             "Paul Hance", "Aline Senna", "Giuseppe Sangalli", "Patrick Förster", "Chris Iwaski", "gazman"
             /* ",Valentino Rossi" TODO add Rossi next time he behaves like a spoiled 5 year old */};
 
          public static Boolean validateName(String name)
          {
             if (wankers.Contains(name))
             {
                 Application.Exit();
             }
              return true;
          }
    }
}
