﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

using LeonReader.AbstractSADE;
using LeonReader.ArticleContentManager;
using LeonReader.Common;
using LeonReader.Model;

namespace GamerSkySADE
{
    public class GamerSkyAnalyzer : Analyzer
    {

        /// <summary>
        /// 页面计数
        /// </summary>
        private int PageCount = 0;

        /// <summary>
        /// 内容计数
        /// </summary>
        private int ContentCount = 0;

        /// <summary>
        /// 文章处理源
        /// </summary>
        public override string SADESource { get; protected set; } = "GamerSky-趣闻";

        protected override void OnProcessStarted(object sender, DoWorkEventArgs e)
        {
            if (!(e.Argument is Article article)) throw new ArgumentNullException($"GS分析器文章参数为空");
            if (string.IsNullOrEmpty(article.ArticleLink)) throw new ArgumentNullException($"GS分析器文章链接为空");

            //初始化
            this.PageCount = ACManager.GetACManager.GetPageCountAnalyzed(article);
            this.ContentCount = article.Contents?.Count ?? 0;

            LogUtils.Debug($"初始化文章内容数据库：{article.Title} ({article.ArticleID})");

            string ScanLink = ACManager.GetACManager.GetLastContentPageLink(article);
            if (string.IsNullOrEmpty(ScanLink))
            {
                ScanLink = article.ArticleLink;
            }
            else
            {
                //从上次分析进行到页面进行续作，需要排除此页已经存入数据库的内容记录
                this.ContentCount -= ACManager.GetACManager.RemoveContentFromPage(article, ScanLink);
            }

            //开始任务
            foreach (var content in this.AnalyseArticle(ScanLink))
            {
                LogUtils.Info($"接收到文章 ({article.ArticleID}) 内容：{content.ID}, {content.ImageLink}, {content.ImageDescription}");

                ACManager.GetACManager.AddContent(article, content);

                //触发事件更新已分析的页面数和图像数 
                this.OnProcessReport(this.PageCount, this.ContentCount);

                //允许用户取消处理
                if (this.ProcessWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
            }

            e.Result = new Tuple<int, int>(this.PageCount, this.ContentCount);
            LogUtils.Info($"文章分析完成：{this.PageCount}页, {this.ContentCount}图 (From：{this.SADESource})");
        }

        /// <summary>
        /// 分析文章
        /// </summary>
        private IEnumerable<ContentItem> AnalyseArticle(string PageAddress)
        {
            if (string.IsNullOrEmpty(PageAddress))
            {
                LogUtils.Error($"分析文章遇到错误，页面地址为空：{PageAddress}，From：{this.SADESource}");
                throw new ArgumentException($"分析文章遇到错误，页面地址为空：{PageAddress}，From：{this.SADESource}");
            }

            //页面链接队列（由递归改为循环）
            Queue<string> PageLinkQueue = new Queue<string>();
            PageLinkQueue.Enqueue(PageAddress);

            while (PageLinkQueue.Count > 0)
            {
                //页面计数自加
                this.PageCount++;

                string PageLink = PageLinkQueue.Dequeue();
                LogUtils.Info($"分析文章页面（第 {this.PageCount} 页）：{PageLink}");

                //页面内容，页数导航内容
                string ArticleContent = string.Empty, PaginationString = string.Empty;
                try
                {
                    ArticleContent = NetUtils.GetWebPage(PageLink);
                }
                catch (Exception ex)
                {
                    LogUtils.Error($"获取页面内容遇到错误（第 {this.PageCount} 页）：{PageLink}，{ex.Message}，From：{this.SADESource}");
                    throw;
                }

                if (string.IsNullOrEmpty(ArticleContent))
                {
                    LogUtils.Error($"获取页面内容遇到错误（第 {this.PageCount} 页）：{PageLink}，From：{this.SADESource}");
                    throw new ArgumentException($"获取页面内容遇到错误（第 {this.PageCount} 页）：{PageLink}，From：{this.SADESource}");
                }

                //获取文章主体内容
                ArticleContent = this.GetArticleContent(ArticleContent);
                if (ArticleContent == string.Empty)
                {
                    LogUtils.Error($"页面主体部分匹配失败（第 {this.PageCount} 页）：{PageLink}From：{this.SADESource}");
                    yield break;
                }

                try
                {
                    Tuple<string, string> tuple = this.SplitContentAndPagination(ArticleContent);
                    ArticleContent = tuple.Item1;
                    PaginationString = tuple.Item2;
                }
                catch (Exception ex)
                {
                    LogUtils.Warn($"分割内容和分页失败（第 {this.PageCount} 页）：{PageLink}，异常：{ex.Message}，From：{this.SADESource}");
                }

                //分析页面内容
                if (ArticleContent == string.Empty)
                {
                    LogUtils.Warn($"文章内容区域匹配为空（第 {this.PageCount} 页）：{PageLink}，From：{this.SADESource}");
                }
                else
                {
                    LogUtils.Debug($"开始处理文章主体内容");
                    //分割文章内容
                    string[] ContentItems = this.GetContentList(ArticleContent);
                    foreach (var content in ContentItems)
                    {
                        //从内容项转换为内容实体
                        ContentItem contentItem = this.ConvertToContentItem(content);
                        if (contentItem == null)
                        {
                            LogUtils.Warn($"转换为内容实体失败（第 {this.PageCount} 页）：{PageLink}，From：{this.SADESource}，内容：\n< ——————————\n{content}\n—————————— >");
                        }
                        else
                        {
                            contentItem.PageLink = PageLink;
                            this.ContentCount++;
                            yield return contentItem;
                        }
                    }
                }

                //分析分页内容
                if (PaginationString == string.Empty)
                {
                    LogUtils.Error($"文章分页区域匹配为空，无法继续。（第 {this.PageCount} 页）：{PageLink}，From：{this.SADESource}");
                    yield break;
                }
                else
                {
                    LogUtils.Debug("开始处理分页区域");
                    //分析下一页链接
                    string NextLink = this.GetNextLink(PaginationString);
                    if (string.IsNullOrEmpty(NextLink))
                    {
                        LogUtils.Info($"文章下一页链接为空，分析结束。（共 {this.PageCount} 页）：{PageLink}，From：{this.SADESource}");
                    }
                    else
                    {
                        LogUtils.Info($"发现下一页链接：{NextLink}，From：{this.SADESource}");
                        //发现新页，将新页链接入队
                        PageLinkQueue.Enqueue(NextLink);
                    }
                }
            }
        }

        /// <summary>
        /// 获取下一页链接
        /// </summary>
        /// <param name="pagination"></param>
        /// <returns></returns>
        private string GetNextLink(string pagination)
        {
            string ContentPattern = "<a href=\"(?<NextPageLink>.+?)\">下一页</a>";
            Regex ContentRegex = new Regex(ContentPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Match ContentMatch = ContentRegex.Match(pagination);

            if (ContentMatch.Success)
            {
                //返回下一页链接
                return ContentMatch.Groups["NextPageLink"].Value as string;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取文章主体内容
        /// </summary>
        /// <param name="articleContent"></param>
        /// <returns></returns>
        private string GetArticleContent(string articleContent)
        {
            string ContentPattern = string.Format("<([a-z]+)(?:(?!class)[^<>])*class=([\"']?){0}\\2[^>]*>(?>(?<o><\\1[^>]*>)|(?<-o></\\1>)|(?:(?!</?\\1).))*(?(o)(?!))</\\1>", Regex.Escape("Mid2L_con"));
            Regex ContentRegex = new Regex(ContentPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match ContentMatch = ContentRegex.Match(articleContent);
            return ContentMatch.Value;
        }

        /// <summary>
        /// 分割内容和分页部分
        /// </summary>
        /// <param name="page"></param>
        private Tuple<string, string> SplitContentAndPagination(string page)
        {
            int PaginationStart = page.IndexOf("<!--{pe.begin.pagination}-->");
            if (PaginationStart < 0) throw new Exception("未找到分割关键词");

            return new Tuple<string, string>(page.Substring(0, PaginationStart), page.Substring(PaginationStart));
        }

        /// <summary>
        /// 从内容项转换为内容实体
        /// </summary>
        /// <param name="contentItem"></param>
        /// <returns></returns>
        private ContentItem ConvertToContentItem(string contentItem)
        {
            string Link = string.Empty;
            string Description = string.Empty;
            string ContentWithoutNL = string.Empty;
            ContentWithoutNL = contentItem.Replace("\n", "");

            //使用第一种匹配策略获取图像路径
            string ContentPattern = "<a.*?shtml\\?(?<ImageLink>.+?)\"";
            Regex ContentRegex = new Regex(ContentPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match ContentMatch = ContentRegex.Match(ContentWithoutNL);

            LogUtils.Debug($"尝试第一种匹配策略...    From：{this.SADESource}");
            if (ContentMatch.Success)
            {
                //匹配成功
                Link = ContentMatch.Groups["ImageLink"].Value as string;
            }
            else
            {
                LogUtils.Debug($"尝试第二种匹配策略...    From：{this.SADESource}");
                //匹配失败，切换策略获取图像路径
                ContentPattern = "<img.*?src=\"(?<ImageLink>.+?)\"";
                ContentRegex = new Regex(ContentPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                ContentMatch = ContentRegex.Match(ContentWithoutNL);
                if (ContentMatch.Success)
                {
                    //匹配成功
                    Link = ContentMatch.Groups["ImageLink"].Value as string;
                }
                else
                {
                    LogUtils.Warn($"无法匹配内容数据（第 {this.PageCount} 页），From：{this.SADESource}，内容：\n< ——————————\n{contentItem}\n—————————— >");
                    //再次匹配失败，自暴自弃~
                    return null;
                }
            }
            //获取图像描述
            string[] TempDescription = Regex.Split(ContentWithoutNL, "<br>");
            Description = TempDescription.Length > 1 ? TempDescription.Last() : "";

            //返回对象
            LogUtils.Debug($"返回内容数据：{Link}，From：{this.SADESource}");
            ContentItem content = new ContentItem(Description, Link, IOUtils.GetFileName(Link));
            return content;
        }

        /// <summary>
        /// 分割文章目录
        /// </summary>
        /// <param name="articleContent"></param>
        /// <returns></returns>
        private string[] GetContentList(string articleContent)
        {
            return Regex.Split(articleContent, "</p>");
        }

    }
}
