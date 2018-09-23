﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using LeonReader.AbstractSADE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using LeonReader.Common;

namespace LeonReader.AbstractSADE.Tests
{
    class TestProcesser : Processer
    {
        /// <summary>
        /// 轮训因子
        /// </summary>
        public int Index { get; private set; } = 0;

        public override string ASDESource { get; protected set; } = "单元测试-ASDE";

        protected override void OnProcessStarted(object sender, DoWorkEventArgs e)
        {
            Index = 0;
            LogUtils.Debug($"内循环开始，Index = {Index}");
            while (Index++ < 10)
            {
                LogUtils.Debug($"内循环：Index = {Index}");
                OnProcessReport(Index, null);
                Thread.Sleep(500);
                if (ProcessWorker.CancellationPending)
                {
                    e.Cancel = true;
                    LogUtils.Debug("内循环：用户取消了处理");
                    break;
                }
            }
            LogUtils.Debug($"内循环结束，当前 Index = {Index}");
        }
    }

    /* —————————————————— */

    [TestClass()]
    public class ProcesserTests
    {
        [TestMethod()]
        public void ProcesserTest()
        {
            LogUtils.Debug("<———— 开始 Process 单元测试（自动结束） ————>");
            TestProcesser processer = new TestProcesser();

            processer.ProcessStarted += ProcesseStarted;
            processer.ProcessReport += ProcessReport;
            processer.ProcessCompleted += ProcesseCompleted;
            processer.Process();
            Thread.Sleep(6000);
        }

        [TestMethod]
        public void ProcesserHeadOff()
        {
            LogUtils.Debug("<———— 开始 Process 单元测试（立即拦截） ————>");
            TestProcesser processer = new TestProcesser();

            processer.ProcessStarted += ProcesseStartedButCancelImmediately;
            processer.ProcessReport += ProcessReport;
            processer.ProcessCompleted += ProcesseCompleted;
            processer.Process();
        }

        [TestMethod]
        public void ProcesserCancel()
        {
            LogUtils.Debug("<———— 开始 Process 单元测试（自动取消） ————>");
            TestProcesser processer = new TestProcesser();

            processer.ProcessStarted += ProcesseStarted;
            processer.ProcessReport += ProcessReportAndCancel;
            processer.ProcessCompleted += ProcesseCompleted;
            processer.Process();
            Thread.Sleep(3000);
        }

        /// <summary>
        /// 处理开始
        /// </summary>
        private void ProcesseStarted(object sender, DoWorkEventArgs e)
        {
            LogUtils.Info("我天，processer 告诉我她要开始处理了。");
        }

        /// <summary>
        /// 处理开始但立即取消
        /// </summary>
        private void ProcesseStartedButCancelImmediately(object sender, DoWorkEventArgs e)
        {
            LogUtils.Info("我天，processer 告诉我她要开始处理了，但我反手就取消了她的处理。");
            e.Cancel = true;
        }

        /// <summary>
        /// 报告处理进度
        /// </summary>
        private void ProcessReport(object sender, ProgressChangedEventArgs e)
        {
            LogUtils.Info($"我天，processer 说她处理进度为：Index = {e.ProgressPercentage}");
        }

        /// <summary>
        /// 报告处理进度并在特定值取消任务
        /// </summary>
        private void ProcessReportAndCancel(object sender, ProgressChangedEventArgs e)
        {
            LogUtils.Info($"我天，processer 说她处理进度为：Index = {e.ProgressPercentage}");
            if (e.ProgressPercentage == 5)
            {
                LogUtils.Info("但是我反手就取消了她");
                (sender as Processer).Cancle();
            }
        }

        /// <summary>
        /// 处理完成
        /// </summary>
        private void ProcesseCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LogUtils.Info("我天，processer 告诉我她处理完成了。");
            LogUtils.Info("<———— Process 单元测试完成 ————>");
        }

    }
}