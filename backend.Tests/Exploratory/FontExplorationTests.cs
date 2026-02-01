using backend.Models;
using backend.Tests.Helpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit.Abstractions;

namespace backend.Tests.Exploratory;

public class FontExplorationTests
{
  private readonly ITestOutputHelper _output;
  public FontExplorationTests(ITestOutputHelper output)
  {
    _output = output;
  }

  [Fact]
  public void Explore_Fonts()
  {
    using var doc = DocxTestHelper.OpenDocxAsRead("Fonts/fonts.docx");
    var body = doc.MainDocumentPart.Document.Body;

    foreach (var paragraph in body.Elements<Paragraph>())
    {
      Console.WriteLine("paragraph:", paragraph);
      foreach (var text in paragraph.Descendants<Text>())
      {
        _output.WriteLine(text.Text);
      }
    }
  }

  [Fact]
  public void Print_text_with_font_info()
  {
    using var doc = DocxTestHelper.OpenDocxAsRead("test.docx");

    var errros = ValidateTimesNewRoman(doc).ToList();
    foreach (var error in errros)
    {
      _output.WriteLine(error.Message);
    }
    _output.WriteLine($"Done Errors Count: {errros.Count.ToString()}");

  }
  public IEnumerable<ValidationResult> ValidateTimesNewRoman(
    WordprocessingDocument doc)
  {
    var body = doc.MainDocumentPart!.Document.Body!;
    var errors = new List<ValidationResult>();

    int paragraphIndex = 0;

    foreach (var paragraph in body.Elements<Paragraph>())
    {
      paragraphIndex++;

      foreach (var run in paragraph.Elements<Run>())
      {
        var text = string.Concat(
          run.Elements<Text>().Select(t => t.Text));

        if (string.IsNullOrWhiteSpace(text))
          continue;

        var font = ResolveEffectiveFont(doc, paragraph, run);

        if (!string.Equals(font, "Times New Roman",
              StringComparison.OrdinalIgnoreCase))
        {
          errors.Add(new ValidationResult
          {
            IsError = true,
            Message =
              $"Invalid font '{font}' in paragraph {paragraphIndex} text: {text}",
          });
        }
      }
    }

    return errors;
  }
  string? ResolveEffectiveFont(
    WordprocessingDocument doc,
    Paragraph paragraph,
    Run run)
  {
    // 1. Run-level font
    var runFont = run
      .RunProperties?
      .RunFonts?
      .Ascii;

    if (!string.IsNullOrEmpty(runFont))
      return runFont;

    // 2. Paragraph style font
    var paraFont = GetParagraphStyleFont(doc, paragraph);
    if (!string.IsNullOrEmpty(paraFont))
      return paraFont;

    // 3. Default document font
    return GetDefaultFont(doc);
  }

  string? GetParagraphStyleFont(
    WordprocessingDocument doc,
    Paragraph paragraph)
  {
    var styleId = paragraph
      .ParagraphProperties?
      .ParagraphStyleId?
      .Val;

    if (styleId == null)
      return null;

    var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;

    var style = styles?
      .Elements<Style>()
      .FirstOrDefault(s => s.StyleId == styleId);

    return style?
      .StyleRunProperties?
      .RunFonts?
      .Ascii;
  }

  string? GetDefaultFont(WordprocessingDocument doc)
  {
    var styles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;

    var defaultStyle = styles?
      .Elements<Style>()
      .FirstOrDefault(s => s.Type == StyleValues.Paragraph && s.Default);

    return defaultStyle?
      .StyleRunProperties?
      .RunFonts?
      .Ascii;
  }



}