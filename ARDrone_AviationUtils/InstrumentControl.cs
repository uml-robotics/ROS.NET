/*****************************************************************************/
/* Project  : AviationInstruments                                  */
/* File     : InstrumentControl.cs                                           */
/* Version  : 1                                                              */
/* Language : C#                                                             */
/* Summary  : Generic class for the instrument control design                */
/* Creation : 15/06/2008                                                     */
/* Autor    : Guillaume CHOUTEAU                                             */
/* History  :                                                                */
/*****************************************************************************/

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Text;
using System.Data;


namespace AviationInstruments
{
    public class InstrumentControl : System.Windows.Forms.Control
    {
        #region Generic methodes

        /// <summary>
        /// Rotate an image on a point with a specified angle
        /// </summary>
		/// <param name="pe">The paint area event where the image will be displayed</param>
		/// <param name="img">The image to display</param>
		/// <param name="alpha">The angle of rotation in radian</param>
		/// <param name="ptImg">The location of the left upper corner of the image to display in the paint area in nominal situation</param>
		/// <param name="ptRot">The location of the rotation point in the paint area</param>
		/// <param name="scaleFactor">Multiplication factor on the display image</param>
        protected void RotateImage(PaintEventArgs pe, Image img, Double alpha, Point ptImg, Point ptRot, float scaleFactor)
        {
            double beta = 0; 	// Angle between the Horizontal line and the line (Left upper corner - Rotation point)
            double d = 0;		// Distance between Left upper corner and Rotation point)		
            float deltaX = 0;	// X componant of the corrected translation
            float deltaY = 0;	// Y componant of the corrected translation

			// Compute the correction translation coeff
            if (ptImg != ptRot)
            {
				//
                if (ptRot.X != 0)
                {
                    beta = Math.Atan((double)ptRot.Y / (double)ptRot.X);
                }

                d = Math.Sqrt((ptRot.X * ptRot.X) + (ptRot.Y * ptRot.Y));

                // Computed offset
                deltaX = (float)(d * (Math.Cos(alpha - beta) - Math.Cos(alpha) * Math.Cos(alpha + beta) - Math.Sin(alpha) * Math.Sin(alpha + beta)));
                deltaY = (float)(d * (Math.Sin(beta - alpha) + Math.Sin(alpha) * Math.Cos(alpha + beta) - Math.Cos(alpha) * Math.Sin(alpha + beta)));
            }

            // Rotate image support
            pe.Graphics.RotateTransform((float)(alpha * 180 / Math.PI));

            // Dispay image
            pe.Graphics.DrawImage(img, (ptImg.X + deltaX) * scaleFactor, (ptImg.Y + deltaY) * scaleFactor, img.Width * scaleFactor, img.Height * scaleFactor);

            // Put image support as found
            pe.Graphics.RotateTransform((float)(-alpha * 180 / Math.PI));

        }


        /// <summary>
        /// Translate an image on line with a specified distance and a spcified angle
        /// </summary>
		/// <param name="pe">The paint area event where the image will be displayed</param>
		/// <param name="img">The image to display</param>
		///<param name="deltaPx">The translation distance in pixel</param>
		/// <param name="alpha">The angle of translation direction in radian</param>
		/// <param name="ptImg">The location of the left upper corner of the image to display in the paint area in nominal situation</param>
		/// <param name="scaleFactor">Multiplication factor on the display image</param>
        protected void TranslateImage(PaintEventArgs pe, Image img, int deltaPx, float alpha, Point ptImg, float scaleFactor)
        {
            // Computed offset
            float deltaX = (float)(deltaPx * (Math.Sin(alpha)));
            float deltaY = (float)(- deltaPx * (Math.Cos(alpha)));

            // Dispay image
            pe.Graphics.DrawImage(img, (ptImg.X + deltaX) * scaleFactor, (ptImg.Y + deltaY) * scaleFactor, img.Width * scaleFactor, img.Height * scaleFactor);
        }


