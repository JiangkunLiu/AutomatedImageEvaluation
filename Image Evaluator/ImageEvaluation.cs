//———————————————————————–———————————————————————–———————————————————————–
// <copyright file="ImageEvaluation.cs" company="Koning Corporation">
//     Copyright (c) Koning Corporation. All rights reserved.
// </copyright>
//———————————————————————–———————————————————————–———————————————————————–

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows;


namespace CBCTQC
{
    /// <summary>
    /// This class holds various methods that calculate detect tumors, calcifications
    /// And calculae image noise, contast, and CT values
    /// And drow measured results on images
    /// </summary>
    class ImgEvaluation
    {
        static public bool readShortData(string fName, short[] data, int offset, int size)
        {
            byte[] tmp = new byte[size * sizeof(short)];
            if (!File.Exists(fName))
                {
                    MessageBox.Show("File does not exist: " + fName, "Error (readShortData)",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            FileStream fs = File.OpenRead(fName);
            fs.Read(tmp, offset, size * sizeof(short));//only read byte data
            fs.Close();
            Buffer.BlockCopy(tmp, 0, data, 0, size * sizeof(short));
            tmp = null;
            return true;
        }

        static public void readDicomData(string fName, short[] data, int size)
        {   // read dicom according to the known image size, remove header and keep the image
            // read dcm file to short array
            // Given that the image is 16 bit signed
           
            if (!File.Exists(fName))
            {
                MessageBox.Show("File does not exsit.", "Error (readDicomData)",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            FileStream fs = File.OpenRead(fName);
            int dcmsize = (int)fs.Length;
           
            byte[] tmp = new byte[dcmsize];
            fs.Read(tmp, 0, dcmsize);//only read byte data
            fs.Close();
            Buffer.BlockCopy(tmp, dcmsize - size * sizeof(short), data, 0, size * sizeof(short));
            tmp = null;
            
        }

        static public void writeShortData(string fName, short[] data, int offset, int size)
        {
            byte[] tmp = new byte[size * sizeof(short)];
            Buffer.BlockCopy(data, 0, tmp, 0, size * sizeof(short));

            // find the directory of the file and create if not existing
            string dir = fName.Substring(0, fName.LastIndexOf(@"\"));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            FileStream fs = File.Create(fName);
            fs.Write(tmp, offset, size * sizeof(short));
            fs.Close();
            tmp = null;
        }

        static public void maxShortImg(string[] fileNames, ImageInformation pmt, short[] output)
        {
            // maxShortImg reads in specified slices and outputs one slice with maximus pixel values across each slice
            // MIP thickness: 2.7 mm
            int i, j, k;
            int sliceN = (int)Math.Ceiling(2.7 / pmt.pixelSize);
            short[] tmp = new short[pmt.Width * pmt.Height];
            for (i = 0; i < pmt.Width * pmt.Height; i++)
                output[i] = -1500;
            for (i = pmt.clIndex - sliceN/2; i <= pmt.clIndex + sliceN/2; i++)
            {
                if (i > 0 && i < pmt.ImageNumber)
                {
                    readDicomData(fileNames[i], tmp, pmt.Height * pmt.Width);
                    for (k = 0; k < pmt.Height; k++)
                        for (j = 0; j < pmt.Width; j++)
                        {
                            if (output[k * pmt.Width + j] < tmp[k * pmt.Width + j])
                                output[k * pmt.Width + j] = tmp[k * pmt.Width + j];

                        }
                }
                else
                {
                    MessageBox.Show("Index out of range when creating calcification image.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            tmp = null;
        }

        static public void avgShortContrastImg(string[] fileNames, ImageInformation pmt, short[] output)
        {
            // 2.7 mm thick average
            int i, k;
            int sliceN = (int)Math.Ceiling(2.7 / pmt.pixelSize);
            int[] tmp = new int[pmt.Width * pmt.Height];
            for (i = 0; i < pmt.Width * pmt.Height; i++)
                tmp[i] = 0;
            for (i = pmt.hcStart + (int)(2.7 / pmt.pixelSize); i < pmt.hcStart + (int)(2.7 / pmt.pixelSize) + sliceN; i++)
            {
                if (i >= 0 && i < pmt.ImageNumber)
                {
                    readDicomData(fileNames[i], output, pmt.Height * pmt.Width);
                    for (k = 0; k < pmt.Height * pmt.Width; k++)
                        tmp[k] += output[k] / sliceN;
                }
                else
                {
                    MessageBox.Show("Index out of range when creating contrast image.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            for (i = 0; i < pmt.Height * pmt.Width; i++)
                output[i] = (short)tmp[i];

            tmp = null;
        }

        static public void avgShortNoiseImg(string[] fileNames, ImageInformation pmt, short[] output)
        {
            int i, k;
            int sliceN = (int)Math.Ceiling(2.7 / pmt.pixelSize);
            int[] tmp = new int[pmt.Height * pmt.Width];
            for (i = 0; i < pmt.Height * pmt.Width; i++)
                tmp[i] = 0;

            for (i = pmt.hcStart - (int)(2.0 / pmt.pixelSize) - sliceN; i < pmt.hcStart - (int)(2.0 / pmt.pixelSize); i++)
            {
                if (i >= 0 && i < pmt.ImageNumber)
                {
                    readDicomData(fileNames[i], output, pmt.Height * pmt.Width);
                    for (k = 0; k < pmt.Height * pmt.Width; k++)
                        tmp[k] += output[k] / sliceN;
                }
                else
                {
                    MessageBox.Show("Index out of range when creating noise image.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            for (i = 0; i < pmt.Height * pmt.Width; i++)
                output[i] = (short)tmp[i];
            tmp = null;
        }

        static public void findCalcification(short[] img, ImageInformation pmt)
        {   
            // This is a general calcification detection method
            // In the future, consider detecting clc according to hc position           
            int i, j, m, n, masksize;
            int clThreshold = 0;
            int[] imgTmp = new int[pmt.Height * pmt.Width];
            for (i = 0; i < pmt.Height * pmt.Width; i++)
                imgTmp[i] = 0;

            //// lowpass filter for denoising            
            if (pmt.filterType == "RL")
            {
                masksize = 3;
                for (i = (masksize - 1) / 2; i < pmt.Height - (masksize - 1) / 2; i++)
                    for (j = (masksize - 1) / 2; j < pmt.Width - (masksize - 1) / 2; j++)
                        for (m = -(masksize - 1) / 2; m <= (masksize - 1) / 2; m++)
                            for (n = -(masksize - 1) / 2; n <= (masksize - 1) / 2; n++)
                                imgTmp[i * pmt.Width + j] += img[(i + m) * pmt.Width + j + n];
                int area = masksize * masksize;
                for (i = 0; i < pmt.Height * pmt.Width; i++)
                {
                    imgTmp[i] /= area;
                    img[i] = (short)imgTmp[i];
                }                    
            }

            //writeIntData(pmt.outputPath + "\\clcTmp_" + pmt.Width + "x" + pmt.Height + ".img", imgTmp, 0, pmt.Height * pmt.Width);
            // only consider the ring area
            int outerBoundary = (int)(pmt.phRadius - 10.0 / pmt.pixelSize) * (int)(pmt.phRadius - 10.0 / pmt.pixelSize);
            int innerBoundary = (int)(28.0 / pmt.pixelSize) * (int)(28.0 / pmt.pixelSize);
            // calculate average value
            int clCount = 0;
            int pxCount = 0;
            clThreshold = 0;
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                {
                    int distance = (i - pmt.phCenterH) * (i - pmt.phCenterH) + (j - pmt.phCenterW) * (j - pmt.phCenterW);
                    if (distance < outerBoundary && distance > innerBoundary)
                    {
                        pxCount++;
                        clThreshold += img[i * pmt.Width + j];
                    }
                }
            // set threshold according to average
            clThreshold /= pxCount;
            clThreshold += 120;
            // clThreshold should larger than hcThreshold
            if (clThreshold < pmt.hcThreshold)
                return;

            int clcBoundary = (int)(2.0 / pmt.pixelSize);

            Console.WriteLine("Calcification threshold: " + clThreshold);
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                {
                    int distance = (i - pmt.phCenterH) * (i - pmt.phCenterH) + (j - pmt.phCenterW) * (j - pmt.phCenterW);
                    if (distance < outerBoundary && distance > innerBoundary)
                        if (img[i * pmt.Width + j] > clThreshold)
                        {
                            // if a candidate pixel found, check the surrounding pixels (radius: clcBrounday)
                            // there should be more than m pixels (excluding noise), and less than n pixels (excluding high contrast)
                            // that are brighter than (clThreshold+hcThreshold)/2
                            pxCount = 0;
                            for (int ii = -clcBoundary; ii < clcBoundary; ii++)
                                for (int jj = -clcBoundary; jj < clcBoundary; jj++)
                                {
                                    //if (img[(i + ii) * pmt.Width + j + jj] > (pmt.hcThreshold + clThreshold)/2)
                                    if (img[(i + ii) * pmt.Width + j + jj] > pmt.hcThreshold)
                                    {
                                        pxCount++;
                                    }
                                    //imgTmp[(i + ii) * pmt.Width + j + jj] = 180;
                                }
                            if (pxCount < (int)(1.8 / pmt.pixelSize * 1.8 / pmt.pixelSize) && pxCount > 4)
                            {
                                clCount++;
                                //img[i * pmt.Width + j] = 1000;
                            }
                        }
                }
            //writeShortData(pmt.outputPath + "\\clc_" + pmt.Width + "x" + pmt.Height + ".img", img, 0, pmt.Height * pmt.Width);
            
            Console.WriteLine("Calcification count: " + clCount);
            if (clCount > 50)
                pmt.clFlag = true;
            imgTmp = null;
        }

        static public void findObject(short[] img, ImageInformation pmt)
        {
            // find phantom center, contrast centers, averaged slice works better
            int i, j, m, n, pcount, masksize;
            
            if (pmt.phFlag)
            {
                // Speeding up the program
                // if phantom is alrady detectoed (meaning phThreshold is available)
                // calculate the phantom area (in pixels), if it is not full size slice, no need to continue
                // because contrast objects are on full size slices
                pcount = 0;
                for (i = 0; i < pmt.Height * pmt.Width; i++)
                    if (img[i] > pmt.phThreshold)
                    {
                        pcount++;
                    }
                // over 13333.3 mm^2
                if (pcount < (int)(13333.3 / pmt.pixelSize / pmt.pixelSize))
                {
                    return;                
                }
            }
            else
            {
                // phantom is not yet detected, use a preliminary phThreshold to rule out blank slices to speed up the program
                // accurate phThreshold will be calculated later
                pcount = 0;
                for (i = 0; i < pmt.Height * pmt.Width; i++)
                    if (img[i] > -500)
                    {
                        pcount++;
                    }

                if (pcount < (int)(5 / pmt.pixelSize / pmt.pixelSize))
                    return;
            }


            int[] imgTmp = new int[pmt.Height * pmt.Width];
            for (i = 0; i < pmt.Height * pmt.Width; i++)
                imgTmp[i] = 0;

            /////// image domain smooth
            masksize = 9;
            if (pmt.filterType == "MSL")
                masksize = 9;
            else if (pmt.filterType == "RL")
                masksize = 13;
            for (i = (masksize - 1) / 2; i < pmt.Height - (masksize - 1) / 2; i++)
                for (j = (masksize - 1) / 2; j < pmt.Width - (masksize - 1) / 2; j++)
                    for (m = -(masksize - 1) / 2; m <= (masksize - 1) / 2; m++)
                        for (n = -(masksize - 1) / 2; n <= (masksize - 1) / 2; n++)
                            imgTmp[i * pmt.Width + j] += img[(i + m) * pmt.Width + j + n];
            int area = masksize * masksize;
            for (i = 0; i < pmt.Height * pmt.Width; i++)
                imgTmp[i] /= area;

            // process edges
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                    if (i < (masksize - 1) / 2 || i >= pmt.Height - (masksize - 1) / 2 || j < (masksize - 1) / 2 || j >= pmt.Width - (masksize - 1) / 2)
                        imgTmp[i * pmt.Width + j] = -1000;

            //writeIntData(pmt.outputPath + "\\smoothedImg_" + pmt.Width + "x" + pmt.Height + ".img", imgTmp, 0, pmt.Height * pmt.Width);
            //return;
            ////calculate threshold values by histogram[-1500, 499]////
            int[] histogram = new int[2014];
            int[] histogramTmp = new int[2014];
            int offst = 1500; // offset value that makes sure pixel values are positive
            for (i = 0; i < 2014; i++)
            {
                histogram[i] = 0;
                histogramTmp[i] = 0;
            }
            //generate histogram: pixel value [-offst, 514]
            for (i = 0; i < pmt.Height * pmt.Width; i++)
                if (imgTmp[i] >= -offst && imgTmp[i] < 514)
                    histogramTmp[imgTmp[i] + offst]++;

            //smooth histogram
            masksize = 5;
            for (i = (masksize - 1) / 2; i < 2014 - (masksize - 1) / 2; i++)
                for (j = -(masksize - 1) / 2; j <= (masksize - 1) / 2; j++)
                    histogram[i] += histogramTmp[j+i] / masksize;
            //writeIntData(pmt.outputPath + "\\histogram_1x2014.img", histogram, 0, 2014);
            //find the phantom body threshold
            int max = 0;
            int phPeak = 0;
            for (i = -500 + offst; i < 2014; i++) //start from pixel value -500
                if (max < histogram[i])
                {
                    max = histogram[i];
                    phPeak = i;
                }
            
            // peak value should be over -300, peak should be larger than a threshold
            if (phPeak - offst < -300 || max < 5/pmt.pixelSize/pmt.pixelSize)
            {
                Console.WriteLine("No phantom was found!");
                return;
            }
            else
                pmt.phFlag = true;

            pmt.phThreshold = (phPeak - offst - 1000) / 2;
            Console.WriteLine("Phantom threshold: " + pmt.phThreshold);
            
            //**find phantom center by thresholding**//
            pcount = 0;
            pmt.phCenterH = 0;
            pmt.phCenterW = 0;
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                    if (imgTmp[i * pmt.Width + j] > pmt.phThreshold)
                    {
                        pmt.phCenterH += i;
                        pmt.phCenterW += j;
                        pcount++;
                    }
            pmt.phCenterH /= pcount;
            pmt.phCenterW /= pcount;

            Console.WriteLine("Phantom center: " + pmt.phCenterH + ", " + pmt.phCenterW);

            // estimate the phantom radius 
            pmt.phRadius = 0;
            // radius in the hight direction
            for (i = 0; i < pmt.phCenterH; i++)
                if (imgTmp[i * pmt.Width + pmt.phCenterW] > pmt.phThreshold)
                    pmt.phRadius++;
            Console.WriteLine("Phantom radius: " + pmt.phRadius);
            
            // calculate radius in the width direction
            pcount = 0;
            for (i = 0; i < pmt.phCenterW; i++)
                if (imgTmp[pmt.phCenterH * pmt.Width + i] > pmt.phThreshold)
                    pcount++;

            // two radius should be the same
            if (Math.Abs(pmt.phRadius - pcount) > 5.0 / pmt.pixelSize)
            {
                MessageBox.Show("The object does not seem to be a QC phantom (not round).",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                pmt.isPhantom = false;
                return;
            }

            // max radius should be 65.3 mm
            if (pmt.phRadius > 70.0 / pmt.pixelSize || pcount > 70.0 / pmt.pixelSize)
            {
                MessageBox.Show("The object does not seem to be a QC phantom (too large).",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                pmt.isPhantom = false;
                return;
            }

            /////Method 1. find high contrast object threshold by detecting peak
            //max = 0;
            //int hcPeak = 0;
            //for (i = phPeak; i < 2014 - 1; i++)
            //{
            //    if (histogram[i] - histogram[i + 1] < -1 && max < histogram[i])
            //    {
            //        max = histogram[i];
            //        hcPeak = i;
            //    }
            //}
            //Console.WriteLine("High contrast peak: " + (hcPeak - phPeak));
            //if (hcPeak - phPeak < 100)
            //{
            //    Console.WriteLine("No contrast objects were found (Peak condition not met).");
            //    return;
            //}
            //else
            //{
            //    hcThreshold = (hcPeak - offst + phPeak - offst) / 2;
            //    //Console.WriteLine("High contrast threshold: " + hcThreshold);
            //}

            /////Method 2. find high contrast object threshold by phantom and high contrast difference
            //max = 0;
            //int hcPeak = 0;
            //for (i = phPeak + pmt.ctDifHc2Ph - 50; i < phPeak + pmt.ctDifHc2Ph + 50; i++)
            //{
            //    if (max < histogram[i])
            //    {
            //        max = histogram[i];
            //        hcPeak = i;
            //    }
            //}
            //// count should be larger than a threshold
            //if (max < 5/pmt.pixelSize/pmt.pixelSize)
            //{
            //    Console.WriteLine("No contrast objects were found (Peak condition not met).");
            //    return;
            //}
            //else
            //{
            //    pmt.hcThreshold = (hcPeak - offst + phPeak - offst) / 2;
            //    //Console.WriteLine("High contrast threshold: " + pmt.hcThreshold);
            //}

            ////Method 3. find high contrast by searching max average of a circular mask
            int distance;
            double aHcMask = 230; // area of circular mask, (high contrast object's area is over 230 mm^2 )
            double rHcMask = Math.Sqrt(aHcMask / 3.1415926);
            double hcMean = double.MinValue; // water mean ct value
            double tmp = double.MinValue;
            //// search circle mean on phantom, in a ring area, find the maximum: hc mean
            for (i = 0; i < pmt.Height; i+=4)
                for (j = 0; j < pmt.Width; j+=4)
                {
                    distance = (i - pmt.phCenterH) * (i - pmt.phCenterH) + (j - pmt.phCenterW) * (j - pmt.phCenterW);
                    // ring area, inner radius: 13 mm, outer radius: 41 mm
                    if (distance * pmt.pixelSize * pmt.pixelSize < 41 * 41 && distance * pmt.pixelSize * pmt.pixelSize > 13 * 13)
                    {
                        tmp = calcCircleMeanInt(imgTmp, rHcMask, i, j, pmt);
                        if (hcMean < tmp)
                        {
                            hcMean = tmp;
                            pmt.wtCenterH = i;
                            pmt.wtCenterW = j;
                        }
                    }
                }

            if (hcMean - (phPeak-offst) < 100)
            {
                Console.WriteLine("No contrast objects were found (Mean value condition not met).");
                return;
            }

            pmt.hcThreshold = (int)(phPeak - offst + hcMean) / 2;
            Console.WriteLine("Hc threshold: " + pmt.hcThreshold);
            //// find the high contrast object center
            pcount = 0;
            int cx = 0;
            int cy = 0;
            // use the max mean value position as initial center, search around it for hc center, radius: 15mm
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                {
                    distance = (i - pmt.wtCenterH) * (i - pmt.wtCenterH) + (j - pmt.wtCenterW) * (j - pmt.wtCenterW);
                    if (distance * pmt.pixelSize * pmt.pixelSize < 15 * 15)
                        if (imgTmp[i * pmt.Width + j] > pmt.hcThreshold)
                        {
                            cx += i;
                            cy += j;
                            pcount++;
                        }
                }

            // hc area is larger than aHcMask, smaller than 470 mm^2
            if (pcount < (aHcMask / pmt.pixelSize / pmt.pixelSize) || pcount > 470.00 / pmt.pixelSize / pmt.pixelSize)
            {
                Console.WriteLine("No contrast objects were found (Area condition not met).");
                return;
            }
            else
            {
                pmt.hcCenterH = cx / pcount;
                pmt.hcCenterW = cy / pcount;

                // the distance between hc center and ph center is known, use it to verify if the calculated hc center is correct
                if (Math.Abs(Math.Sqrt((pmt.hcCenterH - pmt.phCenterH) * (pmt.hcCenterH - pmt.phCenterH) + (pmt.hcCenterW - pmt.phCenterW) * (pmt.hcCenterW - pmt.phCenterW)) - pmt.disPhc2Hcc / pmt.pixelSize) < 2.0 / pmt.pixelSize)
                {
                    pmt.hcFlag = true;                   
                }
                
            }
            ////The distance between hc center and ph center is about 30.36 mm
            ////find the center of the second contrast object according to the HC center position
            ////its distance to the phantom center is 2.13*0.273mm shorter than high contrast object
            ////just rotate for 44.67 degree counter clockwise
            int tmpX;
            int tmpY;
            double angle;
            tmpX = (int)((pmt.hcCenterH - pmt.phCenterH) * Math.Cos(pmt.hc2lcAngle) - (pmt.hcCenterW - pmt.phCenterW) * Math.Sin(pmt.hc2lcAngle) + pmt.phCenterH);
            tmpY = (int)((pmt.hcCenterH - pmt.phCenterH) * Math.Sin(pmt.hc2lcAngle) + (pmt.hcCenterW - pmt.phCenterW) * Math.Cos(pmt.hc2lcAngle) + pmt.phCenterW);
            //distance to phantom center adjustment
            {
                ////adjust lc center distance to ph center in polar coordinate system
                double R = Math.Sqrt((tmpX - pmt.phCenterH)*(tmpX - pmt.phCenterH) + (tmpY - pmt.phCenterW)*(tmpY - pmt.phCenterW));
                angle = Math.Acos((double)(tmpX - pmt.phCenterH) / R);
                angle *= Math.Sign(tmpY - pmt.phCenterW);

                pmt.lcCenterH = (int)(Math.Cos(angle) * (R - (pmt.disPhc2Hcc - pmt.disPhc2Lcc) / 0.273)) + pmt.phCenterH;
                pmt.lcCenterW = (int)(Math.Sin(angle) * (R - (pmt.disPhc2Hcc - pmt.disPhc2Lcc) / 0.273)) + pmt.phCenterW;
            }

            ////find the center of water according to the HC center position
            //// Method 1: based on phantom design
            ////roate the phantom center around HC center to get the water center
            ////this should be calibrated for individual phantom
            //angle = 0.03;
            //pmt.wtCenterH = (int)((pmt.phCenterH - pmt.hcCenterH) * Math.Cos(angle) - (pmt.phCenterW - pmt.hcCenterW) * Math.Sin(angle) + pmt.hcCenterH);
            //pmt.wtCenterW = (int)((pmt.phCenterH - pmt.hcCenterH) * Math.Sin(angle) + (pmt.phCenterW - pmt.hcCenterW) * Math.Cos(angle) + pmt.hcCenterW);
            //// Method 2: based on automated thresholding
            double rWtMask = 3.0f; // radius of circular searching mask
            double rWtSearch = 13.0f; // radius of mask center searching
            double wtMean = double.MinValue; // water mean ct value
            //// search circle mean around phcenter, find the maximum: water mean
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                {
                    distance = (i - pmt.phCenterH) * (i - pmt.phCenterH) + (j - pmt.phCenterW) * (j - pmt.phCenterW);
                    if (distance * pmt.pixelSize * pmt.pixelSize < rWtSearch * rWtSearch)
                    {
                        tmp = calcCircleMeanInt(imgTmp, rWtMask, i, j, pmt);
                        if (wtMean < tmp)
                        {
                            wtMean = tmp;
                            pmt.wtCenterH = i;
                            pmt.wtCenterW = j;
                        }
                    }
                }
            int wtThreshold = (int)(phPeak - offst + wtMean) / 2;

            pmt.wtCenterH = 0;
            pmt.wtCenterW = 0;
            pcount = 0;
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                {
                    distance = (i - pmt.phCenterH) * (i - pmt.phCenterH) + (j - pmt.phCenterW) * (j - pmt.phCenterW);
                    if (distance * pmt.pixelSize * pmt.pixelSize < rWtSearch * rWtSearch)
                    {
                        if (imgTmp[i * pmt.Width + j] > wtThreshold)
                        {
                            pcount ++;
                            pmt.wtCenterH += i;
                            pmt.wtCenterW += j;
                        }
                    }
                }
            // area of water should be larger than a threshold, and smaller than a threshold
            if (pcount > 28.0f / pmt.pixelSize / pmt.pixelSize && pcount < 146.0f / pmt.pixelSize / pmt.pixelSize)
            {
                pmt.wtCenterH /= pcount;
                pmt.wtCenterW /= pcount;
                pmt.wtFlag = true;
            }
            else
            {
                Console.WriteLine("Water not found.");
            }

            imgTmp = null;
            histogram = null;
            histogramTmp = null;
        }

        static double calcCircleMeanShort(short[] img, double rROI, int CenterH, int CenterW, ImageInformation pmt)
        {
            double mean = 0;
            int i, j;
            int pcount = 0;
            int rInPixel = (int)(Math.Ceiling(rROI / pmt.pixelSize));


            for (i = CenterH - rInPixel; i < CenterH + rInPixel; i++)
                for (j = CenterW - rInPixel; j < CenterW + rInPixel; j++)
                    if (Math.Sqrt((i - CenterH) * (i - CenterH) + (j - CenterW) * (j - CenterW)) * pmt.pixelSize <= rROI)
                    {
                        mean += img[i * pmt.Width + j];
                        pcount++;
                    }
            return mean/pcount;
        }

        static double calcCircleMeanInt(int[] img, double rROI, int CenterH, int CenterW, ImageInformation pmt)
        {
            double mean = 0;
            int i, j;
            int pcount = 0;
            int rInPixel = (int)(Math.Ceiling(rROI / pmt.pixelSize));


            for (i = CenterH - rInPixel; i < CenterH + rInPixel; i++)
                for (j = CenterW - rInPixel; j < CenterW + rInPixel; j++)
                    if (Math.Sqrt((i - CenterH) * (i - CenterH) + (j - CenterW) * (j - CenterW)) * pmt.pixelSize <= rROI)
                    {
                        mean += img[i * pmt.Width + j];
                        pcount++;
                    }
            return mean / pcount;
        }

        static double calcCircleStd(short[] img, double rROI, int CenterH, int CenterW, ImageInformation pmt)
        {
            //Welford's method
            int i, j;
            double M = 0.0;
            double S = 0.0;
            int rInPixel = (int)(Math.Ceiling(rROI / pmt.pixelSize));
            int k = 1;
            for (i = CenterH - rInPixel; i < CenterH + rInPixel; i++)
                for (j = CenterW - rInPixel; j < CenterW + rInPixel; j++)
                    if (Math.Sqrt((i - CenterH) * (i - CenterH) + (j - CenterW) * (j - CenterW)) * pmt.pixelSize <= rROI)
                        {
                            double tmpM = M;
                            M += (img[i * pmt.Width + j] - tmpM) / k;
                            S += (img[i * pmt.Width + j] - tmpM) * (img[i * pmt.Width + j] - M);
                            k++;
                        }
            return Math.Sqrt(S / (k - 2));
        }

        static void drawCircle(Bitmap pic, Rectangle rect)
        {
            Color clr = Color.FromArgb(244, 244, 244);           
            Brush crcBrush = new SolidBrush(clr);
            Pen crcPen = new Pen(crcBrush);
            Graphics g = Graphics.FromImage(pic);           
            g.DrawEllipse(crcPen, rect);
            crcPen.Dispose();
            crcBrush.Dispose();
            g.Dispose();
        }

        static void drawString(Bitmap pic, string text, System.Drawing.Point point, Font f)
        {
            Graphics g = Graphics.FromImage(pic);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawString(text, f, Brushes.White, point);
            g.Dispose();
        }

        static public void measureImgQuality(ImageInformation pmt, string[] fileNames)
        {
            int i, j, m;
            double pi = 3.1415926;
            double ROIarea; //area of region of interest, unit mm^2           
            double ROIr; //radius of region of interest, unit mm
            string text, fileName, results;
            int fontsize = 10;
            short[] img = new short[pmt.Height * pmt.Width];
            short[] sgtImg = new short[pmt.ImageNumber * pmt.Width]; // sagittal view

            Font f = new Font("Calibri", fontsize);
            Color clr = Color.FromArgb(0, 0, 0);

            Bitmap measurePicCrn = new Bitmap(pmt.Width, pmt.Height); // the other way of X and Y
            Bitmap measurePicSgt = new Bitmap(pmt.Width, pmt.ImageNumber);
            ////set pic to white
            for (i = 0; i < pmt.Height; i++)
                for (j = 0; j < pmt.Width; j++)
                {
                    measurePicCrn.SetPixel(j, i, clr);
                }
            for (i = 0; i < pmt.ImageNumber; i++)
                for (j = 0; j < pmt.Width; j++)
                {
                    measurePicSgt.SetPixel(j, i, clr);
                }

            ROIarea = 80;
            ROIr = Math.Sqrt(ROIarea / pi);
            int ROIn = 8;
            double[] ROICTval = new double[ROIn];
            double[] ROInoise = new double[ROIn];

            ////CT value and noise
            int[] positions = { pmt.phCenterH, pmt.phCenterW,
                                pmt.phCenterH, pmt.phCenterW - pmt.phRadius / 2,
                                pmt.phCenterH, pmt.phCenterW + pmt.phRadius / 2,
                                pmt.phCenterH - pmt.phRadius / 2, pmt.phCenterW,
                                pmt.phCenterH + pmt.phRadius / 2, pmt.phCenterW,
                                (pmt.phStart + pmt.ImageNumber)/2 - (pmt.ImageNumber - pmt.phStart)/4, pmt.phCenterW,
                                (pmt.phStart + pmt.ImageNumber)/2, pmt.phCenterW,
                                (pmt.phStart + pmt.ImageNumber)/2 + (pmt.ImageNumber - pmt.phStart)/4, pmt.phCenterW
                              };
            Rectangle areaRect = new Rectangle();
            System.Drawing.Point point = new System.Drawing.Point();
            
            
            if (pmt.phFlag)
            {
                ////generate sagittal image
                for (m = 0; m < pmt.ImageNumber; m++)
                {
                    readDicomData(fileNames[m], img, pmt.Height * pmt.Width);
                    for (i = 0; i < pmt.Width; i++)
                        sgtImg[m * pmt.Width + i] = img[(pmt.phCenterH-(int)(10.0/pmt.pixelSize)) * pmt.Width + i]; //10 mm away from center to avoid water
                }
                writeShortData(pmt.outputPath + "\\SagittalImg_" + pmt.Width + "x" + pmt.ImageNumber + ".img", sgtImg, 0, pmt.ImageNumber * pmt.Width);
                /////read to-be-measured noise slice
                readDicomData(fileNames[pmt.hcStart - (int)(2.7 / pmt.pixelSize)], img, pmt.Height * pmt.Width);
                ////measure the noise slice
                for (m = 0; m < 5; m++)
                {
                    ROICTval[m] = calcCircleMeanShort(img, ROIr, positions[m * 2], positions[m * 2 + 1], pmt);
                    ROInoise[m] = calcCircleStd(img, ROIr, positions[m * 2], positions[m * 2 + 1], pmt);
                    areaRect.Y = positions[m * 2] - (int)(ROIr / pmt.pixelSize);
                    areaRect.X = positions[m * 2 + 1] - (int)(ROIr / pmt.pixelSize);
                    areaRect.Height = areaRect.Width = (int)(2 * (ROIr / pmt.pixelSize));
                    drawCircle(measurePicCrn, areaRect);
                    text = string.Format("{0:F2} / {1:F2}", ROICTval[m], ROInoise[m]);
                    point.Y = positions[m * 2] + (int)(ROIr / pmt.pixelSize);
                    point.X = positions[m * 2 + 1] - System.Windows.Forms.TextRenderer.MeasureText(text, f).Width / 2; //shift according to string width;
                    drawString(measurePicCrn, text, point, f);
                }

                ////measure the sagittal image
                for (m = 5; m < ROIn; m++)
                {
                    ROICTval[m] = calcCircleMeanShort(sgtImg, ROIr - 1, positions[m * 2], positions[m * 2 + 1], pmt);
                    ROInoise[m] = calcCircleStd(sgtImg, ROIr - 1, positions[m * 2], positions[m * 2 + 1], pmt);
                    areaRect.Y = positions[m * 2] - (int)(ROIr / pmt.pixelSize);
                    areaRect.X = positions[m * 2 + 1] - (int)(ROIr / pmt.pixelSize);
                    areaRect.Height = areaRect.Width = (int)(2 * (ROIr / pmt.pixelSize));
                    drawCircle(measurePicSgt, areaRect);
                    text = string.Format("{0:F2} / {1:F2}", ROICTval[m], ROInoise[m]);
                    point.Y = positions[m * 2] + (int)(ROIr / pmt.pixelSize);
                    point.X = positions[m * 2 + 1] - System.Windows.Forms.TextRenderer.MeasureText(text, f).Width / 2; 
                    drawString(measurePicSgt, text, point, f);
                }

                pmt.phCTValue = 0;
                pmt.imgNoise = 0;
                for (m = 0; m < ROIn; m++)
                {
                    pmt.phCTValue += ROICTval[m];
                    pmt.imgNoise += ROInoise[m];
                }
                pmt.phCTValue /= ROIn;
                pmt.imgNoise /= ROIn;
                //////uniformity
                pmt.imgUniformity = 0;
                for (m = 0; m < ROIn; m++)
                    if (pmt.imgUniformity < Math.Abs(ROICTval[m] - pmt.phCTValue))
                        pmt.imgUniformity = Math.Abs(ROICTval[m] - pmt.phCTValue);
 
                results = string.Format("Phantom body\nCoronal view\nImage thickness: {0} mm\nDisplayed values: CT value / noise", pmt.pixelSize);
                
                point.X = 10;
                point.Y = 10;
                drawString(measurePicCrn, results, point, new Font("Arial", 12));

                results = string.Format("Phantom body\nSagittal view\nImage thickness: {0} mm\nDisplayed values: CT value / noise", pmt.pixelSize);

                point.X = 10;
                point.Y = 10;
                drawString(measurePicSgt, results, point, new Font("Arial", 12));
                OverLapImage(img, measurePicCrn, pmt.Height, pmt.Width);
                OverLapImage(sgtImg, measurePicSgt, pmt.ImageNumber, pmt.Width);
                writeShortData(pmt.outputPath + "\\NoiseLabel_Coronal_" + pmt.Width + "x" + pmt.Height + ".img", img, 0, pmt.Height * pmt.Width);
                writeShortData(pmt.outputPath + "\\NoiseLabel_Sagittal_" + pmt.Width + "x" + pmt.ImageNumber + ".img", sgtImg, 0, pmt.ImageNumber * pmt.Width);
            }

            if(pmt.hcFlag)
            {
                ////set pic to white
                for (i = 0; i < pmt.Width; i++)
                    for (j = 0; j < pmt.Height; j++)
                    {
                        measurePicCrn.SetPixel(i, j, clr);
                    }
                fileName = pmt.outputPath + "\\ContrastImg_" + pmt.Width + "x" + pmt.Height + ".img";
                readShortData(fileName, img, 0, pmt.Height * pmt.Width);
                ////perform findImageCenter again using a better contrast slice
                findObject(img, pmt);

                ////high contrast
                ROIarea = 180;
                ROIr = Math.Sqrt(ROIarea / pi);
                double angle = -60 * 3.1415 / 180; //rotation angle to get adjcent backgound
                int bgCenterH = (int)(Math.Round((pmt.hcCenterH - pmt.phCenterH) * Math.Cos(angle) - (pmt.hcCenterW - pmt.phCenterW) * Math.Sin(angle) + pmt.phCenterH, 0));
                int bgCenterW = (int)(Math.Round((pmt.hcCenterH - pmt.phCenterH) * Math.Sin(angle) + (pmt.hcCenterW - pmt.phCenterW) * Math.Cos(angle) + pmt.phCenterW, 0));
                double bgCT = calcCircleMeanShort(img, ROIr, bgCenterH, bgCenterW, pmt);
                pmt.hcCTValue = calcCircleMeanShort(img, ROIr, pmt.hcCenterH, pmt.hcCenterW, pmt);

                //label high contrast
                areaRect.Y = pmt.hcCenterH - (int)(ROIr / pmt.pixelSize);
                areaRect.X = pmt.hcCenterW - (int)(ROIr / pmt.pixelSize);
                areaRect.Height = areaRect.Width = (int)(2 * (ROIr / pmt.pixelSize));
                drawCircle(measurePicCrn, areaRect);
                text = string.Format("High Contrast\n{0:F2}", pmt.hcCTValue);
                point.Y = pmt.hcCenterH + (int)(ROIr / pmt.pixelSize);
                point.X = pmt.hcCenterW - System.Windows.Forms.TextRenderer.MeasureText(text, f).Width / 2;
                drawString(measurePicCrn, text, point, f);
                //label background
                areaRect.Y = bgCenterH - (int)(ROIr / pmt.pixelSize);
                areaRect.X = bgCenterW - (int)(ROIr / pmt.pixelSize);
                areaRect.Height = areaRect.Width = (int)(2 * (ROIr / pmt.pixelSize));
                drawCircle(measurePicCrn, areaRect);
                text = string.Format("Background\n{0:F2}", bgCT);
                point.Y = bgCenterH + (int)(ROIr / pmt.pixelSize);
                point.X = bgCenterW - System.Windows.Forms.TextRenderer.MeasureText(text, f).Width / 2;
                drawString(measurePicCrn, text, point, f);
                
                ////low contrast
                ROIarea = 30;
                ROIr = Math.Sqrt(ROIarea / pi);
                pmt.lcCTValue = calcCircleMeanShort(img, ROIr, pmt.lcCenterH, pmt.lcCenterW, pmt);
                pmt.lcContrast = pmt.lcCTValue - bgCT;
                //label low contrast
                areaRect.Y = pmt.lcCenterH - (int)(ROIr / pmt.pixelSize);
                areaRect.X = pmt.lcCenterW - (int)(ROIr / pmt.pixelSize);
                areaRect.Height = areaRect.Width = (int)(2 * (ROIr / pmt.pixelSize));
                drawCircle(measurePicCrn, areaRect);
                text = string.Format("Low Contrast\n{0:F2}", pmt.lcCTValue);
                point.Y = pmt.lcCenterH + (int)(ROIr / pmt.pixelSize);
                point.X = pmt.lcCenterW - System.Windows.Forms.TextRenderer.MeasureText(text, f).Width / 2;
                drawString(measurePicCrn, text, point, f);

                if (pmt.wtFlag)
                {
                    ////water CT value
                    ROIr = 3.0f;
                    pmt.wtCTValue = calcCircleMeanShort(img, 3.0f, pmt.wtCenterH, pmt.wtCenterW, pmt);
                    //label water
                    areaRect.Y = pmt.wtCenterH - (int)(ROIr / pmt.pixelSize);
                    areaRect.X = pmt.wtCenterW - (int)(ROIr / pmt.pixelSize);
                    areaRect.Height = areaRect.Width = (int)(2 * (ROIr / pmt.pixelSize));
                    drawCircle(measurePicCrn, areaRect);
                    text = string.Format("Water\n{0:F2}", pmt.wtCTValue);
                    point.Y = pmt.wtCenterH + (int)(ROIr / pmt.pixelSize);
                    point.X = pmt.wtCenterW - System.Windows.Forms.TextRenderer.MeasureText(text, f).Width / 2;
                    drawString(measurePicCrn, text, point, f);
                }
                else
                {
                    pmt.wtCTValue = double.NaN;
                }

                results = "Contrast structures and water\nCoronal view\nImage thickness: 2.7 mm\nDisplayed value: CT value";
                point.X = 10;
                point.Y = 10;
                drawString(measurePicCrn, results, point, new Font("Arial", 12));
                OverLapImage(img, measurePicCrn, pmt.Height, pmt.Width);               
                writeShortData(pmt.outputPath + "\\ContrastLabel_" + pmt.Width + "x" + pmt.Height + ".img", img, 0, pmt.Height * pmt.Width);
            }

            // label calcification image
            if(pmt.clFlag)
            {
                ////set pic to white
                for (i = 0; i < pmt.Width; i++)
                    for (j = 0; j < pmt.Height; j++)
                    {
                        measurePicCrn.SetPixel(i, j, clr);
                    }
                fileName = pmt.outputPath + "\\CalcificationImg_" + pmt.Width + "x" + pmt.Height + ".img";
                readShortData(fileName, img, 0, pmt.Height * pmt.Width);

                results = "Calcification specks\nCoronal view\n2.7 mm thick MIP";
                point.X = 10;
                point.Y = 10;
                drawString(measurePicCrn, results, point, new Font("Arial", 12));
                OverLapImage(img, measurePicCrn, pmt.Height, pmt.Width); 
                writeShortData(pmt.outputPath + "\\CalcificationImg_" + pmt.Width + "x" + pmt.Height + ".img", img, 0, pmt.Height * pmt.Width);
            }

            ROICTval = null;
            ROInoise = null;
            img = null;
            sgtImg = null;
            f.Dispose();
            measurePicCrn.Dispose();
            measurePicSgt.Dispose();
        }

        static void OverLapImage(short[] image, Bitmap drawing, int Height, int Width)
        {
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Color clr = drawing.GetPixel(j, i);
                    if (clr.R + clr.G + clr.B > 0)
                        image[i * Width + j] = (short)((clr.R + clr.G + clr.B) / 5);
                }
        }
    }
}