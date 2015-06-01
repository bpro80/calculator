using System.Collections.Generic;
using System.Data;
using DA.Entities;
using DA.Filters;

namespace BO.Implementation.Calculator
{  
    public class CalculatorManager
    {
        public static List<string> GetEntities(string columnName, string columnName2 = "", string sWhere = "", string orderBy = "", string dbname = null)
        {
            return EntityAdapter.GetEntitiesByID(dbname, columnName, columnName2, sWhere, orderBy);
        }
        
        public static List<FiltersTypes> GetFiltersTypesSettings()
        {
           return FiltersAdapter.GetFiltersTypesSettings();
        }
        
    }
}