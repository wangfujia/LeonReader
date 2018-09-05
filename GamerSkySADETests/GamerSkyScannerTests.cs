﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using GamerSkySADE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeonReader.AbstractSADE;
using LeonReader.Common;
using System.Reflection;
using LeonReader.Model;

namespace GamerSkySADE.Tests
{
    [TestClass()]
    public class GamerSkyScannerTests
    {
        [TestMethod()]
        public void ProcessTest()
        {
            Scanner scanner = new GamerSkyScanner();
            scanner.Process();
        }

        [TestMethod()]
        public void ScanArticlesTest()
        {
            GamerSkyScanner scanner = new GamerSkyScanner();
            MethodInfo methodInfo = typeof(GamerSkyScanner).GetMethod(
                "ScanArticles",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (methodInfo == null)
            {
                Console.WriteLine("反射获取方法失败");
                Assert.Fail();
            }

            //在线资源测试
            foreach (var article in
                (IEnumerable<Article>)methodInfo.Invoke(
                    scanner,
                    new object[] { NetHelper.GetWebPage(scanner.TargetURI) }
                )
            )
            {
                Console.WriteLine($"扫描到文章：{article.Title} ({article.ArticleID})");
            }

            //离线资源测试
            foreach (var article in
                (IEnumerable<Article>)methodInfo.Invoke(
                    scanner,
                    new object[] { GamerSkySADETests.UnitTestResource.FullTestResource }
                )
            )
            {
                Console.WriteLine($"扫描到文章：{article.Title} ({article.ArticleID})");
            }
        }

        [TestMethod()]
        public void CheckArticleExistTest()
        {
            GamerSkyScanner scanner = new GamerSkyScanner();
            MethodInfo methodInfo = typeof(GamerSkyScanner).GetMethod(
                "CheckArticleExist",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (methodInfo == null)
            {
                Console.WriteLine("反射获取方法失败");
                Assert.Fail();
            }

            Article article = new Article() { ArticleID = "10000", Title = "种子文章", ASDESource = "DataSeed" };

            scanner.TargetDBContext.Articles.RemoveRange(scanner.TargetDBContext.Articles.ToArray());
            scanner.TargetDBContext.SaveChanges();
            Assert.IsFalse((bool)methodInfo.Invoke(scanner, new object[] { article }));

            scanner.TargetDBContext.Articles.Add(article);
            scanner.TargetDBContext.SaveChanges();
            Assert.IsTrue((bool)methodInfo.Invoke(scanner, new object[] { article }));
        }

        [TestMethod()]
        public void ConvertToArticleTest()
        {
            GamerSkyScanner scanner = new GamerSkyScanner();
            MethodInfo methodInfo = typeof(GamerSkyScanner).GetMethod(
                "ConvertToArticle",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (methodInfo == null)
            {
                Console.WriteLine("反射获取方法失败");
                Assert.Fail();
            }

            Article article = (Article)methodInfo.Invoke(scanner, new object[] { GamerSkySADETests.UnitTestResource.ConvertToArticleTestResource });
            
            Assert.IsNotNull(article);
            Assert.AreEqual("文章链接", article.ArticleLink);
            Assert.AreEqual("文章描述", article.Description);
            Assert.AreEqual("图像链接", article.ImageLink);
            Assert.AreEqual("发布时间", article.PublishTime);
            Assert.AreEqual("文章标题", article.Title);
        }

        [TestMethod()]
        public void GetCatalogListTest()
        {
            GamerSkyScanner scanner = new GamerSkyScanner();
            MethodInfo methodInfo = typeof(GamerSkyScanner).GetMethod(
                "GetCatalogList",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (methodInfo == null)
            {
                Console.WriteLine("反射获取方法失败");
                Assert.Fail();
            }

            string[] catalogList = (string[])methodInfo.Invoke(scanner, new object[] { GamerSkySADETests.UnitTestResource.GetCatalogListTestResource });
            Assert.IsTrue(catalogList.Length > 0);
        }

        [TestMethod()]
        public void GetCatalogContentTest()
        {
            GamerSkyScanner scanner = new GamerSkyScanner();
            MethodInfo methodInfo = typeof(GamerSkyScanner).GetMethod(
                "GetCatalogContent",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (methodInfo == null)
            {
                Console.WriteLine("反射获取方法失败");
                Assert.Fail();
            }
            
            //离线资源测试
            string catalogListOffLine = (string)methodInfo.Invoke(scanner, new object[] { GamerSkySADETests.UnitTestResource.FullTestResource });
            Assert.IsTrue(catalogListOffLine.Length > 0);

            //在线资源测试
            string catalogListOnLine = (string)methodInfo.Invoke(scanner, new object[] { NetHelper.GetWebPage(scanner.TargetURI) });
            Assert.IsTrue(catalogListOnLine.Length > 0);
        }
    }
}