        /// <summary>
        /// Rotate an image an apply a translation on the rotated image and the display it
        /// </summary>
		/// <param name="pe">The paint area event where the image will be displayed</param>
		/// <param name="img">The image to display</param>
		/// <param name="alphaRot">The angle of rotation in radian</param>
		/// <param name="alphaTrs">The angle of translation direction in radian, expressed in the rotated image coordonate system</param>
		/// <param name="ptImg">The location of the left upper corner of the image to display in the paint area in nominal situation</param>
		/// <param name="ptRot">The location of the rotation point in the paint area</param>
		/// <param name="deltaPx">The translation distance in pixel</param>
		/// <param name="ptRot">The location of the rotation point in the paint area</param>
		/// <param name="scaleFactor">Multiplication factor on the display image</param>
        protected void RotateAndTranslate(PaintEventArgs pe, Image img, Double alphaRot, Double alphaTrs, Point ptImg, int deltaPx, Point ptRot, float scaleFactor)
        {
            double beta = 0;
            double d = 0;
            float deltaXRot = 0;
            float deltaYRot = 0;
            float deltaXTrs = 0;
            float deltaYTrs = 0;

            // Rotation

            if (ptImg != ptRot)
            {
                // Internals coeffs
                if (ptRot.X != 0)
                {
                    beta = Math.Atan((double)ptRot.Y / (double)ptRot.X);
                }

                d = Math.Sqrt((ptRot.X * ptRot.X) + (ptRot.Y * ptRot.Y));

                // Computed offset
                deltaXRot = (float)(d * (Math.Cos(alphaRot - beta) - Math.Cos(alphaRot) * Math.Cos(alphaRot + beta) - Math.Sin(alphaRot) * Math.Sin(alphaRot + beta)));
                deltaYRot = (float)(d * (Math.Sin(beta - alphaRot) + Math.Sin(alphaRot) * Math.Cos(alphaRot + beta) - Math.Cos(alphaRot) * Math.Sin(alphaRot + beta)));
            }

            // Translation

            // Computed offset
            deltaXTrs = (float)(deltaPx * (Math.Sin(alphaTrs)));
            deltaYTrs = (float)(- deltaPx * (-Math.Cos(alphaTrs)));

            // Rotate image support
            pe.Graphics.RotateTransform((float)(alphaRot * 180 / Math.PI));

            // Dispay image
            pe.Graphics.DrawImage(img, (ptImg.X + deltaXRot + deltaXTrs) * scaleFactor, (ptImg.Y + deltaYRot + deltaYTrs) * scaleFactor, img.Width * scaleFactor, img.Height * scaleFactor);

            // Put image support as found
            pe.Graphics.RotateTransform((float)(-alphaRot * 180 / Math.PI));
        }


		/// <summary>
        /// Display a Scroll counter image like on gas  pumps 
        /// </summary>
		/// <param name="pe">The paint area event where the image will be displayed</param>
		/// <param name="imgBand">The band counter image to display with digts : 0|1|2|3|4|5|6|7|8|9|0</param>
		/// <param name="nbOfDigits">The number of digits displayed by the counter</param>
		/// <param name="counterValue">The value to dispay on the counter</param>
		/// <param name="ptImg">The location of the left upper corner of the image to display in the paint area in nominal situation</param>
		/// <param name="scaleFactor">Multiplication factor on the display image</param>
        protected void ScrollCounter(PaintEventArgs pe, Image imgBand, int nbOfDigits, int counterValue, Point ptImg, float scaleFactor)
        {
            int indexDigit =0;
            int digitBoxHeight = (int)(imgBand.Height/11);
            int digitBoxWidth = imgBand.Width;

            for(indexDigit = 0; indexDigit<nbOfDigits; indexDigit++)
            {
                int currentDigit;
                int prevDigit;
                int xOffset;
                int yOffset;
				double fader;

                currentDigit = (int)((counterValue / Math.Pow(10, indexDigit)) % 10);

                if (indexDigit == 0)
                {
                    prevDigit = 0;
                }
                else
                {
                    prevDigit = (int)((counterValue / Math.Pow(10, indexDigit-1)) % 10);
                }

				// xOffset Computing
                xOffset = (int)(digitBoxWidth * (nbOfDigits - indexDigit - 1));
				
				// yOffset Computing	
                if(prevDigit == 9)
                {
                    fader = 0.33;
                    yOffset = (int)(-((fader + currentDigit) * digitBoxHeight));
                }
                else
                {
                    yOffset = (int)(-(currentDigit * digitBoxHeight));
                }

				// Display Image
                pe.Graphics.DrawImage(imgBand,(ptImg.X + xOffset)*scaleFactor,(ptImg.Y + yOffset)*scaleFactor,imgBand.Width*scaleFactor,imgBand.Height*scaleFactor);
            }
        }

