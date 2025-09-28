using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
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
using scripthea.master;
using scripthea.options;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for cueEditorUC.xaml
    /// </summary>
    public partial class CueEditorUC : UserControl
    {
        ObservableCollection<CueItemUC> cues;
        public enum Mode { open, append, remove, clear, save, saveFlat}
        Mode mode { get { int k = Utils.EnsureRange(cbCommand.SelectedIndex, 0, cbCommand.Items.Count - 1);  return (Mode)k; } }
        public bool radioMode { get { return !mode.Equals(Mode.remove); } }
        public CueEditorUC()
        {
            InitializeComponent();
            cues = new ObservableCollection<CueItemUC>();
            cues.CollectionChanged += new NotifyCollectionChangedEventHandler(NotifyCollectionChanged);
            int k = AddCue(new CueItemUC("", radioMode)); cues[k].index = k; 
        }
        private string filename = "";
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts; 
            if (cues.Count.Equals(0))
            {
                int k = AddCue(new CueItemUC("", radioMode)); cues[k].index = k;
            }
            cues[0].radioChecked = true;
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
        protected void TextChanged(object sender, TextChangedEventArgs e)
        {
            bool bb = false;
            foreach (CueItemUC cue in cues)
                bb |= cue.empty;
            if (!bb)
            {
                int sel = selected;
                AddCue(""); selected = sel;
                scrollViewer.ScrollToEnd();
            }
        }
        private void NotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lbCount.Content = cues.Count.ToString() + " cues";
        }
        private int AddCue(CueItemUC cue) // visual
        {
            cue.rbChecked.Checked += new RoutedEventHandler(Change);
            cue.tbCue.TextChanged += new TextChangedEventHandler(TextChanged);
            cue.tbCue.IsReadOnly = false;
            spCues.Children.Add(cue); cues.Add(cue);
            return cues.Count - 1; 
        }
        private string cleanUp(string cue)
        {                       
            return cue.Trim().Trim('\"').Trim();
        }
        private int AddCue(string cue) // one-line cue / internal 
        {
            return AddCue(new CueItemUC(cleanUp(cue), radioMode));
        }
        private int AddCue(List<string> cuesIn) // multiline cue / internal
        {
            if (cuesIn == null) { AddCue(new CueItemUC("",radioMode)); return cues.Count - 1; }
            return AddCue(new CueItemUC(cuesIn, radioMode));
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
            CommonOpenFileDialog dialog = new CommonOpenFileDialog(); dialog.Multiselect = false;
            if (cbOption.SelectedIndex == 4) // clipboard text 
            {
                List<string> ls = Utils.ReadMultilineTextFromClipboard();
                foreach (string cue in ls)
                    AddCue(cue);
                if (selected == -1) selected = 0; TextChanged(null, null);
                return;
            }
            if (cbOption.SelectedIndex == 3) // text file
            {
                dialog.DefaultExtension = ".txt";
                dialog.Filters.Add(new CommonFileDialogFilter("Text file", "txt"));
                if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
                List<string> ls = Utils.readList(dialog.FileName, true);
                foreach (string cue in ls)
                    AddCue(cue);
                if (selected == -1) selected = 0; TextChanged(null, null);
                return;
            }
            bool inputKind = cbOption.SelectedIndex == 0;
            if (inputKind) dialog.InitialDirectory = Path.Combine(Utils.basePath, "cues");
            else dialog.InitialDirectory = SctUtils.defaultImageDepot;            
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
                if (!File.Exists(filename)) { opts.Log("Error[96]: no <" + filename + "> file found"); return; }
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
                ImageDepot iDepot = new ImageDepot(Path.GetDirectoryName(filename));
                foreach (ImageInfo ii in iDepot.items)
                {
                    string prompt = ii.prompt;
                    int idx = prompt.IndexOf(opts.composer.ModifPrefix);
                    if ((idx > -1) && (cbOption.SelectedIndex == 1)) prompt = prompt.Substring(0, idx);
                    AddCue(prompt);
                }         
                filename = "";           
            }
            if (selected == -1) selected = 0; TextChanged(null, null);
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
        public bool newCues = false; 
        private void SaveAs(bool oneline = true) // flat is one line cue
        {
            var ls = new List<string>();
            for (int i = 0; i < cues.Count; i++)
            {               
                if (cues[i].cueText.Trim().Equals("")) continue;
                if (!cues[i].headerText.Equals("")) ls.Add("# {"+cues[i].headerText+"}");
                if (oneline) ls.Add(cues[i].cueTextAsString(false));
                else ls.AddRange(cues[i].cueTextAsList(false));
                if (!cues[i].footerText.Equals("")) ls.Add("# {" + cues[i].footerText + "}");
                ls.Add("---");
            }
            string fn = Path.GetFileName(Path.ChangeExtension(filename, null)); 
            filename = new InputBox("Cues filename in the active cues pool", fn, "").ShowDialog(); // name only
            if (filename.Equals("")) return;
            string folder = Directory.Exists(opts.composer.WorkCuesFolder) ? opts.composer.WorkCuesFolder : Path.Combine(Utils.basePath, "cues");
            filename = Path.Combine(folder, filename);
            Utils.writeList(Path.ChangeExtension(filename, ".cues"), ls);
            opts.Log("Saved cue file as <" + filename+">", Brushes.Tomato); newCues = true;
        }
        private void SaveFlatAs(bool oneline = true) // flat is one line cue
        {
            var ls = new List<string>();
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].cueText.Trim().Equals("")) continue;
                if (oneline) ls.Add(cues[i].cueTextAsString(false));
                else ls.AddRange(cues[i].cueTextAsList(false));               
            }
            CommonOpenFileDialog dialog = new CommonOpenFileDialog(); dialog.Multiselect = false;            
            dialog.DefaultExtension = ".txt";
            dialog.Filters.Add(new CommonFileDialogFilter("Text file", "txt"));
            dialog.InitialDirectory = Path.Combine(Utils.basePath, "cues");
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            string filename = Path.ChangeExtension(dialog.FileName, ".txt");
            Utils.writeList(filename, ls);
            opts.Log("Saved flat text in " + filename, Brushes.Tomato);  
        }
        private void btnAddCue_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(btnAddCue))
            { 
                AddCue(""); selected = cues.Count - 1;
                scrollViewer.ScrollToVerticalOffset(spCues.ActualHeight);
            }
            else // remove
            {
                if (Utils.InRange(selected, 0, cues.Count - 1))
                {
                    RemoveAt(selected);
                    selected = Utils.EnsureRange(selected, 0, cues.Count - 1);
                }
                else opts.Log("Error[54]: index out of range");
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
                case Mode.saveFlat: SaveFlatAs();
                    break;
            }
        }
        private void cbCommand_DropDownOpened(object sender, EventArgs e)
        {
            //if (!opts.general.debug) cbiSaveFlat.Visibility = Visibility.Collapsed;
        }
    }
}
