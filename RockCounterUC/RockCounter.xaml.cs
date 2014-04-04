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
using System.IO;

// for controller; don't forget to include Microsoft.Xna.Framework in References
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RockCounterUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class RockCounter : UserControl
    {
        string[] rocks = new string[] { "0", "0,", "0", "0", "0", "0" };

        public enum DPadDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        // initialize rock count values to 0
        int red = 0, orange = 0, yellow = 0, green = 0, blue = 0, purple = 0;

        // ring buffer for rocks, main camera index, sub camera index
        int rockRing = 0;

        bool rocks_restored;

        public RockCounter()
        {
            InitializeComponent();
        }

        // dpad up functions
        public void DPadButton(DPadDirection dir, bool state)
        {
            // if up is pressed
            if (state)
            {
                switch(dir)
                {
                    case DPadDirection.Up:
                    case DPadDirection.Down:
                        {
                            // call the function
                            rockIncrement((dir == DPadDirection.Up ? 1 : -1));
                        }
                        break;
                    case DPadDirection.Left:
                    case DPadDirection.Right:
                        {
                            rockRing += (dir == DPadDirection.Right ? 1 : -1);

                            rockRing = rockRing % 6;
                            if (rockRing < 0)
                                rockRing = 5;

                            ringSwitch();
                        }
                        break;
                }
            }
        }

        private void Rock_Restorer_Click(object sender, RoutedEventArgs e)
        {
            string[] rocks;
            rocks = rock_file_reader();
            string rock;
            for (int i = 0; i < 6; i++)
            {
                rock = rocks[i];
                int aux = int.Parse(rock);
                if (i == 0) { red = aux; RedCount.Text = red.ToString(); RedCountShadow.Text = red.ToString(); }
                if (i == 1) { orange = aux; OrangeCount.Text = orange.ToString(); OrangeCountShadow.Text = orange.ToString(); }
                if (i == 2) { yellow = aux; YellowCount.Text = yellow.ToString(); YellowCountShadow.Text = yellow.ToString(); }
                if (i == 3) { green = aux; GreenCount.Text = green.ToString(); GreenCountShadow.Text = green.ToString(); }
                if (i == 4) { blue = aux; BlueCount.Text = blue.ToString(); BlueCountShadow.Text = blue.ToString(); }
                if (i == 5) { purple = aux; PurpleCount.Text = purple.ToString(); PurpleCountShadow.Text = purple.ToString(); }
            }
            Rock_Restorer.IsEnabled = false;
            Rock_Restorer.Visibility = Visibility.Hidden;
            rocks_restored = true;
        }
        private string[] rock_file_reader()
        {
            string[] rocks = new string[] { "0", "0", "0", "0", "0", "0" };
            if (File.Exists(@"rocks.txt"))
            {
                StreamReader rreader = new StreamReader(@"rocks.txt");
                rocks = rreader.ReadToEnd().Split(',');
                rreader.Close();
            }
            return rocks;
        }
        private void rock_file_writer(string[] rocks)
        {
            StreamWriter rwriter = new StreamWriter(@"rocks.txt");
            foreach (string rock in rocks)
            {
                rwriter.Write(rock + ",");
            }
            rwriter.Close();
        }

        //
        //
        //  I WANT SO BADLY to do this with a 5-liner... but it's probably better if I let SOME of this be recognizable.
        //            -Eric
        void ringSwitch()
        {
            switch (rockRing)
            {
                // bold text block for red rocks and normalizes neighboring text blocks
                case 0:
                    PurpleTextBlock.FontWeight = FontWeights.Normal;
                    RedTextBlock.FontWeight = FontWeights.UltraBold;
                    OrangeTextBlock.FontWeight = FontWeights.Normal;

                    PurpleRock.Stroke = Brushes.Black;
                    RedRock.Stroke = Brushes.White;
                    OrangeRock.Stroke = Brushes.Black;

                    PurpleRock.StrokeThickness = 1;
                    RedRock.StrokeThickness = 4;
                    OrangeRock.StrokeThickness = 1;
                    return;
                // bold text block for orange rocks and normalizes neighboring text blocks
                case 1:
                    RedTextBlock.FontWeight = FontWeights.Normal;
                    OrangeTextBlock.FontWeight = FontWeights.UltraBold;
                    YellowTextBlock.FontWeight = FontWeights.Normal;

                    RedRock.Stroke = Brushes.Black;
                    OrangeRock.Stroke = Brushes.White;
                    YellowRock.Stroke = Brushes.Black;

                    RedRock.StrokeThickness = 1;
                    OrangeRock.StrokeThickness = 4;
                    YellowRock.StrokeThickness = 1;
                    return;
                // bold text block for yellow rocks and normalizes neighboring text blocks
                case 2:
                    OrangeTextBlock.FontWeight = FontWeights.Normal;
                    YellowTextBlock.FontWeight = FontWeights.UltraBold;
                    GreenTextBlock.FontWeight = FontWeights.Normal;

                    OrangeRock.Stroke = Brushes.Black;
                    YellowRock.Stroke = Brushes.White;
                    GreenRock.Stroke = Brushes.Black;

                    OrangeRock.StrokeThickness = 1;
                    YellowRock.StrokeThickness = 4;
                    GreenRock.StrokeThickness = 1;
                    return;
                // bold text block for green rocks and normalizes neighboring text blocks
                case 3:
                    YellowTextBlock.FontWeight = FontWeights.Normal;
                    GreenTextBlock.FontWeight = FontWeights.UltraBold;
                    BlueTextBlock.FontWeight = FontWeights.Normal;

                    YellowRock.Stroke = Brushes.Black;
                    GreenRock.Stroke = Brushes.White;
                    BlueRock.Stroke = Brushes.Black;

                    YellowRock.StrokeThickness = 1;
                    GreenRock.StrokeThickness = 4;
                    BlueRock.StrokeThickness = 1;
                    return;
                // bold text block for blue rocks and normalizes neighboring text blocks
                case 4:
                    GreenTextBlock.FontWeight = FontWeights.Normal;
                    BlueTextBlock.FontWeight = FontWeights.UltraBold;
                    PurpleTextBlock.FontWeight = FontWeights.Normal;

                    GreenRock.Stroke = Brushes.Black;
                    BlueRock.Stroke = Brushes.White;
                    PurpleRock.Stroke = Brushes.Black;

                    GreenRock.StrokeThickness = 1;
                    BlueRock.StrokeThickness = 4;
                    PurpleRock.StrokeThickness = 1;
                    return;
                // bold text block for purple rocks and normalizes neighboring text blocks
                case 5:
                    BlueTextBlock.FontWeight = FontWeights.Normal;
                    PurpleTextBlock.FontWeight = FontWeights.UltraBold;
                    RedTextBlock.FontWeight = FontWeights.Normal;

                    BlueRock.Stroke = Brushes.Black;
                    PurpleRock.Stroke = Brushes.White;
                    RedRock.Stroke = Brushes.Black;

                    BlueRock.StrokeThickness = 1;
                    PurpleRock.StrokeThickness = 4;
                    RedRock.StrokeThickness = 1;
                    return;
            }
        }

        // the function that changes rock count
        void rockIncrement(int incrementValue)
        {
            if (!File.Exists(@"rocks.txt") && !rocks_restored) rocks_restored = true;
            if (rocks_restored == false)
            {
                rock_file_writer(rocks); 
                rocks_restored = true;
                Rock_Restorer.Visibility = Visibility.Collapsed;
            }
            switch (rockRing)
            {
                // change red count and display it
                case 0:
                    red = red + incrementValue;
                    if (red < 0)
                        red = 0;
                    RedCount.Text = red.ToString();
                    RedCountShadow.Text = red.ToString();
                    rocks[0] = red.ToString();
                    rock_file_writer(rocks);
                    return;
                // change red count and display it
                case 1:
                    orange = orange + incrementValue;
                    if (orange < 0)
                        orange = 0;
                    OrangeCount.Text = orange.ToString();
                    OrangeCountShadow.Text = orange.ToString();
                    rocks[1] = orange.ToString();
                    rock_file_writer(rocks);
                    return;
                // change red count and display it
                case 2:
                    yellow = yellow + incrementValue;
                    if (yellow < 0)
                        yellow = 0;
                    YellowCount.Text = yellow.ToString();
                    YellowCountShadow.Text = yellow.ToString();
                    rocks[2] = yellow.ToString();
                    rock_file_writer(rocks);
                    return;
                // change red count and display it
                case 3:
                    green = green + incrementValue;
                    if (green < 0)
                        green = 0;
                    GreenCount.Text = green.ToString();
                    GreenCountShadow.Text = green.ToString();
                    rocks[3] = green.ToString();
                    rock_file_writer(rocks);
                    return;
                // change red count and display it
                case 4:
                    blue = blue + incrementValue;
                    if (blue < 0)
                        blue = 0;
                    BlueCount.Text = blue.ToString();
                    BlueCountShadow.Text = blue.ToString();
                    rocks[4] = blue.ToString();
                    rock_file_writer(rocks);
                    return;
                // change red count and display it
                case 5:
                    purple = purple + incrementValue;
                    if (purple == -1)
                        purple = 0;
                    PurpleCount.Text = purple.ToString();
                    PurpleCountShadow.Text = purple.ToString();
                    rocks[5] = purple.ToString();
                    rock_file_writer(rocks);
                    return;
            }
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid g = (sender as Grid);
            if (g == null) return; //WOOPSIE
            int index = Grid.GetColumn(g);
            rockRing = index;
            ringSwitch();
            int dif=0;
            if (e.ChangedButton == MouseButton.Left) dif=1;
            else if (e.ChangedButton == MouseButton.Right) dif=-1;
            if (dif != 0)
                rockIncrement(dif);
        }
    }
}
