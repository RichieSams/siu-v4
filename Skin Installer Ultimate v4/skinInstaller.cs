/*
 * Skin Installer Ultimate v4 (SIU v4)
 * Copyright 2012 Adrian Astley (RichieSams)
 * This file is part of SIU v4
 * 
 * SIU v4 is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * any later version.
 * 
 * SIU v4 is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with SIU v4.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * This Program is designed to help safely install
 * a variety of skins for the League of Legends game.
 * http://www.leagueoflegends.com 
 * 
 * A good website to browse such skins is
 * http://leaguecraft.com/skins 
 * 
 * This program is a re-write of SIU-LGG created by LGG.
 * It can be found here: http://code.google.com/p/siu-lgg/
 * SIU v3 was a modification of the original 
 * Skin installer generously provided and created by sgun
 * It can be found here http://forum.leaguecraft.com/index.php?/topic/5542-tool-lol-skin-installer-skin-installation-and-management-tool 
 * 
 * This program incorporates sound installation code
 * found from http://forum.leaguecraft.com/index.php?/topic/21301-release-lolmod 
 * 
 * And uses RAFlibPlus by RichieSams to read raf files.
 * It can be found here: http://code.google.com/p/raflib-plus/
 * RAFlibPlus is a re-write of the original RAFlib by ItzWarty.
 * It can be found here http://code.google.com/p/raf-manager/
 * 
 * And uses fsbext to read fmod sound files
 * It can be found here http://aluigi.altervista.org/papers.htm 
 * 
 * And also uses code from LoLViewer by SapphireStormLC 
 * It can be found here http://code.google.com/p/lolmodelviewer/
 * 
 * It also uses the devil image library and 7zip.
 * 
 * All external code is licensed and copyright 
 * by their respective owners
 */

// To make navigation of this code easier, inside Microsoft Visual Studio
// Press ctrl+M, then ctrl+O to collapse regions.

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
using System.Net;
using System.Threading;
using System.Globalization;
using System.Data.SQLite;
using System.Diagnostics;

