using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TLL_Snippet_Manager
{
    public class Snippet
    {
        public string Title { get; set; }
        public ObservableCollection<string> Tags { get; set; }
        public string Code { get; set; }
    }
}