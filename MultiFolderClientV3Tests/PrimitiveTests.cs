using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;
using MultiFolderClientV3;
using MultiFolderClientV3.Core;

namespace MultiFolderClientV3Tests
{
    [TestClass]
    public class PrimitiveTests
    {
        private Synchronizer _app = new Synchronizer(true);
        
        [TestMethod]
        public void TestUpdate()
        {
            //Updater updater = new Updater(_app, 2);
            //updater.Update();
        }
        
        [TestMethod]
        public void TestDeleteWithPowerShell()
        {
            //// string command = "Get-ChildItem -Path \"C:\\Users\\zubko\\source\\repos\\MultiFolderClientV3\\MultiFolderClientV3\\bin\\Debug\" -Filter \"*y8FfZm_Temp*\" -Recurse | ForEach-Object { Rename-Item $_.FullName $_.Name.Replace(\"y8FfZm_Temp\", \"\") };";
            //string command = "del \"C:\\Users\\zubko\\source\\repos\\MultiFolderClientV3\\MultiFolderClientV3\\bin\\Debug\\dolphin — копия.ico\"";
            //// string command = "Get-ChildItem -Path \\\"C:\\Users\\zubko\\source\\repos\\MultiFolderClientV3\\MultiFolderClientV3\\bin\\Debug\\\" -Filter \\\"*y8FfZm_Temp*\\\" -Recurse | ForEach-Object { Rename-Item $_.FullName $_.Name.Replace(\\\"y8FfZm_Temp\\\", \\\"\\\") };";
            //Console.WriteLine(command);
            //HelpFunctions.Cmd(command.Replace("\"", "\\\""), true);
        }
        
        [TestMethod]
        public void TestGetWorkingFile()
        {
            //_app.multifolder.GetWorkingFile("dolphin.ico", @"C:\Users\zubko\Pictures\new folder\for test\dolphin.ico");
        }

        [TestMethod]
        public void TestGetExePath()
        {
            Console.WriteLine(HelpFunctions.GetExePath());
            Console.WriteLine(AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("------------------------");
        }

        [TestMethod]
        public void TestCreateShortcut()
        {
            var shortcutPath = Path.Combine(Synchronizer.StartupFolderPath, "MultiFolderV3.lnk");
            if (!File.Exists(shortcutPath))
                HelpFunctions.CreateShortcut(shortcutPath, Synchronizer.ExePath);
        }
    }
}
