using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace TLL_Snippet_Manager
{
    /// <summary>
    /// Interaction logic for AddSnippetDialog.xaml
    /// </summary>
    public partial class AddSnippetDialog : Window
    {
        public Snippet snippet { get; set; }

        public bool SaveSnippet { get; set; }

        public AddSnippetDialog(string codeBody, IEnumerable<string> tags)
        {
            InitializeComponent();
            SaveSnippet = false;
            snippet = new Snippet();
            snippet.Tags = new ObservableCollection<string>();
            listBoxTags.ItemsSource = snippet.Tags;
            var reader = new StringReader(codeBody);
            txtBoxSnippetName.Text = reader.ReadLine();
            comboBoxAddTag.ItemsSource = tags;
        }

        private void txtBoxAddTag_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnAddTag_Click(sender, e);
            }
        }

        private void listBoxTags_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && listBoxTags.SelectedItem != null)
            {
                snippet.Tags.Remove(listBoxTags.SelectedItem.ToString());
            }
        }

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            if (snippet.Tags.Contains(comboBoxAddTag.Text.Trim()) || comboBoxAddTag.Text == "Tag already added")
            {
                comboBoxAddTag.Text = "Tag already added";
            }
            else
            {
                snippet.Tags.Add(comboBoxAddTag.Text.Trim());
                comboBoxAddTag.SelectedItem = null;
                comboBoxAddTag.Text = "";
            }
        }

        // TODO: Add proper binding and proper validation to inputs
        // TODO: Check for invalid chars in title as its the filename
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            char[] invalidPathChars = Path.GetInvalidPathChars();
            if (txtBoxSnippetName.Text == "" || txtBoxSnippetName.Text == "Snippet name can not be empty!")
            {
                txtBoxSnippetName.Text = "Snippet name can not be empty!";
            }
            else if (txtBoxSnippetName.Text.IndexOfAny(invalidPathChars) != -1)
            {
                txtBoxSnippetName.Text = "Snippet name can not contain any of the following characters " + invalidPathChars.ToString();
            }
            else
            {
                snippet.Title = txtBoxSnippetName.Text;
                SaveSnippet = true;
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}