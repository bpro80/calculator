
using System;

namespace Utils
{
    public class QueryUtils
    {
        public static string GetGroupedFabricatedCargoQuery()
        {
            const string query = "CREATE TABLE #FabricatedCargoTbl(" +
                                 "[Номер документа] VARCHAR(20) ," +
                                 "[Дата отправления] VARCHAR(20)," +
                                 "[Номер вагона] VARCHAR(20)," +
                                 "[Номер контейнера] VARCHAR(20) " +
                                 ")" +
                                 "insert into #FabricatedCargoTbl " +
                                 "select  [Номер документа], [Дата отправления],  [Номер вагона], [Номер контейнера] " +
                                 "FROM {0} " +
                                 "{1}" +
                                 "GROUP BY [Номер документа], " +
                                 "[Дата отправления], " +
                                 "[Номер вагона]," +
                                 "[Номер контейнера] "+
                                 "  HAVING COUNT(*)> 1";
            return query;
        }
        //  " Count( distinct case when gr.[Номер вагона] <> '00000000000' then gr.[Номер вагона] end) as [Количество вагонов], " +
        //----  SUM( Case When fb.Count > 1 Then fb.[Объем перевозок (тн)] ELSE gr.[Объем перевозок (тн)] END) as [Объем перевозок (тн)], " +
        public static string GetMainGroupedQuery()
        {
            const string query = "SELECT   {5} from (" +
                                 " SELECT {0} " +//Row_number() OVER (ORDER BY (SELECT 1)) AS  ID
                                // ", Count(*) over() as CountRows" +
                                 " , Case When NOT fb.[Дата отправления] is  null then 'Сборный груз' else  gr.[Наименование груза] end as [Наименование груза]" +
                                 " FROM {1} gr" +
                                 " LEFT JOIN #FabricatedCargoTbl fb ON " +
                                 " fb.[Номер документа] = gr.[Номер документа] AND" +
                                 " fb.[Дата отправления] = gr.[Дата отправления] AND" +
                                 " fb.[Номер вагона] = gr.[Номер вагона] AND" +
                                 " fb.[Номер контейнера] = gr.[Номер контейнера]" +
                /*Where*/ " {2} " +
                                 " GROUP BY {3}" +
                                 " , Case When NOT fb.[Дата отправления] is  null then 'Сборный груз' else  gr.[Наименование груза] end , " +
                                 "  gr. [Номер документа], gr.[Дата отправления], gr.[Номер вагона],  gr.[Номер контейнера]" +
                //Order By
                //" {4} " +
                                 " ) as t" +
                                 "  group by  {4}";
            return query;
        }

        public static string Prefix = "t.";
        public static string PrefixGr = "gr.";
        public static string DropTempTableQuery = " drop table #FabricatedCargoTbl";
        public static string TOPCountQuery = "TOP 50";
    }
}
