using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;

namespace Common.Api
{
    public class ExcelWorker
    {
        private const string SheetName = "Лист1";

        public void DoExcelAnalizeReport(string filePath, DataTable dataTable)
        {
            using (var document = SpreadsheetDocument.Open(filePath, true))
            {
                Sheet sheet;
                try
                {
                    sheet =
                        document.WorkbookPart.Workbook.GetFirstChild<Sheets>()
                            .Elements<Sheet>()
                            .SingleOrDefault(s => s.Name == SheetName);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        String.Format("Возможно в документе существует два листа с названием \"{0}\"!\n", SheetName), ex);
                }

                if (sheet == null)
                {
                    throw new Exception(String.Format("В шаблоне не найден \"{0}\"!\n", SheetName));
                }

                var worksheetPart = (WorksheetPart) document.WorkbookPart.GetPartById(sheet.Id.Value);
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                var rowsForRemove = new List<Row>();
                var fields = new List<Field>();
                foreach (var row in worksheetPart.Worksheet.GetFirstChild<SheetData>().Elements<Row>())
                {
                    var celsForRemove = new List<Cell>();
                    foreach (var cell in row.Descendants<Cell>())
                    {
                        if (cell == null)
                        {
                            continue;
                        }
                        
                        var value = GetCellValue(cell, document.WorkbookPart);
                        cell.CellValue =  new CellValue("2342");

                        #region MyRegion

                        //if (value.IndexOf("DataField:", StringComparison.Ordinal) != -1)
                        //{
                        //    if (!firsIndexFlag)
                        //    {
                        //        firsIndexFlag = true;
                        //        rowTemplate = row;
                        //    }
                        //    fields.Add(new Field(Convert.ToUInt32(Regex.Replace(cell.CellReference.Value, @"[^\d]+", ""))
                        //        , new string(cell.CellReference.Value.ToCharArray().Where(p => !char.IsDigit(p)).ToArray())
                        //        , value.Replace("DataField:", "")));

                        //}

                        //if (value.IndexOf("Label:", StringComparison.Ordinal) != -1 && rowTemplate == null)
                        //{
                        //    var labelName = value.Replace("Label:", "").Trim();
                        //    if (!hashtable.ContainsKey(labelName))
                        //    {
                        //        throw new Exception(String.Format("Нет такого лэйбла \"{0}\"", labelName));
                        //    }
                        //    cell.CellValue = new CellValue(hashtable[labelName].ToString());
                        //    cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        //}

                        //if (rowTemplate == null || row.RowIndex <= rowTemplate.RowIndex || String.IsNullOrWhiteSpace(value))
                        //{
                        //    continue;
                        //}
                        //var item = footer.SingleOrDefault(p => p._Row.RowIndex == row.RowIndex);
                        //if (item == null)
                        //{
                        //    footer.Add(new Footer(row, cell, value.IndexOf("Label:", StringComparison.Ordinal) != -1 ? hashtable[value.Replace("Label:", "").Trim()].ToString() : value));
                        //}
                        //else
                        //{
                        //    item.AddMoreCell(cell, value.IndexOf("Label:", StringComparison.Ordinal) != -1 ? hashtable[value.Replace("Label:", "").Trim()].ToString() : value);
                        //}
                        //celsForRemove.Add(cell);

                        #endregion
                    }

                    //    foreach (var cell in celsForRemove)
                    //    {
                    //        cell.Remove();
                    //    }

                    //    if (rowTemplate != null && row.RowIndex != rowTemplate.RowIndex)
                    //    {
                    //        rowsForRemove.Add(row);
                    //    }
                    //}

                    //if (rowTemplate == null || rowTemplate.RowIndex == null || rowTemplate.RowIndex < 0)
                    //{
                    //    throw new Exception("Не удалось найти ни одного поля, для заполнения!");
                    //}

                    //foreach (var row in rowsForRemove)
                    //{
                    //    row.Remove();
                    //}

                    //var index = rowTemplate.RowIndex;
                    //foreach (var row in from System.Data.DataRow item in dataTable.Rows select CreateRow(rowTemplate, index, item, fields))
                    //{
                    //    sheetData.InsertBefore(row, rowTemplate);
                    //    index++;
                    //}

                    //foreach (var newRow in footer.Select(item => CreateLabel(item, (UInt32)dataTable.Rows.Count)))
                    //{
                    //    sheetData.InsertBefore(newRow, rowTemplate);
                    //}

                    //rowTemplate.Remove();
                }
            }
        }

        private string GetCellValue(Cell cell, WorkbookPart wbPart)
        {
            var value = cell.InnerText;

            if (cell.DataType == null)
            {
                return value;
            }
            switch (cell.DataType.Value)
            {
                case CellValues.SharedString:

                    var stringTable = wbPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();

                    if (stringTable != null)
                    {
                        value = stringTable.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                    }
                    break;
            }

            return value;
        }
    }
}