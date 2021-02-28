﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using epg123.MxfXml;
using epg123.SchedulesDirectAPI;
using epg123.TheMovieDbAPI;
using epg123.XmltvXml;

namespace epg123.sdJson2mxf
{
    internal static partial class sdJson2Mxf
    {
        public static System.ComponentModel.BackgroundWorker BackgroundWorker;
        public static bool Success;

        private static epgConfig config;
        public static Mxf SdMxf = new Mxf();

        public static void Build(epgConfig configuration)
        {
            var errString = string.Empty;

            // initialize schedules direct API
            sdApi.Initialize("EPG123", Helper.SdGrabberVersion);

            // copy configuration to local variable
            config = configuration;

            // initialize event buffer
            Logger.WriteInformation($"Beginning EPG123 update execution. {DateTime.Now.ToUniversalTime():u}");
            Logger.WriteVerbose($"DaysToDownload: {config.DaysToDownload} , TheTVDBNumbers : {config.TheTvdbNumbers} , PrefixEpisodeTitle: {config.PrefixEpisodeTitle} , PrefixEpisodeDescription : {config.PrefixEpisodeDescription} , AppendEpisodeDesc: {config.AppendEpisodeDesc} , OADOverride : {config.OadOverride} , TMDbCoverArt: {config.TMDbCoverArt} , IncludeSDLogos : {config.IncludeSdLogos} , AutoAddNew: {config.AutoAddNew} , CreateXmltv: {config.CreateXmltv} , ModernMediaUiPlusSupport: {config.ModernMediaUiPlusSupport}");

            // populate station prefixes to suppress
            suppressedPrefixes = new List<string>(config.SuppressStationEmptyWarnings.Split(','));

            // login to Schedules Direct and build the mxf file
            if (sdApi.SdGetToken(config.UserAccount.LoginName, config.UserAccount.PasswordHash, ref errString))
            {
                // check server status
                var susr = sdApi.SdGetStatus();
                if (susr == null) return;
                else if (susr.SystemStatus[0].Status.ToLower().Equals("offline"))
                {
                    Logger.WriteError("Schedules Direct server is offline. Aborting update.");
                    return;
                }

                // check for latest version and update the display name that shows in About Guide
                var scvr = sdApi.SdCheckVersion();
                if (scvr != null && !string.IsNullOrEmpty(scvr.Version))
                {
                    SdMxf.Providers[0].DisplayName += " v" + Helper.Epg123Version;
                    if (Helper.SdGrabberVersion != scvr.Version)
                    {
                        SdMxf.Providers[0].DisplayName += $" (v{scvr.Version} Available)";
                        BrandLogo.UpdateAvailable = true;
                    }
                }

                // make sure cache directory exists
                if (!Directory.Exists(Helper.Epg123CacheFolder))
                {
                    Directory.CreateDirectory(Helper.Epg123CacheFolder);
                }
                epgCache.LoadCache();

                // initialize tmdb api
                if (config.TMDbCoverArt)
                {
                    tmdbApi.Initialize(false);
                }

                // prepopulate keyword groups
                InitializeKeywordGroups();

                // read all included and excluded station from configuration
                PopulateIncludedExcludedStations(config.StationId);

                // if all components of the mxf file have been successfully created, save the file
                if (BuildLineupServices() && ServiceCountSafetyCheck() &&
                      GetAllScheduleEntryMd5S(config.DaysToDownload) &&
                      BuildAllProgramEntries() &&
                      BuildAllGenericSeriesInfoDescriptions() && BuildAllExtendedSeriesDataForUiPlus() &&
                      GetAllMoviePosters() &&
                      GetAllSeriesImages() &&
                      GetAllSportsImages() &&
                      BuildKeywords() &&
                      WriteMxf())
                {
                    Success = true;

                    // create the xmltv file if desired
                    if (config.CreateXmltv && CreateXmltvFile())
                    {
                        WriteXmltv();
                        ++processedObjects; ReportProgress();
                    }

                    // remove the guide images xml file
                    Helper.DeleteFile(Helper.Epg123GuideImagesXmlPath);

                    // create the ModernMedia UI+ json file if desired
                    if (config.ModernMediaUiPlusSupport)
                    {
                        ModernMediaUiPlus.WriteModernMediaUiPlusJson(config.ModernMediaUiPlusJsonFilepath ?? null);
                        ++processedObjects; ReportProgress();
                    }

                    // clean the cache folder of stale data
                    CleanCacheFolder();
                    epgCache.WriteCache();

                    Logger.WriteVerbose($"Downloaded and processed {sdApi.TotalDownloadBytes} of data from Schedules Direct.");
                    Logger.WriteVerbose($"Generated .mxf file contains {SdMxf.With[0].Services.Count - 1} services, {SdMxf.With[0].SeriesInfos.Count} series, {SdMxf.With[0].Programs.Count} programs, and {SdMxf.With[0].People.Count} people with {SdMxf.With[0].GuideImages.Count} image links.");
                    Logger.WriteInformation("Completed EPG123 update execution. SUCCESS.");
                }
            }
            else
            {
                Logger.WriteError($"Failed to retrieve token from Schedules Direct. message: {errString}");
            }
            SdMxf = null;
            GC.Collect();
            Helper.SendPipeMessage("Download Complete");
        }
        private static void AddBrandLogoToMxf()
        {
            using (var ms = new MemoryStream())
            {
                BrandLogo.StatusImage(config.BrandLogoImage).Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                SdMxf.DeviceGroup.GuideImage.Image = Convert.ToBase64String(ms.ToArray());
            }
        }

