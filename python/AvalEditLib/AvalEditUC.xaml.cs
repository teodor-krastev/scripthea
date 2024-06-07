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
using UtilsNS;

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
			/*IHighlightingDefinition customHighlighting;
			Assembly assembly = Assembly.GetExecutingAssembly();

			using (Stream s = assembly.GetManifestResourceStream("AvalEditLib.CustomHighlighting.xshd")) //
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

			//textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
			//textEditor.SyntaxHighlighting = customHighlighting;
			// initial highlighting now set by XAML

			textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
			textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;

			/*DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
			foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
			foldingUpdateTimer.Tick += foldingUpdateTimer_Tick;
			foldingUpdateTimer.Start();*/
			
		}
		public string sMacroFolder { get { return Path.Combine(Utils.basePath, "sMacroPy"); } }
		public string Text { get { return textEditor.Text; } set { textEditor.Text = value; } }

		string currentFileName;

		void openFileClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.CheckFileExists = true; dlg.DefaultExt = ".py"; dlg.InitialDirectory = sMacroFolder;
			if (dlg.ShowDialog() ?? false)
			{
				currentFileName = dlg.FileName;
				textEditor.Load(currentFileName);
				textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(currentFileName));
			}
		}
		void saveFileClick(object sender, EventArgs e)
		{
			if (currentFileName == null)
			{
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.DefaultExt = ".py"; dlg.InitialDirectory = sMacroFolder;
				if (dlg.ShowDialog() ?? false)
				{
					currentFileName = dlg.FileName;
				}
				else
				{
					return;
				}
			}
			textEditor.Save(currentFileName);
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
		//FoldingManager foldingManager;
		//AbstractFoldingStrategy foldingStrategy;

		void HighlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			/*if (textEditor.SyntaxHighlighting == null) {
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
			}*/
		}

		void foldingUpdateTimer_Tick(object sender, EventArgs e)
		{
			/*if (foldingStrategy != null) {
				foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
			}*/
		}
		#endregion

        private void chkOptions_Checked(object sender, RoutedEventArgs e)
        {
			if (chkOptions.IsChecked.Value) columnProperty.Width = new GridLength(200); 
			else columnProperty.Width = new GridLength(0); 
		}
	}


}
