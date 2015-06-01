using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Common.Api;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using log4net;

namespace ExcelProcessor
{
        /// <summary>
        /// Создание Excel файла
        /// </summary>
        public class Worker
        {
            public Worker(string siteFolder, string newfileName)
            {
                _siteDirectory = siteFolder;
                _newfileName = newfileName;
            }
            private  string _siteDirectory;
            private string _newfileName;
            private string _newfilenamePath;

            public string GetNewFileNamePath
            {
                get { return _newfilenamePath; }
            }
            /// <summary>
            /// путь к папке с шаблонами 
            /// </summary>
            private const String TemplateFolder = "Templates\\";

            /// <summary>
            /// имя листа шаблона (с которым мы будем работать) 
            /// </summary>
            private const String SheetName = "Лист1";

            /// <summary>
            /// тип документа
            /// </summary>

            /// <summary>
            /// Папка, для хранения выгруженных файлов
            /// </summary>
            public  String Directory
            {
                get
                {
                    string excelFilesPath = @"";//нет подпапки
                    if (System.IO.Directory.Exists(excelFilesPath) == false)
                    {
                        System.IO.Directory.CreateDirectory(excelFilesPath);
                    }

                    return excelFilesPath;
                }
            }

            public void Export(System.Data.DataTable dataTable, System.Collections.Hashtable hashtable, String templateName)
            {

                var filePath = CreateFile(templateName);

                OpenForRewriteFile(filePath, dataTable);

            //    OpenFile(filePath);
            }

            private String CreateFile(String templateName)
            {
                var templateFelePath = String.Format("{0}{1}{2}", String.Format("{0}{1}", _siteDirectory, TemplateFolder), templateName, Constants.FileTypeExcelReport);
                if (!File.Exists(templateFelePath))
                {
                    throw new Exception(String.Format("Не удалось найти шаблон документа \n\"{0}{1}{2}\"!", TemplateFolder, templateName, Constants.FileTypeExcelReport));
                }

                string newfilename = String.Format("{0}{1}{2} ", _siteDirectory, _newfileName, Constants.FileTypeExcelReport);
                if (File.Exists(newfilename))
                {
                    newfilename = String.Format("{0}_{1}{2}", _newfileName, Regex.Replace((DateTime.Now.ToString(CultureInfo.InvariantCulture)), @"[^a-z0-9]+", ""), Constants.FileTypeExcelReport);
                }
                _newfilenamePath = newfilename;
               
                File.Copy(templateFelePath, newfilename, true);
                return newfilename;
            }

            private void OpenForRewriteFile(String filePath, System.Data.DataTable dataTable)
            {
                Row rowTemplate = null;
                var footer = new List<Footer>();
                var firsIndexFlag = false;
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
                            String.Format("Возможно в документе существует два листа с названием \"{0}\"!\n", SheetName),
                            ex);
                    }

                    if (sheet == null)
                    {
                        throw new Exception(String.Format("В шаблоне не найден \"{0}\"!\n", SheetName));
                    }

                    var worksheetPart = (WorksheetPart) document.WorkbookPart.GetPartById(sheet.Id.Value);
                    var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    var fields = new List<Field>();
                    foreach (var row in worksheetPart.Worksheet.GetFirstChild<SheetData>().Elements<Row>())
                    {
                        foreach (var cell in row.Descendants<Cell>())
                        {
                            if (cell == null)
                            {
                                continue;
                            }

                            var value = GetCellValue(cell, document.WorkbookPart);
                            if (value.IndexOf("DataField:", StringComparison.Ordinal) != -1)
                            {
                                if (!firsIndexFlag)
                                {
                                    firsIndexFlag = true;
                                    rowTemplate = row;
                                }
                                fields.Add(
                                    new Field(Convert.ToUInt32(Regex.Replace(cell.CellReference.Value, @"[^\d]+", ""))
                                        ,
                                        new string(
                                            cell.CellReference.Value.ToCharArray()
                                                .Where(p => !char.IsDigit(p))
                                                .ToArray())
                                        , value.Replace("DataField:", "")));
                            }

                            if (rowTemplate == null || row.RowIndex <= rowTemplate.RowIndex ||
                                String.IsNullOrWhiteSpace(value))
                            {
                                continue;
                            }
                          }
                        }