        private static bool ServiceCountSafetyCheck()
        {
            if (!(SdMxf.With[0].Services.Count < config.ExpectedServicecount * 0.95)) return true;
            Logger.WriteError($"The expected number of stations to download is {config.ExpectedServicecount} but there are only {SdMxf.With[0].Services.Count} stations available from Schedules Direct. Aborting update for review by user.");
            return false;
        }

        private static bool WriteMxf()
        {
            // add dummy lineup with dummy channel
            var service = SdMxf.With[0].GetService("DUMMY");
            service.CallSign = "DUMMY";
            service.Name = "DUMMY Station";

            SdMxf.With[0].Lineups.Add(new MxfLineup()
            {
                Index = SdMxf.With[0].Lineups.Count + 1,
                Uid = "ZZZ-DUMMY-EPG123",
                Name = "ZZZ123 Dummy Lineup",
                channels = new List<MxfChannel>()
            });

            var lineupIndex = SdMxf.With[0].Lineups.Count - 1;
            SdMxf.With[0].Lineups[lineupIndex].channels.Add(new MxfChannel()
            {
                Lineup = SdMxf.With[0].Lineups[lineupIndex].Id,
                LineupUid = "ZZZ-DUMMY-EPG123",
                StationId = service.StationId,
                Service = service.Id
            });

            // make sure background worker to download station logos is complete
            processedObjects = 0; totalObjects = 1;
            ++processStage; ReportProgress();
            var waits = 0;
            while (!StationLogosDownloadComplete)
            {
                ++waits;
                System.Threading.Thread.Sleep(100);
            }
            if (waits > 0)
            {
                Logger.WriteInformation($"Waited {waits * 0.1} seconds for the background worker to complete station logo downloads prior to saving files.");
            }

            // reset counters
            processedObjects = 0; totalObjects = 1 + (config.CreateXmltv ? 1 : 0) + (config.ModernMediaUiPlusSupport ? 1 : 0);
            ++processStage; ReportProgress();

            AddBrandLogoToMxf();
            SdMxf.Providers[0].Status = Logger.EventId.ToString();
            try
            {
                using (var stream = new StreamWriter(Helper.Epg123MxfPath, false, Encoding.UTF8))
                {
                    using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
                    {
                        var serializer = new XmlSerializer(typeof(Mxf));
                        var ns = new XmlSerializerNamespaces();
                        ns.Add("", "");
                        serializer.Serialize(writer, SdMxf, ns);
                    }
                }

                Logger.WriteInformation($"Completed save of the MXF file to \"{Helper.Epg123MxfPath}\".");
                ++processedObjects; ReportProgress();
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Failed to save the MXF file to \"{Helper.Epg123MxfPath}\". Message: {ex.Message}");
            }
            return false;
        }

        private static void CleanCacheFolder()
        {
            var delCnt = 0;
            var cacheFiles = Directory.GetFiles(Helper.Epg123CacheFolder, "*.*");

            // reset counters
            processedObjects = 0; totalObjects = cacheFiles.Length;
            ++processStage; ReportProgress();

            foreach (var file in cacheFiles)
            {
                ++processedObjects; ReportProgress();
                if (file.Equals(Helper.Epg123CacheJsonPath)) continue;
                if (file.Equals(Helper.Epg123CompressCachePath)) continue;
                if (Helper.DeleteFile(file)) ++delCnt;
            }

            if (delCnt > 0)
            {
                Logger.WriteInformation($"{delCnt} files deleted from the cache directory during cleanup.");
            }
        }

        private static void WriteXmltv()
        {
            if (!config.CreateXmltv) return;
            try
            {
                using (var stream = new StreamWriter(config.XmltvOutputFile, false, Encoding.UTF8))
                {
                    using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
                    {
                        var serializer = new XmlSerializer(typeof(xmltv));
                        var ns = new XmlSerializerNamespaces();
                        ns.Add("", "");
                        serializer.Serialize(writer, xmltv, ns);
                    }
                }
                Logger.WriteInformation($"Completed save of the XMLTV file to \"{Helper.Epg123XmltvPath}\".");
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Failed to save the XMLTV file to \"{Helper.Epg123XmltvPath}\". Message: {ex.Message}");
            }
        }
    }
}