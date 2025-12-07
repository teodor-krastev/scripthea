using System;
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
using UtilsNS;

namespace scripthea.preview
{
    /// <summary>
    /// Interaction logic for OneSeedUC.xaml
    /// </summary>
    public partial class PreviewItemUC : UserControl
    {
        public PreviewItemUC()
        {
            InitializeComponent();
        }
        public PreviewItemUC(string text, string modifs)
        {
            InitializeComponent();
            tbCue.Text = text.Trim().Trim('\"').Trim();
            modifsText = modifs.Trim().Trim('\"').Trim();
        }
        public PreviewItemUC(Tuple<string, string> prompt) : this(prompt.Item1, prompt.Item2)
        {
        }
        public PreviewItemUC(List<string> text, string modifs)
        {
            InitializeComponent();
            tbCue.Text = ""; int k = 0;
            foreach (string line in text)
            {
                tbCue.Text += line + Environment.NewLine; k++;
            }
            tbCue.Text = tbCue.Text.Trim().Trim('\"');
            modifsText = modifs.Trim().Trim('\"').Trim();
        }
        public PreviewItemUC(Tuple<List<string>, string> prompt) : this(prompt.Item1, prompt.Item2)
        {
        }
        #region Events
        public event RoutedEventHandler OnSelectionChanged; // only to positive selection
        public void SelectionChanged(object sender, RoutedEventArgs e)
        {
            OnSelectionChanged?.Invoke(sender, e);
        }
        public event RoutedEventHandler OnItemChanged;
        public void ItemChanged(object sender, RoutedEventArgs e) // any text or check
        {
            OnItemChanged?.Invoke(sender, e);
        }
        private void tbCue_TextChanged(object sender, TextChangedEventArgs e)
        {
            OnItemChanged?.Invoke(this, e);
        }
        #endregion
        private int _index = 0; // zero based
        public int index
        {
            get => _index;
            set { _index = value; lbIndex.Content = value + 1; } // internal -> zero based; external -> 1 based
        }
        public bool IsReady { get => tbLLMCue.Visibility == Visibility.Visible && !cueLLMText.Equals(""); } // ready to be generated from
        public bool IsBoxChecked
        {
            get { return checkBox.IsChecked.Value; }
            set { checkBox.IsChecked = value; }
        }
        private bool _IsReadOnly;
        public bool IsReadOnly
        {
            get => _IsReadOnly;
            set
            {
                _IsReadOnly = value;
                tbHeader.IsReadOnly = value; tbCue.IsReadOnly = value; tbLLMCue.IsReadOnly = value; tbModifs.IsReadOnly = value;
            }
        }
        private bool _selected = false;
        public bool selected
        {
            get => _selected;
            set
            {
                if (value) SelectionChanged(this, null); // clear all
                if (value) gridFullFrame.Background = Brushes.LightSkyBlue; else gridFullFrame.Background = Brushes.White;
                _selected = value;
            }
        }
        public string cueText
        {
            get => tbCue.Text.Trim().Trim('\"').Trim();
            set => tbCue.Text = value.Trim().Trim('\"').Trim();
        }
        public List<string> extras = new List<string>{"<response>", "</response>", "<prompt>", "</prompt>" }; // to be removed
        public string cueLLMText
        {
            get => tbLLMCue.Text.Trim().Trim('\"').Trim();
            set 
            { 
                if (value == null) { tbLLMCue.Text = ""; return; }
                string resp = value.Trim().Trim('\"').Trim().Replace("\n\n", "\n");
                foreach(string extra in extras) resp = resp.Replace(extra, "");
                tbLLMCue.Text = resp; 
                showLLM = !tbLLMCue.Text.Trim().Equals("") || showBoth;
                if (showLLM && !showBoth) tbCue.Visibility = Visibility.Collapsed;
                else tbCue.Visibility = Visibility.Visible;
            } 
        }
        public string cueConditionText
        {
            get { return string.IsNullOrEmpty(cueLLMText) ? cueText : cueLLMText; }
        }
        public string modifsText
        {
            get => tbModifs.Text.Trim().Trim('\"').Trim();
            set
            {
                tbModifs.Text = value.Trim().Trim('\"').Trim();
                if (tbModifs.Text.Equals("")) tbModifs.Visibility = Visibility.Collapsed;
                else tbModifs.Visibility = Visibility.Visible;
            }
        }
        protected bool showBoth = false; 
        public void UpdateShowing(bool _showBoth)
        {
            if (_showBoth)
            {
                tbCue.Visibility = Visibility.Visible;
                showLLM = true;
            }
            else
            {
                showLLM = !cueLLMText.Equals("");
                if (showLLM) tbCue.Visibility = Visibility.Collapsed;
                else tbCue.Visibility = Visibility.Visible;
            }
            showBoth = _showBoth;
        }
        private bool _showLLM = true; 
        public bool showLLM
        { 
            get => _showLLM; 
            set { _showLLM = value; if (value) tbLLMCue.Visibility = Visibility.Visible; else tbLLMCue.Visibility = Visibility.Collapsed;  }
        }
        public string headerText
        {
            get => tbHeader.Text.Trim(new char[] { '{', '}', '#' }).Trim(); 
            set 
            {
                string txt = value.Trim(new char[] { '{', '}', '#' }).Trim();
                if (txt != string.Empty) tbHeader.Visibility = Visibility.Visible;
                else tbHeader.Visibility = Visibility.Collapsed;
                tbHeader.Text = "# " + txt; 
            }
        }       
        public bool IsCueEmpty
        {
            get { return cueText.Trim().Equals(""); }
        }
        public bool IsCueLLMEmpty
        {
            get { return cueLLMText.Trim().Equals(""); }
        }
        public string cueTextAsString(bool noComment)
        {
            return Utils.stringFlatTextBox(tbCue, noComment).Trim().Trim('\"').Trim();
        }
        public List<string> cueTextAsList(bool noComment)
        {
            cueText = cueText.Trim(); tbCue.UpdateLayout();
            List<string> ls = Utils.listFlatTextBox(tbCue, noComment);            
            return ls;
        }
        public string cueLLMTextAsString(bool noComment)
        {
            return Utils.stringFlatTextBox(tbLLMCue, noComment).Trim().Trim('\"').Trim();
        }
        public List<string> cueLLMTextAsList(bool noComment)
        {
            cueLLMText = cueLLMText.Trim(); tbLLMCue.UpdateLayout();
            List<string> ls = Utils.listFlatTextBox(tbLLMCue, noComment);
            return ls;
        }
        private void gridFullFrame_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            selected = true;
        }      
    }
}

