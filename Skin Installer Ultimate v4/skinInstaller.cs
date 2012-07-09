using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Globalization;
using SevenZip;
//using System.Data.SQLite;

using RAFlibPlus;
using ItzWarty;
using zlib = ComponentAce.Compression.Libs.zlib;

namespace SkinInstaller
{
    public partial class skinInstaller : Form
    {
        #region Variables

        private string euGameDirectory = @"C:\Program Files\League of Legends\";
        private string euGameDirectory64 = @"C:\Program Files (x86)\League of Legends\";
        private string usGameDirectory = @"C:\Riot Games\League of Legends\";
        private string gameDirectory = string.Empty;

        private string versionURL = "";
        private string changeLogURL = "https://sites.google.com/site/siuupdates/version.txt";
        WebClient webClient = new WebClient();

        private FileHandler SIFileOp = new FileHandler();

        //private SQLiteConnection sqLiteCon = new SQLiteConnection();

        private Dictionary<String, RAFFileListEntry> fileEntriesFull = new Dictionary<String, RAFFileListEntry>();
        private Dictionary<String, List<RAFFileListEntry>> fileEntriesShort = new Dictionary<String, List<RAFFileListEntry>>();
        private List<String> airFiles = new List<String>();

        Stopwatch rafTimer = new Stopwatch();
        Stopwatch airTimer = new Stopwatch();

        #endregion // Variables

        #region Structs

        #endregion // Structs

        public skinInstaller()
        {
            InitializeComponent();

            //// Initialize the database
            //sqLiteCon.ConnectionString = "data source=\"" + Application.StartupPath + "\\skins.db\"";
            //sqLiteCon.Open();

            // Set culture info to try and fix errors concerning locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            // Select the last used tab
            tabControl.SelectTab(Properties.Settings.Default.lastSelectedTab);
        }

