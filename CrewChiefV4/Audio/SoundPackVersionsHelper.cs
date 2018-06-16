using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrewChiefV4.Audio
{
    class SoundPackVersionsHelper
    {
        public static float currentSoundPackVersion = -1;
        public static float currentDriverNamesVersion = -1;
        public static float currentPersonalisationsVersion = -1;

        public static float latestSoundPackVersion = -1;
        public static float latestDriverNamesVersion = -1;
        public static float latestPersonalisationsVersion = -1;

        public static String retryReplace;
        public static String retryReplaceWith;

        // if current version <= soundpackdata version, use this one
        //
        // As of sound pack version 146, personalisation version 129, driver names version 130 the approach has changed.
        // Now each update is *not* cumulative. That is, update N-1 does not contain all the sounds from update N. We used
        // to add all new sounds to all old updates.
        // So now, when a user with very old sound data updates he'll get the oldest applicable sound pack download, which 
        // will install and prompt for a restart. He'll then be prompted to download another sound pack update, and so on.
        //
        // The base sound pack will always have everything
        public static SoundPackData[] soundPacks = { new SoundPackData(-1, "basesoundpackurl", false),
                                                     // any sound packs after base will require an additional download if they're not the last in the list
                                                     //
                                                     new SoundPackData(0, "updatesoundpackurl", true),
                                                     new SoundPackData(122, "update2soundpackurl", true),
                                                     new SoundPackData(146, "update3soundpackurl", false)
                                                   };

        public static SoundPackData[] personalisationPacks = { new SoundPackData(-1, "basepersonalisationsurl", false),
                                                               // any personalisations after base will require an additional download if they're not the last in the list
                                                               //
                                                               // TODO: change these additional download flags to true when we publish the next update personalisations pack
                                                               // (the first non-cumulative pack). This will update from version 129
                                                               new SoundPackData(0, "updatepersonalisationsurl", false),
                                                               new SoundPackData(121, "update2personalisationsurl", false)
                                                             };

        public static SoundPackData[] drivernamesPacks = { new SoundPackData(-1, "basedrivernamesurl", false),
                                                           // any name packs after base will require an additional download if they're not the last in the list
                                                           //
                                                           new SoundPackData(0, "updatedrivernamesurl", true),
                                                           new SoundPackData(130, "update2drivernamesurl", false),
                                                         };

        public static Boolean parseUpdateData(String updateXML)
        {
            try
            {
                XDocument doc = XDocument.Parse(updateXML);
                if (doc.Descendants("soundpack").Count() > 0)
                {
                    String languageToCheck = AudioPlayer.soundPackLanguage == null ? "en" : AudioPlayer.soundPackLanguage;
                    Boolean gotLanguageSpecificUpdateInfo = false;
                    try
                    {
                        retryReplace = doc.Descendants("retry_replace").First().Value;
                        retryReplaceWith = doc.Descendants("retry_replace_with").First().Value;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("No retry download location available");
                    }
                    foreach (XElement element in doc.Descendants("soundpack"))
                    {
                        XAttribute languageAttribute = element.Attribute(XName.Get("language", ""));
                        if (languageAttribute.Value == languageToCheck)
                        {
                            // this is the update set for this language
                            float.TryParse(element.Descendants("soundpackversion").First().Value, out latestSoundPackVersion);
                            float.TryParse(element.Descendants("drivernamesversion").First().Value, out latestDriverNamesVersion);
                            float.TryParse(element.Descendants("personalisationsversion").First().Value, out latestPersonalisationsVersion);

                            foreach (SoundPackVersionsHelper.SoundPackData soundPackData in SoundPackVersionsHelper.soundPacks)
                            {
                                soundPackData.setDownloadLocation(element);
                            }
                            foreach (SoundPackVersionsHelper.SoundPackData personalisationsPackData in SoundPackVersionsHelper.personalisationPacks)
                            {
                                personalisationsPackData.setDownloadLocation(element);
                            }
                            foreach (SoundPackVersionsHelper.SoundPackData drivernamePackData in SoundPackVersionsHelper.drivernamesPacks)
                            {
                                drivernamePackData.setDownloadLocation(element);
                            }
                            gotLanguageSpecificUpdateInfo = true;
                            break;
                        }
                    }
                    if (!gotLanguageSpecificUpdateInfo && AudioPlayer.soundPackLanguage == null)
                    {
                        float.TryParse(doc.Descendants("soundpackversion").First().Value, out latestSoundPackVersion);
                        float.TryParse(doc.Descendants("drivernamesversion").First().Value, out latestDriverNamesVersion);
                        float.TryParse(doc.Descendants("personalisationsversion").First().Value, out latestPersonalisationsVersion);

                        foreach (SoundPackVersionsHelper.SoundPackData soundPackData in SoundPackVersionsHelper.soundPacks)
                        {
                            soundPackData.setDownloadLocation(doc);
                        }
                        foreach (SoundPackVersionsHelper.SoundPackData personalisationsPackData in SoundPackVersionsHelper.personalisationPacks)
                        {
                            personalisationsPackData.setDownloadLocation(doc);
                        }
                        foreach (SoundPackVersionsHelper.SoundPackData drivernamePackData in SoundPackVersionsHelper.drivernamesPacks)
                        {
                            drivernamePackData.setDownloadLocation(doc);
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error parsing autoupdate data");
            }
            return false;
        }

        public class SoundPackData
        {
            public int upgradeFromVersion;
            public String elementName;
            public String downloadLocation = null;
            // if this is true, after downloading this pack the user will have to download another. False for
            // the base pack and the old cumulative update packs (everything at the time of writing), true for others except the most recent.
            public Boolean willRequireAnotherUpdate = false;

            public SoundPackData(int upgradeFromVersion, String elementName, Boolean willRequireAnotherUpdate)
            {
                this.upgradeFromVersion = upgradeFromVersion;
                this.elementName = elementName;
                this.willRequireAnotherUpdate = willRequireAnotherUpdate;
            }

            // use the first decendant or null if we can't get it
            public void setDownloadLocation(System.Xml.Linq.XElement parent)
            {
                try
                {
                    this.downloadLocation = parent.Descendants(elementName).First().Value;
                }
                catch (Exception)
                {
                }
            }

            // use the first decendant or null if we can't get it
            public void setDownloadLocation(System.Xml.Linq.XDocument parent)
            {
                try
                {
                    this.downloadLocation = parent.Descendants(elementName).First().Value;
                }
                catch (Exception)
                {
                }
            }
        }

    }
}
