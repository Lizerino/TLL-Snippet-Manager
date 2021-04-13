using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TLL_Snippet_Manager
{
    /// <summary>
    /// Interaction logic for MainToolWindowControl.
    /// </summary>
    public partial class MainToolWindowControl : UserControl
    {
        private Point? initialMousePosition;

        private string tagOpenString = "<TLLSMTAG>";

        private string tagCloseString = "</TLLSMTAG>";

        private string bodyOpenString = "<TLLBODY>";

        private string bodyCloseString = "</TLLBODY>";

        public ObservableCollection<Snippet> snippetSourceList = new ObservableCollection<Snippet>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MainToolWindowControl"/> class.
        /// </summary>
        public MainToolWindowControl()
        {
            this.InitializeComponent();
            listBoxSnippets.AllowDrop = true;
            if (Properties.Settings.Default.TLLRootDirectory != "")
            {
                LoadSnippets();
            }
        }

        /// <summary>
        /// Load snippet files and create the snippet source list
        /// </summary>
        private void LoadSnippets()
        {
            var snippetFileNames = getSnippetPaths().Select(f => Path.GetFileNameWithoutExtension(f));
            foreach (var snippetFileName in snippetFileNames)
            {
                var snippet = new Snippet
                {
                    Tags = new ObservableCollection<string>()
                };
                var selectedSnippetContent = File.ReadAllText(GetSnippetsRootDirectory() + snippetFileName + ".txt");
                snippet.Title = snippetFileName;
                var tags = getTagsFromSnippetContent(selectedSnippetContent);
                foreach (var tag in tags)
                {
                    snippet.Tags.Add(tag);
                }
                snippet.Code = getBodyFromSnippet(selectedSnippetContent);
                snippetSourceList.Add(snippet);
            }
            updateListBoxDisplay();
        }

        /// <summary>
        /// Update listbox lists
        /// </summary>
        private void updateListBoxDisplay()
        {
            listBoxSnippets.ItemsSource = snippetSourceList.Select(snippet => snippet.Title);
            listBoxTags.ItemsSource = snippetSourceList.SelectMany(snippet => snippet.Tags).Distinct();
        }

        private string[] getTagsFromSnippetContent(string selectedSnippetContent)
        {
            var tagStart = selectedSnippetContent.IndexOf(tagOpenString);
            var tagEnd = selectedSnippetContent.IndexOf(tagCloseString);
            var tagstring = selectedSnippetContent.Substring(tagStart + tagOpenString.Length, (tagEnd - (tagStart + tagOpenString.Length)));
            var result = tagstring.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return result;
        }

        public string GetSnippetsRootDirectory()
        {
            if (Properties.Settings.Default.TLLRootDirectory == "")
            {
                SelectRootFolder();
            }
            return Properties.Settings.Default.TLLRootDirectory;
        }

        public List<string> getSnippetPaths()
        {
            return Directory.GetFiles(GetSnippetsRootDirectory()).ToList();
        }

        // TODO: Add "AND" "OR" "NOT" to search function
        private void txtBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            listBoxSnippets.ItemsSource = snippetSourceList.Where(t => t.Title.ToLowerInvariant().Contains(txtBoxSearch.Text.ToLowerInvariant())).Select(t => t.Title);
        }

        // TODO: Change Listbox to treeview using folders as treeview
        private void listBoxSnippets_Selected(object sender, RoutedEventArgs e)
        {
            var selectedSnippet = (sender as ListBox)?.SelectedItem?.ToString();
            if (selectedSnippet != null)
            {
                // Show selected snippets code in the code preview
                var selectedSnippetContent = File.ReadAllText(GetSnippetsRootDirectory() + selectedSnippet + ".txt");
                txtBlockPreview.Text = getBodyFromSnippet(selectedSnippetContent);
            }
            else
            {
                txtBlockPreview.Text = "";
            }
        }

        private void listBoxSnippets_Drop(object sender, DragEventArgs e)
        {
            // Ask user to select folder to save snippets if no folder is selected
            if (Properties.Settings.Default.TLLRootDirectory == "")
            {
                SelectRootFolder();
            }
            // Change this to stop dragover and show a popup
            // If user cancels folder selection or a folder is not selected for any other reason show error message
            if (Properties.Settings.Default.TLLRootDirectory == "")
            {
                MessageBox.Show("No snippet directory selected", "Error");
            }
            else
            {
            string dataString = (string)e.Data.GetData(DataFormats.StringFormat);
            dataString.Trim();

            //Check if snippet exists already exists
            if (snippetSourceList.Where(s => s.Code == dataString).Count() <= 0)
            {
                AddSnippetDialog AddSnippet = new AddSnippetDialog(dataString, snippetSourceList.SelectMany(t => t.Tags).Distinct());
                AddSnippet.ShowDialog();
                e.Effects = DragDropEffects.Copy;
                if (AddSnippet.SaveSnippet == true)
                {
                    StringBuilder snippetFileContent = new StringBuilder();
                    snippetFileContent.Append(tagOpenString);
                    if (AddSnippet.snippet.Tags != null)
                    {
                        foreach (var tag in AddSnippet.snippet.Tags)
                        {
                            snippetFileContent.Append("," + tag + ",");
                        }
                    }
                    snippetFileContent.Append(tagCloseString);
                    snippetFileContent.Append(Environment.NewLine + bodyOpenString + Environment.NewLine);
                    snippetFileContent.Append(dataString);
                    snippetFileContent.Append(Environment.NewLine + bodyCloseString);
                    File.WriteAllText(GetSnippetsRootDirectory() + AddSnippet.snippet.Title + ".txt", snippetFileContent.ToString());
                    snippetSourceList.Add(AddSnippet.snippet);
                    updateListBoxDisplay();
                }
            }
            else
            {
                // TODO: Inform user snippet already exists
            }
            }
            e.Handled = true;
        }

        public void listBoxSnippets_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        // TODO: Make sure the drag only starts if the initial position is over a listbox items
        private void listBoxSnippets_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            initialMousePosition = e.GetPosition(this);
        }

        private void listBoxSnippets_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var movedDistance = (initialMousePosition - e.GetPosition(this)).GetValueOrDefault();

                if (movedDistance.Length > 5)
                {
                    ListBox parent = (ListBox)sender;
                    object data = GetDataFromListBox(parent, e.GetPosition(parent));

                    if (data != null)
                    {
                        DragDrop.DoDragDrop(parent, data, DragDropEffects.Copy);
                    }
                }
            }
        }

        private object GetDataFromListBox(ListBox source, Point point)
        {
            UIElement element = source.InputHitTest(point) as UIElement;
            if (element != null)
            {
                object data = DependencyProperty.UnsetValue;
                while (data == DependencyProperty.UnsetValue)
                {
                    data = source.ItemContainerGenerator.ItemFromContainer(element);

                    if (data == DependencyProperty.UnsetValue)
                    {
                        element = VisualTreeHelper.GetParent(element) as UIElement;
                    }

                    if (element == source)
                    {
                        return null;
                    }
                }

                if (data != DependencyProperty.UnsetValue)
                {
                    string snippetFileContent = File.ReadAllText(GetSnippetsRootDirectory() + data.ToString() + ".txt");
                    string snippetToReturn = getBodyFromSnippet(snippetFileContent);

                    return snippetToReturn;
                }
            }
            return null;
        }

        private string getBodyFromSnippet(string snippetFileContent)
        {
            var codeStart = snippetFileContent.IndexOf(bodyOpenString);
            var codeEnd = snippetFileContent.IndexOf(bodyCloseString);
            var result = snippetFileContent.Substring(codeStart + bodyOpenString.Length + 2, (codeEnd - (codeStart + bodyOpenString.Length) - 2));
            return result;
        }

        private void listBoxTags_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (listBoxTags.SelectedItem != null)
            {
                var selectedTags = new List<string>();

                foreach (var item in listBoxTags.SelectedItems)
                {
                    selectedTags.Add(item.ToString());
                }

                List<Snippet> searchresult = new List<Snippet>();

                foreach (var snippet in snippetSourceList)
                {
                    if (selectedTags.Any(elem => snippet.Tags.Contains(elem)))
                    {
                        searchresult.Add(snippet);
                    }
                }
                listBoxSnippets.ItemsSource = searchresult.Select(s => s.Title);
            }
            else
            {
                listBoxSnippets.ItemsSource = snippetSourceList.Select(t => t.Title);
            }
        }

        private void btnClearTagFilter_Click(object sender, RoutedEventArgs e)
        {
            listBoxTags.SelectedItems.Clear();
        }

        private void btnSelectRootFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectRootFolder();
        }

        private void SelectRootFolder()
        {
            // TODO: Bad design set default folder in whatever vs has as default repo folder            
                System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
                var result = openFileDlg.ShowDialog();
                if (result.ToString() != string.Empty && result != System.Windows.Forms.DialogResult.Cancel)
                {
                    Properties.Settings.Default.TLLRootDirectory = openFileDlg.SelectedPath + @"\";
                    Properties.Settings.Default.Save();
                }            
        }

        private void listBoxSnippets_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // TODO: Add delete confirmation
            if (e.Key == Key.Delete && listBoxSnippets.SelectedItem != null)
            {
                if (MessageBox.Show("Do you want to delete "+ listBoxSnippets.SelectedItem.ToString(), "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {                    
                var NameOfSnippetToRemove = listBoxSnippets.SelectedItem.ToString();
                File.Delete(GetSnippetsRootDirectory() + NameOfSnippetToRemove + ".txt");
                var snippetToRemove = snippetSourceList.Where(s => s.Title == NameOfSnippetToRemove).FirstOrDefault();
                snippetSourceList.Remove(snippetToRemove);
                updateListBoxDisplay();
                }                
            }
        }
    }
}