namespace MarkdownEditor.Models
{
    public sealed class MarkdownSnippet
    {
        public string Name { get; }
        public string Description { get; }
        public string Syntax { get; }

        public MarkdownSnippet(string name, string description, string syntax)
        {
            Name = name;
            Description = description;
            Syntax = syntax;
        }
    }
}
