using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace worktime_collector
{
    class Program
    {
        static int days = 7;

        static int pause = 30;

        static void Main(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                try
                {
                    days = Convert.ToInt32(args[0]);
                }
                catch
                {
                    PrintHelpText();
                    return;
                }
            }

            Console.WriteLine("parsing Eventlogs...\n");
            var allDates = GetEventLogDatesFromLastNDays(days);
            var dates = new List<Day>();

            for (int i = 0; i < days - 1; i++)
            {
                var eventsOnDay = allDates.Where(x => x > DateTime.Today.AddDays(-i) && x < DateTime.Today.AddDays(-(i - 1)));
                if (eventsOnDay.FirstOrDefault() != default(DateTime))
                {
                    dates.Add(new Day(eventsOnDay.Last(), eventsOnDay.First()));
                }
            }

            var currentWeek = new TimeSpan();
            var currentWeeksPause = new TimeSpan();
            for (int i = 0; i < dates.Count; i++)
            {
                var date = dates[i];
                if (date.Start.Date == DateTime.Today)
                {
                    Console.WriteLine("Today Begin: " + date.StartString());
                    Console.WriteLine("Today Time:  " + TimeSpanToString(DateTime.Now.Subtract(date.Start)));
                }
                else
                {
                    Console.WriteLine(date + " Begin: " + date.StartString());
                    Console.WriteLine(date + " End:   " + date.EndString());
                    Console.WriteLine(date + " Time:  " + date.TimeString());
                }

                currentWeek = currentWeek.Add(date.Time);
                currentWeeksPause = currentWeeksPause.Add(new TimeSpan(0, pause, 0));

                var weekNumber = WeekOfYear(date.Start);
                var nextWeekNumber = i + 1 < dates.Count ? WeekOfYear(dates[i + 1].Start) : weekNumber;
                if (weekNumber != nextWeekNumber)
                {
                    Console.WriteLine();
                    Console.WriteLine("Week " + weekNumber + " (" + GetWeekSpan(date.Start) + "): " + TimeSpanToString(currentWeek));
                    Console.WriteLine("Week " + weekNumber + " - Pause " + TimeSpanToString(currentWeeksPause) + ":   " + TimeSpanToString(currentWeek.Subtract(currentWeeksPause)));
                    currentWeek = new TimeSpan();
                    currentWeeksPause = new TimeSpan();
                }

                Console.WriteLine();
            }
        }

        private static string GetWeekSpan(DateTime date)
        {
            return GetFirstDayOfWeek(date).ToString("dd.MM") + " - " + GetLastDayOfWeek(date).ToString("dd.MM");
        }

        private static DateTime GetFirstDayOfWeek(DateTime date)
        {
            var firstDayInWeek = date;
            while (WeekOfYear(firstDayInWeek) == WeekOfYear(date))
            {
                firstDayInWeek = firstDayInWeek.AddDays(-1);
            }
            return firstDayInWeek.AddDays(1);
        }

        private static DateTime GetLastDayOfWeek(DateTime date)
        {
            var LastDayInWeek = date;
            while (WeekOfYear(LastDayInWeek) == WeekOfYear(date))
            {
                LastDayInWeek = LastDayInWeek.AddDays(+1);
            }
            return LastDayInWeek.AddDays(-1);
        }

        private static string TimeSpanToString(TimeSpan span)
        {
            return string.Format("{0:00}:{1:00}", (int)span.TotalMinutes / 60, (int)span.TotalMinutes % 60);
        }

        private static int WeekOfYear(DateTime date)
        {
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        private static List<DateTime> GetEventLogDatesFromLastNDays(int days)
        {
            var minDay = DateTime.Today.AddDays(-days).ToString(@"yyyy-MM-ddTHH\:mm\:ss");
            var times = new List<DateTime>();

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wevtutil.exe",
                    Arguments = @"qe System /rd:true /f:text /q:""*[System[TimeCreated[@SystemTime >= '" + minDay + @"']]]""",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();

            string time = string.Empty;
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                if (line.StartsWith("  Date:"))
                {
                    time = line.Substring(8);
                    times.Add(DateTime.Parse(time));
                }
            }

            return times;
        }

        private static void PrintHelpText()
        {
            Console.WriteLine();
            Console.WriteLine("Usage Examples:");
            Console.WriteLine("    WorktimeCollector.exe     \t\t... Show Worktime for last " + days + " days");
            Console.WriteLine("    WorktimeCollector.exe 14  \t\t... Show Worktime for last 2 weeks");
            Console.WriteLine("    WorktimeCollector.exe 365 \t\t... Show Worktime for last year");
            Console.WriteLine();
            Console.WriteLine("How it work:");
            Console.WriteLine("    WorktimeCollector crunches the systems Eventlog entries and");
            Console.WriteLine("    looks for the first and last entry for each day. It then takes");
            Console.WriteLine("    the difference and prints them out.");
        }
    }
}
