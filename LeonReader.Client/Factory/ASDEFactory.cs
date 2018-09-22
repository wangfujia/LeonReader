﻿using LeonReader.AbstractSADE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LeonReader.Common;

namespace LeonReader.Client.Factory
{
    /// <summary>
    /// 扫描器、分析器、下载器、导出器 工厂
    /// </summary>
    public class SADEFactory
    {

        /// <summary>
        /// 创建扫描器
        /// </summary>
        /// <param name="assembly">扫描器所在程序集</param>
        /// <returns></returns>
        public IEnumerable<Scanner> CreateScanners(Assembly assembly)
        {
            if (assembly == null) throw new Exception("生产扫描器时使用空的程序集");

            LogHelper.Info($"在程序集 {assembly.FullName} 内创建所有扫描器...");
            foreach (var type in assembly.GetSubTypes(typeof(Scanner)))
            {
                yield return assembly.CreateInstance(type) as Scanner;
            }
        }



    }
}