        private void skinInstaller_Load(object sender, EventArgs e)
        {
            //if (Application.StartupPath.Length > 63)
            //{
            //    Cliver.Message.Inform("The path that this program is running from\n\"" +
            //        Application.StartupPath + "\" \nIs too long and could potentially cause errors.\n\n" +
            //        "It is STRONGLY advised to move this folder to a shorter path location (like C:\\siu)\n" +
            //        "Please do this now before continuing");
            //    this.Close();
            //}
            if (Application.StartupPath.ToLower().Contains("temp") && !Properties.Settings.Default.hideTempWarningMessage)
            {

                bool hideTempWarningMessage = false;
                if (Cliver.Message.Show("No no no no noooo!!!!", SystemIcons.Warning,
                    out hideTempWarningMessage,
                    "The path that this program is running from\n\"" + Application.StartupPath +
                    "\" \nAppears to be a temp directory (like from winrar)\r\nThis program will NOT work unless you fully extract it first!\r\n\r\nPlease close this program, extract it instead of running it, and then run the extracted program.", 0, new string[]
				{
					"Ok", 
					"I PROMISE I have fully extracted this, and know what that means"
				}) == 0)
                {
                    base.Close();
                }
                Properties.Settings.Default.hideTempWarningMessage = hideTempWarningMessage;
                Properties.Settings.Default.Save();

            }

            // Set 7zip location for extraction and compression use
            SevenZipCompressor.SetLibraryPath(Application.StartupPath + @"\7z.dll");

            // Cleanup and leftover temp directories
            if (Directory.Exists(Application.StartupPath + @"\extractedFiles\"))
            {
                this.SIFileOp.DirectoryDelete(Application.StartupPath + @"\extractedFiles\", true);
            }

            // Check for program updates
            //
            // Need to ask Greg to add a version.ini
            //
            //
            MessageBox.Show(Application.ProductVersion.ToString());

            // Find League installation
            if (Properties.Settings.Default.gameDir != "" && (File.Exists(Properties.Settings.Default.gameDir + "lol.launcher.exe") || File.Exists(Properties.Settings.Default.gameDir + "league of legends.exe")))
            {
                gameDirectory = Properties.Settings.Default.gameDir;
            }
            else if (File.Exists(usGameDirectory + "lol.launcher.exe"))
            {
                gameDirectory = usGameDirectory = Properties.Settings.Default.gameDir;
            }
            else if (File.Exists(euGameDirectory + "lol.launcher.exe"))
            {
                gameDirectory = euGameDirectory = Properties.Settings.Default.gameDir;
            }
            else if (File.Exists(euGameDirectory64 + "lol.launcher.exe"))
            {
                gameDirectory = euGameDirectory64 = Properties.Settings.Default.gameDir;
            }
            else
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = Application.StartupPath;
                dialog.Title = @"Please locate your League of Legends\lol.launcher.exe file..";
                dialog.CheckFileExists = true;
                dialog.FileName = "lol.launcher.exe";
                dialog.Filter = "Executable (*.exe)|*.exe";

                bool flag = true;
                while ((gameDirectory == string.Empty) && flag)
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string[] strArray = dialog.FileName.ToString().ToLower().Split(new char[] { '\\' });
                        for (int i = 0; i < (strArray.Length - 1); i++)
                        {
                            gameDirectory = gameDirectory + strArray[i] + @"\";
                        }
                        if ((strArray[strArray.Length - 1] == "lol.launcher.exe") || (strArray[strArray.Length - 1] == "league of legends.exe"))
                        {
                            Properties.Settings.Default.gameDir = gameDirectory;
                            flag = false;
                        }
                        else
                        {
                            gameDirectory = string.Empty;
                            if (Cliver.Message.Show("Error", SystemIcons.Error, "Failed to locate League of Legends.exe, do you want to try again?", 0, new string[2] { "Yes", "No" }) == 0)
                            {
                                flag = true;
                            }
                            else
                            {
                                flag = false;
                            }
                        }
                    }
                    else if (Cliver.Message.Show("Error", SystemIcons.Error, "Failed to locate League of Legends.exe, do you want to try again?", 0, new string[2] { "Yes", "No" }) == 0)
                    {
                        flag = true;
                    }
                    else
                    {
                        flag = false;
                    }
                }
            }

            // Error out garena users
            if (gameDirectory.ToLower().Contains("garena"))
            {
                Cliver.Message.Show("Unsupported :<", SystemIcons.Error,
                    "The garena client is currently not supported by SIU,\r\nPlans are to add this soon,\r\n\r\n" +
                    "Most features will not work right now, but you can still use it to check for updates", 0,
                 new string[1] { "OK" });
            }
            else
            {
                rafTimer.Start();
                // Load raf files
                List<String> rafFilePaths = getRAFFiles(gameDirectory + @"RADS\projects\lol_game_client\filearchives");

                foreach (String path in rafFilePaths)
                {
                    RAFArchive raf = new RAFArchive(path);

                    fileEntriesFull = combineFileDicts(fileEntriesFull, raf.FileDictFull);
                    fileEntriesShort = combineFileDicts(fileEntriesShort, raf.FileDictShort);
                }
                rafTimer.Stop();

                airTimer.Start();
                // Load Air files
                airFiles.AddRange(Directory.GetFiles(gameDirectory + @"RADS\projects\lol_air_client\releases", "*", SearchOption.AllDirectories));
                airTimer.Stop();
            }

            this.Focus();
        }

        private void skinInstaller_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save the application settings
            Properties.Settings.Default.lastSelectedTab = tabControl.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void skinInstaller_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void skinInstaller_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                List<string> usable = new List<string>();
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);   
                    FileAttributes attr = File.GetAttributes(fi.FullName);

