using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BO;
using BO.Implementation;
using BO.Implementation.Caching;
using BO.Implementation.Caching.Provider;
using CalculatorZd.Models;
using Common.Api;
using Common.Api.Extensions;
using log4net;
using Utils;

namespace CalculatorZd.Services
{
    public class CalculatorService
    {
        public enum SelectType
        {
            Inner,
            Outer
        }

        public static String GetQueryReport(List<SelectedColumnsViewModel> selectedColumns,
            List<FilterParamstItemViewModel> filterParamstItems, int pageIdFrom, int pageIdTo,
            out string allQuerySummary, out ViewTypeReport viewTypeReport, out string queryForExelReport, VagonSourceTypeParamEnum dbType)
        {
            viewTypeReport = GetTypeReport(selectedColumns);
            string dbName = dbType == VagonSourceTypeParamEnum.Gruzhon ? ServerProperties.Instance.DBGruzhon : ServerProperties.Instance.DBPorozhn;
            //Where
             
            string additionalWhere = "";

            string whereQuery = " WHERE " + GetWhereQuery(filterParamstItems, additionalWhere, viewTypeReport == ViewTypeReport.Svodnaya && dbType == VagonSourceTypeParamEnum.Gruzhon);
            string whereWithLimit = whereQuery;
            string allSelect;
            var groupBy = new StringBuilder();
            var groupingData = new StringBuilder();
            

            if (viewTypeReport == ViewTypeReport.Svodnaya)
            {
                if (selectedColumns.Count == 2 &&
                    selectedColumns.Any(column => column.id == (int) ColumnsMapping.VolumeTransportation) &&
                    selectedColumns.Any(column => column.id == (int) ColumnsMapping.DestinationTariff)
                    ||
                    (selectedColumns.Count == 1 &&
                     (selectedColumns.Any(column => column.id == (int) ColumnsMapping.VolumeTransportation) ||
                      selectedColumns.Any(column => column.id == (int) ColumnsMapping.DestinationTariff) ||
                      selectedColumns.Any(column => column.id == (int) ColumnsMapping.TonnKm)))
                    || (selectedColumns.Count == 2 &&
                        (selectedColumns.Any(column => column.id == (int) ColumnsMapping.TonnKm) &&
                         selectedColumns.Any(column => column.id == (int) ColumnsMapping.DestinationTariff) ||
                         (selectedColumns.Any(column => column.id == (int) ColumnsMapping.TonnKm) &&
                          selectedColumns.Any(column => column.id == (int) ColumnsMapping.VolumeTransportation))))
                    || (selectedColumns.Count == 3 &&
                        (selectedColumns.Any(column => column.id == (int) ColumnsMapping.VolumeTransportation) &&
                         selectedColumns.Any(column => column.id == (int) ColumnsMapping.DestinationTariff) &&
                         selectedColumns.Any(column => column.id == (int) ColumnsMapping.TonnKm))))
                {
                    allSelect = String.Format("SELECT {0}  FROM {1} ", GetSelectedColumns(selectedColumns),
                        dbName);
                    queryForExelReport = allSelect;
                    allSelect += whereWithLimit;
                    allQuerySummary = String.Format("SELECT Count(*) as TotalRowsCount,  {0}  FROM {1} ",
                        GetSelectedColumns(selectedColumns),
                        dbName);
                    return allSelect;
                }

                if (dbType == VagonSourceTypeParamEnum.Gruzhon)
                {
                    string whereQueryTopQuery = " WHERE " + GetWhereQuery(filterParamstItems, additionalWhere, false);
                   
                    string topQuery = String.Format(QueryUtils.GetGroupedFabricatedCargoQuery(), dbName, whereQueryTopQuery);
                    StringBuilder selectedColumnsList = GetInnerSelectedColumnsSborka(selectedColumns, QueryUtils.PrefixGr);

                    // Order By
                    string orderByColumns = null;
                    StringBuilder orderBy = GetOrderColumns(selectedColumns, "");
                    if (orderBy.Length > 0)
                        orderByColumns = String.Format(" Order By {0}", orderBy);

                    ////Group by columns
                    groupingData = GetInnerGroupSborkaByString(selectedColumns, Utils.QueryUtils.PrefixGr);
                    StringBuilder groupingDataOuterQuery = GetOuterGroupBySborkaString(selectedColumns, dbType);

                    StringBuilder outerQuery = GetSelectedColumnsOuterSelectSborka(selectedColumns);

                    string mainQuery = String.Format(" {0} {1}", topQuery, String.Format(QueryUtils.GetMainGroupedQuery(), selectedColumnsList, dbName, whereQuery, groupingData, groupingDataOuterQuery, outerQuery));
                    string queryForExel = mainQuery;

                    var reportBuilder = new StringBuilder();
                    reportBuilder.Append(mainQuery + String.Format("{0}", orderByColumns));
                  
                    reportBuilder.Append(QueryUtils.DropTempTableQuery);
                    allSelect = reportBuilder.ToString();

                    queryForExelReport = queryForExel;

                    allQuerySummary = queryForExel;// " SELECT " + GetSelectedColumns(selectedColumns, false, true) + String.Format("FROM {0}", dbName) + whereQuery + groupBy;
                    
                }
                else//порожний
                {
                  //  const string rowNumberSelect = " Row_number() OVER (ORDER BY (SELECT 1)) AS  ID,";
                    StringBuilder groupedColumn = GetGruzhonDbSelectedColumns(selectedColumns, false);
                    
                    string select = String.Format("{0} FROM {1} ", groupedColumn, dbName);
                    const string selectFormat = "SELECT  {0} {1}";
                    string innerSelect = String.Format(selectFormat, "",  @select);

                    allSelect = innerSelect + whereQuery;
                    ////Group by columns
                    groupingData = GetGroupByString(selectedColumns, viewTypeReport,"", VagonSourceTypeParamEnum.Porozhn);
                    if (groupingData.Length > 0)
                        groupBy.Append(String.Format(" Group By  {0}", groupingData));

                    allSelect += groupBy;

                    var reportBuilder = new StringBuilder();
                   // reportBuilder.Append("WITH OrderedReport AS");
                  //  reportBuilder.Append("(");
                    reportBuilder.Append(allSelect);
                  //  reportBuilder.Append(")");

                 //   string outerSelect = String.Format("SELECT {0} FROM OrderedReport", GetGruzhonDbSelectedColumns(selectedColumns, true));

                    // Order By
                    string orderByColumns = null;
                    StringBuilder orderBy = GetOrderColumns(selectedColumns);
                    if (groupingData.Length > 0 && orderBy.Length > 0)
                        orderByColumns = String.Format(" Order By {0}", orderBy);

                  //  outerSelect += "{0}";
                   // outerSelect += orderByColumns;
                  //  reportBuilder.Append(outerSelect);
                    allSelect = reportBuilder.ToString();

                    queryForExelReport = reportBuilder.ToString().Replace("{0}", "");

                    allQuerySummary = String.Format(selectFormat, "", @select) + whereQuery + groupBy;

                    //summary query
                    var totalReportBuilder = new StringBuilder();
                    totalReportBuilder.Append("WITH totalQuery AS");
                    totalReportBuilder.Append("(");
                    totalReportBuilder.Append(allQuerySummary);
                    totalReportBuilder.Append(")");
                    totalReportBuilder.Append("SELECT  Count(*) as TotalRowsCount from totalQuery");
                    allQuerySummary = totalReportBuilder.ToString();
                }
            }
            else // в виде накладных
            {
                string selectedListColumns = GetListColumnsForNakladniy(selectedColumns, viewTypeReport).ToString();

                string select = String.Format("SELECT ROW_NUMBER() OVER(ORDER BY [Номер вагона] DESC) AS ID, {0} FROM {1} ", 
                    selectedListColumns, dbName);

                allSelect = select + whereQuery;
              
              //  string outerSelect = String.Format("SELECT {0}, {1}  FROM OrderedReport ", "ID",
                 //   GetListColumns(selectedColumns, viewTypeReport, SelectType.Outer));
                
                //var reportBuilder = new StringBuilder();
                //reportBuilder.Append("WITH OrderedReport AS");
                //reportBuilder.Append("(");
                //reportBuilder.Append(allSelect);
                //reportBuilder.Append(")");

              //  outerSelect += "{0}";

                allQuerySummary = "";//.ToString().Replace(QueryUtils.TOPCountQuery, "") +
                //                  String.Format("SELECT Count(*) as TotalRowsCount FROM {0} ", "OrderedReport ");
              //  reportBuilder.Append(outerSelect);
                queryForExelReport = "";//.Replace(QueryUtils.TOPCountQuery, "");

               // allSelect = reportBuilder.ToString();
            }

            
            return allSelect;
        }


