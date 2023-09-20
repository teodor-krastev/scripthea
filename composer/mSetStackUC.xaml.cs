using Newtonsoft.Json;
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

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for mSetStackUC.xaml
    /// </summary>
    public partial class mSetStackUC : UserControl
    {
        private const string resetName = "Reset";
        private Options opts;
        private List<mSetUC> mSets;
        private List<ModifListUC> modifLists;
        public string mSetFilename;
        public mSetStackUC()
        {
            InitializeComponent();            
        }
        public void Init(ref Options _opts, ref List<ModifListUC> _modifLists, string ModifiersFolder)
        {
            opts = _opts;
            mSets = new List<mSetUC>();
            mSets.Clear();
            mSets.Add(new mSetUC(resetName, new List<Tuple<string, string, ModifStatus>>()) { Height = 20 , Width = mSetListBox.ActualWidth }); mSets[0].ReadOnly = true; VisualUpdate();

            modifLists = _modifLists;
            if (!Directory.Exists(ModifiersFolder)) { Utils.TimedMessageBox("No modifiers directory: "+ ModifiersFolder); return; } 
            mSetFilename = Path.Combine(ModifiersFolder, "mSets.json");
            if (!File.Exists(mSetFilename)) return;
            Dictionary<string, List<Tuple<string, string, ModifStatus>>> mSetsContent = JsonConvert.DeserializeObject<Dictionary<string, List<Tuple<string, string, ModifStatus>>>>(File.ReadAllText(mSetFilename));
            foreach (var pair in mSetsContent) mSets.Add(new mSetUC(pair.Key, pair.Value) { Height = 20, Width = mSetListBox.ActualWidth }); VisualUpdate();
        }
        public void Finish()
        {
            Dictionary<string, List<Tuple<string, string, ModifStatus>>> mSetsContent = new Dictionary<string, List<Tuple<string, string, ModifStatus>>>();
            foreach (mSetUC ms in mSets) 
                if (!ms.title.Equals(resetName)) mSetsContent.Add(ms.title, ms.mSet);
            File.WriteAllText(mSetFilename, JsonConvert.SerializeObject(mSetsContent));
        }
        public mSetUC mSetByName(string mSetName)
        {
            foreach (mSetUC mSet in mSets)
                if (mSet.title.Equals(mSetName)) return mSet;
            return null;
        }
        public List<Tuple<string, string, ModifStatus>> GetModifs(bool all = false) // read modifiers state: cat, modif, status
        {
            List<Tuple<string, string, ModifStatus>> mdfs = new List<Tuple<string, string, ModifStatus>>();
            Tuple<string, string, ModifStatus> mdf;
            foreach (ModifListUC sm in modifLists)
            {
                if (!sm.isChecked) continue;
                foreach (ModifItemUC mi in sm.modifList)
                {
                    mdf = new Tuple<string, string, ModifStatus>(sm.CategoryName, mi.Text, mi.modifStatus);
                    if (all) mdfs.Add(mdf);
                    else
                    {
                        if (!mi.modifStatus.Equals(ModifStatus.Off)) mdfs.Add(mdf);
                    }
                }
            }
            return mdfs;
        }
        private void SetModifs_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is mSetUC)) return;
            if (!SetModifs((sender as mSetUC).mSet, chkAdd.IsChecked.Value)) Utils.TimedMessageBox("Error: Not able to set all modifiers"); 
        }
        public bool SetModifs(List<Tuple<string, string, ModifStatus>> mdfs, bool add) // write to modifiers
        {
            bool bb = true;      
            if (!add)            
                foreach (ModifListUC sm in modifLists) sm.Reset();            
            foreach (var mdf in mdfs)
            {
                ModifListUC wsm = null;
                foreach (ModifListUC sm in modifLists) 
                {
                    if (sm.CategoryName.Equals(mdf.Item1)) { wsm = sm; break; } 
                }
                if (wsm == null) { bb = false; continue; }
                wsm.isVisible = true; wsm.isChecked = true;
                ModifItemUC mi = wsm.modifByName(mdf.Item2);
                if (mi == null) { bb = false; continue; }
                mi.modifStatus = mdf.Item3;
            }
            return bb;
        }
        private void VisualUpdate()
        {
            mSetListBox.Items.Clear();
            foreach (mSetUC mSet in mSets)
            {
                mSetListBox.Items.Add(mSet);
                mSet.MouseDoubleClick -= SetModifs_Click;
                mSet.MouseDoubleClick += SetModifs_Click;
            }
        }
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            List<Tuple<string, string, ModifStatus>> mSet = GetModifs();
            if (mSet.Count == 0) { Utils.TimedMessageBox("No checked modifiers to be saved."); return; }
            string newItem = new InputBox("New mSet name", "", "Text input").ShowDialog();
            if (newItem == "") return;
            if (mSetByName(newItem) != null) { Utils.TimedMessageBox("Error: <"+newItem+"> already exits."); return; }

            mSets.Add(new mSetUC(newItem, mSet)); VisualUpdate();
        }
        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            int k = mSetListBox.SelectedIndex;
            if (!Utils.InRange(k, 0, mSets.Count - 1))
            {
                Utils.TimedMessageBox("Error: No mSet is selected."); return;
            }
            if (mSets[k].ReadOnly)
            {
                Utils.TimedMessageBox("Error: <" +  mSets[k].title + "> mSet is read-only."); return;
            }
            if (!Utils.ConfirmationMessageBox("Do you want to remove <" + mSets[k].title + "> mSet ?")) return;
            //Utils.TimedMessageBox(mSets[k].title+" mSet has been removed.");
            mSets.RemoveAt(k); VisualUpdate();
            mSetListBox.SelectedIndex = Utils.EnsureRange(k, 0, mSets.Count - 1);                      
        }
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            int k = mSetListBox.SelectedIndex;
            if (!Utils.InRange(k, 0, mSets.Count - 1))
            {
                Utils.TimedMessageBox("Error: No mSet is selected."); return;
            }
            if (mSets[k].ReadOnly)
            {
                Utils.TimedMessageBox("Error: <" + mSets[k].title + "> mSet is read-only."); return;
            }
            List<Tuple<string, string, ModifStatus>> mSet = GetModifs();
            if (mSet.Count == 0)
            {
                Utils.TimedMessageBox("Error: No modifiers checked."); return;
            }
            mSets[k].mSet = mSet;
            Utils.TimedMessageBox("<"+mSets[k].title + "> mSet has been updated.");
        }
        private void mSetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool bb = mSetListBox.SelectedIndex > -1;
            btnMinus.IsEnabled = bb; btnUpdate.IsEnabled = bb;
        }
        private void mSetListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int k = mSetListBox.SelectedIndex;
                if (!Utils.InRange(k, 0, mSets.Count - 1))
                {
                    Utils.TimedMessageBox("Error: No mSet is selected."); return;
                }
                SetModifs_Click(mSets[k], null);
                e.Handled = true;
            }
        }
    }
}
