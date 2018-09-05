﻿using LeonReader.AbstractSADE;
using LeonReader.Common;
using LeonReader.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        public override string ASDESource { get; protected set; } = "GamerSky-趣闻";

        protected override void OnProcessStarted(object sender, DoWorkEventArgs e)
        {
            base.OnProcessStarted(sender, e);

            if (TargetURI == null)
            {
                LogHelper.Error($"分析器使用了空的 TargetURI，From：{this.ASDESource}");
                throw new Exception($"分析器使用了空的 TargetURI，From：{this.ASDESource}");
            }

            LogHelper.Info($"开始分析文章链接：{TargetURI?.AbsoluteUri}，From：{this.ASDESource}");
            //获取链接关联的文章对象
            Article article = GetArticle(TargetURI.AbsoluteUri, this.ASDESource);
            if (article == null)
            {
                LogHelper.Error($"未找到链接关联的文章实体：{TargetURI.AbsoluteUri}，From：{this.ASDESource}");
                throw new Exception($"未找到链接关联的文章实体：{TargetURI.AbsoluteUri}，From：{this.ASDESource}");
            }
            LogHelper.Debug($"匹配到链接关联的文章实体：{article.Title} ({article.ArticleID}) => {article.ArticleLink}");

            //初始化
            PageCount = 0;
            ContentCount = 0;
            LogHelper.Debug($"初始化文章内容数据库：{article.Title} ({article.ArticleID})");
            if(article.Contents!=null && article.Contents.Count>0)
                TargetDBContext.Contents.RemoveRange(article.Contents);
            article.AnalyzeTime = DateTime.Now;
            TargetDBContext.SaveChanges();

            //开始任务
            foreach (var content in AnalyseArticle(article.ArticleLink))
            {
                LogHelper.Info($"接收到文章 ({article.ArticleID}) 内容：{content.ID}, {content.ImageLink}, {content.ImageDescription}");
                article.Contents.Add(content);
                //TODO: 触发事件更新已分析的页面数和图像数 ContentCount & PageCount

                //允许用户取消处理
                if (ProcessWorker.CancellationPending) break;
            }

            //全部分析后保存文章内容数据
            TargetDBContext.SaveChanges();
            LogHelper.Info($"文章分析完成：{TargetURI.AbsoluteUri} (From：{this.ASDESource})");
        }

        /// <summary>
        /// 获取关联的文章实体
        /// </summary>
        /// <param name="link"></param>
        /// <param name="asdeSource"></param>
        /// <returns></returns>
        private Article GetArticle(string link, string asdeSource)
        {
            LogHelper.Debug($"获取链接关联的文章ID：{link}，Form：{asdeSource}");
            if (string.IsNullOrEmpty(link) || string.IsNullOrEmpty(asdeSource)) return default(Article);

            Article article = TargetDBContext.Articles
                .FirstOrDefault(
                    art =>
                    art.ArticleLink == link &&
                    art.ASDESource == asdeSource
                );
            return article;
        }

        /// <summary>
        /// 分析文章
        /// </summary>
        private IEnumerable<ContentItem> AnalyseArticle(string PageAddress)
        {
            if (string.IsNullOrEmpty(PageAddress))
            {
                LogHelper.Error($"分析文章遇到错误，页面地址为空：{TargetURI.AbsoluteUri}，From：{this.ASDESource}");
                throw new Exception($"分析文章遇到错误，页面地址为空：{TargetURI.AbsoluteUri}，From：{this.ASDESource}");
            }

            //页面链接队列（由递归改为循环）
            Queue<string> PageLinkQueue = new Queue<string>();
            PageLinkQueue.Enqueue(PageAddress);

            while(PageLinkQueue.Count>0)
            {
                //页面计数自加
                PageCount++;

                string PageLink = PageLinkQueue.Dequeue();
                LogHelper.Info($"分析文章页面（第 {PageCount} 页）：{PageLink}");

                //页面内容，页数导航内容
                string ArticleContent = string.Empty, PaginationString = string.Empty;
                try
                {
                    ArticleContent = NetHelper.GetWebPage(PageLink);
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"获取页面内容遇到错误（第 {PageCount} 页）：{PageLink}，{ex.Message}，From：{this.ASDESource}");
                    throw ex;
                }

                if (string.IsNullOrEmpty(ArticleContent))
                {
                    LogHelper.Error($"获取页面内容遇到错误（第 {PageCount} 页）：{PageLink}，From：{this.ASDESource}");
                    throw new Exception($"获取页面内容遇到错误（第 {PageCount} 页）：{PageLink}，From：{this.ASDESource}");
                }

                //获取文章主体内容
                ArticleContent = GetArticleContent(ArticleContent);
                if (ArticleContent == string.Empty)
                {
                    LogHelper.Error($"页面主体部分匹配失败（第 {PageCount} 页）：{PageLink}From：{this.ASDESource}");
                    yield break;
                }

                try
                {
                    Tuple<string,string> tuple = SplitContentAndPagination(ArticleContent);
                    ArticleContent = tuple.Item1;
                    PaginationString = tuple.Item2;
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"分割内容和分页失败（第 {PageCount} 页）：{PageLink}，异常：{ex.Message}，From：{this.ASDESource}");
                }
            
                //分析页面内容
                if (ArticleContent == string.Empty)
                {
                    LogHelper.Warn($"文章内容区域匹配为空（第 {PageCount} 页）：{PageLink}，From：{this.ASDESource}");
                }
                else
                {
                    LogHelper.Debug($"开始处理文章主体内容");
                    //分割文章内容
                    string[] ContentItems = GetContentList(ArticleContent);
                    foreach (var content in ContentItems)
                    {
                        //从内容项转换为内容实体
                        ContentItem contentItem = ConvertToContentItem(content);
                        if (contentItem == null)
                        {
                            LogHelper.Warn($"转换为内容实体失败（第 {PageCount} 页）：{PageLink}，From：{this.ASDESource}，内容：\n< ——————————\n{content}\n—————————— >");
                        }
                        else
                        {
                            ContentCount++;
                            yield return contentItem;
                        }
                    }
                }

                //分析分页内容
                if (PaginationString == string.Empty)
                {
                    LogHelper.Error($"文章分页区域匹配为空，无法继续。（第 {PageCount} 页）：{PageLink}，From：{this.ASDESource}");
                    yield break;
                }
                else
                {
                    LogHelper.Debug("开始处理分页区域");
                    //分析下一页链接
                    string NextLink = GetNextLink(PaginationString);
                    if (string.IsNullOrEmpty(NextLink))
                    {
                        LogHelper.Info($"文章下一页链接为空，分析结束。（共 {PageCount} 页）：{PageLink}，From：{this.ASDESource}");
                    }
                    else
                    {
                        LogHelper.Info($"发现下一页链接：{NextLink}，From：{this.ASDESource}");
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

            LogHelper.Debug($"尝试第一种匹配策略...    From：{this.ASDESource}");
            if (ContentMatch.Success)
            {
                //匹配成功
                Link = ContentMatch.Groups["ImageLink"].Value as string;
            }
            else
            {
                LogHelper.Debug($"尝试第二种匹配策略...    From：{this.ASDESource}");
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
                    LogHelper.Warn($"无法匹配内容数据（第 {PageCount} 页），From：{this.ASDESource}，内容：\n< ——————————\n{contentItem}\n—————————— >");
                    //再次匹配失败，自暴自弃~
                    return null;
                }
            }
            //获取图像描述
            string[] TempDescription = Regex.Split(ContentWithoutNL, "<br>");
            Description = TempDescription.Length > 1 ? TempDescription.Last() : "";

            //返回对象
            LogHelper.Debug($"返回内容数据：{Link}，From：{this.ASDESource}");
            ContentItem content = new ContentItem(Description, Link, IOHelper.GetFileName(Link));
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