        private static ViewTypeReport GetTypeReport(IEnumerable<SelectedColumnsViewModel> selectedColumns)
        {
            var typeNakl = ViewTypeReport.Svodnaya;
            foreach (SelectedColumnsViewModel column in selectedColumns)
            {
                if (column.id == (int)ColumnsMapping.NumberVagon || column.id == (int)ColumnsMapping.NumberContainer ||
                       column.id == (int)ColumnsMapping.NumberDoc || column.id == (int)ColumnsMapping.DateSending)
                    {
                        typeNakl = ViewTypeReport.Nakladnaya;
                        break;
                    }
            }
            return typeNakl;
        }

        private static StringBuilder GetListColumnsForNakladniy(IEnumerable<SelectedColumnsViewModel> selectedColumns,
            ViewTypeReport viewTypeNakl = ViewTypeReport.None)
        {
            bool existTonnKm = false;
            var columnsQuery = new StringBuilder();

            if (selectedColumns.Any(column => column.id == (int) ColumnsMapping.TonnKm))
            {
                existTonnKm = true;
            }
            
            foreach (SelectedColumnsViewModel column in selectedColumns)
            {
                ColumnsMapping columnName;
                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                        columnsQuery.Append(ColumnsMapping.CompanySendingCode.GetStringValue() + " as " + ColumnsMapping.ZdCodePluskpo.GetStringValue());
                        columnsQuery.Append(",");
                        continue;
                    }

                    if (columnName == ColumnsMapping.QuantityVagon )
                    {
                        //bool columnExistsNumberWagonInSelect = false;
                        //foreach (SelectedColumnsViewModel selected in selectedColumns)
                        //{
                        //    ColumnsMapping columnCheck;
                        //    if (Enum.TryParse(selected.id.ToString(CultureInfo.InvariantCulture), true, out columnCheck))
                        //    {
                        //        if (columnCheck == ColumnsMapping.NumberVagon)
                        //        {
                        //            columnExistsNumberWagonInSelect = true;
                        //            break;                                    
                        //        }
                        //    }
                        //}

                        //if (!columnExistsNumberWagonInSelect)
                        //{
                        //    columnsQuery.Append(ColumnsMapping.NumberVagon.GetStringValue());
                        //    columnsQuery.Append(",");
                        //}
                        continue;
                    }
                    else if (columnName == ColumnsMapping.QuantityContainer )
                    {
                        bool columnExistsNumberContainerInSelect = false;
                        foreach (SelectedColumnsViewModel selected in selectedColumns)
                        {
                            ColumnsMapping columnCheck;
                            if (Enum.TryParse(selected.id.ToString(CultureInfo.InvariantCulture), true, out columnCheck))
                            {
                                if (columnCheck == ColumnsMapping.NumberContainer)
                                {
                                      columnExistsNumberContainerInSelect = true;
                                    break;
                                }
                            }
                        }
                    
                        if (!columnExistsNumberContainerInSelect)
                        {
                            columnsQuery.Append(ColumnsMapping.NumberContainer.GetStringValue());
                            columnsQuery.Append(",");
                        }
                    }
                   // else if (selectType == SelectType.Outer && columnName == ColumnsMapping.QuantityVagon || columnName == ColumnsMapping.QuantityContainer)
                    //{
                    //    if ( columnName == ColumnsMapping.QuantityVagon )
                    //    {
                    //        columnsQuery.Append(" case when [Номер вагона] <> '00000000000' then 1 else 0 end as [Количество вагонов]");
                    //        columnsQuery.Append(",");
                    //    }

                    //    if ( columnName == ColumnsMapping.QuantityContainer )
                    //    {
                    //        columnsQuery.Append(" case when [Номер контейнера] <> '00000000000' then 1 else 0 end as [Количество контейнеров]");
                    //        columnsQuery.Append(",");
                    //    }
                    //}
                    else if (existTonnKm &&
                        (columnName == ColumnsMapping.VolumeTransportation ||
                         columnName == ColumnsMapping.DestinationTariff))
                        continue;
                    //else if (selectType == SelectType.Outer && columnName == ColumnsMapping.TonnKm)
                    //{
                    //    if (viewTypeNakl == ViewTypeReport.Nakladnaya)
                    //    {
                    //        columnsQuery.Append(String.Format("CONVERT(bigint,{0}) * CONVERT(bigint,{1}) as {2}",
                    //            ColumnsMapping.VolumeTransportation.GetStringValue(),
                    //            ColumnsMapping.DestinationTariff.GetStringValue(),
                    //            ColumnsMapping.TonnKm.GetStringValue()));
                    //    }
                    //}
                    else if (columnName == ColumnsMapping.DateSending)
                    {
                        columnsQuery.Append(ColumnsMapping.DateSending.GetStringValue());
                        columnsQuery.Append(",");
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation)
                    {
                        columnsQuery.Append(String.Format(" CAST ( MONTH( {0} ) as nvarchar(20)) + '.' + CAST ( YEAR( {0} ) as nvarchar(20)) as [{1}]",
                        ColumnsMapping.DateSending.GetStringValue(),
                        ColumnsMapping.MonthTransportation.GetStringValue()));
                        columnsQuery.Append(",");
                    }
                    else if (columnName == ColumnsMapping.CODE_NAME_STATION_SENDING_RUS_SNG)
                    {
                        columnsQuery.Append(ColumnsMapping.StationSendingCodeRUS.GetStringValue());
                        columnsQuery.Append(",");
                        columnsQuery.Append(ColumnsMapping.StationSendingSNG.GetStringValue());
                        columnsQuery.Append(",");
                    }
                    else if (columnName == ColumnsMapping.CODE_NAME_STATION_DELIVERING_RUS_SNG)
                    {
                        columnsQuery.Append(ColumnsMapping.StationDeliveringCodeRUS.GetStringValue());
                        columnsQuery.Append(",");
                        columnsQuery.Append(ColumnsMapping.StationDeliveringCodeSNG.GetStringValue());
                        columnsQuery.Append(",");
                    }
                    else if (columnName == ColumnsMapping.TonnKm)
                    {
                        if (existTonnKm)
                        {
                            columnsQuery.Append(ColumnsMapping.VolumeTransportation.GetStringValue());
                            columnsQuery.Append(",");
                            columnsQuery.Append(ColumnsMapping.DestinationTariff.GetStringValue());
                            columnsQuery.Append(",");
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        columnsQuery.Append(columnName.GetStringValue());
                        columnsQuery.Append(",");
                    }
                }
            }

