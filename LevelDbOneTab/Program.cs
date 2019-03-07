using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ConsoleApp1
{

    class Program
    {

        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DbOpen(string path);

        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DbKeyOpen(string key);

        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DbKeyClose();

        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DbClose();

        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern byte* DbGet(out UInt32 bufferLen);

        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern byte* DbSet(string data, out UInt32 bufferLen);

        //[DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        //unsafe public static extern byte* DbSet3(int bufferLen);


        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DbSaveBinary(string filename);

        //[DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern string DbReadBinary(string filename);

        // [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern bool ApplyChangesToDb(string key, string data);

        [DllImport("MyLevelDb.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ApplyChangesToDb(string key, string data, UInt32 length);


        static void Main(string[] args)
        {
            String[] arguments = Environment.GetCommandLineArgs();
            
            //Console.WriteLine("GetCommandLineArgs: {0}", String.Join(", ", arguments));

            // LevelDbOneTab.exe -leveldbfolderpath "C:\Users\<USER>\AppData\Local\Google\Chrome\User Data\Default\Local Storage\leveldb" -action export -archivefolderpath "B:/OneNoteBackups/backups" -archiveprevious

            // LevelDbOneTab.exe -leveldbfolderpath "C:\Users\<USER>\AppData\Local\Google\Chrome\User Data\Default\Local Storage\leveldb" -action import -archivefolderpath "B:/OneNoteBackups/backups" -archiveprevious 

            try
            {
                string levelDbFolderPath = "";
                string action = "";
                string archiveFolderpath = "";
                bool saveToArchive = false;
                bool archivePreviousExport = false;

                for (int i = 1; i < arguments.Length; i++)
                {
                    switch (arguments[i])
                    {
                        case "-leveldbfolderpath":
                            levelDbFolderPath = FixArgPath(arguments[i + 1]);

                            break;
                        case "-action":
                            action = arguments[i + 1];
                            break;
                        case "-archivefolderpath":
                            archiveFolderpath = FixArgPath(arguments[i + 1]);
                            break;
                        case "-savetoarchive":
                           // bool.TryParse(arguments[i + 1], out saveToArchive);
                            saveToArchive = true;
                            break;
                        case "-archiveprevious":
                            archivePreviousExport = true;
                            break;
                    }
                }

                // Args validation.
                if (string.IsNullOrEmpty(levelDbFolderPath))
                {
                    throw new Exception("Please specify -leveldbfolderpath. Please set path to chrome or vivaldi leveldb folder. It will look like \"C:/Users/<YourUserName>/AppData/Local/Google/Chrome/User Data/Default/Local Storage/leveldb/\"");
                }
                else if (!Directory.Exists(levelDbFolderPath))
                {
                    throw new Exception("Specified -leveldbfolderpath \"" + levelDbFolderPath + "\" cannot be found.");
                }
                if (string.IsNullOrEmpty(action) || (action != "import" && action != "export"))
                {
                    throw new Exception("Please specify a valid -action. Possible values are import or export. Your value: " + action);
                }

                if (string.IsNullOrEmpty(archiveFolderpath))
                {
                    throw new Exception("Please specify -archivefolderpath where your archived data files will go.");
                }

                bool isExportMode = (action == "export");

                // "--" is replaced by zero bytes in c++ dll
                string extensionKey = "_chrome-extension://chphlpgkkbolifaimnlloiipkdnihall--";

                //var keys = new string[] {
                //    "state", "topSites", "settings", "lastSeenVersion",
                //    "installDate", "idCounter", "extensionKey"
                //};
                var subKeys = new string[] { "state" };

                if (!isExportMode)
                {

                    RestoreDb(archiveFolderpath, levelDbFolderPath, extensionKey, subKeys);

                }
                else
                {
                    if (archivePreviousExport) {
                        ArchiveExistingExport(archiveFolderpath);
                    }
                    ExportDb(levelDbFolderPath, extensionKey, subKeys, archiveFolderpath, saveToArchive);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

            }

            Console.ReadKey();
        }

        internal static string FixArgPath(string pathFromCmdLine) {
            if (!string.IsNullOrEmpty(pathFromCmdLine)) {
                pathFromCmdLine = pathFromCmdLine.Replace("\"","").Replace('/', '\\');;
            }
            return pathFromCmdLine;
        }

        static void ExportDb(string path, string key, string[] keys, string targetPath, bool saveToArchive)
        {
            try
            {
                if (DbOpen(path))
                {

                    string[] files = (string[])keys.Clone();

                    // Binary output of data. These can be used for importing.
                    bool first = true;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        _ExportKey(targetPath, key, keys[i], first);
                        files[i] = keys[i] + ".bin";
                        first = false;
                    }

                    Array.Resize(ref files, files.Length + 1);
                    files[files.Length - 1] = keys[0] + ".json";

                    // DbClose();
                    // If saving to archive, MOVE all files to ZIP file.
                    if (saveToArchive) {
                        string fn = string.Format("OneTab-{0:yyyy-MM-dd_hh-mm}.zip", DateTime.Now);

                        ZipFileCreator.CreateZipFile(fn, files);

                        foreach (var item in files)
                            if (File.Exists(item))
                                File.Delete(item);
                    }
                } else {
                    Console.WriteLine("ERROR: Could not open the filedb database. Please make sure you have closed all browser windows, then try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
            finally
            {
                DbClose();
                //if (ums != null) ums.Dispose();
                // ums = null;
            }

        }

        static void ArchiveExistingExport(string targetPath) {
            Console.WriteLine("Archiving existing export data...");
            try {
            if (!Directory.Exists(targetPath)) {
                throw new Exception("Export folder does not exist.");

            }

                var files = Directory.GetFiles(targetPath,"*.bin");


                string fn = System.IO.Path.Combine(targetPath,string.Format("OneTab-Archive-{0:yyyy-MM-dd_hh-mm}.zip", DateTime.Now));

                ZipFileCreator.CreateZipFile(fn, files);

                foreach (var item in files)
                    if (File.Exists(item))
                        File.Delete(item);
            
            } catch (Exception ex) {
                Console.WriteLine("Error archiving existing export data: " + ex.Message);

            }

        }

        static void RestoreDb(string importFolder, string path, string key, string[] keys)
        {
            try
            {
                if (DbOpen(path))
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        StreamReader fileReader = null;
                        String p = System.IO.Path.Combine(importFolder, keys[i] + ".bin");
                        try
                        {
                            if (!File.Exists(p))
                            {
                                Console.WriteLine("Could not find file " + p + ". Skipping import of that key.");
                            }
                            else
                            {
                                fileReader = File.OpenText(p);
                                string content = fileReader.ReadToEnd();

                                Console.WriteLine("File content length: " + content.Length);
                                unsafe
                                {
                                    bool result = ApplyChangesToDb(key + keys[i], content, (uint)content.Length);
                                    //bool result = ApplyChangesToDb2(key + keys[i], importFolder + "\\" + keys[i] + ".bin");
                                    if (!result)
                                    {
                                        Console.WriteLine("Import failed for " + key + keys[i] + "! Not sure why...");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error importing file " + p + ": " + ex.Message);
                        }
                        finally
                        {
                            if (fileReader != null)
                            {
                                fileReader.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error restoring the DB: " + ex.Message);
            }
            finally
            {
                DbClose();
                //if (ums != null) ums.Dispose();
                // ums = null;
            }
        }

        private static void _ExportKey(string folderPath, string k, string add, bool mode = false)
        {

            if (DbKeyOpen(k + add))
            {
                unsafe
                {
                    UInt32 bufferLen = 0;
                    byte* buffer = DbGet(out bufferLen);

                    // ++buffer - this is correction for zero byte artefact 
                    var ums = new UnmanagedMemoryStream(++buffer, (Int32)bufferLen, (Int32)bufferLen, FileAccess.Read);
                    try
                    {

                        // Create a byte array to hold data from unmanaged memory.
                        byte[] data = new byte[bufferLen - 1];

                        // Read from unmanaged memory to the byte array.
                        ums.Read(data, 0, (int)bufferLen - 1);

                        // Save data as is 
                        String p = System.IO.Path.Combine(folderPath, add + ".bin");


                        // PROBLEMS:
                        // After import, Last char is missing, and 4 bytes with val 253 are added.
                        
                        // First export, good.
                        // First import of good data file: OneTab works.
                        // Export again to data file: 4 weird y chars added.
                        // Import again: good
                        // Export again: 8 wierd chars total at end (????yyyy)
                        // Import again: good

                        /*
                        byte xxx = data[data.Length-1];
                        Console.WriteLine(" byte " + char.ConvertFromUtf32( xxx));
                        Console.WriteLine(" byte " + (int)xxx);
                        Console.WriteLine(" byte " + ((int)xxx == 253));
                        while ((int)xxx == 253) {
                            byte[] truncArray = new byte[data.Length-1];
                            Array.Copy(data, truncArray , truncArray.Length);
                            data = truncArray;
                            //byte[] data2 = new byte[data.Length-1];
                            //data = Array.Copy(data,data2,data.Length-1);
                            Console.WriteLine("Removed byte " + (int)xxx + "; data length is now "+data.Length);
                            xxx = data[data.Length-1];
                        }
                        */


                        DbSaveBinary(p);

                        // Save data as formated json 
                        if (mode)
                        {
                            p = System.IO.Path.Combine(folderPath, add + ".json");

                            string s = Encoding.Unicode.GetString(data);
                            
                            try {
                                File.WriteAllText(p, JToken.Parse(s).ToString(Formatting.Indented));
                            } catch (Exception ex) {
                                Console.WriteLine("WARNING: Could not format output data as JSON. Writing as raw string instead: " + ex.Message);
                                File.WriteAllText(p,s);
                            }
                        }

                    }
                    finally
                    {
                        if (ums != null) ums.Dispose();
                        ums = null;
                    }

                    DbKeyClose();
                }
            }

        }

        private static void _ImportKey(string k, string add, string val, bool mode = false)
        {
            
        }
        






        // Create a ZIP file of the files provided.
        public static class ZipFileCreator
        {
            public static void CreateZipFile(string fileName, IEnumerable<string> files)
            {

                // Create and open a new ZIP file
                using (var zip = ZipFile.Open(fileName, ZipArchiveMode.Create))
                {

                    // Add the entry for each file
                    foreach (var file in files)
                    {
                        zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                    }
                }
            }

        }




    }
}