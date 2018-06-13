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
        public static float soundPackVersion = -1;
        public static float driverNamesVersion = -1;
        public static float personalisationsVersion = -1;

        public static float latestSoundPackVersion = -1;
        public static float latestDriverNamesVersion = -1;
        public static float latestPersonalisationsVersion = -1;

        public static String retryReplace;
        public static String retryReplaceWith;

        // if sound pack version >= 146 use update3, >= 122 use update2, >= 0 use update, else use base
        // To add a new pack, add to the front of the appropriate list
        //
        // Note that the first entry in each of these 3 lists doesn't (yet) exist in the autoupdate data,
        // so the XML element from the parser will be null, which is interpreted as 'no update available'.
        public static SoundPackData[] soundPacks = { new SoundPackData(146, "update3soundpackurl"),
                                                     new SoundPackData(122, "update2soundpackurl"),
                                                     new SoundPackData(0, "updatesoundpackurl"),
                                                     new SoundPackData(-1, "basesoundpackurl")
                                                   };

        public static SoundPackData[] personalisationPacks = { new SoundPackData(129, "update3personalisationsurl"),
                                                               new SoundPackData(121, "update2personalisationsurl"),
                                                               new SoundPackData(0, "updatepersonalisationsurl"),
                                                               new SoundPackData(-1, "basepersonalisationsurl")
                                                             };

        public static SoundPackData[] drivernamesPacks = { new SoundPackData(130, "update2drivernamesurl"),
                                                           new SoundPackData(0, "updatedrivernamesurl"),
                                                           new SoundPackData(-1, "basedrivernamesurl")
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
            public SoundPackData(int upgradeFromVersion, String elementName)
            {
                this.upgradeFromVersion = upgradeFromVersion;
                this.elementName = elementName;
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
