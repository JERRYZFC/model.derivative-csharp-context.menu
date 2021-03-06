﻿using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Translator
{
  public partial class Progress : Form
  {
    private string FilePath { get; set; }

    public Progress(string filePath)
    {
      FilePath = filePath;

      InitializeComponent();
    }

    private async void Progress_Load(object sender, EventArgs e)
    {
      Rectangle workingArea = Screen.GetWorkingArea(this);
      this.Location = new Point(workingArea.Right - Size.Width,
                                workingArea.Bottom - Size.Height);

      trayNotify.BalloonTipTitle = "Forge extractor";

      // upload file
      NotifyUser("Uploading " + Path.GetFileName(FilePath));
      WindowState = FormWindowState.Minimized;
      string guid = await Server.UploadFile(FilePath);
      
      int progress = -1;
      do
      {
        // check if translation is ready       
        int lastInfo = await Server.GetTranslationStatus(guid);
        if (lastInfo > progress)
        {
          progress = lastInfo;
          NotifyUser(string.Format("Translating {0}% of {1}", progress, Path.GetFileName(FilePath)));
          progressBar1.Value = progress;
        }

        System.Threading.Thread.Sleep(1000); // wait until ask again
      } while (progress < 100);

      // status is returning 100%, inform the user
      NotifyUser("Excel file created.");
      progressBar1.Value = 100;
      System.Threading.Thread.Sleep(1000); // so this last message appears...

      // download results
      byte[] file = await Server.Download(guid);
      File.WriteAllBytes(FilePath.Replace(".rvt", ".xls"), file); // use same name as RVT, just replace extension

      // restore window
      WindowState = FormWindowState.Normal;
    }

    private void Progress_Resize(object sender, EventArgs e)
    {
      if (WindowState == FormWindowState.Minimized)
      {
        ShowInTaskbar = false;
        trayNotify.Visible = true;
        trayNotify.ShowBalloonTip(1000);
      }
    }

    private void NotifyUser(string message)
    {
      trayNotify.Text = message;
      trayNotify.BalloonTipText = message;
      trayNotify.ShowBalloonTip(800);
      logOutput.Text = message;
    }

    private void trayNotify_MouseDoubleClick(object sender, MouseEventArgs e)
    {
      ShowInTaskbar = true;
      trayNotify.Visible = false;
      WindowState = FormWindowState.Normal;
    }

    private void Progress_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (progressBar1.Value < 100)
      {
        // prevent closing the app before it reaches 100%
        DialogResult res = MessageBox.Show(
          "If you close the application, it will not download the results. Proceed?",
          "Are you sure?",
          MessageBoxButtons.YesNo);
        e.Cancel = (res == DialogResult.No);
      }
    }
  }
}
