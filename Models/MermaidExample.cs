namespace MarkdownEditor.Models
{
    public sealed class MermaidExample
    {
        public string Name { get; }
        public string Description { get; }
        public string Code { get; }

        public MermaidExample(string name, string description, string code)
        {
            Name = name;
            Description = description;
            Code = code;
        }
    }
}
