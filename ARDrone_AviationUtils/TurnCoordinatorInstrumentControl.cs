/*****************************************************************************/
/* Project  : AviationInstruments                                  */
/* File     : TurnCoordinatorInstrumentControl.cs                            */
/* Version  : 1                                                              */
/* Language : C#                                                             */
/* Summary  : The turn coordinator instrument control                        */
/* Creation : 15/06/2008                                                     */
/* Autor    : Guillaume CHOUTEAU                                             */
/* History  :                                                                */
/*****************************************************************************/

using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Text;
using System.Data;

namespace AviationInstruments
{
    public class TurnCoordinatorInstrumentControl : InstrumentControl
    {
        #region Fields

        // Parameters
        float TurnRate;
        float TurnQuality;

        // Images
        Bitmap bmpCadran = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.TurnCoordinator_Background);
        Bitmap bmpBall = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.TurnCoordinatorBall);
        Bitmap bmpAircraft = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.TurnCoordinatorAircraft);
        Bitmap bmpMarks = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.TurnCoordinatorMarks);

        #endregion

        #region Contructor

        /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public TurnCoordinatorInstrumentControl()
		{
			// Double bufferisation
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint |
				ControlStyles.AllPaintingInWmPaint, true);
        }

        #endregion

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion

        #region Paint

        protected override void OnPaint(PaintEventArgs pe)
        {
            // Calling the base class OnPaint
            base.OnPaint(pe);

            // Pre Display computings
            Point ptRotationAircraft = new Point(150, 150);
            Point ptImgAircraft = new Point(57,114);
            Point ptRotationBall = new Point(150, -155);
            Point ptImgBall = new Point(136, 216);
            Point ptMarks = new Point(134, 216);

            bmpCadran.MakeTransparent(Color.Yellow);
            bmpBall.MakeTransparent(Color.Yellow);
            bmpAircraft.MakeTransparent(Color.Yellow);
            bmpMarks.MakeTransparent(Color.Yellow);

            double alphaAircraft = InterpolPhyToAngle(TurnRate,-6,6,-30,30);
            double alphaBall = InterpolPhyToAngle(TurnQuality, -10, 10, -11, 11);

            float scale = (float)this.Width / bmpCadran.Width;

            // diplay mask
            Pen maskPen = new Pen(this.BackColor, 30 * scale);
            pe.Graphics.DrawRectangle(maskPen, 0, 0, bmpCadran.Width * scale, bmpCadran.Height * scale);

            // display cadran
            pe.Graphics.DrawImage(bmpCadran, 0, 0, (float)(bmpCadran.Width * scale), (float)(bmpCadran.Height * scale));

            // display Ball
            RotateImage(pe,bmpBall, alphaBall, ptImgBall, ptRotationBall, scale);

            // display Aircraft
            RotateImage(pe, bmpAircraft, alphaAircraft, ptImgAircraft, ptRotationAircraft, scale);

            // display Marks
            pe.Graphics.DrawImage(bmpMarks, (int)(ptMarks.X * scale), (int)(ptMarks.Y * scale), bmpMarks.Width * scale, bmpMarks.Height * scale);

        }

        #endregion

        #region Methods

        /// <summary>
        /// Define the physical value to be displayed on the indicator
        /// </summary>
        /// <param name="aircraftTurnRate">The aircraft turn rate in °deg per minutes</param>
        /// <param name="aircraftTurnQuality">The aircraft turn quality</param>
        public void SetTurnCoordinatorParameters(float aircraftTurnRate, float aircraftTurnQuality)
        {
            TurnRate = aircraftTurnRate;
            TurnQuality = aircraftTurnQuality;

            this.Refresh();
        }

        #endregion
    }
}
