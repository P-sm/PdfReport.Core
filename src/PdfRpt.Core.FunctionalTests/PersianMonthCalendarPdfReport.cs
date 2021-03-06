﻿using System;
using System.Collections.Generic;
using System.Linq;
using iTextSharp.text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfRpt.Calendar;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.FunctionalTests.Models;
using PdfRpt.FluentInterface;

namespace PdfRpt.Core.FunctionalTests
{
    [TestClass]
    public class PersianMonthCalendarPdfReport
    {
        [TestMethod]
        public void Verify_PersianMonthCalendarPdfReport_Can_Be_Created()
        {
            var report = CreatePersianMonthCalendarPdfReport();
            TestUtils.VerifyPdfFileIsReadable(report.FileName);
        }

        public IPdfReportData CreatePersianMonthCalendarPdfReport()
        {
            return new PdfReport().DocumentPreferences(doc =>
            {
                doc.RunDirection(PdfRunDirection.RightToLeft);
                doc.Orientation(PageOrientation.Portrait);
                doc.PageSize(PdfPageSize.A4);
                doc.DocumentMetadata(new DocumentMetadata { Author = "Vahid", Application = "PdfRpt", Keywords = "IList Rpt.", Subject = "Test Rpt", Title = "Test" });
                doc.Compression(new CompressionSettings
                {
                    EnableCompression = true,
                    EnableFullCompression = true
                });
            })
            .DefaultFonts(fonts =>
            {
                fonts.Path(System.IO.Path.Combine(TestUtils.GetBaseDir(), "fonts", "irsans.ttf"),
                           TestUtils.GetVerdanaFontPath());
                fonts.Size(9);
                fonts.Color(System.Drawing.Color.Black);
            })
            .PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("MM/dd/yyyy"));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.RightToLeft);
                    defaultHeader.ImagePath(TestUtils.GetImagePath("01.png"));
                    defaultHeader.Message("گزارش ساعات كاركرد كاركنان");
                });
            })
            .MainTableTemplate(template =>
            {
                template.BasicTemplate(BasicTemplate.ClassicTemplate);
            })
            .MainTablePreferences(table =>
            {
                table.ColumnsWidthsType(TableColumnWidthType.Relative);
                table.SplitRows(true);
            })
            .MainTableDataSource(dataSource =>
            {
                var listOfRows = PersianMonthCalendarDataSource.CreateDataSource();
                dataSource.StronglyTypedList(listOfRows);
            })
            .MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.PropertyName("rowNo");
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(0.5f);
                    column.HeaderCell("#");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<UserMonthCalendar>(x => x.Id);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(0.5f);
                    column.HeaderCell("شماره");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<UserMonthCalendar>(x => x.Name);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(1);
                    column.HeaderCell("نام");
                });

                columns.AddColumn(column =>
                {
                    // Calendar's cell data type should be PdfRpt.Calendar.CalendarData
                    column.PropertyName<UserMonthCalendar>(x => x.MonthCalendarData);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(3);
                    column.HeaderCell("تقويم ماهيانه");
                    column.ColumnItemsTemplate(itemsTemplate =>
                    {
                        itemsTemplate.MonthCalendar(new CalendarAttributes
                        {
                            CalendarType = CalendarType.PersianCalendar,
                            UseLongDayNamesOfWeek = true,
                            Padding = 3,
                            DescriptionHorizontalAlignment = HorizontalAlignment.Center,
                            SplitRows = true,
                            CellsCustomizer = info =>
                            {
                                if (info.Year == 1391 && info.Month == 1 && info.DayNumber == 1)
                                {
                                    info.NumberCell.BackgroundColor = new BaseColor(System.Drawing.Color.LimeGreen.ToArgb());
                                    var phrase = info.NumberCell.Phrase;
                                    foreach (Chunk chunk in phrase.Chunks)
                                        chunk.Font.Color = new BaseColor(System.Drawing.Color.Yellow.ToArgb());
                                }
                            }
                        });
                    });
                });

            })
            .MainTableEvents(events =>
            {
                events.DataSourceIsEmpty(message: "There is no data available to display.");
            })
            .Generate(data => data.AsPdfFile(TestUtils.GetOutputFileName()));
        }
    }

    public static class PersianMonthCalendarDataSource
    {
        public static IList<UserMonthCalendar> CreateDataSource()
        {
            var usersWorkedHours = createUsersWorkedHours();
            // Mapping a list of normal Users WorkedHours to a list of Users + CalendarData
            return usersWorkedHours
                        .GroupBy(x => new
                        {
                            Id = x.Id,
                            Name = x.Name
                        })
                        .Select(
                                 x => new UserMonthCalendar
                                 {
                                     Id = x.Key.Id,
                                     Name = x.Key.Name,
                                     // Calendar's cell data type should be PdfRpt.Calendar.CalendarData
                                     MonthCalendarData = new CalendarData
                                     {
                                         Year = x.First().Year,
                                         Month = x.First().Month,
                                         MonthDaysInfo = x.ToList().Select(y => new DayInfo
                                         {
                                             Description = y.Description,
                                             ShowDescriptionInFooter = false,
                                             DayNumber = y.DayNumber
                                         }).ToList()
                                     }
                                 }).ToList();
        }

        private static List<UserWorkedHours> createUsersWorkedHours()
        {
            var usersWorkedHours = new List<UserWorkedHours>();
            for (int i = 1; i < 11; i++)
            {
                for (int j = 1; j < 28; j++)
                {
                    usersWorkedHours.Add(new UserWorkedHours
                    {
                        Id = i,
                        Name = "كارمند " + i,
                        Year = 1391, // سال و ماه بر اساس نوع تقويم انتخابي مشخص مي‌شود
                        Month = i,
                        DayNumber = j,
                        Description = i % 2 == 0 ? "05:00" : "08:00"
                    });
                }
            }
            return usersWorkedHours;
        }
    }
}