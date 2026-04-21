using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace MarkdownEditor.Services
{
    /// <summary>
    /// Provides a Markdown syntax highlighting definition for AvalonEdit,
    /// built from an embedded XSHD definition in memory.
    /// </summary>
    public static class MarkdownHighlightingDefinition
    {
        private static IHighlightingDefinition? _instance;

        public static IHighlightingDefinition Get()
        {
            return _instance ??= Load();
        }

        private static IHighlightingDefinition Load()
        {
            using var reader = XmlReader.Create(new StringReader(XshdXml));
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        // Rules are tested in order: the first match at the same position wins.
        // Code fences (multiline Span) come first to remove any
        // internal highlighting inside code blocks.
        private const string XshdXml = @"<?xml version=""1.0""?>
<SyntaxDefinition name=""Markdown"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">

    <!-- Color palette aligned with ModernTheme.xaml -->
    <Color name=""Heading1""       foreground=""#6366F1"" fontWeight=""bold""/>
    <Color name=""Heading2""       foreground=""#7C7FEE"" fontWeight=""bold""/>
    <Color name=""Heading3""       foreground=""#9295F0"" fontWeight=""bold""/>
    <Color name=""HeadingN""       foreground=""#9295F0""/>
    <Color name=""Bold""           foreground=""#16163A"" fontWeight=""bold""/>
    <Color name=""Italic""         foreground=""#3A3A5C"" fontStyle=""italic""/>
    <Color name=""BoldItalic""     foreground=""#16163A"" fontWeight=""bold"" fontStyle=""italic""/>
    <Color name=""Strikethrough""  foreground=""#9496A1""/>
    <Color name=""InlineCode""     foreground=""#D63384"" background=""#FAF0F5""/>
    <Color name=""CodeBlock""      foreground=""#555770""/>
    <Color name=""CodeFence""      foreground=""#10B981"" fontWeight=""bold""/>
    <Color name=""Link""           foreground=""#6366F1""/>
    <Color name=""Image""          foreground=""#8B6CF7""/>
    <Color name=""Quote""          foreground=""#9496A1"" fontStyle=""italic""/>
    <Color name=""ListMarker""     foreground=""#6366F1"" fontWeight=""bold""/>
    <Color name=""HorizontalRule"" foreground=""#C0C2CC""/>

    <RuleSet>

        <!-- ① Code fences FIRST — the multiline Span removes any internal highlighting -->
        <Span color=""CodeBlock"" multiline=""true"">
            <Begin color=""CodeFence"">```[^\n]*</Begin>
            <End   color=""CodeFence"">```</End>
        </Span>

        <!-- ② Inline code -->
        <Span color=""InlineCode"">
            <Begin>`</Begin>
            <End>`</End>
        </Span>

        <!-- ③ Headings — from longest to shortest to avoid shadowing -->
        <Rule color=""HeadingN"">^#{4,6}[ \t]+[^\n]*</Rule>
        <Rule color=""Heading3"">^###[ \t]+[^\n]*</Rule>
        <Rule color=""Heading2"">^##[ \t]+[^\n]*</Rule>
        <Rule color=""Heading1"">^#[ \t]+[^\n]*</Rule>

        <!-- ④ Gras + Italique *** / ___ -->
        <Rule color=""BoldItalic"">\*\*\*\S[^\n]*?\*\*\*</Rule>
        <Rule color=""BoldItalic"">___\S[^\n]*?___</Rule>

        <!-- ⑤ Gras ** / __ -->
        <Rule color=""Bold"">\*\*\S[^\n]*?\*\*</Rule>
        <Rule color=""Bold"">__\S[^\n]*?__</Rule>

        <!-- ⑥ Strikethrough ~~ -->
        <Rule color=""Strikethrough"">~~\S[^\n]*?~~</Rule>

        <!-- ⑦ Italic * / _ (after ** to avoid overlapping bold) -->
        <Rule color=""Italic"">\*[^\s*\n][^\n*]*?\*(?!\*)</Rule>
        <Rule color=""Italic"">_[^\s_\n][^\n_]*?_(?!_)</Rule>

        <!-- ⑧ Images (before links to capture the prefix !) -->
        <Rule color=""Image"">!\[[^\]\n]*\]\([^)\n]*\)</Rule>

        <!-- ⑨ Inline links [text](url) -->
        <Rule color=""Link"">\[[^\]\n]*\]\([^)\n]*\)</Rule>

        <!-- ⑩ Blockquotes -->
        <Rule color=""Quote"">^[ \t]*&gt;[^\n]*</Rule>

        <!-- ⑪ List markers -->
        <Rule color=""ListMarker"">^[ \t]*[-*+][ \t]+</Rule>
        <Rule color=""ListMarker"">^[ \t]*[0-9]+\.[ \t]+</Rule>

        <!-- ⑫ Horizontal rules -->
        <Rule color=""HorizontalRule"">^(-{3,}|\*{3,}|_{3,})[ \t]*$</Rule>

    </RuleSet>
</SyntaxDefinition>";
    }
}
