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
                ['misc', ['undo', 'redo']],
                ['style', ['style']],
                ['font', ['bold', 'underline', 'clear', 'fontsize', 'fontname', 'superscript', 'subscript']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture', 'video', 'hr', 'elfinder']],
                ['height', ['height']],
                ['view', ['fullscreen', 'codeview', 'help']]
            ]       
        ";
    }
}
