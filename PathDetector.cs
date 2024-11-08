using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GModContentInstaller
{
    internal class PathDetector
    {
        static private string Search()
        {
            var drive = new List<string> { "A:\\", "B:\\", "C:\\", "D:\\", "E:\\", "F:\\", "G:\\", "H:\\", "I:\\", "J:\\", "K:\\", "L:\\", "M:\\", "N:\\", "O:\\", "P:\\", "Q:\\", "R:\\", "S:\\", "T:\\", "U:\\", "V:\\", "W:\\", "X:\\", "Y:\\", "Z:\\" };
            var path = new List<string> { "SteamLibrary\\steamapps\\common\\GarrysMod\\garrysmod\\addons", "Program Files (x86)\\Steam\\steamapps\\common\\GarrysMod\\garrysmod\\addons" };
            foreach (var item in drive)
            {
                foreach (var item2 in path)
                {
                    var test = Path.Combine(item, item2);
                    if (Directory.Exists(test))
                    {
                        return test;
                    }
                }
            }
            return null;
        }

        static public string Select()
        {
            var SearchReturn = Search();
            if (SearchReturn == null)
            {
                var filePath = string.Empty;

                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Title = "Please select hl2.exe from GarrysMod";
                    openFileDialog.Filter = "GMod Application|hl2.exe";
                    openFileDialog.FilterIndex = 2;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        filePath = openFileDialog.FileName;
                    }
                }
                try
                {
                    Path.GetDirectoryName(filePath);
                }
                catch (Exception)
                {
                    DialogResult dialogResult = MessageBox.Show("You haven't selected anything.\nDo you wanna retry?", "Path Selector", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                    if (dialogResult == DialogResult.Yes)
                    {
                        Select();
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        return null;
                    }
                }
                var Paks = Path.GetDirectoryName(filePath) + "\\garrysmod\\addons";
                if (Directory.Exists(Paks))
                {
                    return Paks;
                }
                else
                {
                    MessageBox.Show("Somehow you managed to select the wrong File.\nNow be a good person and select the right one. XD", "Path Selector", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    Select();
                }
            }
            else
            {
                return (SearchReturn);
            }
            return "ERROR";
        }
    }
}
