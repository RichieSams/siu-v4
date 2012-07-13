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

        RAFMasterFileList rafFiles;
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

        #region Load and Close events

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
                this.SIFileOp.DirectoryDelete(Application.StartupPath + @"\extractedFiles\", true);
            if (Directory.Exists(Application.StartupPath + @"\filesToBeInstalled\"))
                this.SIFileOp.DirectoryDelete(Application.StartupPath + @"\filesToBeInstalled\", true);

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
                rafFiles = new RAFMasterFileList(gameDirectory + @"RADS\projects\lol_game_client\filearchives");
                rafTimer.Stop();

                airTimer.Start();
                // Load Air files
                airFiles.AddRange(Directory.GetFiles(gameDirectory + @"RADS\projects\lol_air_client\releases", "*", SearchOption.AllDirectories));
                for (int i = 0; i < airFiles.Count; i++)
                {
                    airFiles[i] = @"AirFiles\" + airFiles[i].Remove(0, (gameDirectory + @"RADS\projects\lol_air_client\releases\").Count());
                }
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

        #endregion // Load and Close events

        #region Adding files to new skins

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
            List<String> skippedFiles = new List<String>();
            foreach (String file in finalAddedFiles)
            {
                String location = getFileLocation(file);
                if (location != null)
                {
                    installFiles_ListBox.Items.Add(location);
                    SIFileOp.FileCopy(file, Application.StartupPath + @"\filesToBeInstalled\" + location.Replace('/', '\\'));
                }
                else
                    skippedFiles.Add(file);
            }
            if (skippedFiles.Count > 0)
            {
                String message = "Sucessfully added " + installFiles_ListBox.Items.Count.ToString() + " files.\n\nThe following files were invalid or could not be identified and were skipped:\n\n";
                foreach (String file in skippedFiles)
                    message += file + "\n";
                Cliver.Message.Ok("Hey!", SystemIcons.Information, message);
            }
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
                Cliver.Message.Inform("Sound installation is not supported at this moment. File will be skipped");
                return null;
            }
            if (filePath.ToLower().Contains("siuinfo.txt"))
            {
                //
                // Do something
                //
                return null;
            }
            if (filePath.ToLower().Contains("customtext.txt"))
            {
                //
                // Do something
                //
                return null;
            }
            if (filePath.ToLower().Contains("fontconfig_en_us"))
            {
                // this is guna break stuff, dont do it!
                Cliver.Message.Inform("Custom in-game text mods should be installed by creating a customtext.txt file, not by installing fonconfig_en_us. File will be skipped.");
                return null;
            }
            if (filePath.ToLower().Contains("siupreview"))
            {
                //
                // Do something
                //
                return null;
            }
            if (filePath.ToLower().Contains("animations.list") || filePath.ToLower().Contains("animations.ini"))
            {
                // this is guna break stuff, dont do it!
                Cliver.Message.Inform("Animations.list and animationos.ini files are known to break a LoL install. File will be skipped.");
                return null;
            }

            // Find all potential matches
            List<String> options = new List<String>();
            FileInfo fi = new FileInfo(filePath);

            // Search RAF files
            if (rafFiles.FileDictShort.ContainsKey(fi.Name.ToLower()))
            {
                foreach (RAFFileListEntry entry in rafFiles.FileDictShort[fi.Name.ToLower()])
                    options.Add(entry.FileName);
            }

            // Search Air files
            foreach (String airFile in airFiles)
            {
                FileInfo airFI = new FileInfo(airFile);
                if (fi.Name.ToLower() == airFI.Name.ToLower())
                    options.Add(airFile);
            }

            if (options.Count == 0)
                return null;
            else if (options.Count > 1)
            {
                return reduceOptions(fi.Directory, options);
            }
            else
                return options[0];
        }

        private String reduceOptions(DirectoryInfo parentDirectory, List<String> options)
        {
            int counter = 1;
            while (options.Count > 1)
            {
                counter++;
                List<String> remaining = new List<String>();
                foreach (String file in options)
                {
                    String[] filePath = file.Split('/');
                    if (parentDirectory.Name.ToLower() == filePath[filePath.Length - counter].ToLower())
                    {
                        remaining.Add(file);
                    }
                }
                if (remaining.Count == 0)
                    break;
                else if (remaining.Count == 1)
                    return remaining[0];
                else
                {
                    options = remaining;
                }
            }

            // Ask the user to select the correct file
            FileLocForm getCorrectFile = new FileLocForm(options);
            if (getCorrectFile.ShowDialog() == DialogResult.OK)
            {
                getCorrectFile = null;
                return getCorrectFile.possibleLocs.SelectedItem.ToString();
            }
            else
            {
                getCorrectFile = null;
                return null;
            }
        }

        #endregion // Adding files to new skins

    }
}
