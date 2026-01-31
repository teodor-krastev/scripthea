using System;
using System.Collections.Generic;
using System.IO;
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

using scripthea.options;
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea.preview
{
    /// <summary>
    /// Interaction logic for FashioningUC.xaml
    /// </summary>
    public partial class FashioningUC : UserControl
    {
        public FashioningUC()
        {
            InitializeComponent();
        }        
        List<List<string>> cons, llms; const string consClr = "#FFF6FFF7"; const string llmClr = "#FFFFFEF4";
        protected Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            ReadCons(ref cons, Path.Combine(Utils.configPath,"context.btx")); ReadCons(ref llms, Path.Combine(Utils.configPath, "ask_llm.btx"));
            ConfigureComboForWrapping(cbCons);
        }
        public void Finish()
        {
            WriteCons(cons, Path.Combine(Utils.configPath, "context.btx")); WriteCons(llms, Path.Combine(Utils.configPath, "ask_llm.btx"));
        }
        // call this once (e.g., in Window ctor after InitializeComponent)
        private void ConfigureComboForWrapping(ComboBox combo)
        {
            // Stretch item containers so wrapping can happen
            combo.ItemContainerStyle = new Style(typeof(ComboBoxItem))
            {
                Setters =
                {
                    new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
                }
            };
            // Optional: prevent horizontal scrollbar in dropdown
            ScrollViewer.SetHorizontalScrollBarVisibility(combo, ScrollBarVisibility.Disabled);
        }
        public void SwitchMode()
        {
            cbCons.Items.Clear();
            switch (fashionMode)
            {
                case FashionModeTypes.context:
                    UpdateCombo(cons, consClr, opts.llm.SelectedPretext);
                    break;
                case FashionModeTypes.ask_llm:
                    UpdateCombo(llms, llmClr, opts.llm.SelectedQuestion);                    
                    break;
            }
            rowText.Height = fashionMode == FashionModeTypes.none ? new GridLength(0) : new GridLength(100);
        }
        // add one item with a wrapping TextBlock inside a ComboBoxItem
        private void AddWrappedTextItem(ComboBox combo, List<string> text, string clr)
        {
            var tb = new TextBlock
            {
                Text = "",
                TextWrapping = TextWrapping.Wrap,
                // optional for nicer look:
                Margin = new Thickness(2)
            };
            foreach (string ss in text) tb.Text += ss + '\n';
            tb.Text = tb.Text.TrimEnd('\n');
            if (cbCons.Items.Count % 2 == 1) tb.Background = Utils.ToSolidColorBrush(clr);
            // Constrain width so the TextBlock wraps inside the dropdown
            tb.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
            {
                Source = combo
            });
            var cbi = new ComboBoxItem { Content = tb };
            combo.Items.Add(cbi);
        }
        private void UpdateCombo(List<List<string>> allCons, string clr, int selectedItem)
        {
            cbCons.Items.Clear();
            foreach (List<string> ls in allCons)
                AddWrappedTextItem(cbCons, ls, clr);
            cbCons.SelectedIndex = selectedItem;
        }
        public List<string> getContext() { if (fashionMode == FashionModeTypes.context) return listFlatTextBox(tbEditor); else return null; }
        public List<string> getAskLLM() { if (fashionMode == FashionModeTypes.ask_llm) return listFlatTextBox(tbEditor); else return null; }
        public string joinStrList(List<string> ls) { return String.Join("\n", ls.ToArray()); }
        public List<string> listFlatTextBox(TextBox textBox, bool skipComment = true)
        {
            string[] sa = textBox.Text.Split(new[] { '\n' }, StringSplitOptions.None); //Environment.NewLine
            List<string> lt = new List<string>();
            foreach (string ss in sa)
            {
                if (skipComment && ss.Trim().StartsWith("#")) continue;
                lt.Add(ss.Trim());
            }               
            return lt;
        }
        public enum FashionModeTypes { none = 0, context = 1, ask_llm = 2 }
        public FashionModeTypes fashionMode
        { 
            get
            {
                if (rbContext.IsChecked.Value) return FashionModeTypes.context;
                if (rbAskLLM.IsChecked.Value) return FashionModeTypes.ask_llm;
                return FashionModeTypes.none;
            }
            set
            {
                switch (value)
                {
                    case FashionModeTypes.none: rbNone.IsChecked = true;
                        break;
                    case FashionModeTypes.context: rbContext.IsChecked = true;
                        break;
                    case FashionModeTypes.ask_llm: rbAskLLM.IsChecked = true;
                        break;
                }
            }
        }
        private void cbCons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbi = cbCons.SelectedItem as ComboBoxItem;
            btnUpdate.IsEnabled = cbi != null; btnUpdate.BorderBrush = btnUpdate.IsEnabled ? Brushes.Black : Brushes.Gray;
            if (!btnUpdate.IsEnabled) return;
            TextBlock tb = cbi.Content as TextBlock;
            if (tb is null) return;
            tbEditor.Text = tb.Text;
            switch (fashionMode)
            {
                case FashionModeTypes.context:
                    opts.llm.SelectedPretext = cbCons.SelectedIndex;
                    break;
                case FashionModeTypes.ask_llm:
                    opts.llm.SelectedQuestion = cbCons.SelectedIndex;
                    break;
            }
        }
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            List<string> ls = listFlatTextBox(tbEditor,false);
            int idx = cbCons.SelectedIndex;
            if (ls.Count == 0 || idx == -1) return;
            switch (fashionMode)
            {
                case FashionModeTypes.context:
                    cons[cbCons.SelectedIndex] = ls; UpdateCombo(cons, consClr, opts.llm.SelectedPretext); 
                    break;
                case FashionModeTypes.ask_llm:
                    llms[cbCons.SelectedIndex] = ls; UpdateCombo(llms, llmClr, opts.llm.SelectedQuestion);
                    break;
            }
            cbCons.SelectedIndex = idx;
            _ = new PopupText(btnUpdate, "Updated");
        }
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            List<string> ls = listFlatTextBox(tbEditor,false);
            int idx = cbCons.SelectedIndex;
            if (ls.Count == 0) return;
            switch (fashionMode)
            {
                case FashionModeTypes.context:
                    cons.Insert(0, ls); UpdateCombo(cons, consClr, opts.llm.SelectedPretext);
                    break;
                case FashionModeTypes.ask_llm:
                    llms.Insert(0, ls); UpdateCombo(llms, llmClr, opts.llm.SelectedQuestion);
                    break;
            }
            cbCons.SelectedIndex = 0;
            _ = new PopupText(btnAdd, "Added");
        }
        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            switch (fashionMode)
            {
                case FashionModeTypes.context: 
                    cons.RemoveAt(cbCons.SelectedIndex); UpdateCombo(cons, consClr, opts.llm.SelectedPretext);
                    break;
                case FashionModeTypes.ask_llm:
                    llms.RemoveAt(cbCons.SelectedIndex);  UpdateCombo(llms, llmClr, opts.llm.SelectedQuestion);
                    break;
            }
            _ = new PopupText(btnMinus, "Removed");
        }
        protected void WriteCons(List<List<string>> allCons, string fn)
        {
            List<string> ls = new List<string>();
            foreach (List<string> lt in allCons)
            {
                ls.AddRange(lt); ls.Add("---");
            }
            File.WriteAllLines(fn, ls);
        }
        protected void ReadCons(ref List<List<string>> allCons, string fn)
        {
            if (!File.Exists(fn)) { MessageBox.Show("Error[22]: no <" + fn + "> file found"); return; }
            List<string> conLines = new List<string>(File.ReadAllLines(fn));
            
            if (allCons is null) allCons = new List<List<string>>();
            List<string> ls = new List<string>();
            foreach (string ss in conLines)
            {
                if (ss.Trim().Equals("")) continue; // empty line
                if (ss.Equals("---"))
                {
                    allCons.Add(ls); ls = new List<string>();
                }
                else
                {
                    ls.Add(ss);
                }
            }
        }
    }
}
