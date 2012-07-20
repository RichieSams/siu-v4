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
using SevenZip;
using System.Data.SQLite;
using System.Diagnostics;

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
        private WebClient webClient = new WebClient();

        private FileHandler fileHandler = new FileHandler();

        private SQLiteDatabase database;

        private RAFMasterFileList rafFiles;
        private List<String> airFiles = new List<String>();
        String airFileLocation = String.Empty;

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
                database.ExecuteNonQuery(@"CREATE TABLE [skinFiles] (
                                                [FileID] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                [SkinID] INTEGER  NULL,
                                                [Path] VARCHAR(200)  NULL
                                                );

                                           CREATE TABLE [skins] (
                                                [SkinID] INTEGER  PRIMARY KEY AUTOINCREMENT NOT NULL,
                                                [Name] VARCHAR(50)  UNIQUE NULL,
                                                [Author] VARCHAR(50)  NULL,
                                                [Installed] BOOLEAN DEFAULT '0' NULL,
                                                [DateInstalled] TIMESTAMP  NULL,
                                                [DateAdded] TIMESTAMP DEFAULT CURRENT_TIMESTAMP NULL
                                                );");
            }

            // Set culture info to try and fix errors concerning locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            // Cleanup and leftover temp directories
            if (Directory.Exists(Application.StartupPath + @"\extractedFiles\"))
                fileHandler.DirectoryDelete(Application.StartupPath + @"\extractedFiles\");
            if (Directory.Exists(Application.StartupPath + @"\filesToBeInstalled\"))
                fileHandler.DirectoryDelete(Application.StartupPath + @"\filesToBeInstalled\");

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
                    this.fileHandler.FileDelete(Application.StartupPath + @"\filesToBeInstalled\" + installFiles_ListBox.SelectedItem.ToString());
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
                    this.fileHandler.DirectoryDelete(Application.StartupPath + @"\extractedFiles\");
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
                    fileHandler.FileCopy(file, Application.StartupPath + @"\filesToBeInstalled\" + location.Replace('/', '\\'));
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
            FileLocForm getCorrectFile = new FileLocForm(fileInfo, options);
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
                        //clear that folder and contiunue
                        fileHandler.DirectoryDelete(Application.StartupPath + @"\Skins\" + skinNameTextbox.Text);
                    }
                }
            }

            // Create database skin entry
            //
            //
            //
            // Can just do a stright insert once skinInstall/skinDelete methods are written
            //
            //
            //
            DataTable dataTable = database.Query("SELECT SkinID FROM skins WHERE Name='" + skinNameTextbox.Text + "'");
            if (dataTable == null)
            {
                database.ExecuteNonQuery("INSERT INTO skins (Name, Author) VALUES ('" + skinNameTextbox.Text + "', '" + authorNameTextbox.Text + "')");
            }
            else
            {
                database.ExecuteNonQuery("DELETE FROM skinFiles WHERE SkinID=" + dataTable.Rows[0][0].ToString());
                database.ExecuteNonQuery("DELETE FROM skins WHERE Name='" + skinNameTextbox.Text + "'");
                database.ExecuteNonQuery("INSERT INTO skins (Name, Author) VALUES ('" + skinNameTextbox.Text + "', '" + authorNameTextbox.Text + "')");
            }

            dataTable = database.Query("SELECT SkinID FROM skins WHERE Name='" + skinNameTextbox.Text + "'");
            int skinID = int.Parse(dataTable.Rows[0][0].ToString());

            // Move files and create an entry for them
            String insertString = "INSERT INTO skinFiles (SkinID, Path) VALUES ";
            foreach (String item in installFiles_ListBox.Items)
            {
                fileHandler.FileMove(Application.StartupPath + @"\filesToBeInstalled\" + item.Replace('/', '\\'), Application.StartupPath + @"\Skins\" + skinNameTextbox.Text + @"\" + item.Replace('/', '\\'));
                insertString += "('" + skinID + "', '" + item + "'), ";
            }
            insertString = insertString.Remove(insertString.Length - 2);
            database.ExecuteNonQuery(insertString);

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
            fileHandler.DirectoryDelete(Application.StartupPath + @"\extractedFiles\");
            fileHandler.DirectoryDelete(Application.StartupPath + @"\filesToBeInstalled\");
        }

        private void updateListView()
        {
            DataTable table = database.Query("SELECT s.Installed, s.Name, s.Author, (SELECT COUNT(f.FileID) FROM skinFiles AS f WHERE SkinID=s.SkinID ), s.DateInstalled, s.DateAdded FROM skins AS s");
            skinDatabaseListView.Items.Clear();
            foreach (DataRow row in table.Rows)
            {
                ListViewItem item = new ListViewItem();
                if (row[0].ToString() == "1")
                    item.Font = new Font(item.Font, FontStyle.Bold);
                for (int i = 1; i < table.Columns.Count; i++)
                {
                    item.SubItems.Add(row[i].ToString());
                }
                skinDatabaseListView.Items.Add(item);
            }
        }

        #endregion // Adding files to new skins

        #region Installation

        private void installSkin(String skinName)
        {

        }

        private void uninstallSkin(String skinName)
        {

        }

        private void deleteSkin(String skinName)
        {

        }

        #endregion // Installation

    }
}
