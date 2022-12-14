using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
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
using Path = System.IO.Path;
using UtilsNS;
using scripthea.viewer;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for cueEditorUC.xaml
    /// </summary>
    public partial class CueEditorUC : UserControl
    {
        List<CueItemUC> cues;
        public enum Mode { open, append, remove, clear, save}
        Mode mode { get { int k = Utils.EnsureRange(cbCommand.SelectedIndex, 0, cbCommand.Items.Count - 1);  return (Mode)k; } }
        public bool radioMode { get { return !mode.Equals(Mode.remove); } }
        public CueEditorUC()
        {
            InitializeComponent();
            cues = new List<CueItemUC>();
            AddCue(new CueItemUC("", radioMode));
        }
        private string filename = "";
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            if (cues.Count.Equals(0))
                AddCue(new CueItemUC("", radioMode));
            cues[0].radioChecked = true;
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }

        private int _selected; 
        public int selected
        {
            get 
            { 
                if (!radioMode) return -1;
                return _selected;
            }
            set // set from outside
            {
                if (!radioMode) { _selected = -1; return; }
                for (int i = 0; i < cues.Count; i++)
                {
                    cues[i].radioChecked = (i == value);
                }
                _selected = value;     
                if (value == 0) scrollViewer.ScrollToVerticalOffset(0);
            }
        }
        protected void Change(object sender, RoutedEventArgs e)
        {
            if (!radioMode) _selected = -1; 
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].rbChecked.Equals(sender)) { _selected = i; break; }
            }
        }

        private void AddCue(CueItemUC cue) // visual
        {
            cue.OnLog += new Utils.LogHandler(Log);
            cue.rbChecked.Checked += new RoutedEventHandler(Change); 
            cue.tbCue.IsReadOnly = false;
            spCues.Children.Add(cue); cues.Add(cue);
        }
        private void AddCue(List<string> cue) // internal
        {
            if (cue == null) { AddCue(new CueItemUC("",radioMode)); return; }
            AddCue(new CueItemUC(cue, radioMode));
        }
        private void AddCue(string cue) // internal
        {            
            AddCue(new CueItemUC(cue, radioMode));
        }
        private void RemoveAt(int idx)
        {
            if (!Utils.InRange(idx, 0, cues.Count - 1)) return;
            spCues.Children.RemoveAt(idx);
            cues.RemoveAt(idx);
        }
        private void Clear(bool complete)
        {
            int st = complete ? 0 : 1;
            while (!cues.Count.Equals(st))
                RemoveAt(st);
            if (cues.Count > 0) cues[0].cueText = ""; 
        }
        private void cbCommand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cues == null) return;
            foreach (var cue in cues)
                cue.radioMode = radioMode;
            if (mode.Equals(Mode.open) || mode.Equals(Mode.append)) cbOption.Visibility = Visibility.Visible;
            else cbOption.Visibility = Visibility.Collapsed;           
        }
        private void AppendFile()
        {
            bool inputKind = cbOption.SelectedIndex == 0;
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (inputKind) dialog.InitialDirectory = Path.Combine(Utils.basePath, "cues");
            else dialog.InitialDirectory = ImgUtils.defaultImageDepot;
            dialog.Multiselect = false;
            if (inputKind) // .cues
            { 
                dialog.DefaultExtension = ".cues";
                dialog.Filters.Add(new CommonFileDialogFilter("Cues text", "cues"));
            }
            else // image depot
            {
                dialog.DefaultExtension = ".idf";
                dialog.Filters.Add(new CommonFileDialogFilter("Image depot folder", "idf"));
            }
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            filename = dialog.FileName;  
            if (inputKind) // .cues
            {
                if (!File.Exists(filename)) { Log("Err: no <" + filename + "> file found"); return; }
                List<string> cueText = new List<string>(File.ReadAllLines(filename));
                List<string> cueList = new List<string>();
                foreach (string ss in cueText)
                {
                    if (ss.Length > 1)
                        if (ss.Substring(0, 2).Equals("##")) continue;
                    if (ss.Trim().Equals("")) continue;
                    cueList.Add(ss.Trim());
                }
                List<string> ls = new List<string>();
                foreach (string ss in cueList)
                {
                    if (ss.Equals("---")) { AddCue(ls); ls.Clear(); }
                    else ls.Add(ss);
                }
            }
            else // .idf
            {                
                DepotFolder iDepot = new DepotFolder(Path.GetDirectoryName(filename));
                foreach (ImageInfo ii in iDepot.items)
                {
                    string prompt = ii.prompt;
                    int idx = prompt.IndexOf(opts.ModifPrefix);
                    if ((idx > -1) && (cbOption.SelectedIndex == 1)) prompt = prompt.Substring(0, idx);
                    AddCue(prompt);
                }         
                filename = "";           
            }
            if (selected == -1) selected = 0;
        }  
        private void Remove()
        {
            if (radioMode) return; int idx = 0;
            while (idx > -1)
            {
                idx = -1;
                for (int i = 0; i < cues.Count; i++)
                {
                    if (cues[i].boxChecked) { idx = i; break; }
                }
                RemoveAt(idx);
            }
        }
        private void SaveAs()
        {
            CommonSaveFileDialog dialog = new CommonSaveFileDialog();
            dialog.InitialDirectory = Path.Combine(Utils.basePath, "cues");
            dialog.DefaultExtension = ".cues";
            dialog.Filters.Add(new CommonFileDialogFilter("Cues text", "cues"));
            if (!filename.Equals("")) // cues file 
                dialog.DefaultFileName = filename;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            var ls = new List<string>();
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].cueText.Trim().Equals("")) continue;
                ls.AddRange(cues[i].cueTextAsList());
                ls.Add("---");
            }
            Utils.writeList(Path.ChangeExtension(dialog.FileName,".cues"), ls);
        }
        private void btnAddCue_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(btnAddCue))
            { 
                AddCue(""); selected = cues.Count - 1;
                scrollViewer.ScrollToVerticalOffset(spCues.ActualHeight);
            }
            else
            {
                if (Utils.InRange(selected, 0, cues.Count - 1))
                {
                    RemoveAt(selected);
                    selected = Utils.EnsureRange(selected, 0, cues.Count - 1);
                }
                else Log("err: index out of range");
            }
        }

        private void btnDoIt_Click(object sender, RoutedEventArgs e)
        {
            switch (mode)
            {
                case Mode.open: Clear(true); AppendFile(); selected = 0;
                    break;
                case Mode.append: int idx = selected; AppendFile(); selected = idx;
                    break;
                case Mode.clear: Clear(false); selected = 0;
                    break;
                case Mode.remove: Remove(); selected = 0;
                    break;
                case Mode.save: SaveAs();
                    break;
            }
        }

    }
}
