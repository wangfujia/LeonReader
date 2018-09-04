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

namespace LeonReader.Client
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //输出程序集内定义的类型全名称
            Console.WriteLine($"程序集内定义的类型：\n\t{string.Join("\t\n", Assembly.LoadFrom("GamerSkySADE.dll").DefinedTypes.Select(type=>type.FullName))}");
            Console.WriteLine($"全局配置-下载目录：{ConfigHelper.GetConfigHelper.DownloadDirectory}");

            Assembly GS_ASDE = AssemblyHelper.CreateAssembly("GamerSkySADE.dll");
            if (GS_ASDE == null)
            {
                Console.WriteLine("创建陈晓估计反射失败，终止");
                return;
            }

            Type ScannerType = GS_ASDE.GetSubTypes(typeof(Scanner)).FirstOrDefault();
            if (ScannerType == null)
            {
                Console.WriteLine("未发现程序集内存在扫描器类型，终止");
                return;
            }

            Scanner scanner = GS_ASDE.CreateInstance(ScannerType) as Scanner;
            scanner.Process();

            Type AnalyzerType = GS_ASDE.GetSubTypes(typeof(Analyzer)).FirstOrDefault();
            if (ScannerType == null)
            {
                Console.WriteLine("未发现程序集内存在分析器类型，终止");
                return;
            }

            Analyzer analyzer = GS_ASDE.CreateInstance(AnalyzerType) as Analyzer;
            analyzer.SetTargetURI(@"https://www.gamersky.com/ent/201808/1094495.shtml");
            analyzer.Process();
        }

    }
}
