using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Petzold.Media3D;


namespace DREAMPioneer
{

    public partial class ViewportMD : UserControl
    {
        DirectionalLight directionalLight = new DirectionalLight();
        AmbientLight ambientLight = new AmbientLight();
        PerspectiveCamera perspectiveCamera = new PerspectiveCamera();

        Cuboid mainBody = new Cuboid();
        Cuboid frontBumper = new Cuboid();
        Cuboid rearBumper = new Cuboid();
        Cuboid frontLeftTyre = new Cuboid();
        Cuboid frontRightTyre = new Cuboid();
        Cuboid rearLeftTyre = new Cuboid();
        Cuboid rearRightTyre = new Cuboid();
        WireLine[] yAxis = new WireLine[9];
        WireLine[] xAxis = new WireLine[9];

        WirePolyline laserLines = new WirePolyline();
        WirePolyline laserUrgLines = new WirePolyline();
        WirePolyline sonarLines = new WirePolyline();

        WireLine userVector = new WireLine();
        WireLine robotVector = new WireLine();
        WireLine finalVector = new WireLine();

        DiffuseMaterial redMaterial;
        DiffuseMaterial blackMaterial;

        double dRotateValue = 0;
        bool bPerspectified = false;
        bool bLaserNotDisplayed = true;

        Point3D[] sonarPose = new Point3D[21]; // x in inches , y in inches, theta in degrees 
        const double dSonarHalfCone = 8;
        double dSinHalfCone = Math.Sin(dSonarHalfCone * Math.PI / 180);
        double dCosHalfCone = Math.Cos(dSonarHalfCone * Math.PI / 180);
        double dRadiansHalfCone = Math.PI * dSonarHalfCone / 180;
        int iSkipSonars = 3;


        public ViewportMD()
        {
            InitializeComponent();
            InitStuff();
            SetBackground(Colors.DarkGray);
        }

        #region Public_Stuff
        /// <summary>
        /// This will set the background of the viewport to whatever is desired.
        /// </summary>
        /// <param name="c">This must be the desired color value for the background.</param>
        public void SetBackground(Color c)
        {
            SolidColorBrush b = new SolidColorBrush(c);
            dockPanel1.Background = b;
        }

