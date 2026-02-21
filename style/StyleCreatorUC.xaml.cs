using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Net.Http;
using scripthea.options;
using scripthea.viewer;
using scripthea.preview;
using UtilsNS;
using System.Diagnostics;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace scripthea.style
{
    /// <summary>
    /// Interaction logic for StyleMakerUC.xaml
    /// </summary>
    public partial class StyleCreatorUC : UserControl
    {
        public StyleCreatorUC()
        {
            InitializeComponent();
        }
        private Options opts;
        private LMstudioUC LMstudio { get => opts.llm.LMstudio; }
        public void Init(ref Options _opts)
        {
            opts = _opts;
            col_0.Width = new GridLength(opts.style.col_0); col_1.Width = new GridLength(opts.style.col_1);
            row_1.Height = new GridLength(opts.style.row_1); row_2.Height = new GridLength(opts.style.row_2);
            iPicker.Init(ref opts); iPicker.Configure('S', new List<string>(), "", "", "", false);
            iPicker.OnChangeDepot += new RoutedEventHandler(ChangeDepot);
            iPicker.OnSelectEvent += new TableViewUC.PicViewerHandler(PicSelect);
            ChangeDepot(null, null);

            dblTemperature.IsEnabled = false;
            dblTemperature.Minimum = 0; dblTemperature.Maximum = 1; dblTemperature.Interval = 0.1; dblTemperature.DoubleFormat = "F1"; dblTemperature.Value = opts.style.LMStemperature;
            tbQuestion.Text = opts.style.StyleQuery; tbMQuestion.Text = opts.style.StyleMQuery;
            dblTemperature.IsEnabled = true;
            intMaxTokens.Minimum = 5; intMaxTokens.Maximum = 500; intMaxTokens.Value = opts.style.LMSmax_tokens;

            refSet.Init(ref _opts);
            refSet.btnCopySelected.Click += new RoutedEventHandler(CopySelected); refSet.btnCopyChecked.Click += new RoutedEventHandler(CopySelected);
        }
        protected BitmapImage activeBitmap = null; protected int activeIdx = -1; protected ImageDepot activeIDepot = null;
        protected void ChangeDepot(object sender, RoutedEventArgs e) // allow button access by iPicker states
        {
        }
        protected void PicSelect(int idx, ImageDepot iDepot)
        {           
            activeBitmap = iPicker.image.Source as BitmapImage;
            activeIdx = idx; activeIDepot = iDepot;
        }
        protected void CopySelected(object sender, RoutedEventArgs e)
        {
            if (iPicker is null) return; if (iPicker.iDepot is null) return;
            if (!iPicker.iDepot.isEnabled) return;
            if (!Directory.Exists(iPicker.imageFolder)) { opts.Log("Error: directory <" + iPicker.imageFolder + "> is not there."); return; }
            PicItemUC ps = null;
            if (sender == refSet.btnCopySelected)
            {
                ps = refSet.AddImageInfo(iPicker.imageFolder, iPicker.iDepot.items[activeIdx]);
                if (ps is null) return;
                refSet.AddPicItem(ps);
            }
            if (sender == refSet.btnCopyChecked)
            {

            }
        }
        public void Finish()
        {
            opts.style.col_0 = col_0.ActualWidth;
            opts.style.col_1 = col_1.ActualWidth;
            opts.style.row_1 = row_1.ActualHeight;
            opts.style.row_2 = row_2.ActualHeight;
        }
        public void UpdateLLMVisuals()
        {
            if (opts is null) { IsLLMEnabled = false; return; }
            IsLLMEnabled = LMstudio != null && LMstudio.IsReady();
        }
        protected bool _IsLLMEnabled = false;
        public bool IsLLMEnabled
        {
            get => _IsLLMEnabled && LMstudio != null && LMstudio.Connected;
            set
            {
                btnRunLLMServer.Foreground = value ? Brushes.Gray : Brushes.Maroon; btnRunLLMServer.BorderBrush = value ? Brushes.Gray : Brushes.Maroon;
                btnAskLLM.Background = value ? Utils.ToSolidColorBrush("#FFF2FCDD") : Utils.ToSolidColorBrush("#FFFCE6CB");
                btnMAskLLM.Background = btnAskLLM.Background;
                _IsLLMEnabled = value;
            }
        }
        private async void btnRunLLMServer_Click(object sender, RoutedEventArgs e)
        {
            _ = new PopupText(btnRunLLMServer, "Launching...", 1);
            bool bb = await LMstudio.FullLaunchAsync(true, true);
            if (bb)
            {
                LMstudio.LmStudioCloseMonitor(ref LmProcess);
                if (LmProcess != null) LmProcess.Exited += new EventHandler(LmProcess_Exited);
            }
            IsLLMEnabled = LMstudio.IsReady();
        }
        Process LmProcess;
        private void LmProcess_Exited(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    IsLLMEnabled = false;
                    LMstudio.LmProcess_Exited();
                }));
        }
        private void dblTemperature_OnValueChanged(object sender, RoutedEventArgs e)
        {
            if (!dblTemperature.IsEnabled || opts is null) return;
            opts.style.LMStemperature = dblTemperature.Value;
            if (opts.llm.LMStemperature > 0.79) dblTemperature.Background = Brushes.Tomato;
            else dblTemperature.Background = null;
            opts.style.LMSmax_tokens = intMaxTokens.Value;
        }
        private void tbQuestion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!dblTemperature.IsEnabled || opts is null) return;
            opts.style.StyleQuery = tbQuestion.Text; opts.style.StyleMQuery = tbMQuestion.Text;
        }
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnCopy && tbReply.Text.Trim() != "")
            {
                Clipboard.SetText(tbReply.Text.Trim());
                _ = new PopupText(btnCopy, "Style copied");
            }
            if (sender == btnMCopy && tbMReply.Text.Trim() != "")
            {
                Clipboard.SetText(tbMReply.Text.Trim());
                _ = new PopupText(btnMCopy, "Style copied");
            }
        }
        private async void btnAskLLM_Click(object sender, RoutedEventArgs e)
        {
            tbStatus.Text = "Status: thinking..."; tbStatus.Foreground = Brushes.DarkGreen; 
            if (activeBitmap is null) { tbReply.Foreground = Brushes.Red; tbMReply.Text = ""; tbStatus.Text = "Error: no selected image found."; return; }
            string rep = await LMstudio.LMSclient.SimpleImageQueryAsync(tbQuestion.Text, activeBitmap, opts.llm.LMScontext, opts.style.LMStemperature, opts.style.LMSmax_tokens);
            if (rep is null) 
            { 
                tbStatus.Foreground = Brushes.Red; tbReply.Text = ""; tbStatus.Text = LMstudio.CheclLMSerror(); return;
            }
            tbReply.Text = rep; tbStatus.Foreground = Brushes.Black; tbStatus.Text = "Status: DONE"; Utils.DelayExec(5000, () => { tbStatus.Text = "Status: idle"; });
        }
        private async void btnMAskLLM_Click(object sender, RoutedEventArgs e)
        {
            tbMStatus.Text = "Status: thinking..."; tbMStatus.Foreground = Brushes.DarkGreen;
            if (!iPicker.isValid) { tbMStatus.Text = "Error: invalid image depot"; return; }
            List <BitmapImage> lbm = new List<BitmapImage>();
            List<Tuple<int, string, int, string>> lst = iPicker.ListOfTuples(true, false);
            if (lst.Count > 10)
                if (!Utils.ConfirmationMessageBox("Number of images seems too large (>10). Continue?")) return;
            foreach(var tp in lst)
            {
                string fn = Path.Combine(iPicker.imageFolder, tp.Item4);
                lbm.Add(ImgUtils.LoadBitmapImageFromFile(fn));
            }
            string rep = await LMstudio.LMSclient.MultiImageQueryAsync(tbQuestion.Text, lbm, opts.llm.LMScontext, opts.style.LMStemperature, opts.style.LMSmax_tokens);
            if (rep is null)
            {
                tbMStatus.Foreground = Brushes.Red; tbMReply.Text = ""; tbMStatus.Text = LMstudio.CheclLMSerror(); return;
            }
            tbMReply.Text = rep; tbStatus.Foreground = Brushes.Black; tbMStatus.Text = "Status: DONE"; Utils.DelayExec(5000, () => { tbMStatus.Text = "Status: idle"; });
        }
    }
}
