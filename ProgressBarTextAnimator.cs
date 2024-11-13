namespace GModContentWizard
{
    /// <summary>
    /// Handles the animation of text on a Guna2ProgressBar.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ProgressBarTextAnimator"/> class.
    /// </remarks>
    /// <param name="progressBar">The progress bar to animate.</param>
    internal class ProgressBarTextAnimator(Guna2ProgressBar progressBar)
    {
        private readonly Guna2ProgressBar _progressBar = progressBar;
        private bool _isAnimating;
        private string _baseText = "Progress";
        private int _dotsCount = 0;

        /// <summary>
        /// Starts the text animation on the progress bar.
        /// </summary>
        /// <param name="baseText">The base text to display before the animation dots. Default is "Downloading".</param>
        public void StartAnimation(string baseText = "Downloading")
        {
            _baseText = baseText;

            if (_isAnimating)
                return;

            _isAnimating = true;
            AnimateText();
        }

        /// <summary>
        /// Stops the text animation on the progress bar.
        /// </summary>
        /// <param name="baseText">The text to display when the animation stops. Default is "Progress".</param>
        public void StopAnimation(string baseText = "Progress")
        {
            _isAnimating = false;
            _progressBar.Text = baseText;
        }

        /// <summary>
        /// Animates the text on the progress bar by appending dots to the base text.
        /// </summary>
        private async void AnimateText()
        {
            while (_isAnimating)
            {
                _progressBar.Text = _baseText + new string('.', _dotsCount);
                _dotsCount = (_dotsCount + 1) % 4;

                await Task.Delay(500);
            }
        }
    }
}
