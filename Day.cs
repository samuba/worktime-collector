using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace worktime_collector
{
    internal class Day
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public TimeSpan Time
        {
            get { return this.End.Subtract(this.Start); }

        }

        public Day(DateTime start, DateTime end)
        {
            this.Start = start;
            this.End = end;
        }

        public override string ToString()
        {
            return this.Start.ToString("dd.MM.yyyy");
        }

        public string StartString()
        {
            return this.Start.ToString("HH:mm");
        }

        public string EndString()
        {
            return this.End.ToString("HH:mm");
        }

        public string TimeString()
        {
            return new DateTime(this.Time.Ticks).ToString("HH:mm");
        }
    }
}