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
        // controller
        GamePadState currentState;

        // ring buffer for rocks, main camera index, sub camera index
        int rockRing = 0, incrementValue;

        // initialize rock count values to 0
        int red = 0, orange = 0, yellow = 0, green = 0, blue = 0, purple = 0;

        private bool ringIsFree;

        public RockCounter()
        {
            InitializeComponent();
        }

        // stuff that happens when to move arond the ring buffer
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
        void rockIncrement()
        {
            switch (rockRing)
            {
                // change red count and display it
                case 0:
                    red = red + incrementValue;
                    if (red < 0)
                        red = 0;
                    RedCount.Text = red.ToString();
                    RedCountShadow.Text = red.ToString();
                    return;
                // change red count and display it
                case 1:
                    orange = orange + incrementValue;
                    if (orange < 0)
                        orange = 0;
                    OrangeCount.Text = orange.ToString();
                    OrangeCountShadow.Text = orange.ToString();
                    return;
                // change red count and display it
                case 2:
                    yellow = yellow + incrementValue;
                    if (yellow < 0)
                        yellow = 0;
                    YellowCount.Text = yellow.ToString();
                    YellowCountShadow.Text = yellow.ToString();
                    return;
                // change red count and display it
                case 3:
                    green = green + incrementValue;
                    if (green < 0)
                        green = 0;
                    GreenCount.Text = green.ToString();
                    GreenCountShadow.Text = green.ToString();
                    return;
                // change red count and display it
                case 4:
                    blue = blue + incrementValue;
                    if (blue < 0)
                        blue = 0;
                    BlueCount.Text = blue.ToString();
                    BlueCountShadow.Text = blue.ToString();
                    return;
                // change red count and display it
                case 5:
                    purple = purple + incrementValue;
                    if (purple == -1)
                        purple = 0;
                    PurpleCount.Text = purple.ToString();
                    PurpleCountShadow.Text = purple.ToString();
                    return;
            }
        }

        // dpad up functions
        public void DPadUpButton()
        {
            // if up is pressed
            if (currentState.DPad.Up == ButtonState.Pressed)
            {
                // run this ring stuff one time when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;

                    // set the increment value to 1
                    incrementValue = 1;
                    // call the function
                    rockIncrement();
                }
            }

            // allow ring stuff to run again when pressed
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;
        }

        // dpad left functions
        public void DPadLeftButton()
        {
            // if dpad left is pressed
            if (currentState.DPad.Left == ButtonState.Pressed)
            {
                // run this ring stuff once when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;
                    rockRing--;

                    if (rockRing < 0)
                        rockRing = 5;

                    ringSwitch();
                }
            }

            // allow ring stuff to run again 
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;
        }

        // dpad right functions
        public void DPadRightButton()
        {
            // if dpad right is pressed
            if (currentState.DPad.Right == ButtonState.Pressed)
            {
                // run this ring stuff once when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;
                    rockRing++;
                    rockRing = rockRing % 6;
                    ringSwitch();
                }
            }

            // allow ring stuff to run again when pressed again
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;
        }

        // dpad down functions
        public void DPadDownButton()
        {
            // if dpad down is pressed
            if (currentState.DPad.Down == ButtonState.Pressed)
            {
                // run this ring stuff when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;
                    // set the increment value to -1
                    incrementValue = -1;
                    // call this function
                    rockIncrement();
                }
            }

            // allow ring stuff to run again when pressed again
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;
        }

    }
}
