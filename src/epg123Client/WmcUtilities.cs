﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using epg123;

namespace epg123Client
{
    internal static class WmcUtilities
    {
        public static System.ComponentModel.BackgroundWorker BackgroundWorker;

        private static readonly string Mcupdate = Environment.ExpandEnvironmentVariables("%WINDIR%") + @"\ehome\mcupdate.exe";
        private static readonly string Loadmxf = Environment.ExpandEnvironmentVariables("%WINDIR%") + @"\ehome\loadmxf.exe";

        #region ========== MCUpdate ==========
        public static bool PerformWmcConfigurationsBackup()
        {
            var ret = false;
            try
            {
                // establish program to run and environment for import
                var startInfo = new ProcessStartInfo()
                {
                    FileName = Mcupdate,
                    Arguments = "-b -nogc",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // begin import
                var proc = Process.Start(startInfo);
                if (proc != null)
                {
                    // wait for exit and process exit code
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        ret = true;
                        Logger.WriteInformation("Successfully forced a Media Center database configuration backup. Exit code: 0");
                    }
                    else
                    {
                        Logger.WriteError($"Error using mcupdate to force a Media Center database configuration backup. Exit code: {proc.ExitCode}");
                    }
                }
                else
                {
                    Logger.WriteError($"Error using mcupdate to force a Media Center database configuration backup.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Exception thrown during PerformWmcConfigurationsBackup(). {ex.Message}\n{ex.StackTrace}");
            }
            return ret;
        }

        public static bool PerformGarbageCleanup()
        {
            var ret = false;
            const string hklmEpgKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Media Center\Service\EPG";
            const string nextDbgcKey = "dbgc:next run time";
            var nextRunTime = DateTime.Now + TimeSpan.FromDays(5);

            try
            {
                // read registry to see if database garbage cleanup is needed
                using (var key = Registry.LocalMachine.OpenSubKey(hklmEpgKey, true))
                {
                    if (key != null)
                    {
                        // verify periodic downloads are not enabled
                        if ((int)key.GetValue("dl", 1) != 0) key.SetValue("dl", 0);

                        // write a last index time in the future to avoid the dbgc kicking off a reindex while importing the mxf file
                        key.SetValue("LastFullReindex", Convert.ToString(nextRunTime, CultureInfo.InvariantCulture));

                        string nextRun;
                        if ((nextRun = (string)key.GetValue(nextDbgcKey)) != null)
                        {
                            var deltaTime = DateTime.Parse(nextRun) - DateTime.Now;
                            if (deltaTime > TimeSpan.FromHours(12) && deltaTime < TimeSpan.FromDays(5)) return true;
                        }
                    }
                    else
                    {
                        Logger.WriteInformation("Could not verify when garbage cleanup was last run.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInformation($"Exception thrown during PerformGarbageCleanup(). {ex.Message}\n{ex.StackTrace}");
            }

            Logger.WriteMessage("Entering PerformGarbageCleanup().");
            Helper.SendPipeMessage("Importing|Performing garbage cleanup...");
            try
            {
                // establish program to run and environment
                var startInfo = new ProcessStartInfo()
                {
                    FileName = Mcupdate,
                    Arguments = "-dbgc -updateTrigger",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // begin import
                var proc = Process.Start(startInfo);
                if (proc != null)
                {
                    proc.OutputDataReceived += Process_OutputDataReceived;
                    proc.BeginOutputReadLine();
                    proc.ErrorDataReceived += Process_ErrorDataReceived;
                    proc.BeginErrorReadLine();

                    // wait for exit and process exit code
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        ret = true;
                        Logger.WriteInformation("Successfully completed garbage cleanup. Exit code: 0");
                    }
                    else
                    {
                        Logger.WriteError($"Error using mcupdate.exe to perform database garbage cleanup. Exit code: {proc.ExitCode}");
                    }
                }
                else
                {
                    Logger.WriteError($"Error using mcupdate.exe to perform database garbage cleanup.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Exception thrown during PerformGarbageCleanup(). {ex.Message}\n{ex.StackTrace}");
            }

            if (ret)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(hklmEpgKey, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(nextDbgcKey, nextRunTime.ToString("s"));
                        }
                        else
                        {
                            Logger.WriteError("Could not set next garbage cleanup time in registry.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteError($"Exception thrown during PerformGarbageCleanup(). {ex.Message}\n{ex.StackTrace}");
                }
            }

            Logger.WriteMessage($"Exiting PerformGarbageCleanup(). {(ret ? "SUCCESS" : "FAILURE")}.");
            return ret;
        }
        #endregion

        #region ========== LoadMxf ==========
        public static bool ImportMxfFile(string mxfFile)
        {
            var ret = false;
            Logger.WriteMessage($"Entering ImportMxfFile() for file \"{mxfFile}\".");
            try
            {
                // establish program to run and environment for import
                var startInfo = new ProcessStartInfo()
                {
                    FileName = Loadmxf,
                    Arguments = $"-i \"{mxfFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // begin import
                var proc = Process.Start(startInfo);
                if (proc != null)
                {
                    proc.OutputDataReceived += Process_OutputDataReceived;
                    proc.BeginOutputReadLine();
                    proc.ErrorDataReceived += Process_ErrorDataReceived;
                    proc.BeginErrorReadLine();

                    // wait for exit and process exit code
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        ret = true;
                        Logger.WriteInformation("Successfully imported .mxf file into Media Center database. Exit code: 0");
                    }
                    else
                    {
                        Logger.WriteError($"Error using loadmxf.exe to import new guide information. Exit code: {proc.ExitCode}");
                    }
                }
                else
                {
                    Logger.WriteError($"Error using loadmxf.exe to import new guide information.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Exception thrown during ImportMxfFile(). {ex.Message}\n{ex.StackTrace}");
            }

            Logger.WriteMessage($"Exiting ImportMxfFile(). {(ret ? "SUCCESS" : "FAILURE")}.");
            return ret;
        }

        public static bool SetWmcTunerLimits(int tuners)
        {
            var ret = false;
            string[] countries = { /*"default", */"au", "be", "br", "ca", "ch", "cn", "cz", "de", "dk", "es", "fi", "fr", "gb", "hk", "hu", "ie", "in",/* "it",*/ "jp", "kr", "mx", "nl", "no", "nz", "pl",/* "pt",*/ "ru", "se", "sg", "sk",/* "tr", "tw",*/ "us", "za" };

            // create mxf file with increased tuner limits
            var xml = "<?xml version=\"1.0\" standalone=\"yes\"?>\r\n" +
                      "<MXF version=\"1.0\" xmlns=\"\">\r\n" +
                      "  <Assembly name=\"mcstore\">\r\n" +
                      "    <NameSpace name=\"Microsoft.MediaCenter.Store\">\r\n" +
                      "      <Type name=\"StoredType\" />\r\n" +
                      "    </NameSpace>\r\n" +
                      "  </Assembly>\r\n" +
                      "  <Assembly name=\"ehshell\">\r\n" +
                      "    <NameSpace name=\"ServiceBus.UIFramework\">\r\n" +
                      "      <Type name=\"TvSignalSetupParams\" />\r\n" +
                      "    </NameSpace>\r\n" +
                      "  </Assembly>\r\n";
            xml += string.Format("  <With maxRecordersForHomePremium=\"{0}\" maxRecordersForUltimate=\"{0}\" maxRecordersForRacing=\"{0}\" maxRecordersForBusiness=\"{0}\" maxRecordersForEnterprise=\"{0}\" maxRecordersForOthers=\"{0}\">\r\n", tuners);

            foreach (var country in countries)
            {
                if (country.Equals("ca"))
                {
                    // sneak this one in for our Canadian friends just north of the (contiguous) border to be able to tune ATSC stations from the USA
                    xml += $"    <TvSignalSetupParams uid=\"tvss-{country}\" atscSupported=\"true\" autoSetupLikelyAtscChannels=\"34, 35, 36, 43, 31, 39, 38, 32, 41, 27, 19, 51, 44, 42, 30, 28\" tvRatingSystem=\"US\" />\r\n";
                }
                else
                {
                    xml += $"    <TvSignalSetupParams uid=\"tvss-{country}\" />\r\n";
                }
            }

            xml += "  </With>\r\n";
            xml += "</MXF>";

            try
            {
                // create temporary file
                var mxfFilepath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mxf");
                using (var writer = new StreamWriter(mxfFilepath, false))
                {
                    writer.Write(xml);
                }

                // import tweak using loadmxf.exe because for some reason the MxfImporter doesn't work for this
                ret = ImportMxfFile(mxfFilepath);

                // delete temporary file
                Helper.DeleteFile(mxfFilepath);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Exception thrown during SetWmcTunerLimits(). {ex.Message}\n{ex.StackTrace}");
            }
            return ret;
        }
        #endregion

        #region ========== ReIndex Tasks ==========
        public static bool ReindexDatabase()
        {
            return RunWmcIndexTask("ReindexSearchRoot", "ehPrivJob.exe", "/DoReindexSearchRoot");
        }

        public static bool ReindexPvrSchedule()
        {
            return RunWmcIndexTask("PvrScheduleTask", "mcupdate.exe", "-PvrSchedule -nogc");
        }

        private static bool RunWmcIndexTask(string task, string program, string argument)
        {
            var ret = false;
            Logger.WriteMessage($"Entering RunWmcIndexTask({task})");
            try
            {
                // establish program to run and environment
                var startInfo = new ProcessStartInfo()
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/run /tn \"Microsoft\\Windows\\Media Center\\{task}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // begin reindex
                var proc = Process.Start(startInfo);
                if (proc != null)
                {
                    proc.OutputDataReceived += Process_OutputDataReceived;
                    proc.BeginOutputReadLine();
                    proc.ErrorDataReceived += Process_ErrorDataReceived;
                    proc.BeginErrorReadLine();

                    // wait for exit and process exit code
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        ret = true;
                        Logger.WriteInformation($"Successfully started the {task} task. Exit code: 0");
                        goto Finish;
                    }
                    Logger.WriteInformation($"Error using schtasks.exe to start {task} task. Exit code: {proc.ExitCode}");
                }
                else
                {
                    Logger.WriteInformation($"Error using schtasks.exe to start {task} task.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInformation($"Exception thrown during RunWmcIndexTask() using schtasks.exe. {ex.Message}\n{ex.StackTrace}");
            }

            try
            {
                // if schtasks did not work, try using the program directly
                Logger.WriteVerbose($"Attempting \"{program} {argument}\" to index data.");
                var startInfo = new ProcessStartInfo()
                {
                    FileName = Environment.ExpandEnvironmentVariables("%WINDIR%") + $"\\ehome\\{program}",
                    Arguments = argument,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // begin reindex again
                var proc = Process.Start(startInfo);
                if (proc != null)
                {
                    proc.OutputDataReceived += Process_OutputDataReceived;
                    proc.BeginOutputReadLine();
                    proc.ErrorDataReceived += Process_ErrorDataReceived;
                    proc.BeginErrorReadLine();

                    // wait for exit and process exit code
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        ret = true;
                        Logger.WriteInformation($"Successfully completed the {task} task. Exit code: 0");
                    }
                    else
                    {
                        Logger.WriteError($"Error using {program} to start {task} task. Exit code: {proc.ExitCode}");
                    }
                }
                else
                {
                    Logger.WriteError($"Error using {program} to start {task} task.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Exception thrown during RunWmcIndexTask() using {program}. {ex.Message}\n{ex.StackTrace}");
            }

            Finish:
            Logger.WriteMessage($"Exiting RunWmcIndexTask({task}). {(ret ? "SUCCESS" : "FAILURE")}.");
            return ret;
        }
        #endregion

        public static bool DetermineRecordingsInProgress()
        {
            try
            {
                // establish program to run and environment for import
                var startInfo = new ProcessStartInfo()
                {
                    FileName = "schtasks.exe",
                    Arguments = "/query /tn \"\\Microsoft\\Windows\\Media Center\\RecordingRestart\" /fo table",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                // begin import
                var proc = Process.Start(startInfo);
                if (proc != null)
                {
                    var error = proc.StandardError.ReadToEnd();
                    var table = proc.StandardOutput.ReadToEnd();

                    // wait for exit and process exit code
                    proc.WaitForExit();

                    // check for error
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Logger.WriteInformation($"An error occurred trying to determine if recordings are in progress. {error}");
                    }
                    else
                    {
                        return table.Contains("Ready");
                    }
                }
                else
                {
                    Logger.WriteInformation($"An error occurred trying to determine if recordings are in progress.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInformation($"Exception thrown during DetermineRecordingsInProgress() using schtasks.exe. {ex.Message}\n{ex.StackTrace}");
            }
            return false;
        }

        #region ========== Process Outputs ==========
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            var data = e.Data;
            if (data.Length > 0)
            {
                Logger.WriteInformation(data);
            }
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            var data = e.Data;
            if (data.Length <= 0) return;
            if (data.StartsWith("Loading... "))
            {
                Helper.SendPipeMessage($"Importing|{data}");
                BackgroundWorker?.ReportProgress(int.Parse(data.Substring(11).TrimEnd('%')));
            }
            else
            {
                Logger.WriteVerbose(data);
            }
        }
        #endregion
    }
}