            return columnsQuery.Remove(columnsQuery.Length - 1, 1);
        }

        private static bool ExistsColumn(IEnumerable<SelectedColumnsViewModel> selectedColumns,
            ColumnsMapping columnsMapping)
        {
            bool existVolumeTransportation = false;
            foreach (SelectedColumnsViewModel column in selectedColumns)
            {
                ColumnsMapping columnName;

                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == columnsMapping)
                    {
                        existVolumeTransportation = true;
                        break;
                    }
                }
            }
            return existVolumeTransportation;
        }

        private static StringBuilder GetSelectedColumns(IEnumerable<SelectedColumnsViewModel> selectedColumns,
             bool isOuterSelect = false, string prefix = "", bool groupingQuery = false,
            bool isChangeMonthTransportation = false)
        {
            var columnsQuery = new StringBuilder();
           
            foreach (SelectedColumnsViewModel column in selectedColumns)
            {
                ColumnsMapping columnName;

                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                        columnsQuery.Append(ColumnsMapping.CompanySendingCode.GetStringValue() + " as " + ColumnsMapping.ZdCodePluskpo.GetStringValue());
                        columnsQuery.Append(",");
                        continue;
                    }
                    if (groupingQuery && columnName == ColumnsMapping.CargoName)
                    {
                        columnsQuery.Append(
                            String.Format("Case When fb.Count > 1 Then 'Сборный груз' Else gr.{0} end {0}",
                                ColumnsMapping.CargoName.GetStringValue()));
                    }
                    else if (groupingQuery && columnName == ColumnsMapping.VolumeTransportation)
                    {
                        columnsQuery.Append(String.Format(" SUM ( Cast(gr.{0} as bigint)) as {0}", ColumnsMapping.VolumeTransportation.GetStringValue()));
                    }
                    else if (groupingQuery && columnName == ColumnsMapping.PaymentSum)
                    {
                        columnsQuery.Append(String.Format(" SUM ( Cast(gr.{0} as bigint)) as {0}", ColumnsMapping.PaymentSum.GetStringValue()));
                    }
                    else if (groupingQuery && columnName == ColumnsMapping.QuantityContainer)//columnName == ColumnsMapping.QuantityVagon || 
                    {
                        columnsQuery.Append(string.Format(" Count( case when gr.{0} <> '00000000000' then gr.{0} end) as {1}", ColumnsMapping.NumberContainer.GetStringValue(), ColumnsMapping.QuantityContainer.GetStringValue()));

                    }
                    else if (groupingQuery && columnName == ColumnsMapping.QuantityVagon) 
                    {
                        columnsQuery.Append(string.Format(" Count( case when gr.{0} <> '00000000' then gr.{0} end) as {1}", ColumnsMapping.NumberVagon.GetStringValue(), ColumnsMapping.QuantityVagon.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation && !isChangeMonthTransportation)
                    {
                        if (isOuterSelect)
                            columnsQuery.Append(String.Format("[{0}]",
                                ColumnsMapping.MonthTransportation.GetStringValue()));
                        else
                            columnsQuery.Append(String.Format("CAST ( MONTH( {0} ) as nvarchar(20)) + '.' + CAST ( YEAR( {0} ) as nvarchar(20)) as [{1}]",
                               prefix + ColumnsMapping.DateSending.GetStringValue(),
                               ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation && isChangeMonthTransportation)
                    {
                        columnsQuery.Append(String.Format("[{0}] ", ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.TonnKm)
                    {
                        if (isOuterSelect)
                            columnsQuery.Append(prefix + String.Format("{0}", ColumnsMapping.TonnKm.GetStringValue()));
                        else
                            columnsQuery.Append(
                                String.Format("SUM(CONVERT(bigint,{0})) * SUM(CONVERT(bigint,{1})) as {2}",
                                   prefix + ColumnsMapping.VolumeTransportation.GetStringValue(),
                                   prefix + ColumnsMapping.DestinationTariff.GetStringValue(),
                                   ColumnsMapping.TonnKm.GetStringValue()));
                    }
                    else
                    {
                        columnsQuery.Append(prefix + columnName.GetStringValue());
                    }
                    columnsQuery.Append(",");
                }
            }

            return columnsQuery.Length > 0 ? columnsQuery.Remove(columnsQuery.Length - 1, 1) : columnsQuery;
        }

        private static StringBuilder GetInnerSelectedColumnsSborka(IEnumerable<SelectedColumnsViewModel> selectedColumns,
             string prefix = ""
          )
        {
            var columnsQuery = new StringBuilder();

            foreach (SelectedColumnsViewModel column in selectedColumns)
            {
                ColumnsMapping columnName;

                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                        columnsQuery.Append(ColumnsMapping.CompanySendingCode.GetStringValue() + " as " + ColumnsMapping.ZdCodePluskpo.GetStringValue());
                        columnsQuery.Append(",");
                        continue;
                    }
                    if (columnName == ColumnsMapping.CargoName)
                    {
                        continue;//в шаблоек
                    }
                    else if (columnName == ColumnsMapping.VolumeTransportation)
                    {
                        columnsQuery.Append(String.Format(" SUM ( Cast(gr.{0} as bigint)) as {0}", ColumnsMapping.VolumeTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.PaymentSum)
                    {
                        columnsQuery.Append(String.Format(" SUM ( Cast(gr.{0} as bigint)) as {0}", ColumnsMapping.PaymentSum.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.QuantityContainer)//columnName == ColumnsMapping.QuantityVagon || 
                    {
                      //  columnsQuery.Append(string.Format(" Count( case when gr.{0} <> '00000000000' then gr.{0} end) as {1}", ColumnsMapping.NumberContainer.GetStringValue(), ColumnsMapping.QuantityContainer.GetStringValue()));
                        continue;
                    }
                    else if (columnName == ColumnsMapping.QuantityVagon)
                    {
                        columnsQuery.Append(string.Format(" Count( case when gr.{0} <> '00000000' then gr.{0} end) as {1}", ColumnsMapping.NumberVagon.GetStringValue(), ColumnsMapping.QuantityVagon.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation)
                    {
                            columnsQuery.Append(String.Format("CAST ( MONTH( {0} ) as nvarchar(20)) + '.' + CAST ( YEAR( {0} ) as nvarchar(20)) as [{1}]",
                               prefix + ColumnsMapping.DateSending.GetStringValue(),
                               ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation)
                    {
                        columnsQuery.Append(String.Format("[{0}] ", ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.TonnKm)
                    {
                        columnsQuery.Append(
                            String.Format("SUM(CONVERT(bigint,{0})) * SUM(CONVERT(bigint,{1})) as {2}",
                                prefix + ColumnsMapping.VolumeTransportation.GetStringValue(),
                                prefix + ColumnsMapping.DestinationTariff.GetStringValue(),
                                ColumnsMapping.TonnKm.GetStringValue()));
                    }
                    else
                    {
                        columnsQuery.Append(prefix + columnName.GetStringValue());
                    }
                    columnsQuery.Append(",");
                }
            }

            return columnsQuery.Length > 0 ? columnsQuery.Remove(columnsQuery.Length - 1, 1) : columnsQuery;
        }

        private static StringBuilder GetSelectedColumnsOuterSelectSborka(IEnumerable<SelectedColumnsViewModel> selectedColumns)
        {
            var columnsQuery = new StringBuilder();

            foreach (SelectedColumnsViewModel column in selectedColumns)
            {
                ColumnsMapping columnName;

                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                        columnsQuery.Append(ColumnsMapping.CompanySendingCode.GetStringValue() + " as " + ColumnsMapping.ZdCodePluskpo.GetStringValue());
                        columnsQuery.Append(",");
                        continue;
                    }
                    if (columnName == ColumnsMapping.CargoName)
                    {
                        columnsQuery.Append(ColumnsMapping.CargoName.GetStringValue());
                    }
                    else if (columnName == ColumnsMapping.VolumeTransportation)
                    {
                        columnsQuery.Append(String.Format(" SUM ( Cast({0} as bigint)) as {0}", ColumnsMapping.VolumeTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.PaymentSum)
                    {
                        columnsQuery.Append(String.Format(" SUM ( Cast({0} as bigint)) as {0}", ColumnsMapping.PaymentSum.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.QuantityContainer)//columnName == ColumnsMapping.QuantityVagon || 
                    {
                        //нужны ли контейнеры , надо спросить
                      //  columnsQuery.Append(string.Format(" Count( case when {0} <> '00000000000' then gr.{0} end) as {1}", ColumnsMapping.NumberContainer.GetStringValue(), ColumnsMapping.QuantityContainer.GetStringValue()));
                        columnsQuery.Append(string.Format(" Count(*) as {0}", ColumnsMapping.QuantityContainer.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.QuantityVagon)
                    {
                        columnsQuery.Append(string.Format(" Count(*) as {0}", ColumnsMapping.QuantityVagon.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation)
                    {
                        columnsQuery.Append(String.Format("[{0}] ", ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.TonnKm)
                    {
                            columnsQuery.Append(
                                String.Format("SUM(CONVERT(bigint,{0})) * SUM(CONVERT(bigint,{1})) as {2}",
                                    ColumnsMapping.VolumeTransportation.GetStringValue(),
                                    ColumnsMapping.DestinationTariff.GetStringValue(),
                                   ColumnsMapping.TonnKm.GetStringValue()));
                    }
                    else
                    {
                        columnsQuery.Append(columnName.GetStringValue());
                    }
                    columnsQuery.Append(",");
                }
            }

            return columnsQuery.Length > 0 ? columnsQuery.Remove(columnsQuery.Length - 1, 1) : columnsQuery;
        }

        private static StringBuilder GetGruzhonDbSelectedColumns(IEnumerable<SelectedColumnsViewModel> selectedColumns, bool isOuterSelect)
        {
            var columnsQuery = new StringBuilder();

            var selectedColumnsViewModels = selectedColumns as SelectedColumnsViewModel[] ?? selectedColumns.ToArray();

            foreach (SelectedColumnsViewModel column in selectedColumnsViewModels)
            {
                ColumnsMapping columnName;

                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                      columnsQuery.Append(ColumnsMapping.CompanySendingCode.GetStringValue() + " as " + ColumnsMapping.ZdCodePluskpo.GetStringValue());
                      columnsQuery.Append(",");
                      continue;
                    }

                    if (columnName == ColumnsMapping.MonthTransportation)
                    {
                        if (isOuterSelect)
                            columnsQuery.Append(String.Format("[{0}]",
                                ColumnsMapping.MonthTransportation.GetStringValue()));
                        else
                            columnsQuery.Append(String.Format("CAST ( MONTH( {0} ) as nvarchar(20)) + '.' + CAST ( YEAR( {0} ) as nvarchar(20)) as [{1}]",
                               ColumnsMapping.DateSending.GetStringValue(),
                               ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else if (columnName == ColumnsMapping.TonnKm)
                    {
                        if (isOuterSelect)
                            columnsQuery.Append(String.Format("{0}", ColumnsMapping.TonnKm.GetStringValue()));
                        else
                            columnsQuery.Append(
                                String.Format("SUM(CONVERT(bigint,{0})) * SUM(CONVERT(bigint,{1})) as {2}",
                                   ColumnsMapping.VolumeTransportation.GetStringValue(),
                                   ColumnsMapping.DestinationTariff.GetStringValue(),
                                   ColumnsMapping.TonnKm.GetStringValue()));
                    }
                    else if (isOuterSelect == false && columnName == ColumnsMapping.QuantityContainer)
                    {
                        columnsQuery.Append(String.Format("Count(*) as {0}", ColumnsMapping.QuantityContainer.GetStringValue())); 
                    }
                    else if (isOuterSelect == false && columnName == ColumnsMapping.QuantityVagon)
                    {
                        columnsQuery.Append(String.Format("Count(*) as {0}", ColumnsMapping.QuantityVagon.GetStringValue()));
                    }
                    else
                    {
                        columnsQuery.Append(columnName.GetStringValue());
                    }
                    columnsQuery.Append(",");
                }
            }

            return columnsQuery.Length > 0 ? columnsQuery.Remove(columnsQuery.Length - 1, 1) : columnsQuery;
        }

        private static StringBuilder GetOrderColumns(IEnumerable<SelectedColumnsViewModel> selectedColumns, string prefix = "")
        {
            var orderColumns = new StringBuilder();
            foreach (SelectedColumnsViewModel column in selectedColumns)
            {
                ColumnsMapping columnName;
                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if(columnName == ColumnsMapping.QuantityContainer || columnName == ColumnsMapping.QuantityVagon)
                        continue;

                    if (columnName == ColumnsMapping.MonthTransportation)
                    {
                        orderColumns.Append(prefix + String.Format("[{0}]", ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else
                    {
                        orderColumns.Append(prefix + columnName.GetStringValue());
                    }
                    orderColumns.Append(",");
                }
            }
            return orderColumns.Length > 0 ? orderColumns.Remove(orderColumns.Length - 1, 1) : new StringBuilder();
        }

        private static StringBuilder GetWhereQuery(IEnumerable<FilterParamstItemViewModel> filterParamstItem, string additionalWhere, bool needPrefix)
        {
            var sbWhere = new StringBuilder();
            bool periodAdded = false;

            string expressionWhere = null;
            string prefix = needPrefix ? QueryUtils.PrefixGr : "";

            foreach (FilterParamstItemViewModel paramstItem in filterParamstItem)
            {
                bool allSelected = paramstItem.cv == -1; //"ВСЕ";

                if (allSelected || paramstItem.filterId == 45) //45 это радио баттон станции отправления - выключатель
                    continue;

                ColumnsMapping filterName;

                if (Enum.TryParse(paramstItem.filterId.ToString(CultureInfo.InvariantCulture), true, out filterName))
                {
                    if (filterName == ColumnsMapping.DateSending && !periodAdded)
                    {
                        DateTime dateBegin;
                        DateTime dateEnd;
                        var enUS = new CultureInfo("en-US");
                        DateTime.TryParseExact(paramstItem.sv[0].name, "dd.MM.yyyy", enUS, DateTimeStyles.None,
                            out dateBegin);

                        DateTime.TryParseExact(paramstItem.sv[1].name, "dd.MM.yyyy", enUS, DateTimeStyles.None,
                            out dateEnd);

                        TimeSpan dateDifferent = dateEnd - dateBegin;
                        int months = dateDifferent.GetMonths();
                        if (dateBegin != DateTime.MinValue || dateEnd != DateTime.MinValue && months >= 0)
                        {
                            string dateBeginQuery = "", dateEndQuery = "";
                            string dateBeginFormated = String.Format("{0:dd.MM.yyyy}", dateBegin);
                            string dateEndFormated = String.Format("{0:dd.MM.yyyy}", dateEnd);

                            if (dateBegin != DateTime.MinValue)
                            {
                               dateBeginQuery = "{0} >= convert(date,'{1}', 104 )";
                            }
                            if (dateEnd != DateTime.MinValue)
                            {
                                dateEndQuery = "{0} <= convert(date,'{1}', 104 )";
                            }

                            if (!String.IsNullOrEmpty(dateBeginQuery) && !String.IsNullOrEmpty(dateEndQuery))
                            {
                                expressionWhere = String.Format(dateBeginQuery, prefix + ColumnsMapping.DateSending.GetStringValue(), dateBeginFormated) + " AND "
                                                                                        + prefix+String.Format(dateEndQuery, ColumnsMapping.DateSending.GetStringValue(), dateEndFormated);
                            }
                            else if(!String.IsNullOrEmpty(dateBeginQuery))
                            {
                                expressionWhere = String.Format(dateBeginQuery, prefix + ColumnsMapping.DateSending.GetStringValue(), dateBeginFormated); 
                            }
                            else
                            {
                                expressionWhere = String.Format(dateEndQuery,
                                     prefix + ColumnsMapping.DateSending.GetStringValue(), dateEndFormated);
                            }
                        }
                        else
                            continue;

                        periodAdded = true;
                    }
                    else if (filterName == ColumnsMapping.CODE_NAME_STATION_SENDING_RUS_SNG)
                    {
                          expressionWhere = String.Format("({0} {1} OR {2} {3})",
                          prefix + ColumnsMapping.StationSendingCodeRUS.GetStringValue(),
                         " IN (" + GetColumnFilterByValue(paramstItem.sv, FilterNameHelper.StationsSendingFilterKey) + ")",
                          prefix + ColumnsMapping.StationSendingCodeSNG.GetStringValue(),
                         " IN (" + GetColumnFilterByValue(paramstItem.sv, FilterNameHelper.StationsSendingFilterKey) + ")");
                     
                    }
                    else if (filterName == ColumnsMapping.CODE_NAME_STATION_DELIVERING_RUS_SNG)
                    {
                        foreach (FilterParamstItemViewModel paramsFilter in filterParamstItem)
                        {
                            if (paramsFilter.filterId == 45)
                            {
                                if (paramsFilter.sv[0].name != SearchStationListVariantsEnum.None.GetStringValue())
                                {
                                    int lengthDigitalCodeSearch = paramsFilter.sv[0].name == SearchStationListVariantsEnum.TwoDigitalCodeStation.GetStringValue() ? 2 : 3;
                                   
                                    expressionWhere = String.Format("({0} {1} OR {2} {3})",
                                    prefix + ColumnsMapping.StationDeliveringCodeRUS.GetStringValue(),
                                   " like (" + GetColumnFilterByValue(paramstItem.sv, true, lengthDigitalCodeSearch) + ")",
                                   prefix + ColumnsMapping.StationDeliveringCodeSNG.GetStringValue(),
                                   " like (" + GetColumnFilterByValue(paramstItem.sv, true, lengthDigitalCodeSearch) + ")");
                                }
                                else
                                {
                                    expressionWhere = String.Format("({0} {1} OR {2} {3})",
                                    prefix + ColumnsMapping.StationDeliveringCodeRUS.GetStringValue(),
                                     " IN (" + GetColumnFilterByValue(paramstItem.sv, true) + ")",
                                    prefix + ColumnsMapping.StationDeliveringCodeSNG.GetStringValue(),
                                     " IN (" + GetColumnFilterByValue(paramstItem.sv, true) + ")");
                                }
                            }
                        }
                    }
                    else if (filterName == ColumnsMapping.WagonType)
                    {
                        expressionWhere = prefix + GetWagonTypesWhereQuery(paramstItem);
                    }
                    else if (filterName == ColumnsMapping.CompanySendingEGRPO)
                    {
                        expressionWhere = GetCompanyFilter(paramstItem.sv, ColumnsMapping.CompanySendingCode.GetStringValue());
                        //  expressionWhere = prefix + ColumnsMapping.CompanySendingCode.GetStringValue() + " LIKE '%" + GetColumnFilterByValue(paramstItem.sv, FilterNameHelper.CompanySendingFilterName, true) + "'";
                    }
                    else if (filterName == ColumnsMapping.CompanyRecipientERPGO)
                    {
                        expressionWhere = GetCompanyFilter(paramstItem.sv, ColumnsMapping.CompanyRecipientCode.GetStringValue());
                       // expressionWhere = prefix + ColumnsMapping.CompanyRecipientCode.GetStringValue() + " IN (" + GetColumnFilterByValue(paramstItem.sv, FilterNameHelper.CompanyRecipientFilterName, true) + ")";
                    }
                    else
                    {
                        expressionWhere = prefix + filterName.GetStringValue() + " IN (" +
                                        GetColumnFilterByValue(paramstItem.sv) +
                                        ")";
                    }


                    sbWhere.Append(expressionWhere);
                    sbWhere.Append(Constants.AND);
                }
            }

            if (additionalWhere.Length > 0)
            {
                sbWhere.Append(additionalWhere);
                sbWhere.Append(Constants.AND);
            }

            if (sbWhere.Length > 0)
                sbWhere.Remove(sbWhere.Length - Constants.AND.Length, Constants.AND.Length);
            return sbWhere;
        }

        private static string GetWagonTypesWhereQuery(FilterParamstItemViewModel paramstItem)
        {
            string expressionWhere = "";
            string wagonTypeExpWhere = null;
            string containerExpWhere = null;
            string delimetersValues = FilterHelper.GetWagonTypeFilterByValue(paramstItem.sv).ToString();
            string[] selectedValues = delimetersValues.Split(',');

            List<string> containerList;
            Http.Get(FilterNameHelper.ContainerVolumeFilterName, out containerList);


            if (containerList == null)
            {
                FilterService.GetFilterListAll(FilterNameHelper.ContainerVolumeFilterName, "", "");
                Http.Get(FilterNameHelper.ContainerVolumeFilterName, out containerList);
                if (containerList == null)
                {
                    return "";
                }
            }
            //выбираем тоннажность
            List<WagonGroupType> listWagonTypes;
            Http.Get(FilterNameHelper.WagonTypeHierarhyListFilterKey, out listWagonTypes);

            if (listWagonTypes == null)
            {
                FilterService.GetFilterListAll(FilterNameHelper.WagonTypeHierarhyListFilterKey, "", "");
                Http.Get(FilterNameHelper.WagonTypeHierarhyListFilterKey, out listWagonTypes);
                if (listWagonTypes == null)
                {
                    return "";
                }
            }

            foreach (var value in selectedValues)
            {
                bool endSearch = false;
                foreach (var containerTypeItem in containerList)
                {
                    if (containerTypeItem.Trim() == value.Trim().Replace("'", ""))
                    {
                        containerExpWhere += value + ",";
                        endSearch = true;
                        break;
                    }
                }
                if (endSearch)
                    continue;

              //  foreach (var wagonTypeItem in listWagonTypes)
              //  {
                  //  if (wagonTypeItem.WagonTypes.ToString() == value.Trim().Replace("'", ""))
                 //   {
                        wagonTypeExpWhere += value + ",";
                  //  }
               // }
            }

            if (!string.IsNullOrEmpty(containerExpWhere))
            {
                expressionWhere = ColumnsMapping.ContainerVolume.GetStringValue() + " IN (" + containerExpWhere.Remove(containerExpWhere.Length-1,1) + ")";
            }
            
            if (!string.IsNullOrEmpty(wagonTypeExpWhere))
            {
                if (!String.IsNullOrEmpty(expressionWhere))
                {
                    expressionWhere += " " + Constants.AND;
                }

                expressionWhere += " " + ColumnsMapping.WagonType.GetStringValue() + " IN (" +
                    wagonTypeExpWhere.Remove(wagonTypeExpWhere.Length-1,1) +
                    ")";
            }
       
            return expressionWhere;
        }

        private static readonly ILog _logger = LogManager.GetLogger(typeof(CalculatorService));

        static readonly HttpCache Http = new HttpCache();
        private static string FindCalculatorFieldCode(string filterName, string selectedValue)
        {
            string foundStringValue = null;
            try
            {
                List<string> list;

                Http.Get(filterName, out list);
                if (list == null)
                    return "";

                string[] selectedValueArr = selectedValue.Split('|');
                string selectedCode = selectedValueArr[0];

                foreach (var item in list)
                {
                    string[] listArr = item.Split('|');
                    string listName = listArr[0];

                    if (listName.Trim() == selectedCode.Trim())
                    {
                        foundStringValue = listArr[0].Trim();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Info(e.StackTrace);
                _logger.Error(e.Message);
            }
          
            return foundStringValue;
        }

        private enum SearchStationListVariantsEnum
        {
            [StringValue("выключено")] 
            None = 0,
            [StringValue("поиск по 2-м цифрам")]
            TwoDigitalCodeStation = 1,
            [StringValue("поиск по 3-м цифрам")]
            ThreeDigitalCodeStation = 2
        }

        private static StringBuilder GetGroupByString(IEnumerable<SelectedColumnsViewModel> selectedColumns,
            ViewTypeReport viewTypeReport, string prefix ="", 
            VagonSourceTypeParamEnum dbType = VagonSourceTypeParamEnum.Gruzhon)
        {
            var groupingColumns = new StringBuilder();

            IList<SelectedColumnsViewModel> selectedColumnsViewModels =
                selectedColumns as IList<SelectedColumnsViewModel> ?? selectedColumns.ToList();
            
            foreach (SelectedColumnsViewModel column in selectedColumnsViewModels)
            {
                ColumnsMapping columnName;
                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.QuantityVagon)
                        continue;
                    if (columnName == ColumnsMapping.QuantityContainer)
                        continue;
                    if (columnName == ColumnsMapping.VolumeTransportation)
                        continue;
                    if (columnName == ColumnsMapping.PaymentSum && dbType == VagonSourceTypeParamEnum.Gruzhon)
                        continue;

                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                        if (selectedColumnsViewModels.Any(r => r.id == (int)ColumnsMapping.CompanySendingCode))
                        {
                            continue;
                        }
                        groupingColumns.Append(ColumnsMapping.CompanySendingCode.GetStringValue());
                        groupingColumns.Append(",");
                        continue;
                    }
                    if (columnName == ColumnsMapping.CargoName && dbType == VagonSourceTypeParamEnum.Gruzhon)
                    {
                        if (viewTypeReport == ViewTypeReport.Svodnaya)
                        {
                            groupingColumns.Append(String.Format("Case When fb.Count > 1 Then 'Сборный груз' Else {0} end", prefix + ColumnsMapping.CargoName.GetStringValue()));
                        }
                    }
                    else if (columnName == ColumnsMapping.TonnKm && viewTypeReport == ViewTypeReport.Svodnaya)
                    {
                        groupingColumns.Append(ColumnsMapping.VolumeTransportation.GetStringValue());
                        groupingColumns.Append(",");
                        groupingColumns.Append(ColumnsMapping.DestinationTariff.GetStringValue());
                        
                        //if (viewTypeReport == ViewTypeReport.Svodnaya && existTonnKm &&
                        //    (columnName == ColumnsMapping.VolumeTransportation ||
                        //     columnName == ColumnsMapping.DestinationTariff))
                        //{
                        //    continue;
                        //}
                    } 
                    else
                    if (columnName == ColumnsMapping.QuantityContainer)
                    {
                        if (!ExistsColumn(selectedColumnsViewModels, ColumnsMapping.NumberContainer))
                            groupingColumns.Append(ColumnsMapping.NumberContainer.GetStringValue());
                        else
                        {
                            continue;
                        }
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation)
                    {
                        groupingColumns.Append(String.Format("CAST ( MONTH( {0} ) as nvarchar(20)) + '.' + CAST ( YEAR( {0} ) as nvarchar(20)) ",
                          prefix + ColumnsMapping.DateSending.GetStringValue()));
                    }
                    else
                    {
                        groupingColumns.Append(prefix + columnName.GetStringValue());
                    }
                    groupingColumns.Append(",");
                }
            }
            return groupingColumns.Length > 0
                ? groupingColumns.Remove(groupingColumns.Length - 1, 1)
                : new StringBuilder();
        }


        private static StringBuilder GetInnerGroupSborkaByString(IEnumerable<SelectedColumnsViewModel> selectedColumns,
         string prefix = "")
        {
            var groupingColumns = new StringBuilder();

            IList<SelectedColumnsViewModel> selectedColumnsViewModels =
                selectedColumns as IList<SelectedColumnsViewModel> ?? selectedColumns.ToList();

            foreach (SelectedColumnsViewModel column in selectedColumnsViewModels)
            {
                ColumnsMapping columnName;
                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.QuantityVagon)
                        continue;
                    if (columnName == ColumnsMapping.QuantityContainer)
                        continue;
                    if (columnName == ColumnsMapping.VolumeTransportation)
                        continue;
                    if (columnName == ColumnsMapping.PaymentSum)
                        continue;

                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                        if (selectedColumnsViewModels.Any(r => r.id == (int)ColumnsMapping.CompanySendingCode))
                        {
                            continue;
                        }
                        groupingColumns.Append(ColumnsMapping.CompanySendingCode.GetStringValue());
                        groupingColumns.Append(",");
                        continue;
                    }
                    if (columnName == ColumnsMapping.CargoName)
                    {
                        continue;//в шаблоне есть
                    }
                    else if (columnName == ColumnsMapping.TonnKm)
                    {
                        groupingColumns.Append(ColumnsMapping.VolumeTransportation.GetStringValue());
                        groupingColumns.Append(",");
                        groupingColumns.Append(ColumnsMapping.DestinationTariff.GetStringValue());
                        
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation)
                        {
                            groupingColumns.Append(String.Format("CAST ( MONTH( {0} ) as nvarchar(20)) + '.' + CAST ( YEAR( {0} ) as nvarchar(20)) ",
                              prefix + ColumnsMapping.DateSending.GetStringValue()));
                        }
                        else
                        {
                            groupingColumns.Append(prefix + columnName.GetStringValue());
                        }
                    groupingColumns.Append(",");
                }
            }
            return groupingColumns.Length > 0
                ? groupingColumns.Remove(groupingColumns.Length - 1, 1)
                : new StringBuilder();
        }

        private static StringBuilder GetOuterGroupBySborkaString(IEnumerable<SelectedColumnsViewModel> selectedColumns,
            VagonSourceTypeParamEnum dbType = VagonSourceTypeParamEnum.Gruzhon
           )
        {
            var groupingColumns = new StringBuilder();

            IList<SelectedColumnsViewModel> selectedColumnsViewModels =
                selectedColumns as IList<SelectedColumnsViewModel> ?? selectedColumns.ToList();

            foreach (SelectedColumnsViewModel column in selectedColumnsViewModels)
            {
                ColumnsMapping columnName;
                if (Enum.TryParse(column.id.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    if (columnName == ColumnsMapping.QuantityVagon)
                        continue;
                    if (columnName == ColumnsMapping.QuantityContainer)
                        continue;
                    if (columnName == ColumnsMapping.VolumeTransportation)
                        continue;
                    if (columnName == ColumnsMapping.PaymentSum && dbType == VagonSourceTypeParamEnum.Gruzhon)
                        continue;

                    if (columnName == ColumnsMapping.ZdCodePluskpo)
                    {
                        if (selectedColumnsViewModels.Any(r => r.id == (int)ColumnsMapping.CompanySendingCode))
                        {
                            continue;
                        }
                        groupingColumns.Append(ColumnsMapping.CompanySendingCode.GetStringValue());
                    } 
                    else if (columnName == ColumnsMapping.CargoName)
                    {
                       groupingColumns.Append(ColumnsMapping.CargoName.GetStringValue());
                    }
                    else if (columnName == ColumnsMapping.MonthTransportation)
                    {
                       groupingColumns.Append(String.Format("[{0}] ", ColumnsMapping.MonthTransportation.GetStringValue()));
                    }
                    else
                    {
                         groupingColumns.Append(columnName.GetStringValue());
                    }
                    groupingColumns.Append(",");
                }
            }
            return groupingColumns.Length > 0
                ? groupingColumns.Remove(groupingColumns.Length - 1, 1)
                : new StringBuilder();
        }

        private static StringBuilder GetColumnFilterByValue(IEnumerable<Filter> filtesList, bool neededCode = false, int startLengthCode = 0)
        {
            var sb = new StringBuilder();

            foreach (Filter filter in filtesList)
            {
                string[] values = filter.name.Split('|');
                if (neededCode == false)
                {
                     sb.Append(String.Format("'{0}',",
                     values.Length > 1 ? values[1].Trim() : filter.name));
                }
                else
                {
                    if (startLengthCode > 0)
                    {
                        sb.Append(String.Format("'{0}%',", values.Length > 0 ? values[0].Trim().Substring(0, startLengthCode) : filter.name));
                    }
                    else
                    {
                        sb.Append(String.Format("'{0}',", values[0].Trim())); 
                    }
                }
            }
            return sb.Remove(sb.Length - 1, 1);
        }

        public static string GetCompanyFilter(IEnumerable<Filter> filtesList, string filterKey)
        {
            string codes = "";
            string devider = " OR ";
            foreach (Filter filter in filtesList)
            {
                codes += filterKey + " LIKE '%" + filter.name + "'";
                codes += devider;
            }
            codes = codes.Length > 0 ? codes.Remove(codes.Length - devider.Length, devider.Length) : "";
            
            return codes;
        }

        private static StringBuilder GetColumnFilterByValue(IEnumerable<Filter> filtesList, string filterName)
        {
            var sb = new StringBuilder();
           
               foreach (Filter filter in filtesList)
               {
                   string code = String.Format("'{0}',", FindCalculatorFieldCode(filterName, filter.name));
                   sb.Append(code);
               }
               return sb.Length > 0 ? sb.Remove(sb.Length - 1, 1) : sb;
           return sb;
        }
    }
}