                    //detect whether its a directory or file
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        string[] fileArray = Directory.GetFiles(fi.FullName, "*.*", SearchOption.AllDirectories);
                        foreach (string path in fileArray)
                        {
                            usable.Add(path);
                        }
                    }
                    else
                    {
                        usable.Add(fi.FullName);
                    }
                }

                // Switch to the add new skins tab
                tabControl.SelectTab(0);

                // Validate the added files
                processAddedFiles(usable);
            }
        }

        private void processAddedFiles(List<String> addedFiles)
        {
            if (!Directory.Exists(Application.StartupPath + @"\extractedFiles"))
            {
                Directory.CreateDirectory(Application.StartupPath + @"\extractedFiles");
            }

            bool extracted = false;

            foreach (String file in addedFiles)
            {
                if (file.Trim().ToLower().EndsWith(".zip") ||
                    file.Trim().ToLower().EndsWith(".rar") ||
                    file.Trim().ToLower().EndsWith(".7z") ||
                    file.Trim().ToLower().EndsWith(".bzip2") ||
                    file.Trim().ToLower().EndsWith(".gzip") ||
                    file.Trim().ToLower().EndsWith(".tar"))
                {
                    extracted = true;

                    SevenZipExtractor extractor = new SevenZipExtractor(file);
                    extractor.ExtractArchive(Application.StartupPath + @"\extractedFiles"); 
                }
                else if (file.Trim().ToLower().EndsWith(".u9lolpatch"))
                {
                    extracted = true;

                    File.Move(file, file.Replace(".u9lolpatch", ".zip"));
                    SevenZipExtractor extractor = new SevenZipExtractor(file.Replace(".u9lolpatch", ".zip"));
                    extractor.ExtractArchive(Application.StartupPath + @"\extractedFiles");
                }
                
                // Check to make sure none of the extracted files are archives
                if (extracted)
                {
                    while (extracted)
                    {
                        extracted = false;
                        String[] extractedFiles = Directory.GetFiles(Application.StartupPath + @"\extractedFiles", "*", SearchOption.AllDirectories);

                        foreach (String extractedFile in extractedFiles)
                        {
                            if (extractedFile.Trim().ToLower().EndsWith(".zip") ||
                                extractedFile.Trim().ToLower().EndsWith(".rar") ||
                                extractedFile.Trim().ToLower().EndsWith(".7z") ||
                                extractedFile.Trim().ToLower().EndsWith(".bzip2") ||
                                extractedFile.Trim().ToLower().EndsWith(".gzip") ||
                                extractedFile.Trim().ToLower().EndsWith(".tar"))
                            {
                                extracted = true;

                                SevenZipExtractor extractor = new SevenZipExtractor(extractedFile);
                                extractor.ExtractArchive(Application.StartupPath + @"\extractedFiles");
                                File.Delete(extractedFile);
                            }
                            else if (extractedFile.Trim().ToLower().EndsWith(".u9lolpatch"))
                            {
                                extracted = true;

                                File.Move(extractedFile, extractedFile.Replace(".u9lolpatch", ".zip"));
                                SevenZipExtractor extractor = new SevenZipExtractor(extractedFile.Replace(".u9lolpatch", ".zip"));
                                extractor.ExtractArchive(Application.StartupPath + @"\extractedFiles");
                                File.Delete(extractedFile.Replace(".u9lolpatch", ".zip"));
                            }
                        }
                    }
                }
            }

            // All files should now be fully extracted and all archives deleted

            // Create final list of all files to be added
            List<String> finalAddedFiles = Directory.GetFiles(Application.StartupPath + @"\extractedFiles", "*", SearchOption.AllDirectories).ToList();
            foreach (String file in addedFiles)
            {
                if (!file.Trim().ToLower().EndsWith(".zip") &&
                    !file.Trim().ToLower().EndsWith(".rar") &&
                    !file.Trim().ToLower().EndsWith(".7z") &&
                    !file.Trim().ToLower().EndsWith(".bzip2") &&
                    !file.Trim().ToLower().EndsWith(".gzip") &&
                    !file.Trim().ToLower().EndsWith(".tar"))
                {
                    finalAddedFiles.Add(file);
                }
            }

            //
            //
            //
            // Ask Greg about addInOldFiles() and the need for it anymore.
            // 
            //
            //
            //

            //
            //
            //
            // Ask Greg if he thinks it is still necessary to check for old naming style
            //
            //
            //
            //

            // Try to find the corresponding RAFFileListEntry/Air file to match the added file
            foreach (String file in finalAddedFiles)
            {
                getFileLocation(file);
            }
        }

        #region Helper Functions

        private List<String> getRAFFiles(String baseDir)
        {
            String[] folders = Directory.GetDirectories(baseDir);

            List<String> returnFiles = new List<String>();

            foreach (String folder in folders)
            {
                returnFiles.AddRange(Directory.GetFiles(folder, "*.raf", SearchOption.TopDirectoryOnly));
            }
            return returnFiles;
        }

        private Dictionary<String, RAFFileListEntry> combineFileDicts(Dictionary<String, RAFFileListEntry> Dict1, Dictionary<String, RAFFileListEntry> Dict2)
        {
            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in Dict2)
            {
                if (!Dict1.ContainsKey(entryKVP.Key))
                    Dict1.Add(entryKVP.Key, entryKVP.Value);
                else
                {
                    if (Convert.ToInt32(entryKVP.Value.RAFArchive.GetID().Replace(".", "")) > Convert.ToInt32(Dict1[entryKVP.Key].RAFArchive.GetID().Replace(".", "")))
                        Dict1[entryKVP.Key] = entryKVP.Value;
                }
            }
            return Dict1;
        }

        private Dictionary<String, List<RAFFileListEntry>> combineFileDicts(Dictionary<String, List<RAFFileListEntry>> Dict1, Dictionary<String, List<RAFFileListEntry>> Dict2)
        {
            foreach (KeyValuePair<String, List<RAFFileListEntry>> entryKVP in Dict2)
            {
                if (!Dict1.ContainsKey(entryKVP.Key))
                    Dict1.Add(entryKVP.Key, entryKVP.Value);
                else
                {
                    for (int i = 0; i < entryKVP.Value.Count; i++)
                    {
                        Boolean conflict = false;
                        for (int j = 0; j < Dict1[entryKVP.Key].Count; j++)
                        {
                            if (entryKVP.Value[i].FileName == Dict1[entryKVP.Key][j].FileName)
                            {
                                conflict = true;
                                if (Convert.ToInt32(entryKVP.Value[i].RAFArchive.GetID().Replace(".", "")) > Convert.ToInt32(Dict1[entryKVP.Key][j].RAFArchive.GetID().Replace(".", "")))
                                {
                                    Dict1[entryKVP.Key][j] = entryKVP.Value[i];
                                }
                            }
                        }
                        if (!conflict)
                            Dict1[entryKVP.Key].Add(entryKVP.Value[i]);
                    }
                }
            }
            return Dict1;
        }

        private String getFileLocation(String filePath)
        {
            if (filePath.ToLower().Contains("thumbs.db"))
            {
                if (Properties.Settings.Default.showAllWarnings)
                    Cliver.Message.Inform("thumbs.db files are silly, not going to use\r\n" + filePath);

                return null;
            }
            if (filePath.ToLower().Contains(".inibin"))
            {
                if (Properties.Settings.Default.showAllWarnings)
                    Cliver.Message.Inform("Inibin files are known to cause issues\r\nNot going to use\r\n" + filePath);
                return null;
            }
            if (filePath.ToLower().EndsWith(".wav"))
            {
                //
                // Do something
                //
            }
            if (filePath.ToLower().Contains("siuinfo.txt"))
            {
                //
                // Do something
                //
            }
            if (filePath.ToLower().Contains("customtext.txt"))
            {
                //
                // Do something
                //
            }
            if (filePath.ToLower().Contains("fontconfig_en_us"))
            {
                // this is guna break stuff, dont do it!
                return null;
            }
            if (filePath.ToLower().Contains("siupreview"))
            {
                //
                // Do something
                //
            }
            if (filePath.ToLower().Contains("animations.list") || filePath.ToLower().Contains("animations.ini"))
            {
                // this is guna break stuff, dont do it!
                return null;
            }

            // Find all potential matches
            List<String> options = new List<String>();
            FileInfo fi = new FileInfo(filePath);

            // Search RAF files
            if (fileEntriesShort.ContainsKey(fi.Name))
            {
                foreach (RAFFileListEntry entry in fileEntriesShort[fi.Name])
                    options.Add(entry.FileName);
            }

            // Search Air files
            foreach (String airFile in airFiles)
            {
                FileInfo airFI = new FileInfo(airFile);
                if (fi.Name == airFI.Name)
                    options.Add(airFile);
            }

            if (options.Count == 0)
                return null;

            if (options.Count > 1)
            {

            }

            return options[0];
        }

        #endregion // Helper Functions
    }
}
