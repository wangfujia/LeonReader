﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

using LeonReader.Common;
using LeonReader.AbstractSADE;
using LeonDirectUI.Container;

namespace LeonReader.Client
{
    public partial class MainForm : Form
    {
        Assembly GS_ASDE;
        Type ScannerType;
        Scanner scanner;
        Type AnalyzerType;
        Analyzer analyzer;
        Type DownloaderType;
        Downloader downloader;
        Type ExporterType;
        Exporter exporter;

        SizableContainer sizableContainer = new SizableContainer();

        public MainForm()
        {
            InitializeComponent();

            sizableContainer.SetBounds(20, 20, 107, 91);
            sizableContainer.Dragable = true;
            sizableContainer.Sizable = true;
            sizableContainer.MinimumSize = new Size(107, 91);
            sizableContainer.BackgroundImage = UnityResource.BrokenImage;
            sizableContainer.BackgroundImageLayout = ImageLayout.Stretch;

            this.Controls.Add(sizableContainer);
            sizableContainer.BringToFront();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            GS_ASDE = AssemblyHelper.CreateAssembly("GamerSkySADE.dll");
            if (GS_ASDE == null)
            {
                LogHelper.Fatal("创建程序集反射失败，终止");
                MessageBox.Show("创建程序集反射失败，终止");
                Application.Exit();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //输出程序集内定义的类型全名称
            //LogHelper.Debug($"程序集内定义的类型：\n\t{string.Join("\t\n", Assembly.LoadFrom("GamerSkySADE.dll").DefinedTypes.Select(type => type.FullName))}");
            //LogHelper.Info($"全局配置-下载目录：{ConfigHelper.GetConfigHelper.DownloadDirectory}");

            ScannerType = GS_ASDE.GetSubTypes(typeof(Scanner)).FirstOrDefault();
            if (ScannerType == null)
            {
                LogHelper.Fatal("未发现程序集内存在扫描器类型，终止");
                return;
            }

            scanner = GS_ASDE.CreateInstance(ScannerType) as Scanner;
            scanner.ProcessStarted += (s, v) => { this.Invoke(new Action(() => { button1.Enabled = false; button2.Enabled = false; button3.Enabled = false; button4.Enabled = false; })); };
            scanner.ProcessReport += (s, v) => { this.Text = $"已扫描：{v.ProgressPercentage} 篇文章"; };
            scanner.ProcessCompleted += (s, v) => { this.Text = $"{this.Text} - [扫描完成]"; button1.Enabled = true; button2.Enabled = true; };
            scanner.Process();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AnalyzerType = GS_ASDE.GetSubTypes(typeof(Analyzer)).FirstOrDefault();
            if (ScannerType == null)
            {
                LogHelper.Fatal("未发现程序集内存在分析器类型，终止");
                return;
            }

            analyzer = GS_ASDE.CreateInstance(AnalyzerType) as Analyzer;
            analyzer.ProcessStarted += (s, v) => { this.Invoke(new Action(() => { button2.Enabled = false; button3.Enabled = false; button4.Enabled = false; })); };
            analyzer.ProcessReport += (s, v) => { this.Text = $"已分析：{v.ProgressPercentage} 页，{(int)v.UserState} 图"; };
            analyzer.ProcessCompleted += (s, v) => { this.Text = $"{this.Text} - [分析完成]"; button2.Enabled = true; button3.Enabled = true; };
            analyzer.SetTargetURI(@"https://www.gamersky.com/ent/201809/1096176.shtml");
            analyzer.Process();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DownloaderType = GS_ASDE.GetSubTypes(typeof(Downloader)).FirstOrDefault();
            if (DownloaderType == null)
            {
                LogHelper.Fatal("未发现程序集内存在下载器类型，终止");
                return;
            }

            downloader = GS_ASDE.CreateInstance(DownloaderType) as Downloader;
            downloader.ProcessStarted += (s, v) => { this.Invoke(new Action(() => { button2.Enabled = false; button3.Enabled = false; button4.Enabled = false; })); };
            downloader.ProcessReport += (s, v) => { this.Text = $"已下载：{v.ProgressPercentage} 张图片，{(int)v.UserState} 张失败"; };
            downloader.ProcessCompleted += (s, v) => { this.Text = $"{this.Text} - [下载完成]"; button2.Enabled = true; button3.Enabled = true; button4.Enabled = true; };
            downloader.SetTargetURI(@"https://www.gamersky.com/ent/201809/1096176.shtml");
            downloader.Process();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ExporterType = GS_ASDE.GetSubTypes(typeof(Exporter)).FirstOrDefault();
            if (ExporterType == null)
            {
                LogHelper.Fatal("未发现程序集内存在导出器类型，终止");
                return;
            }

            exporter = GS_ASDE.CreateInstance(ExporterType) as Exporter;
            exporter.ProcessStarted += (s, v) => { this.Invoke(new Action(() => { button2.Enabled = false; button3.Enabled = false; button4.Enabled = false; })); };
            exporter.ProcessReport += (s, v) => { this.Text = $"已导出：{v.ProgressPercentage} / {(int)v.UserState} 张图片"; };
            exporter.ProcessCompleted += (s, v) => { this.Text = $"{this.Text} - [导出完成]"; button2.Enabled = true; button3.Enabled = true; button4.Enabled = true; };
            exporter.SetTargetURI(@"https://www.gamersky.com/ent/201809/1096176.shtml");
            exporter.Process();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Form form = new Form();
            WebBrowser browser = new WebBrowser();
            form.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
            browser.Navigate(@"F:\C Sharp\LeonReader\Debug\Articles\201809051640044034\日本30岁的女装大佬 这么娇小可爱竟然是男人.html");
            form.ShowDialog();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            sizableContainer.Dispose();
        }

    }
}
