namespace SkinInstaller
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using System.Collections.Generic;

    public class FileHandler
    {
        public static void DeleteFullDirectory(string dirName)
        {
            try
            {
                DirectoryInfo root = new DirectoryInfo(dirName);
                Stack<DirectoryInfo> fols = new Stack<DirectoryInfo>();
                DirectoryInfo fol;

                fols.Push(root);
                while (fols.Count > 0)
                {
                    fol = fols.Pop();
                    fol.Attributes = fol.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
                    foreach (DirectoryInfo d in fol.GetDirectories())
                    {
                        fols.Push(d);
                    }
                    foreach (FileInfo f in fol.GetFiles())
                    {
                        f.Attributes = f.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
                        f.Delete();
                    }
                }
                root.Delete(true);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        public static void FileWriteAllBytes(string fileName, byte[] content)
        {
            try
            {
                string[] strArray = fileName.Split(new char[] { '\\' });
                string path = fileName.Remove(fileName.Length - strArray[strArray.Length - 1].Length);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                File.WriteAllBytes(fileName, content);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + "\r\n trying to write the file " + fileName);
            }
        }

        public static void FileCopy(string fileName, string fileDest)
        {
            try
            {
                string[] strArray = fileDest.Split(new char[] { '\\' });
                string path = fileDest.Remove(fileDest.Length - strArray[strArray.Length - 1].Length);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                File.Copy(fileName, fileDest, true);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + "\r\n on the file " + fileName + "\r\ngoing to \r\n" + fileDest);
            }
        }

        public static void FileDelete(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    File.SetAttributes(fileName,
                        FileAttributes.Normal);

                    File.Delete(fileName);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        public static void FileMove(string fileName, string fileDest)
        {
            try
            {
                string[] strArray = fileDest.Split(new char[] { '\\' });
                string path = fileDest.Remove(fileDest.Length - strArray[strArray.Length - 1].Length);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                File.Move(fileName, fileDest);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + "\r\n on the file " + fileName + "\r\ngoing to \r\n" + fileDest);
            }
        }
        public static void DirectoryMove(string dirPath, string dirDest)
        {
            try
            {
                if (!Directory.Exists(dirDest))
                {
                   // Directory.CreateDirectory(dirDest);
                }
                Directory.Move(dirPath, dirDest);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
    }
}

