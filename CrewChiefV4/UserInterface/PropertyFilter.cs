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
        internal List<PropertiesForm.PropertyCategory> categoryList = null;
        internal bool includeFilter = true;
        internal string propertyLabelUpper = null;

        internal PropertyFilter(string filter, string category, string propertyId, string propertyLabel)
        {
            this.propertyLabelUpper = propertyLabel.ToUpperInvariant();

            // Process category filter.
            if (!string.IsNullOrWhiteSpace(category))
            {
                var categoryNames = category.Split(';');

                this.categoryList = new List<PropertiesForm.PropertyCategory>();
                foreach (var cat in categoryNames)
                {
                    var catEnum = PropertiesForm.PropertyCategory.UNKNOWN;
                    if (Enum.TryParse(cat, out catEnum) 
                        && Enum.IsDefined(typeof(PropertiesForm.PropertyCategory), catEnum))
                        this.categoryList.Add(catEnum);
                    else
                    {
                        Console.WriteLine("Failed to parse category: \"" + cat + "\"  property: \"" + propertyId + "\"");
                        this.categoryList = null;
                    }
                }
            }

            // Process game filter.
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
                if (Enum.TryParse(game, out gameEnum)
                    && Enum.IsDefined(typeof(GameEnum), gameEnum))
                    this.filterList.Add(gameEnum);
                else
                {
                    Console.WriteLine("Failed to parse filter: \"" + filter + "\"  property: \"" + propertyId + "\"");
                    this.filterList = null;
                    return;
                }
            }
        }

        internal bool Applies(string textFilterUpper, GameEnum gameFilter, PropertiesForm.SpecialFilter specialFilter, bool includeCommon, PropertiesForm.PropertyCategory categoryFilter)
        {
            if (categoryFilter != PropertiesForm.PropertyCategory.ALL)
            {
                if (this.categoryList == null)
                {
                    // By default, properties go to MISC.
                    if (categoryFilter != PropertiesForm.PropertyCategory.MISC)
                        return false;
                }
                else if (!this.categoryList.Contains(categoryFilter))
                    return false;
            }

            if (specialFilter != PropertiesForm.SpecialFilter.UNKNOWN)
            {
                // Any property with non empty filter is not common in a sense that it does not apply to all games ("Common Properties" selected in the UI).
                // However, if specific game is selected, and "Show common" checkbox is checked, it is only excluded from "common" properties if the exclude filter
                // for the selected game is specified.  Should've named that checkbox "Show shared" instead.
                // Note: this check has a limitation that if filter has list of all games, this won't be true, even though property is common.
                // Easy to overcome if needed by adding method to check if filter covers all games.
                if (specialFilter == PropertiesForm.SpecialFilter.COMMON_PREFERENCES
                    && this.filterList != null) 
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
