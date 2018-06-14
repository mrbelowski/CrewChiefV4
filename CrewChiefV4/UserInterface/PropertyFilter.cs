/*
 * Implements property filtering logic.  Used to filter out properties in Properties form.  Currently supports Include/Exclude, common vs not and text filtering.
 * Include/Exclude filters are specified in ui_text.txt.
 * 
 * Official website: thecrewchief.org 
 * License: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4
{
    internal class PropertyFilter
    {
        internal List<GameEnum> filterList = null;
        internal bool includeFilter = true;
        internal string propertyLabelUpper = null;

        internal PropertyFilter(string filter, string propertyId, string propertyLabel)
        {
            this.propertyLabelUpper = propertyLabel.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(filter))
                return;

            var gameDefinitionNames = filter.Split(';');
            if (gameDefinitionNames[0].StartsWith("!"))  // Exclude filter.
            {
                this.includeFilter = false;
                // Remove '!' char.
                gameDefinitionNames[0] = gameDefinitionNames[0].Substring(1);
            }
            else  // Include filter.
                this.includeFilter = true;

            this.filterList = new List<GameEnum>();
            foreach (var game in gameDefinitionNames)
            {
                GameEnum gameEnum;
                if (Enum.TryParse(game, out gameEnum))
                    this.filterList.Add(gameEnum);
                else
                {
                    Console.WriteLine("Failed to parse filter: \"" + filter + "\"  property: \"" + propertyId + "\"");
                    this.filterList = null;
                    return;
                }
            }
        }

        internal bool Applies(string textFilterUpper, GameEnum gameFilter, PropertiesForm.SpecialFilter specialFilter, bool includeCommon)
        {
            if (specialFilter != PropertiesForm.SpecialFilter.UNKNOWN)
            {
                if (specialFilter == PropertiesForm.SpecialFilter.COMMON_PREFERENCES
                    && (this.filterList != null  // This has a limitation that if filter has list of all games, this won't be true, even though property is common.
                        || !this.includeFilter))  // If exclude filter is set, this is not a common preference.
                    return false;
            }
            else if (gameFilter != GameEnum.UNKNOWN)
            {
                if (includeCommon && this.filterList == null)
                {
                    // If we are asked to include common preferences, and this preference has no filter, it is common.  Fall through to the text filtering.
                }
                else
                {
                    if (this.includeFilter)
                    {
                        if (this.filterList == null || !this.filterList.Contains(gameFilter))
                            return false;
                    }
                    else  // Exclude filter.
                    {
                        Debug.Assert(this.filterList != null);
                        if (this.filterList.Contains(gameFilter))
                            return false;

                        if (!includeCommon)
                            return false;
                    }
                }
            }

            return string.IsNullOrWhiteSpace(textFilterUpper) || this.propertyLabelUpper.Contains(textFilterUpper);
        }
    }
}
