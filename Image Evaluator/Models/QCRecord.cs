using System;
using System.Reflection;

namespace CBCTQC.Models
{
    /// <summary>
    /// This class defines QC calculation result object
    /// </summary>
    public class QCRecord : NotifyPropertyChangedBase
    {
        public int ID { get; set; }

        public short WaterCTNumber
        {
            get { return Get<short>(); }
            set { Set(value); }
        }

        public short HighContrastCTNumber
        {
            get { return Get<short>(); }
            set { Set(value); }
        }

        public short LowContrastCTNumber
        {
            get { return Get<short>(); }
            set { Set(value); }
        }

        public double Uniformity
        {
            get { return Get<double>(); }
            set { Set(value); }
        }

        public double Noise
        {
            get { return Get<double>(); }
            set { Set(value); }
        }

        public double CNR
        {
            get { return Get<double>(); }
            set { Set(value); }
        }

        public String SeriesInstanceUID
        {
            get { return Get<String>(); }
            set { Set(value); }
        }

        public String Comment
        {
            get { return Get<String>(); }
            set { Set(value); }
        }

        public DateTime UpdatedTime
        {
            get { return Get<DateTime>(); }
            set { Set(value); }
        }

        public LowContrastVisibility LowContrastVisibility
        {
            get { return Get<LowContrastVisibility>(); }
            set { Set(value); }
        }

        public CalcificationVisibility CalcificationVisibility
        {
            get { return Get<CalcificationVisibility>(); }
            set { Set(value); }
        }


        public void CopyValuesTo(QCRecord copy)
        {
            foreach (PropertyInfo pi in typeof(QCRecord).GetProperties())
            {
                if (!pi.GetGetMethod().IsVirtual)
                {
                    pi.SetValue(copy, pi.GetValue(this));
                }
            }
        }
    }
}
