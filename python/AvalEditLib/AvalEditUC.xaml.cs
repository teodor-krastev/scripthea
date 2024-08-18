using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
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

using ICSharpCode.AvalonEdit.CodeCompletion;
//using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.Reflection;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Folding;

namespace AvalEditLib
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class AvalEditUC : UserControl
	{
		public AvalEditUC()
		{
			// Load our custom highlighting definition
			IHighlightingDefinition customHighlighting;
			Assembly assembly = Assembly.GetExecutingAssembly();

			/*using (Stream s = assembly.GetManifestResourceStream("AvalEditLib.CustomHighlighting.xshd")) //
			{
				if (s == null) throw new InvalidOperationException("Could not find embedded resource");
				using (XmlReader reader = new XmlTextReader(s))
				{
					customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
						HighlightingLoader.Load(reader, HighlightingManager.Instance);
				}
			}
			// and register it in the HighlightingManager
			HighlightingManager.Instance.RegisterHighlighting("Custom Highlighting", new string[] { ".cool" }, customHighlighting);*/

			InitializeComponent();
			propertyGridComboBox.SelectedIndex = 2;

			textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Python");
			//textEditor.SyntaxHighlighting = customHighlighting;
			// initial highlighting now set by XAML

			textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
			textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;

			/*DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
			foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
			foldingUpdateTimer.Tick += foldingUpdateTimer_Tick;
			foldingUpdateTimer.Start();*/

			history = new List<string>();
		}
		public string Text { get { return textEditor.Text; } set { textEditor.Text = value; } }
		public string DefaultDirectory { get; set; } // sMacro folder
		public string InitialDirectory
		{
			get
			{
				if (File.Exists(currentFileName)) return Path.GetDirectoryName(currentFileName);
				else return DefaultDirectory;
			} 
		}
		/*public event RoutedEventHandler OnSaveModule;
		public void SaveModule(object sender, RoutedEventArgs e)
        {
			if (OnSaveModule != null) OnSaveModule(sender, e);
        }*/
		private string _currentFileName;
		public string currentFileName
		{
			get { return _currentFileName; }
			set 
			{
				int lenWish = 32;
				_currentFileName = value;
				if (_currentFileName.Length > lenWish)
				{ tbFilename.Text = "..." + _currentFileName.Substring(_currentFileName.Length - lenWish); tbFilename.ToolTip = _currentFileName; }
				else tbFilename.Text = _currentFileName;
			}
		}
		// !!!
		// for now on start loading the file workMacro.py and on close save in the same
		// meanwhile user can load and save *.py
		// later add buttons for "new macro", "save", "save as..." and history
		public bool Open(string filename)
        {
			if (File.Exists(filename))
			{
				currentFileName = filename;
				textEditor.Load(currentFileName);
				textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(currentFileName));
				return true;
			}
			else return false;
        }
		void openFileClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.InitialDirectory = InitialDirectory;
			dlg.CheckFileExists = false;
			dlg.Filter = "Python module (.py)|*.py";
			if (dlg.ShowDialog() ?? false)
			{
				Text = "";
				Open(dlg.FileName); 
			}
		}
		public List<string> history; 
		public bool Save(string filename = "")
        {
			string fn = filename.Equals(string.Empty) ? currentFileName : filename;
			if (string.IsNullOrEmpty(fn)) return false; 
			textEditor.Save(fn);
			if (history.Count == 0) history.Add(fn);
			else
            {
				if (!fn.Equals(history[0],StringComparison.InvariantCultureIgnoreCase)) history.Add(fn);
			}
			return true;
		}

		void saveFileClick(object sender, EventArgs e)
		{
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.DefaultExt = ".py";
			dlg.InitialDirectory = InitialDirectory;
			dlg.Filter = "Python module (.py)|*.py";
			if (File.Exists(currentFileName)) dlg.FileName = currentFileName;
			if (dlg.ShowDialog() ?? false)
			{
				currentFileName = dlg.FileName;
				Save(currentFileName);
			}			
		}

		void propertyGridComboBoxSelectionChanged(object sender, RoutedEventArgs e)
		{
			if (propertyGrid == null)
				return;
			switch (propertyGridComboBox.SelectedIndex)
			{
				case 0:
					propertyGrid.SelectedObject = textEditor;
					break;
				case 1:
					propertyGrid.SelectedObject = textEditor.TextArea;
					break;
				case 2:
					propertyGrid.SelectedObject = textEditor.Options;
					break;
			}
		}

		CompletionWindow completionWindow;

		void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
		{
			if (e.Text == ".")
			{
				// open code completion after the user has pressed dot:
				completionWindow = new CompletionWindow(textEditor.TextArea);
				// provide AvalonEdit with the data:
				IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
				/*data.Add(new MyCompletionData("Item1"));
				data.Add(new MyCompletionData("Item2"));
				data.Add(new MyCompletionData("Item3"));
				data.Add(new MyCompletionData("Another item"));
				completionWindow.Show();*/
				completionWindow.Closed += delegate {
					completionWindow = null;
				};
			}
		}

		void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
		{
			if (e.Text.Length > 0 && completionWindow != null)
			{
				if (!char.IsLetterOrDigit(e.Text[0]))
				{
					// Whenever a non-letter is typed while the completion window is open,
					// insert the currently selected element.
					completionWindow.CompletionList.RequestInsertion(e);
				}
			}
			// do not set e.Handled=true - we still want to insert the character that was typed
		}

		#region Folding
		/*FoldingManager foldingManager;
		AbstractFoldingStrategy foldingStrategy;

		void HighlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (textEditor.SyntaxHighlighting == null) {
				foldingStrategy = null;
			} else {
				switch (textEditor.SyntaxHighlighting.Name) {
					case "XML":
						foldingStrategy = new XmlFoldingStrategy();
						textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
						break;
					case "C#":
					case "C++":
					case "PHP":
					case "Java":
						textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(textEditor.Options);
						foldingStrategy = new BraceFoldingStrategy();
						break;
					default:
						textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
						foldingStrategy = null;
						break;
				}
			}
			if (foldingStrategy != null) {
				if (foldingManager == null)
					foldingManager = FoldingManager.Install(textEditor.TextArea);
				foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
			} else {
				if (foldingManager != null) {
					FoldingManager.Uninstall(foldingManager);
					foldingManager = null;
				}
			}
		}

		void foldingUpdateTimer_Tick(object sender, EventArgs e)
		{
			if (foldingStrategy != null) {
				foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
			}
		}*/
		#endregion
        private void chkOptions_Checked(object sender, RoutedEventArgs e)
        {
			if (chkOptions.IsChecked.Value) columnProperty.Width = new GridLength(200); 
			else columnProperty.Width = new GridLength(0); 
		}
    }
}
