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
        public static List<SoundPackData> voiceMessageUpdatePacks = new List<SoundPackData>();
        public static List<SoundPackData> personalisationUpdatePacks = new List<SoundPackData>();
        public static List<SoundPackData> drivernamesUpdatePacks = new List<SoundPackData>();

        private static List<SoundPackData> createNewPackData(XElement element)
        {
            List<SoundPackData> packData = new List<SoundPackData>();
            if (float.TryParse(element.Attribute(XName.Get("latest-version", "")).Value, out latestSoundPackVersion))
            {
                int finalVersion = 0;
                foreach (XElement packUpdateElement in element.Descendants("update-pack"))
                {
                    String url = packUpdateElement.Attribute(XName.Get("url", "")).Value;
                    int updatesFromVersion;
                    if (int.TryParse(packUpdateElement.Attribute(XName.Get("updates-from-version", "")).Value, out updatesFromVersion))
                    {
                        // create the entry
                        packData.Add(new SoundPackData(updatesFromVersion, url));
                        if (updatesFromVersion > finalVersion) {
                            finalVersion = updatesFromVersion;
                        }
                    }
                }
                // now get the biggest version and tag the intermediate versions as requiring another download
                foreach (SoundPackData data in packData)
                {
                    if (data.upgradeFromVersion > -1 && data.upgradeFromVersion < finalVersion)
                    {
                        data.willRequireAnotherUpdate = true;
                    }
                }
            }
            // now sort the list, lowest first
            packData.Sort((a, b) => a.upgradeFromVersion.CompareTo(b.upgradeFromVersion));
            return packData;
        }

        public static Boolean parseUpdateData(String updateXML)
        {
            try
            {
                XDocument doc = XDocument.Parse(updateXML);
                if (doc.Descendants("sounds").Count() > 0)
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
                    foreach (XElement element in doc.Descendants("sounds"))
                    {
                        XAttribute languageAttribute = element.Attribute(XName.Get("language", ""));
                        if (languageAttribute.Value == languageToCheck)
                        {
                            // this is the update set for this language
                            XElement voiceMessagesElement = element.Descendants("voice-messages").First();
                            if (float.TryParse(voiceMessagesElement.Attribute(XName.Get("latest-version", "")).Value, out latestSoundPackVersion)) 
                            {
                                voiceMessageUpdatePacks.AddRange(createNewPackData(voiceMessagesElement));
                            }
                            XElement driverNamesElement = element.Descendants("driver-names").First();
                            if (float.TryParse(driverNamesElement.Attribute(XName.Get("latest-version", "")).Value, out latestDriverNamesVersion))
                            {
                                drivernamesUpdatePacks.AddRange(createNewPackData(driverNamesElement));
                            }
                            XElement personalisationElement = element.Descendants("personalisations").First();
                            if (float.TryParse(personalisationElement.Attribute(XName.Get("latest-version", "")).Value, out latestPersonalisationsVersion))
                            {
                                personalisationUpdatePacks.AddRange(createNewPackData(personalisationElement));
                            }
                            gotLanguageSpecificUpdateInfo = true;
                            break;
                        }
                    }
                    if (!gotLanguageSpecificUpdateInfo && AudioPlayer.soundPackLanguage == null)
                    {
                        if (float.TryParse(doc.Descendants("soundpackversion").First().Value, out latestSoundPackVersion))
                        {
                            voiceMessageUpdatePacks.Add(new SoundPackData(-1, doc.Descendants("basesoundpackurl").First().Value));
                            voiceMessageUpdatePacks.Add(new SoundPackData(0, doc.Descendants("updatesoundpackurl").First().Value));
                            voiceMessageUpdatePacks.Add(new SoundPackData(122, doc.Descendants("update2soundpackurl").First().Value));
                        }
                        if (float.TryParse(doc.Descendants("drivernamesversion").First().Value, out latestDriverNamesVersion))
                        {
                            drivernamesUpdatePacks.Add(new SoundPackData(-1, doc.Descendants("basedrivernamesurl").First().Value));
                            drivernamesUpdatePacks.Add(new SoundPackData(0, doc.Descendants("updatedrivernamesurl").First().Value));
                        }
                        if (float.TryParse(doc.Descendants("personalisationsversion").First().Value, out latestPersonalisationsVersion))
                        {
                            personalisationUpdatePacks.Add(new SoundPackData(-1, doc.Descendants("basepersonalisationsurl").First().Value));
                            personalisationUpdatePacks.Add(new SoundPackData(-1, doc.Descendants("updatepersonalisationsurl").First().Value));
                            personalisationUpdatePacks.Add(new SoundPackData(-1, doc.Descendants("update2personalisationsurl").First().Value));
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
            public String url;
            // if this is true, after downloading this pack the user will have to download another. False for
            // the base pack and the old cumulative update packs (everything at the time of writing), true for others except the most recent.
            public Boolean willRequireAnotherUpdate = false;

            public SoundPackData(int upgradeFromVersion, String url)
            {
                this.upgradeFromVersion = upgradeFromVersion;
                this.url = url;
            }
        }
    }
}
