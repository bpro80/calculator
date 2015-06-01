using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Web;
using BLToolkit.Data;
using BLToolkit.DataAccess;
using BO;
using Common.Api;
using Common.Api.Extensions;
using Configuration;
using DA.Accessors;
using ExcelProcessor;
using log4net;
using log4net.Util;
using NetOffice.ExcelApi;
using Constants = Common.Api.Constants;
using DataTable = System.Data.DataTable;

namespace DA.Entities
{
    public class EntityAdapter
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof (EntityAdapter));

        public static List<String> GetEntitiesByID(string dbName, string columnName, string columnName2 = "",
            string sWhere = "", string orderBy = "")
        {
            List<String> entities = null;
            using (var db = new DbManager(DB.LocalDb))
            {
                db.Command.CommandTimeout = 600;
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);

                try
                {
                    entities = fa.GetEntitiesByID(columnName, columnName2, sWhere, orderBy, dbName);
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return entities;
        }

        public static DataTable GetCalculatorResultByFilter(Guid firmId, string ip, string allQuery, string allQuerySummary,
            out int totalRowsCount, bool needLogTrack = false)
        {
            DataTable dtReportQuery;
            totalRowsCount = 0;
            using (var db = new DbManager(DB.LocalDb))
            {
                db.Command.CommandTimeout = 900;
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                int idQueryStatistic = -1;
                try
                {
                    if (needLogTrack)
                    // Для статистики
                    idQueryStatistic = InsertFirmStatisticQueries(firmId, allQuery, ip, "", 1,
                        DateTime.Now.ToString(CultureInfo.InvariantCulture), "");
                  
                    dtReportQuery = fa.GetCalculatorResultByFilter(allQuery, "");
                    if (dtReportQuery.Columns.Contains("countRows"))
                    {
                        dtReportQuery.Columns.Remove(dtReportQuery.Columns["countRows"]);
                        dtReportQuery.AcceptChanges();
                    }
                    totalRowsCount = dtReportQuery.Rows.Count;
                 
                    if (needLogTrack)
                      UpdateFirmStatisticQueries(idQueryStatistic, "", 2, 
                        DateTime.Now.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                    string innerException = "";
                    if (e.InnerException != null)
                    {
                      innerException =  " Inner Exception: ";
                        innerException += e.InnerException != null
                            ? e.InnerException.Message
                            : "";      
                    }
                    if (needLogTrack)  
                        UpdateFirmStatisticQueries(idQueryStatistic, e.Message + innerException, 3, DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    throw;
                }
            }

            return dtReportQuery;
        }


        public static OperationStatus SaveCalculatorResultForReportByFilter(string path, string fileName,
            string templateFileName, string allQuery, string allQuerySummary, out int totalRowsCount)
        {
            var status = OperationStatus.Failure;
            totalRowsCount = 0;
            using (var db = new DbManager(DB.LocalDb))
            {
                db.Command.CommandTimeout = 900;
                var a = new Application();
                
                Workbook book = a.Workbooks.Open(String.Format("{0}\\{1}", path, templateFileName));
                try
                {
                    db.SetCommand(allQuery);

                    var sheet = (Worksheet) book.Worksheets[1];
                    using (IDataReader reader = db.ExecuteReader())
                    {
                        int excelCurrentPosNumber = 2;
                        int colCount = 0;
                        bool schemaSaved = false;

                        // Stopwatch stopWatch = new Stopwatch();
                        //  stopWatch.Start();

                        while (reader.Read())
                        {
                            int indexDateColumn = -1;

                            if (!schemaSaved)
                            {
                                DataTable schemaTable = reader.GetSchemaTable();
                                if (schemaTable != null)
                                {
                                    colCount = schemaTable.Rows.Count;
                                    int colIndex = 1;
                                    foreach (DataRow dr in schemaTable.Rows)
                                    {
                                        sheet.Cells[1, colIndex].Value = dr[0];
                                        if (dr[0].ToString() ==
                                            ColumnsMapping.DateSending.GetStringValue()
                                                .Replace("[", "")
                                                .Replace("]", ""))
                                        {
                                            indexDateColumn = colIndex;
                                        }
                                        colIndex++;
                                    }
                                }
                                schemaSaved = true;
                            }

                            for (int colIndex = 0; colIndex < colCount; colIndex++)
                            {
                                if (indexDateColumn != colIndex + 1)
                                    sheet.Cells[excelCurrentPosNumber, colIndex + 1].NumberFormat = "@";
                                sheet.Cells[excelCurrentPosNumber, colIndex + 1].Value = reader[colIndex];
                            }

                            excelCurrentPosNumber++;
                            //if(excelCurrentPosNumber == 10000)
                            //break;
                        }
                        //  stopWatch.Stop();
                        // Get the elapsed time as a TimeSpan value.
                        //  TimeSpan ts = stopWatch.Elapsed;

                        // Format and display the TimeSpan value.
                        //  string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        //     ts.Hours, ts.Minutes, ts.Seconds,
                        //     ts.Milliseconds / 10);
                        //  Console.WriteLine("RunTime " + elapsedTime);
                        book.SaveAs(String.Format(@"{0}{1}", path, fileName));
                        status = OperationStatus.Success;
                    }
                }
                catch (Exception e)
                {
                    _logger.Info(e.StackTrace);
                    _logger.Error(e.Message);
                }
                finally
                {
                    book.Dispose();
                    a.Quit();
                    a.Dispose();
                }
            }

            return status;
        }


        public static OperationStatus SaveCalculatorResultAnalizeReport(DataTable dt, string path, string fileName, string templateName, out string fullPathReport)
        {
            var status = OperationStatus.Failure;
            try
            {
                var worker = new Worker(path, fileName);
                worker.Export(dt, new Hashtable(), "TemplateAnalizeReport");
                fullPathReport = worker.GetNewFileNamePath;
                status = OperationStatus.Success;
            }
            catch (Exception e)
            {
                _logger.Error(e.StackTrace);
                _logger.Error(e.Message);
                if (e.InnerException != null)
                {
                    _logger.Error(e.InnerException.Message);
                    _logger.Error(e.InnerException.StackTrace);

                }
                fullPathReport = null;
            }
            return status;
        }


        public static int InsertFirmStatisticQueries(Guid firmId, string text, string ip, string texterror,
            int statusId, string timeBegin, string timeEnd)
        {
            int id = -1;

            using (var db = new DbManager(DB.LocalDb))
            {
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    fa.InsertFirmStatisticQueries(text, firmId, ip, statusId, timeBegin, out id);
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return id;
        }

        public static bool UpdateFirmStatisticQueries(int id, string texterror,
         int statusId, string timeEnd)
        {
            bool success = false;
            using (var db = new DbManager(DB.LocalDb))
            {
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    fa.UpdateFirmStatisticQueries(id, statusId, texterror, timeEnd);
                    success = true;
                }
                catch (Exception e)
                {
                    _logger.Info(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return success;
        }

        public static List<QueryStatistic> GetQueryStatisticAll()
        {
            List<QueryStatistic> firmQueries = null;
            bool success = false;
            using (var db = new DbManager(DB.LocalDb))
            {
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                   firmQueries = fa.GetQueryStaticsticAll();
                    success = true;
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return firmQueries;
        }

        public static List<FeedBackMessage> GetFeedBackChart(Guid firmID)
        {
            List<FeedBackMessage> messages = null;
            bool success = false;
            using (var db = new DbManager(DB.LocalDb))
            {
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    messages = fa.GetFeedBackChart(firmID);
                    success = true;
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return messages;
        }

        public static List<FeedBackMessage> GetFeedBackMessagesAll()
        {
            List<FeedBackMessage> messages = null;
            bool success = false;
            using (var db = new DbManager(DB.LocalDb))
            {
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    messages = fa.GetFeedBackAll();
                    success = true;
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return messages;
        }

        public static List<string> GetCompanySendingDictionary()
        {
            List<string> list = null;
            using (var db = new DbManager(DB.LocalDb))
            {
                db.Command.CommandTimeout = 600;
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    list = fa.GetCompanySendingDictionary();
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return list;
        }

        public static List<string> GetStationSendingDictionary()
        {
            List<string> list = null;
            using (var db = new DbManager(DB.LocalDb))
            {
                db.Command.CommandTimeout = 600;
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    list = fa.GetStationSendingDictionary();
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return list;
        }

        public static List<string> GetStationDeliveringDictionary()
        {
            List<string> list = null;
            using (var db = new DbManager(DB.LocalDb))
            {
                db.Command.CommandTimeout = 600;
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    list = fa.GetDeliveringDictionary();
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return list;
        }
        public static List<string> GetCompanyRecipientDictionary()
        {
            List<string> list = null;
            using (var db = new DbManager(DB.LocalDb))
            {
                db.Command.CommandTimeout = 600;
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                    list = fa.GetCompanyRecipientDictionary();
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return list;
        }

        public static int FeedBackChartInsert(string text, int parentId, bool isAdmin, Guid firmID)
        {
            List<FeedBackMessage> messages = null;
            int id = -1;
            using (var db = new DbManager(DB.LocalDb))
            {
                var fa = DataAccessor.CreateInstance<EntityAccessor>(db);
                try
                {
                   id =  fa.FeedBackChartInsert(text,parentId,isAdmin,firmID);
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                }
            }

            return id;
        }
    }
}