using SevenZip;
using RAFlibPlus;

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
        private WebClient webClient = new WebClient();

        private SQLiteDatabase database;

        private RAFMasterFileList rafFiles;
        private List<String> airFiles = new List<String>();
        String airFileLocation = String.Empty;
        // Made this global so I don't have to continually pass it from function to function
        List<RAFArchive> usedRAFArchives;

        Stopwatch timer = new Stopwatch();

        String lastFolderPath = String.Empty;

        #endregion // Variables

        public skinInstaller()
        {
            InitializeComponent();

            // Select the last used tab
            tabControl.SelectTab(Properties.Settings.Default.lastSelectedTab);
        }

        #region Load and Close events

        private void skinInstaller_Load(object sender, EventArgs e)
        {
            timer.Start();

            if (Application.StartupPath.Length > 160)
            {
                Cliver.Message.Inform("The path that this program is running from\n\"" +
                    Application.StartupPath + "\" \nIs too long and could potentially cause errors.\n\n" +
                    "It is STRONGLY advised to move this folder to a shorter path location (like C:\\siu)\n" +
                    "Please do this now before continuing");
                this.Close();
            }
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

            // Check for program updates
            //
            // Need to ask Greg to add a version.ini
            //
            //
            //MessageBox.Show(Application.ProductVersion.ToString());

            // Initialize the database
            database = new SQLiteDatabase(Application.StartupPath + @"\skins.s3db");

            // Create the database if it doesn't exist
            if (!File.Exists(Application.StartupPath + @"\skins.s3db"))
            {
                database.ExecuteNonQuery(@"CREATE TABLE [skins] (
                                                [Name] VARCHAR(50)  PRIMARY KEY NOT NULL,
                                                [Author] VARCHAR(50)  NULL,
                                                [DateInstalled] TIMESTAMP  NULL,
                                                [DateAdded] TIMESTAMP DEFAULT CURRENT_TIMESTAMP NULL,
                                                [Character] VARCHAR(50)  NULL
                                                );");
            }

            // Set culture info to try and fix errors concerning locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            // Cleanup and leftover temp directories
            if (Directory.Exists(Application.StartupPath + @"\extractedFiles\"))
                FileHandler.DeleteFullDirectory(Application.StartupPath + @"\extractedFiles\");
            if (Directory.Exists(Application.StartupPath + @"\filesToBeInstalled\"))
                FileHandler.DeleteFullDirectory(Application.StartupPath + @"\filesToBeInstalled\");

            // Find League installation
            if (Properties.Settings.Default.gameDir != "" && (File.Exists(Properties.Settings.Default.gameDir + "lol.launcher.exe") || File.Exists(Properties.Settings.Default.gameDir + "league of legends.exe"))) // I think league of legends.exe is garena users
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
                // Load raf files
                try 
                {
                    rafFiles = new RAFMasterFileList(gameDirectory + @"RADS\projects\lol_game_client\filearchives");
                }
                catch (InvalidOperationException err)
                {
                    Cliver.Message.Show(SystemIcons.Error, err.Message, 0, new string[1] {"OK"});
                }

                if (rafFiles.FileDictFull.Count == 0)
                    Cliver.Message.Show(SystemIcons.Error, "No RAF files were found. Make sure your League of Lengends is installed correctly. If it is, contact us", 0, new String[1] { "OK" });

                // Load Air files
                airFileLocation = Directory.GetDirectories(gameDirectory + @"RADS\projects\lol_air_client\releases").Last();
                
                airFiles.AddRange(Directory.GetFiles(airFileLocation, "*", SearchOption.AllDirectories));
                for (int i = 0; i < airFiles.Count; i++)
                {
                    airFiles[i] = @"AirFiles" + airFiles[i].Remove(0, airFileLocation.Count());
                }
            }

            // Update the list view
            updateListView();

            timer.Stop();

            this.Select();
        }

        private void skinInstaller_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save the application settings
            Properties.Settings.Default.lastSelectedTab = tabControl.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        #endregion // Load and Close events

        #region Adding files to new skins

        #region GUI functions

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
                List<String> usable = new List<String>();
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

        private void b_IAddFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.FileName = "";
            fileDialog.CheckFileExists = true;
            fileDialog.Multiselect = true;
            fileDialog.Title = "Please select skin files..";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                processAddedFiles(fileDialog.FileNames.ToList());
            }
        }

        private void b_IAddDirectory_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Please select skin folder..";
            if (lastFolderPath != String.Empty)
                folderDialog.SelectedPath = lastFolderPath;
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                processAddedFiles(Directory.GetFiles(folderDialog.SelectedPath, "*", SearchOption.AllDirectories).ToList());

                lastFolderPath = folderDialog.SelectedPath;
            }
        }

        private void b_IRemoveFiles_Click(object sender, EventArgs e)
        {
            if (installFiles_ListBox.SelectedItem != null)
            {
                if (File.Exists(Application.StartupPath + @"\filesToBeInstalled\" + installFiles_ListBox.SelectedItem.ToString()))
                {
                    FileHandler.FileDelete(Application.StartupPath + @"\filesToBeInstalled\" + installFiles_ListBox.SelectedItem.ToString());
                }

                installFiles_ListBox.Items.Remove(installFiles_ListBox.SelectedItem);
            }
        }

        private void b_IClearAll_Click(object sender, EventArgs e)
        {
            if (Cliver.Message.Show("Confirm", SystemIcons.Question, "Are you sure you wish to remove all loaded files?", 0, new string[2] { "Yes", "No" }) == 0)
            {
                this.skinNameTextbox.Text = "";
                this.authorNameTextbox.Text = "Unknown";
                this.installFiles_ListBox.Items.Clear();

                if (Directory.Exists(Application.StartupPath + @"\extractedFiles\"))
                    FileHandler.DeleteFullDirectory(Application.StartupPath + @"\extractedFiles\");
            }
        }

        #endregion // GUI functions

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
                    FileHandler.FileCopy(file, Application.StartupPath + @"\filesToBeInstalled\" + location);
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
                Cliver.Message.Inform("Animations.list and animations.ini files are known to break a LoL install. File will be skipped.");
                return null;
            }

            // Find all potential matches
            List<String> options = new List<String>();
            FileInfo fi = new FileInfo(filePath);

            // Search RAF files
            if (rafFiles.FileDictShort.ContainsKey(fi.Name.ToLower()))
            {
                foreach (RAFFileListEntry entry in rafFiles.FileDictShort[fi.Name.ToLower()])
                    options.Add("RAFFiles\\" + entry.FileName.Replace('/', '\\'));
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
                return reduceOptions(fi, options);
            }
            else
                return options[0];
        }

        private String reduceOptions(FileInfo fileInfo, List<String> options)
        {
            DirectoryInfo parentDirectory = fileInfo.Directory;

            int counter = 1;
            while (options.Count > 1)
            {
                counter++;
                List<String> remaining = new List<String>();
                foreach (String file in options)
                {
                    String[] filePath = file.Split('\\');
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
            FileLocForm getCorrectFile = new FileLocForm(fileInfo, options);
            if (getCorrectFile.ShowDialog() == DialogResult.OK)
            {
                return getCorrectFile.possibleLocs.SelectedItem.ToString();
            }
            else
            {
                getCorrectFile = null;
                return null;
            }
        }

        private void addToDBButton_Click(object sender, EventArgs e)
        {
            if (installFiles_ListBox.Items.Count == 0)
            {
                Cliver.Message.Inform("You don't have any files loaded!\nDrag and drop files into SIU or click 'Add Files' or 'Add Directory' and select the files you wish to install.");
                return;
            }
            if (skinNameTextbox.Text == "")
            {
                InputBox inputbox = new InputBox("Please enter a name for this skin", "Please enter a name for this skin");
                if (inputbox.ShowDialog() == DialogResult.OK)
                {
                    skinNameTextbox.Text = inputbox.textBoxText.Text;
                }
                else
                    return;
            }

            // Remove harmful characters
            skinNameTextbox.Text = skinNameTextbox.Text.Replace("\\", "-").Replace("/", "-").Replace(":", "-").Replace("*", "-").Replace("\"", "-").Replace("|", "-").Replace(">", "-").Replace("<", "-").Replace("?", "-");
            authorNameTextbox.Text = authorNameTextbox.Text.Replace("\\", "-").Replace("/", "-").Replace(":", "-").Replace("*", "-").Replace("\"", "-").Replace("|", "-").Replace(">", "-").Replace("<", "-").Replace("?", "-");

            if (Directory.Exists(Application.StartupPath + @"\Skins\" + skinNameTextbox.Text))
            {
                DataTable table = database.Query("SELECT * FROM skins WHERE Name='" + skinNameTextbox.Text + "'");
                // Skin exists in the database and the folder exists
                if (table == null)
                {
                    int replace = Properties.Settings.Default.replaceSkinWarning;
                    Boolean askedUser = false;

                    if (replace == -1)
                    {
                        bool saveThis = false;
                        askedUser = true;

                        replace = Cliver.Message.Show("Replace Skin?",
                            SystemIcons.Information,out saveThis,
                            "A skin with this name is already installed!\r\n" +
                            "You should type in a new name, then press the \"Add to Database\" Button.\r\n" +
                            "\r\nIf you named it this way intentionally, and wish to update a skin,\r\n" +
                            "click the corresponding button.\r\n",
                            0, new string[2] { "Stop and Let Me Choose a New Name", "I am updating a skin, and am ready to replace it." });
                        if (saveThis) 
                            Properties.Settings.Default.replaceSkinWarning=replace;
                    }
                    if (replace == 1)
                    {
                        uninstallSkin(skinNameTextbox.Text);
                        deleteSkin(skinNameTextbox.Text);
                    }
                    else
                    {
                        if (!askedUser)
                            Cliver.Message.Show(SystemIcons.Error, "A skin with this name is already installed. Choose a new name", 0, new String[1] { "OK" });
                        return;
                    }
                }
                // Not in the database but the folder exists
                else
                {
                    int repFold = Properties.Settings.Default.replaceFolderWarning;
                    Boolean askedUser = false;

                    if (repFold == -1)
                    {
                        bool saveThis = false;
                        askedUser = true;

                        repFold = Cliver.Message.Show("Replace Folder",
                            SystemIcons.Information, out saveThis,
                            "A folder with this name already exists\r\n" +
                            "But the skin inside is not yet in the database...\r\n" +
                            "\r\nWould you like to update this folder and replace its contents?",
                            0, new string[2] { "Yes", "No" });
                        if (saveThis) 
                            Properties.Settings.Default.replaceFolderWarning = repFold;
                    }
                    if (repFold == 1)
                    {
                        if (!askedUser)
                            Cliver.Message.Show(SystemIcons.Error, "A skin folder with this name is already exists. Choose a new name", 0, new String[1] { "OK" });
                        return;
                    }
                    else
                    {
                        //clear that folder and continue
                        FileHandler.DeleteFullDirectory(Application.StartupPath + @"\Skins\" + skinNameTextbox.Text);
                    }
                }
            }

            // Move files and simultaneously figure out character
            String character = String.Empty;
            foreach (String item in installFiles_ListBox.Items)
            {
                FileHandler.FileMove(Application.StartupPath + @"\filesToBeInstalled\" + item.Replace('/', '\\'), Application.StartupPath + @"\Skins\" + skinNameTextbox.Text + @"\" + item.Replace('/', '\\'));

                if (character == String.Empty)
                {
                    if (item.ToLower().Contains("data/characters"))
                        character = splitAtUpperCase(item.Split('/')[2]);
                    else if (item.ToLower().Contains(@"assets\images\champions"))
                        character = splitAtUpperCase(item.Substring(item.IndexOf(@"assets\images\champions") + 23).Split('_')[0]);
                }
            }

            // Insert into sqlite db
            database.ExecuteNonQuery("INSERT INTO skins (Name, Author, Character) VALUES ('" + skinNameTextbox.Text + "', '" + authorNameTextbox.Text + "', '" + character + "')");

            // Update the list view
            updateListView();

            // See if the user wants to install the skin right now
            int install = Properties.Settings.Default.installWhenAddingSkin;
            if (install == -1)
            {
                bool saveThis = false;
                Cliver.Message.NextTime_ButtonColors = new Color[] { Color.LightGreen, Color.LightGreen };
                install = Cliver.Message.Show("Done Adding " + skinNameTextbox.Text,
                        SystemIcons.Information, out saveThis,
                     "Added " + this.skinNameTextbox.Text + " to the Skin Database!\nYou can go to the other tab to install it!",
                        0, new string[2] { "OK", "Please automatically install it at this time." });
                if (saveThis) Properties.Settings.Default.installWhenAddingSkin = install;
                Properties.Settings.Default.Save();
            }
            if (install == 1)
            {
                installSkin(skinNameTextbox.Text);
            }

            // Reset listBox
            installFiles_ListBox.Items.Clear();
            skinNameTextbox.Text = "";
            authorNameTextbox.Text = "Unknown";

            // Cleanup of temp folders
            FileHandler.DeleteFullDirectory(Application.StartupPath + @"\extractedFiles\");
            FileHandler.DeleteFullDirectory(Application.StartupPath + @"\filesToBeInstalled\");
        }

        String splitAtUpperCase(String input)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 1; i < input.Length; i++)
            {
                if (Char.IsUpper(input[i]) && Char.IsLower(input[i - 1]))
                    sb.Append(' ');
                sb.Append(input[i]);
            }
            return sb.ToString();
        }

        #endregion // Adding files to new skins

        #region Skin Database GUI functions

        private void updateListView()
        {
            DataTable table = database.Query("SELECT Name, Author, STRFTIME('%m/%d/%Y', DateInstalled, 'localtime'), STRFTIME('%m/%d/%Y', DateAdded, 'localtime'), Character FROM skins");
            skinDatabaseListView.Items.Clear();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    ListViewItem item = new ListViewItem();
                    if (row[2].ToString() != "")
                        item.Font = new Font(item.Font, FontStyle.Bold);
                    item.SubItems.Add(row[0].ToString());
                    item.SubItems.Add(row[1].ToString());
                    item.SubItems.Add(Directory.GetFiles(Application.StartupPath + @"\Skins\" + row[0].ToString(), "*", SearchOption.AllDirectories).Count().ToString());
                    item.SubItems.Add(row[2].ToString());
                    item.SubItems.Add(row[3].ToString());
                    item.SubItems.Add(row[4].ToString());

                    skinDatabaseListView.Items.Add(item);
                }
            }
        }

        private void checkBox1dispTitle_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
                skinDatabaseListView.Columns[1].Width = 190;
            else
                skinDatabaseListView.Columns[1].Width = 0;
        }

        private void checkBox1dispAuthor_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
                skinDatabaseListView.Columns[2].Width = 100;
            else
                skinDatabaseListView.Columns[2].Width = 0;
        }

        private void checkBox1dispFileCount_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
                skinDatabaseListView.Columns[3].Width = 60;
            else
                skinDatabaseListView.Columns[3].Width = 0;
        }

        private void checkBox1dispInstalled_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
                skinDatabaseListView.Columns[4].Width = 75;
            else
                skinDatabaseListView.Columns[4].Width = 0;
        }

        private void checkBox1dispDateAdded_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
                skinDatabaseListView.Columns[5].Width = 75;
            else
                skinDatabaseListView.Columns[5].Width = 0;
        }

        private void checkBox1dispCharacter_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
                skinDatabaseListView.Columns[6].Width = 91;
            else
                skinDatabaseListView.Columns[6].Width = 0;
        }

        #endregion // Skin Database GUI functions

        #region Installation

        #region Installation GUI functions

        private void installButton_Click(object sender, EventArgs e)
        {
            // Inform the user of the usage
            if (skinDatabaseListView.CheckedItems.Count < 1)
            {
                Cliver.Message.Inform("Please 'check-mark' which skin(s) you would like to install\n" +
                    "\nIf you do not see any skins above, you can add them in the tab: \"Add New Skin\"");
                return;
            }

            // Install all checked skins
            foreach (ListViewItem item in skinDatabaseListView.CheckedItems)
            {
                // Check if the skin is already installed
                if ((item.SubItems[4].Text != "") &&
                    (Cliver.Message.Show("Skin Already Installed", SystemIcons.Information, item.SubItems[1].Text + " is already installed. Do you want to reinstall it?", 0, new string[2] { "Yes", "No" }) == 1))
                {
                    continue;
                }

                installSkin(item.SubItems[1].Text);
            }

            // Update the ListView to reflect the changes
            updateListView();

            Cliver.Message.Inform("Successfully installed skins");
        }

        private void uninstallButton_Click(object sender, EventArgs e)
        {
            // Inform the user of the usage
            if (skinDatabaseListView.CheckedItems.Count < 1)
            {
                Cliver.Message.Inform("Please 'check-mark' which skin(s) you would like to install\n" +
                    "\nIf you do not see any skins above, you can add them in the tab: \"Add New Skin\"");
                return;
            }

            // Uninstall all checked skins
            foreach (ListViewItem item in skinDatabaseListView.CheckedItems)
            {
                uninstallSkin(item.SubItems[1].Text);
            }

            // Update the ListView to reflect the changes
            updateListView();

            Cliver.Message.Inform("Successfully uninstalled skins");
        }

        private void deleteSkinButton_Click(object sender, EventArgs e)
        {
            // Inform the user of the usage
            if (skinDatabaseListView.CheckedItems.Count < 1)
            {
                Cliver.Message.Inform("Please 'check-mark' which skin(s) you would like to delete\n" +
                    "\nIf you do not see any skins above, you can add them in the tab: \"Add New Skin\"");
                return;
            }

            // Double check with the user
            if (Cliver.Message.Show("Are you sure?",
                SystemIcons.Question, "Are you sure want delete these skins?", 0, new string[2] { "Yes", "No" }) == 0)
            {
                foreach (ListViewItem item in skinDatabaseListView.CheckedItems)
                {
                    // Recursively delete everything in the skin folder and the folder itself
                    if (Directory.Exists(Application.StartupPath + @"\Skins\" + item.SubItems[1].Text))
                    {
                        FileHandler.DeleteFullDirectory(Application.StartupPath + @"\Skins\" + item.SubItems[1].Text);
                    }

                    // Delete the entry in the database
                    database.ExecuteNonQuery("DELETE FROM skins WHERE Name='" + item.SubItems[1].Text + "'");
                }
            }

            // Update the ListView
            updateListView();
        }

        #endregion // Installation GUI functions

        private bool installSkin(String skinName)
        {
            // Get the files to install
            String[] files = Directory.GetFiles(Application.StartupPath + @"\Skins\" + skinName, "*", SearchOption.AllDirectories);

            // List of RAFArchives that are used so I know which .raf files to rebuild after injection
            usedRAFArchives = new List<RAFArchive>();

            // List of files that could not be installed
            List<String> filesNotInstalled = new List<String>();

            foreach (String file in files)
            {
                // Try to install a specific file
                String cleanPath = file.Substring((Application.StartupPath + @"\Skins\" + skinName).Length + 1);
                if(!installFile(file, cleanPath, false))
                {
                    // File failed to install
                    filesNotInstalled.Add(cleanPath);
                }
            }

            // Rebuild any .raf files that were changed
            foreach (RAFArchive archive in usedRAFArchives)
            {
                archive.SaveRAFFile();
            }

            // Clear the list for re-use
            usedRAFArchives.Clear();

            //
            //
            // Alert the user of any failed file installs
            // Need to make a custom form for this. Just a simple un-editable text box with a scroll bar
            //
            //

            // Update the database
            database.ExecuteNonQuery("UPDATE skins SET DateInstalled=CURRENT_TIMESTAMP WHERE Name='" + skinName + "'");

            return true;
        }

        enum FileType
        {
            RAF,
            Air,
            TextMod
        };

        private bool installFile(String file, String cleanPath, bool ignoreBackup)
        {
            // RAF file
            if (cleanPath.ToLower().Contains("raffiles"))
            {
                cleanPath = cleanPath.Replace('\\', '/').Substring(9); // Substring chops off 'RAFFiles\'
                RAFFileListEntry entry = rafFiles.GetFileEntry(cleanPath);

                // If there isn't an entry, first try to re-identify the file. The file might have been manually added to the directory
                if (entry == null)
                {
                    String location = getFileLocation(file);
                    FileHandler.FileMove(file, file.Replace(cleanPath, "") + @"\" + location);
                    file = file.Replace(cleanPath, "") + @"\" + location;
                    cleanPath = location;
                    // Try again. If it fails, skip it
                    entry = rafFiles.GetFileEntry(location.Replace('\\', '/').Substring(9));
                    if (entry == null)
                        return false;
                }

                // Backup
                // Install will always backup
                // Uninstall will always skip backup since it's actually installing *from* the backups
                if (!ignoreBackup)
                {
                    if (!backupFile(cleanPath, FileType.RAF))
                        return false;
                }

                // Replace
                if (!entry.ReplaceContent(File.ReadAllBytes(file)))
                    return false;

                usedRAFArchives.Add(entry.RAFArchive);

                return true;
            }
            // Air file
            else if (cleanPath.ToLower().Contains("airfiles"))
            {
                cleanPath = cleanPath.Substring(9); // Substring chops off 'AirFiles\'

                // Backup
                if (!backupFile(cleanPath, FileType.Air))
                    return false;

                // Replace
                FileHandler.FileCopy(file, airFileLocation + "\\" + cleanPath);

                return true;
            }
            // Text Mod
            else if (cleanPath.ToLower().Contains("textmods"))
            {
                return false;
            }
            // Try to re-identify and run again
            else
            {
                return false;
            }

            // Should never get here
            return false;
        }

        private bool backupFile(String fileName, FileType fileType)
        {
            String fullPath = String.Empty;

            switch (fileType)
            {
                case FileType.Air:
                    fullPath = Application.StartupPath + @"\Backups\Air\" + fileName;
                    if (!File.Exists(fullPath))
                        FileHandler.FileCopy(airFileLocation + "\\" + fileName, fullPath);
                    break;

                case FileType.RAF:
                    fullPath = Application.StartupPath + @"\Backups\RAF\" + fileName.Replace("/", "\\");
                    if (!File.Exists(fullPath))
                        FileHandler.FileWriteAllBytes(fullPath, rafFiles.GetFileEntry(fileName).GetContent());
                    break;
                case FileType.TextMod:

                    break;
            }
            return true;
        }

        private bool uninstallSkin(String skinName)
        {
            // Get the files to uninstall
            String[] files = Directory.GetFiles(Application.StartupPath + @"\Skins\" + skinName, "*", SearchOption.AllDirectories);

            // List of RAFArchives that are used so I know which .raf files to rebuild after injection
            usedRAFArchives = new List<RAFArchive>();

            // List of files whose backups could not be found or failed to uninstall
            List<String> failedFiles = new List<String>();

            foreach (String file in files)
            {
                // Try to find a backup for a file
                String cleanPath = file.Substring((Application.StartupPath + @"\Skins\" + skinName).Length + 1);
                String backupPath = Application.StartupPath + @"\Backups\" + cleanPath;

                // Try to install the backup
                if (File.Exists(backupPath))
                {
                    if (!installFile(file, cleanPath, true))
                    {
                        // File failed to install
                        failedFiles.Add(cleanPath);
                    }
                }
                else
                {
                    failedFiles.Add(cleanPath);
                }
            }

            // Rebuild any .raf files that were changed
            foreach (RAFArchive archive in usedRAFArchives)
            {
                archive.SaveRAFFile();
            }

            // Clear the list for re-use
            usedRAFArchives.Clear();

            //
            //
            // Alert the user of any failed files
            // Need to make a custom form for this. Just a simple un-editable text box with a scroll bar
            //
            //

            // Update the database
            database.ExecuteNonQuery("UPDATE skins SET DateInstalled=NULL WHERE Name='" + skinName + "'");

            return true;
        }

        private bool deleteSkin(String skinName)
        {
            return true;
        }

        #endregion // Installation

        #region Helper functions

        private void RunProgram(string fileName, string args, string workingDirectory, bool visible)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = fileName;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.Arguments = args;
            startInfo.CreateNoWindow = !visible;
            startInfo.WindowStyle = visible ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
        }

        private void startLoLButton_Click(object sender, EventArgs e)
        {
            RunProgram(gameDirectory + "lol.launcher.exe", "", "", true);
        }

        #endregion // Helper functions

    }
}
