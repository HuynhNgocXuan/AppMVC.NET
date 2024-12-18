namespace webMVC.Models
{
    public class SummerNote
    {
        public SummerNote(string idEditor, bool isLoadLibrary = true)
        {
            IdEditor = idEditor;
            IsLoadLibrary = isLoadLibrary;
        }

        public string IdEditor { get; set; }

        public bool IsLoadLibrary { get; set; }

        public int Height { get; set; } = 120;

        public string Toolbar { get; set; } = @"
            [
                ['style', ['style']],
                ['font', ['bold', 'underline', 'clear']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture', 'video']],
                ['height', ['height']],
                ['view', ['fullscreen', 'codeview', 'help']]
            ]       
        ";
    }
}