                        if (rowTemplate == null || rowTemplate.RowIndex == null || rowTemplate.RowIndex < 0)
                        {
                            throw new Exception("Не удалось найти ни одного поля, для заполнения!");
                        }
                        var index = rowTemplate.RowIndex;
                        foreach (
                            var row in
                                from System.Data.DataRow item in dataTable.Rows
                                select CreateRow(rowTemplate, index, item, fields))
                        {
                            sheetData.InsertBefore(row, rowTemplate);
                            index++;
                        }

                        foreach (var newRow in footer.Select(item => CreateLabel(item, (UInt32) dataTable.Rows.Count)))
                        {
                            sheetData.InsertBefore(newRow, rowTemplate);
                        }

                        rowTemplate.Remove();
                    }
                }
            

            private Row CreateLabel(Footer item, uint count)
            {
                var row = item._Row;
                row.RowIndex = new UInt32Value(item._Row.RowIndex + (count - 1));
                foreach (var cell in item.Cells)
                {
                    cell._Cell.CellReference = new StringValue(cell._Cell.CellReference.Value.Replace(Regex.Replace(cell._Cell.CellReference.Value, @"[^\d]+", ""), row.RowIndex.ToString()));
                    cell._Cell.CellValue = new CellValue(cell.Value);
                    cell._Cell.DataType = new EnumValue<CellValues>(CellValues.String);
                    row.Append(cell._Cell);
                }
                return row;
            }
            private static readonly ILog _logger = LogManager.GetLogger(typeof(Worker));
            string CBC_Format="=ЕСЛИ(X{0}=\"вне аренды\";W{0};X{0})";
            private Row CreateRow(Row rowTemplate, uint index, System.Data.DataRow item, List<Field> fields)
            {
                var newRow = (Row)rowTemplate.Clone();
                newRow.RowIndex = new UInt32Value(index);
                try
                {
                    string XValue = "";
                    string WValue = "";

                    foreach (var cell in newRow.Elements<Cell>())
                    {
                        cell.CellReference = new StringValue(cell.CellReference.Value.Replace(Regex.Replace(cell.CellReference.Value, @"[^\d]+", ""), index.ToString(CultureInfo.InvariantCulture)));
                        foreach (var fil in fields.Where(fil => cell.CellReference == fil.Column + index))
                        {
                            //=ЕСЛИ(X18="вне аренды";W18;X18)
                            if (fil.Column == "X")
                            {
                                XValue = item[fil._Field].ToString();
                            }

                            if (fil.Column == "W")
                            {
                                WValue = item[fil._Field].ToString();
                            }

                            if (fil._Field.ToLower() == "сбс")
                            {
                                cell.CellValue = new CellValue(XValue.Trim() == "вне аренды" ? WValue : XValue); //new CellValue("ОТПРАВКА");
                                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                            }
                            else if (fil._Field.ToLower() == "статус")
                            {
                                cell.CellValue = new CellValue("ОТПРАВКА");
                                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                            }
                            else
                            {
                                cell.CellValue = new CellValue(item[fil._Field].ToString());
                                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e.StackTrace);
                    _logger.Error(e.Message);
                    throw e;
                }
             
                return newRow;
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

            private void OpenFile(string filePath)
            {
                if (!File.Exists(filePath))
                {
                    throw new Exception(String.Format("Не удалось найти файл \"{0}\"!", filePath));
                }

                var process = Process.Start(filePath);
                if (process != null)
                {
                    process.WaitForExit();
                }
            }
        }
    }


