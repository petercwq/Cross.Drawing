using System;
using System.Windows.Forms;

namespace DemoWinForm
{
    public partial class fmMain : Form
    {
        #region Initialize
        public fmMain()
        {
            InitializeComponent();
        }

        private void fmMain_Load(object sender, EventArgs e)
        {

        }
        #endregion

        #region Primitives
        private void btnPrimitives_Click(object sender, EventArgs e)
        {
            fmPrimitiveRendering fm = new fmPrimitiveRendering();
            fm.Show();
        }
        #endregion

        #region Transformations
        private void btnTransformations_Click(object sender, EventArgs e)
        {
            fmTransformDemo fm = new fmTransformDemo();
            fm.Show();
        }
        #endregion

        #region Fills
        private void btnFills_Click(object sender, EventArgs e)
        {
            //new fmFillDemo().Show();
            new fmFill().Show();
        }
        #endregion

        #region Gamma Correction
        private void btnGammaCorrection_Click(object sender, EventArgs e)
        {
            fmGammaCorrection fm = new fmGammaCorrection();
            fm.Show();
        }
        #endregion

        #region Pixel Buffer
        private void btnPixelBuffer_Click(object sender, EventArgs e)
        {
            fmPixelBufferDemo fm = new fmPixelBufferDemo();
            fm.Show();
        }
        #endregion

        #region Opacity Mask
        private void btnOpacityMask_Click(object sender, EventArgs e)
        {
            fmOpacityMask fm = new fmOpacityMask();
            fm.Show();
        }
        #endregion
    }
}
