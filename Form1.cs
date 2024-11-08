using Resources = GModContentInstaller.Properties.Resources;

namespace GModContentInstaller
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async Task<bool> canBeEnabledAsync(ContentInfo contentInfo)
        {
            bool isReachable = await UrlChecker.IsUrlReachableAsync(contentInfo.Url);
            return isReachable;
        }

        private async Task loadInfo()
        {
            var urlDictionary = new UrlDictionary(Resources.urls);

            ContentInfo cssMaps = urlDictionary.GetContentInfoByName("CSS Maps");
            ContentInfo cssContent = urlDictionary.GetContentInfoByName("CSS Content");
            ContentInfo dodContent = urlDictionary.GetContentInfoByName("DOD Content");
            ContentInfo dodMaps = urlDictionary.GetContentInfoByName("DOD Maps");
            ContentInfo hl1Content = urlDictionary.GetContentInfoByName("HL1 Content");
            ContentInfo hl1Maps = urlDictionary.GetContentInfoByName("HL1 Maps");
            ContentInfo hl2Ep1Content = urlDictionary.GetContentInfoByName("HL2 Ep1 Content");
            ContentInfo hl2Ep1Maps = urlDictionary.GetContentInfoByName("HL2 Ep1 Maps");
            ContentInfo hl2Ep2Content = urlDictionary.GetContentInfoByName("HL2 Ep2 Content");
            ContentInfo hl2Ep2Maps = urlDictionary.GetContentInfoByName("HL2 Ep2 Maps");
            ContentInfo hl2ExtrasContent = urlDictionary.GetContentInfoByName("HL2 Extras Content");
            ContentInfo hl2ExtrasMaps = urlDictionary.GetContentInfoByName("HL2 Extras Maps");
            ContentInfo l4dContent = urlDictionary.GetContentInfoByName("L4D Content");
            ContentInfo l4dMaps = urlDictionary.GetContentInfoByName("L4D Maps");
            ContentInfo l4d2Content = urlDictionary.GetContentInfoByName("L4D2 Content");
            ContentInfo portal2Content = urlDictionary.GetContentInfoByName("Portal 2 Content");
            ContentInfo portal2Maps = urlDictionary.GetContentInfoByName("Portal 2 Maps");
            ContentInfo portalContent = urlDictionary.GetContentInfoByName("Portal Content");
            ContentInfo portalMaps = urlDictionary.GetContentInfoByName("Portal Maps");
            ContentInfo tf2Content = urlDictionary.GetContentInfoByName("TF2 Content");
            ContentInfo tf2Maps = urlDictionary.GetContentInfoByName("TF2 Maps");

            async Task UpdateContentAndMapsAsync(ContentInfo content, Guna.UI2.WinForms.Guna2HtmlLabel? contentLabel, Guna.UI2.WinForms.Guna2ToggleSwitch? contentButton, Guna.UI2.WinForms.Guna2HtmlLabel? mapLabel, Guna.UI2.WinForms.Guna2ToggleSwitch? mapButton)
            {
                if (contentLabel != null && contentButton != null)
                {
                    // Update content label and button asynchronously
                    bool canEnable = await canBeEnabledAsync(content);  // Asynchron warten auf Ergebnis
                    if (canEnable)
                    {
                        contentLabel.Text = $"Content ({content.DownloadSize}MB / {content.InstallSize}MB)";
                    }
                    else
                    {
                        contentLabel.ForeColor = Color.Red;
                        contentButton.Checked = false;
                        contentButton.Enabled = false;
                    }
                }

                if (mapLabel != null && mapButton != null)
                {
                    // Update map label and button asynchronously
                    bool canEnable = await canBeEnabledAsync(content);  // Asynchron warten auf Ergebnis
                    if (canEnable)
                    {
                        mapLabel.Text = $"Maps ({content.DownloadSize}MB / {content.InstallSize}MB)";
                    }
                    else
                    {
                        mapLabel.ForeColor = Color.Red;
                        mapButton.Checked = false;
                        mapButton.Enabled = false;
                    }
                }
            }

            var tasks = new List<Task>
            {
                UpdateContentAndMapsAsync(cssContent, cssLabelContent, cssButtonContent, null, null),
                UpdateContentAndMapsAsync(cssMaps, null, null, cssLabelMap, cssButtonMap),

                UpdateContentAndMapsAsync(dodContent, dodLabelContent, dodButtonContent, null, null),
                UpdateContentAndMapsAsync(dodMaps, null, null, dodLabelMap, dodButtonMap),

                UpdateContentAndMapsAsync(hl1Content, hlLabelContent, hlButtonContent, null, null),
                UpdateContentAndMapsAsync(hl1Maps, null, null, hlLabelMap, hlButtonMap),

                UpdateContentAndMapsAsync(hl2Ep1Content, hl2e1LabelContent, hl2e1ButtonContent, null, null),
                UpdateContentAndMapsAsync(hl2Ep1Maps, null, null, hl2e1LabelMap, hl2e1ButtonMap),

                UpdateContentAndMapsAsync(hl2Ep2Content, hl2e2LabelContent, hl2e2ButtonContent, null, null),
                UpdateContentAndMapsAsync(hl2Ep2Maps, null, null, hl2e2LabelMap, hl2e2ButtonMap),

                UpdateContentAndMapsAsync(hl2ExtrasContent, hl2_eLabelContent, hl2_eButtonContent, null, null),
                UpdateContentAndMapsAsync(hl2ExtrasMaps, null, null, hl2_eLabelMap, hl2_eButtonMap),

                UpdateContentAndMapsAsync(portalContent, portalLabelContent, portalButtonContent, null, null),
                UpdateContentAndMapsAsync(portalMaps, null, null, portalLabelMap, portalButtonMap),

                UpdateContentAndMapsAsync(portal2Content, portal2LabelContent, portal2ButtonContent, null, null),
                UpdateContentAndMapsAsync(portal2Maps, null, null, portal2LabelMap, portal2ButtonMap),

                UpdateContentAndMapsAsync(tf2Content, tf2LabelContent, tf2ButtonContent, null, null),
                UpdateContentAndMapsAsync(tf2Maps, null, null, tf2LabelMap, tf2ButtonMap),

                UpdateContentAndMapsAsync(l4dContent, l4d1LabelContent, l4d1ButtonContent, null, null),
                UpdateContentAndMapsAsync(l4dMaps, null, null, l4d1LabelMap, l4d1ButtonMap),

                UpdateContentAndMapsAsync(l4d2Content, l4d2LabelContent, l4d2ButtonContent, null, null),
            };
            await Task.WhenAll(tasks);
        }

        private async void pathDetectButton_Click(object sender, EventArgs e)
        {
            pathDetectButton.Enabled = false;
            string addonsPath = PathDetector.Select();
            pathShowLabel.Text = addonsPath;
            progressIndicator.Start();
            await loadInfo();
            downloadButton.Enabled = true;
            progressIndicator.Stop();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void guna2HtmlLabel1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
