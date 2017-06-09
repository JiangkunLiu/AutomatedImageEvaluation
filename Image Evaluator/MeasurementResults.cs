//———————————————————————–———————————————————————–———————————————————————–
// <copyright file="MeasurementResults.cs" company="Koning Corporation">
//     Copyright (c) Koning Corporation. All rights reserved.
// </copyright>
//———————————————————————–———————————————————————–———————————————————————–

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Win32;
using System.Windows;

namespace CBCTQC
{
    /// <summary>
    /// This class holds and manages detection result data and measurement result data to be shown in the form
    /// </summary>

    public class DetectionResult
    {
        public List<Items> Result = new List<Items>()
        {
            new Items { Phantom = "Phantom", Tumor = "Tumor", Water = "Water", Calcification = "Calcification" },
            new Items { Phantom = "", Tumor = "", Water = "", Calcification = "" }
        };

        public class Items
        {           
            /// <summary>
            /// Property shows up in datagird, field does not.
            /// </summary>
            public string Phantom { get; set; }
            public string Tumor { get; set; }
            public string Water { get; set; }
            public string Calcification { get; set; }

            //// Find property by Name to get or set values
            public object this[string propertyName]
            {
                get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
                set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
            }

            //// Go through all properties, get and set values
            public void SetAllValue(string Value)
            {
                foreach (PropertyInfo prop in typeof(Items).GetProperties())
                {
                    if (prop.GetIndexParameters().GetLength(0) == 0)
                    {
                        prop.SetValue(this, Value);
                    }
                }
            }
        }

        public DetectionResult()
        {
        }

        public void SetAllNull()
        {
            Result[1].SetAllValue("");
        }

        public void SetAllNotDetected()
        {
            Result[1].SetAllValue("X");
        }

        public void SetAllDetected()
        {
            Result[1].SetAllValue("✓");
        }

        public void SetDetected(string Name)
        {
            try
            {
                Result[1][Name] = "✓";
            }
            catch
            {
                MessageBox.Show("Item name is incorrect in Detection Results.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }

        public void SetNotDetected(string Name)
        {
            try
            {
                Result[1][Name] = "X";
            }
            catch
            {
                MessageBox.Show("Item name is incorrect in Detection Results.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);              
                return;
            }

        }

    }

    public class MeasurementResult
    {
        public Dictionary<string, string> Result = new Dictionary<string, string>()
        {
            {"Image Noise:", "Not Measured"}, 
            {"Water CT Number:", "Not Measured"},
            {"Tumor CT Number:", "Not Measured"},
            {"Field Uniformity:", "Not Measured"},
            {"Contrast to Noise Ratio:", "Not Measured"}
        };

        public MeasurementResult()
        {
        }

        public void SetValue(string Key, string Value)
        {
            Result[Key] = Value;
        }

        public void SetAllNotMeasured()
        {
            foreach (var key in Result.Keys.ToList())
            {
                Result[key] = "Not Measured";
            }
        }

        public void SetAllMeasuring()
        {
            foreach (var key in Result.Keys.ToList())
            {
                Result[key] = "Measuring";
            }
        }
        public void SetAllFailed()
        {
            foreach (var key in Result.Keys.ToList())
            {
                Result[key] = "Failed";
            }
        }
        public void SetAllCancelled()
        {
            foreach (var key in Result.Keys.ToList())
            {
                Result[key] = "Cancelled";
            }
        }
    }
}
