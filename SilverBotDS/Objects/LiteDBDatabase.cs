﻿using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace SilverBotDS.Objects
{
    internal class LiteDBDatabase : ISBDatabase
    {
        /// <summary>
        /// Checks if guild is opted in
        /// </summary>
        /// <param name="serverid">the guild id</param>
        /// <returns><c>True</c> if opted in
        /// <c>False</c> if can not be opted in (ShadowBanned)
        /// <c>Null</c> if not opted in (no data)
        /// </returns>
        public Task<bool?> IsOptedInEmotes(ulong serverid)
        {
            try
            {
                using LiteDatabase db = new LiteDatabase(@"Filename=database.db; Connection=shared");
                ILiteCollection<Serveroptin> col = db.GetCollection<Serveroptin>();
                col.EnsureIndex(x => x.ServerId);
                return Task.FromResult(col.FindOne(x => x.ServerId == serverid)?.Optedin);
            }
            catch (Exception exep)
            {
                Program.SendLog(exep);
            }
            return Task.FromResult<bool?>(null);
        }

        public const int RESULT_LIMIT = 1000;

        /// <summary>
        /// Runs sql and returns a string or an image
        /// </summary>
        /// <param name="sql">the sql to run</param>
        /// <returns>A string and a image one of them is a null</returns>
        public async Task<Tuple<string, Image>> RunSqlAsync(string sql)
        {
            try
            {
                using LiteDatabase db = new LiteDatabase(@"Filename=database.db; Connection=shared");
                var da = db.Execute(sql);
                StringBuilder thing = new(ISBDatabase.HtmlStart);
                ulong index = 0;
                while (da.Read())
                {
                    if (index++ <= RESULT_LIMIT)
                    {
                        thing.AppendLine("<td>" + da.Current.RawValue + "</td>");
                    }
                    else
                    {
                        thing.AppendLine("<td>AND MORE</td>");
                    }
                }

                thing.AppendLine("</table></body></html>");

                if (index == 0)
                {
                    return new Tuple<string, Image>("nodata", null);
                }
                return new Tuple<string, Image>(null, await Browser.ScreenshotHtmlAsync(thing.ToString()));
            }
            catch (Exception e)
            {
                Program.SendLog(e);
                return new Tuple<string, Image>("Error", null);
            }
        }

        public Task InsertEmoteOptinAsync(Serveroptin e)
        {
            using LiteDatabase db = new LiteDatabase(@"Filename=database.db; Connection=shared");
            ILiteCollection<Serveroptin> col = db.GetCollection<Serveroptin>();
            col.Insert(e);
            return Task.CompletedTask;
        }

        public async Task<List<Serveroptin>> ServersOptedInEmotesAsync()
        {
            try
            {
                using LiteDatabase db = new LiteDatabase(@"Filename=database.db; Connection=shared");
                ILiteCollection<Serveroptin> col = db.GetCollection<Serveroptin>();
                col.EnsureIndex(x => x.ServerId);
                return col.FindAll().Where(x => x.Optedin == true).ToList();
            }
            catch (Exception exep)
            {
                Program.SendLog(exep);
            }
            return await Task.FromResult<List<Serveroptin>>(new());
        }

        public Task<string> GetLangCodeGuild(ulong id) => GetLangCodeGeneric(id, "GuildLang");

        public Task<string> GetLangCodeUser(ulong id) => GetLangCodeGeneric(id, "UserLang");

        public Task InserOrUpdateLangCodeUser(DbLang l) => InserOrUpdateLangCodeGeneric(l, "UserLang");

        public Task InserOrUpdateLangCodeGuild(DbLang l) => InserOrUpdateLangCodeGeneric(l, "GuildLang");

        public Task InserOrUpdateLangCodeGeneric(DbLang l, string dbname)
        {
            try
            {
                using LiteDatabase db = new LiteDatabase(@"Filename=database.db; Connection=shared");
                ILiteCollection<DbLang> col = db.GetCollection<DbLang>(dbname);

                col.Upsert(l);

                return Task.CompletedTask;
            }
            catch (Exception exep)
            {
                Program.SendLog(exep);
                return Task.FromException(exep);
            }
        }

        public Task<string> GetLangCodeGeneric(ulong id, string dbname)
        {
            try
            {
                using LiteDatabase db = new LiteDatabase(@"Filename=database.db; Connection=shared");
                ILiteCollection<DbLang> col = db.GetCollection<DbLang>(dbname);
                col.EnsureIndex(x => x.DId);
                return Task.FromResult(col.FindAll()
                          .First(x => x.DId == id)?.Name);
            }
            catch (Exception exep)
            {
                Program.SendLog(exep);
            }
            return Task.FromResult("en");
        }
    }
}