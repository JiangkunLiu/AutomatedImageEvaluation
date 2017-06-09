//———————————————————————–———————————————————————–———————————————————————–
// <copyright file="ImageInformation.cs" company="Koning Corporation">
//     Copyright (c) Koning Corporation. All rights reserved.
// </copyright>
//———————————————————————–———————————————————————–———————————————————————–

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBCTQC
{
    /// <summary>
    /// This class defines image properties and parameters used in image evaluation
    /// </summary>
    public class ImageInformation
    {
        public string inputPath;
        public string outputPath;
        public string[] fileNames;
        public string filterType;
        public string measurementTime;
        public bool IsImageFound;

        public double pixelSize;
        // the following parameter unit is pixel
        public int Height; // image height
        public int Width; // image width
        public int ImageNumber; // number of slice
        public int phCenterH; //phantom center
        public int phCenterW;
        public int hcCenterH; //high contrast object center
        public int hcCenterW;
        public int lcCenterH; //low contrast object center
        public int lcCenterW;
        public int wtCenterH; //water center
        public int wtCenterW;
        public int phRadius; //phantom radius
        public int hcThreshold; //value to threshold out high contrast against phantom body
        public int phThreshold; //value to threshold out phantom body against backgournd air
        
        // adjust the following two parameters for low contrast object position
        public double hc2lcAngle; // angle between high contrast center and low contast center
        public float disPhc2Hcc; // phantom center to high contrast center distance, in mm
        public float disPhc2Lcc; // phantom center to low contrast center distance, in mm

        public double phCTValue;
        public double wtCTValue;
        public double hcCTValue;
        public double lcCTValue;
        public double lcContrast;
        public double imgUniformity;
        public double imgNoise;
        public bool isPhantom; // true by default
        public bool phFlag; // if phantom found
        public bool hcFlag; // if high contrast found
        public bool clFlag; // if calcification found
        public bool wtFlag;
        public int phStart; //slice indexe in the array, not slice numbers shown in file name
        public int hcStart;
        public int clIndex;

        public ImageInformation()
        {
            IsImageFound = false;
            Height = 800;
            Width = 800;
            ImageNumber = 0;
            pixelSize = 0.273;
            phCenterH = 0;
            phCenterW = 0;
            hcCenterH = 0;
            hcCenterW = 0;
            lcCenterH = 0;
            lcCenterW = 0;
            wtCenterH = 0;
            wtCenterW = 0;
            phRadius = 0;
            hc2lcAngle = 46 * 3.1415926 / 180;
            disPhc2Hcc = 30.36f;
            disPhc2Lcc = 29.27f;
            isPhantom = true;
            phFlag = false;
            hcFlag = false;
            clFlag = false;
            wtFlag = false;
            phStart = 0;
            hcStart = 0;
            clIndex = 0;
        }

        public ImageInformation(int X, int Y, double psize)
        {
            IsImageFound = false;
            Height = X;
            Width = Y;
            ImageNumber = 0;
            pixelSize = psize;
            phCenterH = 0;
            phCenterW = 0;
            hcCenterH = 0;
            hcCenterW = 0;
            lcCenterH = 0;
            lcCenterW = 0;
            wtCenterH = 0;
            wtCenterW = 0;
            phRadius = 0;
            hc2lcAngle = 46 * 3.1415926 / 180;
            disPhc2Hcc = 30.36f;
            disPhc2Lcc = 29.27f;
            isPhantom = true;
            phFlag = false;
            hcFlag = false;
            clFlag = false;
            wtFlag = false;
            phStart = 0;
            hcStart = 0;
            clIndex = 0;
        }

        public void resetParameter()
        {
            isPhantom = true;
            phFlag = false;
            hcFlag = false;
            clFlag = false;
            wtFlag = false;
        }
    }
}