        /// <summary>
        /// This function will set the pan value of the camera. In reality is will simply rotate everything in the world. 
        /// This is a stateless function. It will not care about the existing value of the rotation. 
        /// </summary>
        /// <param name="thetaInDegrees">This must be the absolute value of the pan/rotation desired. 
        /// The value must be in degrees and based on the cartesian coordinate system.</param>
        public void SetPan(double thetaInDegrees)
        {
            dRotateValue = thetaInDegrees;
            foreach (Visual3D v in viewport.Children.ToArray())
            {
                RotateTransform3D rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), dRotateValue));
                v.Transform = rotateTransform;
            }
        }

        /// <summary>
        /// This function will accept and draw laser values. These values are then converted to lines and then displayed.
        /// </summary>
        /// <param name="dDistanceValues">This array must have exactly 181 values. The values must be in cm. 
        /// The values must be distances from the right (0 degrees) to the left (180 degrees).</param>
        public void SetLaser(double[] dDistanceValues, double res, double start)
        {
            // Convert from mm to meters.
            /*for (int i = 0; i < dDistanceValues.Length; i++)
            {
                dDistanceValues[i] /= 100.0;
            }*/
            // TODO: Assigning the second last value to the last one is a temp workaround. 
            dDistanceValues[dDistanceValues.Length-1] = dDistanceValues[dDistanceValues.Length-2];
            drawLaser(dDistanceValues, res, start);
            //drawSonar();
        }

        public void SetLaserUrg(double[] dDistanceValues)
        {
            // Convert from mm to meters.
            for (int i = 0; i < dDistanceValues.Length; i++)
            {
                dDistanceValues[i] /= 100;
            }
            // TODO: Assigning the second last value to the last one is a temp workaround. 
            dDistanceValues[dDistanceValues.Length - 1] = dDistanceValues[dDistanceValues.Length - 2];
            drawLaserUrg(dDistanceValues);
        }

        /// <summary>
        /// This function will accept and draw sonar values. These values are then converted to lines and then displayed.
        /// </summary>
        /// <param name="dDistanceValues">This array must have exactly 21 values. The values must be in cm. 
        /// The values must be distances from the left front sonar on Jr and counter-clockwise from then on.</param>
        public void SetSonar(double[] dDistanceValues)
        {
            // Convert from mm to meters.
            for (int i = 0; i < dDistanceValues.Length; i++)
            {
                dDistanceValues[i] /= 100;
            }
            drawSonar(dDistanceValues);
        }

        /// <summary>
        /// This function will return the status of the camera. It can be true (perspectified view) or false (top-down view). 
        /// In reality the camera is a perspective camera. Only the location changes.
        /// </summary>
        /// <returns>
        /// Return true indicating that the camera is in perspective view else false for top-down.</returns>
        public bool IsPerspectified()
        {
            return bPerspectified;
        }

        public void DrawVectors(Point r, Point u, Point f)
        {
            userVector.Point1 = new Point3D(-1 * u.X, u.Y, 0.02);
            userVector.Point2 = new Point3D(0, 0, 0.02);
            robotVector.Point1 = new Point3D(-1 * r.X, r.Y, 0.02);
            robotVector.Point2 = new Point3D(0, 0, 0.02);
            finalVector.Point1 = new Point3D(-1 * f.X, f.Y, 0.02);
            finalVector.Point2 = new Point3D(0, 0, 0.2);

            userVector.Color = Colors.White;
            userVector.Thickness = 4;
            robotVector.Color = Colors.Blue;
            robotVector.Thickness = 4;
            finalVector.Color = Colors.Red;
            finalVector.Thickness = 4;

            viewport.Children.Remove(userVector);
            viewport.Children.Remove(robotVector);
            //viewport.Children.Remove(finalVector);
            viewport.Children.Add(userVector);
            viewport.Children.Add(robotVector);
            //viewport.Children.Add(finalVector);
        }
        #endregion

        #region Private_Stuff
        void InitStuff()
        {
            Brush b = new SolidColorBrush(Colors.Red);
            redMaterial = new DiffuseMaterial(b);

            b = new SolidColorBrush(Colors.Black);
            blackMaterial = new DiffuseMaterial(b);

            createAxisY();
            createAxisX();

            createMainBody();
            createRearBumper();
            createFrontBumper();
            createFrontLeftTyre();
            createFrontRightTyre();
            createRearLeftTyre();
            createRearRightTyre();

            createLaserLines();
            createLaserUrgLines();
            createSonarLines();
        }

        private void sonarify(double[] dLaserValues, int segments)
        {
            // Emulating sonars by grouping the laser values into segments.
            int segmentWidth = dLaserValues.Length / segments;
            double[] range = new Double[segmentWidth];
            for (int j = 0; j < segments; j++)
            {
                // Find the min value
                Array.Copy(dLaserValues, j * segmentWidth, range, 0, segmentWidth);
                double min = range.Min();
                // Set the min value
                for (int i = 0; i < range.Length; i++)
                {
                    dLaserValues[j * segmentWidth + i] = min;
                }
            }
        }

        void drawLaser(double[] dLaserValues,double res, double start)
        {
            //if (MainWindow.sFrontendMode == "SA")
            //   sonarify(dLaserValues, 180 / 30);
            start += Math.PI / 2;

            Point3DCollection ranges = new Point3DCollection();
            for (int i = 0; i < dLaserValues.Length; i++)
            {
                //i * res * start
                // The reason for adding i2m(15.748) to the laser values is to avoid a translate transform later on. 
                // By avoiding the translate transform, the process of rotating everything becomes easy. 
                ranges.Add(new Point3D(dLaserValues[i] * Math.Cos((i * res + start)), i2m(15.748) + dLaserValues[i] * Math.Sin(i * res + start), 0));
                if (i == 0 || i == dLaserValues.Length-1)
                    Console.WriteLine(ranges[ranges.Count - 1]);
               
            }
            laserLines.Points = ranges;
            bLaserNotDisplayed = false;
        }

        void drawLaserUrg(double[] dLaserValues)
        {

           // if (MainWindow.sFrontendMode == "SA")
           //    sonarify(dLaserValues, 240 / 30);

            Point3DCollection ranges = new Point3DCollection();
            double theta = 150;

            for (int i = 0; i < dLaserValues.Length; i++)
            {
                // The reason for adding i2m(-11.811) to the laser values is to avoid a translate transform later on. 
                // By avoiding the translate transform, the process of rotating everything becomes easy. 
                //ranges.Add(new Point3D(dLaserValues[i] * Math.Cos(i * Math.PI / 180), i2m(15.5) + dLaserValues[i] * Math.Sin(i * Math.PI / 180), 0));
                ranges.Add(new Point3D(dLaserValues[i] * Math.Cos(theta * Math.PI / 180), i2m(-11.811) + dLaserValues[i] * Math.Sin(theta * Math.PI / 180), 0));
                // The increment value should be auto generated by: thetaIncrement = 240/dLaserValues.Length. However, too lazy to do that.
                theta += 0.3513;
            }
            laserUrgLines.Points = ranges;
            //bLaserNotDisplayed = false;
        }

        void drawSonar(double[] sonarRange)
        {
            Point3DCollection sonarValues = new Point3DCollection();
            Point3D[,] sonarPoints = new Point3D[21, 2]; // left, right

            // This is to avoid the problem where the sonar values are received before the laser values. This is only an issue 
            // when the interface first starts up.
            if (!bLaserNotDisplayed)
            {
                sonarValues.Add(laserLines.Points[180]);
            }

            Point tempPointLeft = new Point();
            Point tempPointRight = new Point();
            for (int i = iSkipSonars; i < 21 - iSkipSonars; i++)
            {
                double x, y;
                // Step 1. 
                x = sonarRange[i] / dCosHalfCone;
                y = 0;
                // Step 2. 
                tempPointLeft.X = x * Math.Cos(sonarPose[i].Z + dRadiansHalfCone) - y * Math.Sin(sonarPose[i].Z + dRadiansHalfCone);
                tempPointLeft.Y = x * Math.Sin(sonarPose[i].Z + dRadiansHalfCone) + y * Math.Cos(sonarPose[i].Z + dRadiansHalfCone);
                tempPointRight.X = x * Math.Cos(sonarPose[i].Z - dRadiansHalfCone) - y * Math.Sin(sonarPose[i].Z - dRadiansHalfCone);
                tempPointRight.Y = x * Math.Sin(sonarPose[i].Z - dRadiansHalfCone) + y * Math.Cos(sonarPose[i].Z - dRadiansHalfCone);
                // Step 3.
                sonarPoints[i, 0].X = sonarPose[i].X + tempPointLeft.X + i2m(-8);
                sonarPoints[i, 0].Y = sonarPose[i].Y + tempPointLeft.Y + i2m(-12.5);
                sonarPoints[i, 0].Z = 0.2;
                sonarPoints[i, 1].X = sonarPose[i].X + tempPointRight.X + i2m(-8);
                sonarPoints[i, 1].Y = sonarPose[i].Y + tempPointRight.Y + i2m(-12.5);
                sonarPoints[i, 1].Z = 0.2;
                // Final step.
                sonarValues.Add(sonarPoints[i, 1]);
                sonarValues.Add(sonarPoints[i, 0]);
            }

            // This is to avoid the problem where the sonar values are received before the laser values. This is only an issue 
            // when the interface first starts up.
            if (!bLaserNotDisplayed)
            {
                sonarValues.Add(laserLines.Points[0]);
            }
            sonarLines.Points = sonarValues;
        }
        #endregion

        #region CreateJr
        void createAxisX()
        {
            // Main X-axis line
            xAxis[0] = new WireLine();
            xAxis[0].Point1 = new Point3D(i2m(-13) - 1, 0, 0);
            xAxis[0].Point2 = new Point3D(i2m(13) + 1, 0, 0);

            // 1 meter right mark
            xAxis[1] = new WireLine();
            xAxis[1].Point1 = new Point3D(i2m(13) + 1, i2m(-7.5), 0);
            xAxis[1].Point2 = new Point3D(i2m(13) + 1, i2m(7.5), 0);

            // 1 meter left mark
            xAxis[2] = new WireLine();
            xAxis[2].Point1 = new Point3D(i2m(-13) - 1, i2m(-7.5), 0);
            xAxis[2].Point2 = new Point3D(i2m(-13) - 1, i2m(7.5), 0);

            // 0.5 meter right mark
            xAxis[3] = new WireLine();
            xAxis[3].Point1 = new Point3D(i2m(13) + 0.5, i2m(-5), 0);
            xAxis[3].Point2 = new Point3D(i2m(13) + 0.5, i2m(5), 0);

            // 0.5 meter left mark
            xAxis[4] = new WireLine();
            xAxis[4].Point1 = new Point3D(i2m(-13) - 0.5, i2m(-5), 0);
            xAxis[4].Point2 = new Point3D(i2m(-13) - 0.5, i2m(5), 0);

            // 0.25 meter right mark
            xAxis[5] = new WireLine();
            xAxis[5].Point1 = new Point3D(i2m(13) + 0.25, i2m(-2.5), 0);
            xAxis[5].Point2 = new Point3D(i2m(13) + 0.25, i2m(2.5), 0);

            // 0.25 meter left mark
            xAxis[6] = new WireLine();
            xAxis[6].Point1 = new Point3D(i2m(-13) - 0.25, i2m(-2.5), 0);
            xAxis[6].Point2 = new Point3D(i2m(-13) - 0.25, i2m(2.5), 0);

            // 0.75 meter right mark
            xAxis[7] = new WireLine();
            xAxis[7].Point1 = new Point3D(i2m(13) + 0.75, i2m(-2.5), 0);
            xAxis[7].Point2 = new Point3D(i2m(13) + 0.75, i2m(2.5), 0);

            // 0.75 meter left mark
            xAxis[8] = new WireLine();
            xAxis[8].Point1 = new Point3D(i2m(-13) - 0.75, i2m(-2.5), 0);
            xAxis[8].Point2 = new Point3D(i2m(-13) - 0.75, i2m(2.5), 0);

            foreach (WireLine l in xAxis)
            {
                l.Thickness = 1.5;
                l.Color = Colors.Black;
            }

            viewport.Children.Add(xAxis[0]);
            viewport.Children.Add(xAxis[1]);
            viewport.Children.Add(xAxis[2]);
            viewport.Children.Add(xAxis[3]);
            viewport.Children.Add(xAxis[4]);
            viewport.Children.Add(xAxis[5]);
            viewport.Children.Add(xAxis[6]);
            viewport.Children.Add(xAxis[7]);
            viewport.Children.Add(xAxis[8]);
        }

        void createAxisY()
        {
            // Main Y-axis line
            yAxis[0] = new WireLine();
            yAxis[0].Point1 = new Point3D(0, i2m(22.5) + 1, 0);
            yAxis[0].Point2 = new Point3D(0, i2m(-17.5) - 1, 0);

            // 1 meter mark top
            yAxis[1] = new WireLine();
            yAxis[1].Point1 = new Point3D(i2m(-7.5), i2m(22.5) + 1, 0);
            yAxis[1].Point2 = new Point3D(i2m(7.5), i2m(22.5) + 1, 0);

            // 1 meter mark bottom
            yAxis[2] = new WireLine();
            yAxis[2].Point1 = new Point3D(i2m(-7.5), i2m(-17.5) - 1, 0);
            yAxis[2].Point2 = new Point3D(i2m(7.5), i2m(-17.5) - 1, 0);

            // 0.5 meter mark top
            yAxis[3] = new WireLine();
            yAxis[3].Point1 = new Point3D(i2m(-5), i2m(22.5) + 0.5, 0);
            yAxis[3].Point2 = new Point3D(i2m(5), i2m(22.5) + 0.5, 0);

            // 0.5 meter mark bottom
            yAxis[4] = new WireLine();
            yAxis[4].Point1 = new Point3D(i2m(-5), i2m(-17.5) - 0.5, 0);
            yAxis[4].Point2 = new Point3D(i2m(5), i2m(-17.5) - 0.5, 0);

            // 0.25 meter mark top
            yAxis[5] = new WireLine();
            yAxis[5].Point1 = new Point3D(i2m(-2.5), i2m(22.5) + 0.25, 0);
            yAxis[5].Point2 = new Point3D(i2m(2.5), i2m(22.5) + 0.25, 0);

            // 0.75 meter mark top
            yAxis[6] = new WireLine();
            yAxis[6].Point1 = new Point3D(i2m(-2.5), i2m(22.5) + 0.75, 0);
            yAxis[6].Point2 = new Point3D(i2m(2.5), i2m(22.5) + 0.75, 0);

            // 0.25 meter mark bottom
            yAxis[7] = new WireLine();
            yAxis[7].Point1 = new Point3D(i2m(-2.5), i2m(-17.5) - 0.25, 0);
            yAxis[7].Point2 = new Point3D(i2m(2.5), i2m(-17.5) - 0.25, 0);

            // 0.75 meter mark bottom
            yAxis[8] = new WireLine();
            yAxis[8].Point1 = new Point3D(i2m(-2.5), i2m(-17.5) - 0.75, 0);
            yAxis[8].Point2 = new Point3D(i2m(2.5), i2m(-17.5) - 0.75, 0);

            foreach (WireLine l in yAxis)
            {
                l.Thickness = 1.5;
                l.Color = Colors.Black;
            }

            viewport.Children.Add(yAxis[0]);
            viewport.Children.Add(yAxis[1]);
            viewport.Children.Add(yAxis[2]);
            viewport.Children.Add(yAxis[3]);
            viewport.Children.Add(yAxis[4]);
            viewport.Children.Add(yAxis[5]);
            viewport.Children.Add(yAxis[6]);
            viewport.Children.Add(yAxis[7]);
            viewport.Children.Add(yAxis[8]);
        }

        void createRearRightTyre()
        {
            rearRightTyre.Width = i2m(5);
            rearRightTyre.Height = i2m(12);
            rearRightTyre.Depth = 0;
            rearRightTyre.Origin = new Point3D(i2m(8), i2m(-13.5), 0.01);
            rearRightTyre.Material = blackMaterial;
            viewport.Children.Add(rearRightTyre);
        }

        void createRearLeftTyre()
        {
            rearLeftTyre.Width = i2m(5);
            rearLeftTyre.Height = i2m(12);
            rearLeftTyre.Depth = 0;
            rearLeftTyre.Origin = new Point3D(i2m(-13), i2m(-13.5), 0.01);
            rearLeftTyre.Material = blackMaterial;
            viewport.Children.Add(rearLeftTyre);
        }

        void createFrontRightTyre()
        {
            frontRightTyre.Width = i2m(5);
            frontRightTyre.Height = i2m(12);
            frontRightTyre.Depth = 0;
            frontRightTyre.Origin = new Point3D(i2m(8), i2m(1.5), 0.01);
            frontRightTyre.Material = blackMaterial;
            viewport.Children.Add(frontRightTyre);
        }

        void createFrontLeftTyre()
        {
            frontLeftTyre.Width = i2m(5);
            frontLeftTyre.Height = i2m(12);
            frontLeftTyre.Depth = 0;
            frontLeftTyre.Origin = new Point3D(i2m(-13), i2m(1.5), 0.01);
            frontLeftTyre.Material = blackMaterial;
            viewport.Children.Add(frontLeftTyre);
        }

        void createFrontBumper()
        {
            frontBumper.Width = i2m(16);
            frontBumper.Height = i2m(10);
            frontBumper.Depth = 0;
            frontBumper.Origin = new Point3D(i2m(-8), i2m(12.5), 0.01);
            frontBumper.Material = blackMaterial;
            viewport.Children.Add(frontBumper);
        }

        void createRearBumper()
        {
            rearBumper.Width = i2m(16);
            rearBumper.Height = i2m(5);
            rearBumper.Depth = 0;
            rearBumper.Origin = new Point3D(i2m(-8), i2m(-17.5), 0.01);
            rearBumper.Material = blackMaterial;
            viewport.Children.Add(rearBumper);
        }

        void createMainBody()
        {
            mainBody.Width = i2m(16);
            mainBody.Height = i2m(25);
            mainBody.Depth = 0;
            mainBody.Origin = new Point3D(i2m(-8), i2m(-12.5), 0.01);
            mainBody.Material = redMaterial;
            viewport.Children.Add(mainBody);
        }

        void createLaserLines()
        {
            laserLines.Thickness = 2;
            laserLines.Color = Colors.White;
            laserLines.Rounding = 1;
            viewport.Children.Add(laserLines);
        }

        void createLaserUrgLines()
        {
            laserUrgLines.Thickness = 2;
            laserUrgLines.Color = Colors.White;
            laserUrgLines.Rounding = 1;
            viewport.Children.Add(laserUrgLines);
        }

        void createSonarLines()
        {
            sonarPose[0].X = 0; sonarPose[0].Y = 21.5; sonarPose[0].Z = 135;
            sonarPose[1].X = 0; sonarPose[1].Y = 19; sonarPose[1].Z = 150;
            sonarPose[2].X = 0; sonarPose[2].Y = 17; sonarPose[2].Z = 165;
            sonarPose[3].X = 0; sonarPose[3].Y = 15; sonarPose[3].Z = 180;

            sonarPose[4].X = 0; sonarPose[4].Y = 10; sonarPose[4].Z = 180;
            sonarPose[5].X = 0; sonarPose[5].Y = 8; sonarPose[5].Z = 195;
            sonarPose[6].X = 0; sonarPose[6].Y = 6; sonarPose[6].Z = 210;
            sonarPose[7].X = 0; sonarPose[7].Y = 3.5; sonarPose[7].Z = 225;

            sonarPose[8].X = 4; sonarPose[8].Y = 0; sonarPose[8].Z = 240;
            sonarPose[9].X = 6; sonarPose[9].Y = 0; sonarPose[9].Z = 255;
            sonarPose[10].X = 8; sonarPose[10].Y = 0; sonarPose[10].Z = 270;
            sonarPose[11].X = 10; sonarPose[11].Y = 0; sonarPose[11].Z = 285;
            sonarPose[12].X = 12; sonarPose[12].Y = 0; sonarPose[12].Z = 300;

            sonarPose[13].X = 16; sonarPose[13].Y = 3.5; sonarPose[13].Z = 315;
            sonarPose[14].X = 16; sonarPose[14].Y = 6; sonarPose[14].Z = 330;
            sonarPose[15].X = 16; sonarPose[15].Y = 8; sonarPose[15].Z = 345;
            sonarPose[16].X = 16; sonarPose[16].Y = 10; sonarPose[16].Z = 0;

            sonarPose[17].X = 16; sonarPose[17].Y = 15; sonarPose[17].Z = 0;
            sonarPose[18].X = 16; sonarPose[18].Y = 17; sonarPose[18].Z = 15;
            sonarPose[19].X = 16; sonarPose[19].Y = 19; sonarPose[19].Z = 30;
            sonarPose[20].X = 16; sonarPose[20].Y = 21.5; sonarPose[20].Z = 45;

            // Convert to radians.
            for (int i = 0; i < sonarPose.Length; i++)
            {
                sonarPose[i].Z = sonarPose[i].Z * Math.PI / 180;
            }

            // Convert to meters.
            for (int i = 0; i < sonarPose.Length; i++)
            {
                sonarPose[i].X = i2m(sonarPose[i].X);
                sonarPose[i].Y = i2m(sonarPose[i].Y);
            }

            sonarLines.Points = null;
            sonarLines.Thickness = 2;
            sonarLines.Color = Colors.White;
            sonarLines.Rounding = 1;
            viewport.Children.Add(sonarLines);
        }

        void createUserVector()
        {
            userVector.Point1 = new Point3D(0, 0, 0.02);
            userVector.Point2 = new Point3D(0, 0, 0.02);
            userVector.Thickness = 10;
            userVector.Color = Colors.White;

            robotVector.Point1 = new Point3D(0, 0, 0.02);
            robotVector.Point2 = new Point3D(1, 0, 0.02);
            robotVector.Thickness = 10;
            robotVector.Color = Colors.Pink;

            finalVector.Point1 = new Point3D(0, 0, 0.02);
            finalVector.Point2 = new Point3D(1, 0, 0.02);
            finalVector.Thickness = 10;
            finalVector.Color = Colors.Blue;

            //viewport.Children.Add(robotVector);
            //viewport.Children.Add(userVector);
            //viewport.Children.Add(finalVector);
        }
        #endregion

        #region Event_Handling
        void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Lights
            ambientLight.Color = Colors.White;
            ModelVisual3D modelVisual3D = new ModelVisual3D();
            Model3DGroup model3DGroup = new Model3DGroup();
            model3DGroup.Children.Add(ambientLight);
            modelVisual3D.Content = model3DGroup;
            viewport.Children.Add(modelVisual3D);

            // Camera
            perspectiveCamera.Position = new Point3D(0, 0, 5);
            perspectiveCamera.UpDirection = new Vector3D(0, 1, 0);
            perspectiveCamera.LookDirection = new Vector3D(0, 0, -10);
            perspectiveCamera.FieldOfView = 45;

            viewport.Camera = perspectiveCamera;
        }

        void dockPanel1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            dRotateValue += e.Delta / 10;
            foreach (Visual3D v in viewport.Children.ToArray())
            {
                RotateTransform3D rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), dRotateValue));
                v.Transform = rotateTransform;
            }
        }

        void dockPanel1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // The value of 3.5 and -3.5 is selected such that the distance of the robot from the camera, 
            // remains the same as before. 
            // Top-down view: distance from camera = sqrt( 0*0 + 5*5 ) = 5.
            // Perspective view: distance from camera = sqrt( 3.5*3.5 + (-3.5)*(-3.5) ) = 5.
            perspectiveCamera.Position = new Point3D(0, -3.5, 3.5);
            perspectiveCamera.LookDirection = new Vector3D(0, 10, -10);

            //DrawVectors(new Point(1, 1), new Point(1, 1), new Point(1, 1));
        }

        void dockPanel1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            perspectiveCamera.Position = new Point3D(0, 0, 5);
            perspectiveCamera.LookDirection = new Vector3D(0, 0, -10);
        }
        #endregion

        double i2m(double inches)
        {
            return 0.0254 * inches;
        }
    }
}