        private void DisplayRoundMark(PaintEventArgs pe, Image imgMark, InstrumentControlMarksDefinition insControlMarksDefinition, Point ptImg, int radiusPx, Boolean displayText, float scaleFactor)
        {
            Double alphaRot;
            int textBoxLength;
            int textPointRadiusPx;
            int textBoxHeight = (int)(insControlMarksDefinition.fontSize*1.1/scaleFactor);      
            Point textPoint = new Point();
            Point rotatePoint = new Point();
            Font markFont = new Font("Arial", insControlMarksDefinition.fontSize);
            SolidBrush markBrush = new SolidBrush(insControlMarksDefinition.fontColor);
            InstrumentControlMarkPoint[] markArray = new InstrumentControlMarkPoint[2 + insControlMarksDefinition.numberOfDivisions];

            // Buid the markArray
            markArray[0].value = insControlMarksDefinition.minPhy;
            markArray[0].angle = insControlMarksDefinition.minAngle;
            markArray[markArray.Length - 1].value = insControlMarksDefinition.maxPhy;
            markArray[markArray.Length - 1].angle = insControlMarksDefinition.maxAngle;

            for (int index = 1; index < insControlMarksDefinition.numberOfDivisions+1; index++)
            {
                markArray[index].value = (insControlMarksDefinition.maxPhy - insControlMarksDefinition.minPhy) / (insControlMarksDefinition.numberOfDivisions + 1) * index + insControlMarksDefinition.minPhy;
                markArray[index].angle = (insControlMarksDefinition.maxAngle - insControlMarksDefinition.minAngle) / (insControlMarksDefinition.numberOfDivisions + 1) * index + insControlMarksDefinition.minAngle;
            }

            // Define the rotate point (center of the indicator)
            rotatePoint.X = (int)((this.Width/2)/scaleFactor);
            rotatePoint.Y = rotatePoint.X;
            
            // Display mark array
            foreach (InstrumentControlMarkPoint markPoint in markArray)
            {
                // pre computings
                alphaRot = (Math.PI / 2) - markPoint.angle;
                textBoxLength = (int)(Convert.ToString(markPoint.value).Length * insControlMarksDefinition.fontSize*0.8/scaleFactor);
                textPointRadiusPx = (int)(radiusPx - 1.2*imgMark.Height - 0.5 * textBoxLength);
                textPoint.X = (int)((textPointRadiusPx * Math.Cos(markPoint.angle) - 0.5 * textBoxLength + rotatePoint.X) * scaleFactor);
                textPoint.Y = (int)((-textPointRadiusPx * Math.Sin(markPoint.angle) - 0.5 * textBoxHeight + rotatePoint.Y) * scaleFactor);
                
                // Display mark
                RotateImage(pe, imgMark, alphaRot, ptImg, rotatePoint, scaleFactor);

                // Display text
                if (displayText == true)
                {
                    pe.Graphics.DrawString(Convert.ToString(markPoint.value), markFont, markBrush, textPoint);
                }
            }
        }


        /// <summary>
        /// Convert a physical value in an rad angle used by the rotate function
        /// </summary>
		/// <param name="phyVal">Physical value to interpol/param>
		/// <param name="minPhy">Minimum physical value</param>
		/// <param name="maxPhy">Maximum physical value</param>
		/// <param name="minAngle">The angle related to the minumum value, in deg</param>
		/// <param name="maxAngle">The angle related to the maximum value, in deg</param>
		/// <returns>The angle in radian witch correspond to the physical value</returns>
        protected float InterpolPhyToAngle(float phyVal, float minPhy, float maxPhy, float minAngle, float maxAngle)
        {
            float a;
            float b;
            float y;
            float x;

            if (phyVal < minPhy)
            {
                return (float)(minAngle * Math.PI / 180);
            }
            else if (phyVal > maxPhy)
            {
                return (float)(maxAngle * Math.PI / 180);
            }
            else
            {

                x = phyVal;
                a = (maxAngle - minAngle) / (maxPhy - minPhy);
                b = (float)(0.5 * (maxAngle + minAngle - a * (maxPhy + minPhy)));
                y = a * x + b;

                return (float)(y * Math.PI / 180);
            }
        }

        protected Point FromCartRefToImgRef(Point cartPoint)
        {
            Point imgPoint = new Point();
            imgPoint.X = cartPoint.X + (this.Width/2);
            imgPoint.Y = -cartPoint.Y + (this.Height/2);
            return (imgPoint);
        }

        protected double FromDegToRad(double degAngle)
        {
            double radAngle = degAngle * Math.PI / 180;
            return radAngle;
        }


        #endregion
    }

    struct InstrumentControlMarksDefinition
    {
        public InstrumentControlMarksDefinition(float myMinPhy, float myMinAngle, float myMaxPhy, float myMaxAngle, int myNumberOfDivisions, int myFontSize, Color myFontColor, InstumentMarkScaleStyle myScaleStyle)
        {
            this.minPhy = myMinPhy;
            this.minAngle = myMinAngle;
            this.maxPhy = myMaxPhy;
            this.maxAngle = myMaxAngle;
            this.numberOfDivisions = myNumberOfDivisions;
            this.fontSize = myFontSize;
            this.fontColor = myFontColor;
            this.scaleStyle = myScaleStyle;
        }
        internal float minPhy;
        internal float minAngle;
        internal float maxPhy;
        internal float maxAngle;
        internal int numberOfDivisions;
        internal int fontSize;
        internal Color fontColor;
        internal InstumentMarkScaleStyle scaleStyle;
    }

    struct InstrumentControlMarkPoint
    {
        public InstrumentControlMarkPoint(float myValue, float myAngle)
        {
            this.value = myValue;
            this.angle = myAngle;
        }
        internal float value;
        internal float angle;
    }
   
    enum InstumentMarkScaleStyle
    {
        Linear = 0,
        Log = 1,
    };
}
