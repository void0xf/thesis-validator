using backend.Models;
using backend.Services.Comments;
using backend.Services.Exceptions;
using backend.Services.Extraction;
using backend.Services.Skipping;
using backend.Services.Structure;
using backend.RuleOptions;
using Backend.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using ThesisValidator.Rules;

namespace backend.Services.Analysis;

public class ThesisValidatorService
{
    private readonly IReadOnlyList<IModernValidationRule> _modernRules;
    private readonly RulePolicyResolver _policyResolver;
    private readonly RuleOptionsBinder _optionsBinder;
    private readonly ValidationResultComposer _resultComposer;
    private readonly DocumentContentAnalyzer _contentAnalyzer;

    public ThesisValidatorService(IEnumerable<IValidationRule> legacyRules)
        : this(legacyRules, ruleConfigurationService: null)
    {
    }

    public ThesisValidatorService(
        IEnumerable<IValidationRule> legacyRules,
        backend.Services.Rules.IRuleConfigurationService? ruleConfigurationService)
        : this(
            legacyRules.Select(rule => new LegacyModernRuleAdapter(rule)),
            CreateDefaultPolicyResolver(),
            new RuleOptionsBinder(CreateEmptyConfiguration()),
            new ValidationResultComposer())
    {
    }

    public ThesisValidatorService(
        IEnumerable<IModernValidationRule> modernRules,
        RulePolicyResolver policyResolver,
        RuleOptionsBinder optionsBinder,
        ValidationResultComposer resultComposer,
        DocumentContentAnalyzer? contentAnalyzer = null)
    {
        _modernRules = modernRules.ToList();
        _policyResolver = policyResolver;
        _optionsBinder = optionsBinder;
        _resultComposer = resultComposer;
        _contentAnalyzer = contentAnalyzer ?? new DocumentContentAnalyzer();

    }

    public IReadOnlyList<RuleDefinition> GetAvailableRules()
    {
        var definitions = new List<RuleDefinition>();

        foreach (var rule in _modernRules)
        {
            var descriptor = rule.Descriptor;
            var policy = _policyResolver.Resolve(descriptor);
            if (policy.Availability == RuleAvailability.Hidden)
                continue;

            definitions.Add(new RuleDefinition(
                descriptor.Name,
                descriptor.DisplayName,
                descriptor.Category,
                policy.Severity.ToString()));
        }

        return definitions;
    }

    public IReadOnlyList<string> GetUnknownRuleNames(IEnumerable<string> selectedRules)
    {
        var knownRules = _modernRules
            .Select(rule => rule.Descriptor.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return selectedRules
            .Where(ruleName => !knownRules.Contains(ruleName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }


    public (IEnumerable<ValidationResult> Results, List<HeadingInfo> Headings)
        Validate(Stream fileStream,
        UniversityConfig config,
        IEnumerable<string>? selectedRules = null)
    {
        using var doc = OpenDocument(fileStream, isEditable: false);
        var errors = new List<ValidationResult>();
        var content = _contentAnalyzer.Analyze(doc, config);


        var context = new RuleContext
        {
            RawDocument = doc,
            Content = content
        };

        foreach (var rule in GetAvailableRules(selectedRules))
        {
            var policy = _policyResolver.Resolve(rule.Descriptor);

            if (policy.Availability == RuleAvailability.Hidden)
                continue;

            var options = _optionsBinder.Bind(rule);
            var problems = rule.Validate(context, options);

            foreach (var problem in problems)
            {
                errors.Add(_resultComposer.Compose(
                    rule.Descriptor,
                    policy,
                    problem));
            }
        }

        var headings = ExtractHeadings(doc, config);
        var (elementsMap, descendantsMap) = BuildSectionMaps(doc, config);
        PopulateSectionContext(errors, elementsMap, descendantsMap);

        return (errors, headings);
    }
    public IReadOnlyList<IModernValidationRule> GetAvailableRules(
        IEnumerable<string>? selectedRules)
    {
        if (selectedRules is null)
            return _modernRules;

        var selectedSet = selectedRules.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedSet.Count == 0)
            return _modernRules;

        return _modernRules
            .Where(rule => selectedSet.Contains(rule.Descriptor.Name))
            .ToList();
    }


    public static List<HeadingInfo> ExtractHeadings(WordprocessingDocument doc, UniversityConfig? config = null)
    {
        var headings = new List<HeadingInfo>();
        var effectiveConfig = config ?? new UniversityConfig();
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
            return headings;

        var tocParagraphIndex = SkipDecisionService.ShouldSkipBeforeTableOfContents(effectiveConfig)
            ? TableOfContentsDetectionService.Detect(doc).ParagraphIndex
            : 0;

        int paragraphIndex = 0;
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            paragraphIndex++;
            var text = TextExtractionService.GetParagraphText(paragraph, effectiveConfig).Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (TableOfContentsDetectionService.IsTableOfContentsHeadingText(text))
            {
                headings.Add(new HeadingInfo { Level = 1, Text = text });
                continue;
            }

            if (tocParagraphIndex > 0 && paragraphIndex <= tocParagraphIndex)
                continue;

            var level = HeadingDetectionService.GetHeadingLevel(doc, paragraph);
            if (level is null) continue;

            headings.Add(new HeadingInfo { Level = level.Value, Text = text });
        }

        return headings;
    }

    /// <summary>
    /// Validates the document and adds comments for each error found.
    /// Returns both the validation results and a stream containing the annotated document.
    /// </summary>
    public (IEnumerable<ValidationResult> Results, MemoryStream AnnotatedDocument)
        ValidateWithComments(Stream fileStream, UniversityConfig config, IEnumerable<string>? selectedRules = null)
    {
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var doc = OpenDocument(memoryStream, isEditable: true);
        var rulesToRun = GetAvailableRules(selectedRules);

        var validationResults = new List<ValidationResult>();
        var commentService = new DocumentCommentService();
        var content = _contentAnalyzer.Analyze(doc, config);
        var context = new RuleContext
        {
            RawDocument = doc,
            Content = content
        };

        foreach (var rule in rulesToRun)
        {
            var policy = _policyResolver.Resolve(rule.Descriptor);
            if (policy.Availability == RuleAvailability.Hidden)
                continue;

            var options = _optionsBinder.Bind(rule);
            var problems = rule.Validate(context, options);

            foreach (var problem in problems)
            {
                validationResults.Add(_resultComposer.Compose(
                    rule.Descriptor,
                    policy,
                    problem));
                AddCommentForProblem(commentService, doc, problem);
            }
        }

        var (elementsMap, descendantsMap) = BuildSectionMaps(doc, config);
        PopulateSectionContext(validationResults, elementsMap, descendantsMap);

        try
        {
            doc.MainDocumentPart?.Document.Save();
            var annotatedStream = DocumentCommentService.SaveDocumentWithComments(doc);

            return (validationResults, annotatedStream);
        }
        catch (Exception ex) when (IsDocumentProcessingException(ex))
        {
            throw new InvalidThesisDocumentException("The uploaded file could not be saved as an annotated DOCX document.", ex);
        }
    }

    private static WordprocessingDocument OpenDocument(Stream stream, bool isEditable)
    {
        try
        {
            var doc = WordprocessingDocument.Open(stream, isEditable);
            if (doc.MainDocumentPart?.Document is not null)
            {
                return doc;
            }

            doc.Dispose();
            throw new InvalidThesisDocumentException("The uploaded file is not a valid Wordprocessing document.");
        }
        catch (InvalidThesisDocumentException)
        {
            throw;
        }
        catch (Exception ex) when (IsDocumentProcessingException(ex))
        {
            throw new InvalidThesisDocumentException("The uploaded file could not be opened as a DOCX document.", ex);
        }
    }

    private static void AddCommentForProblem(
        DocumentCommentService commentService,
        WordprocessingDocument doc,
        RuleProblem problem)
    {
        switch (problem.AnnotationTarget)
        {
            case ParagraphAnnotationTarget paragraphTarget:
                commentService.AddCommentToParagraph(doc, paragraphTarget.Paragraph, problem.Message);
                break;
            case RunAnnotationTarget runTarget:
                commentService.AddCommentToRun(doc, runTarget.Run, problem.Message);
                break;
        }
    }

    private static bool IsDocumentProcessingException(Exception exception)
    {
        return exception is OpenXmlPackageException
            or FileFormatException
            or InvalidDataException
            or IOException
            or NotSupportedException
            or InvalidOperationException
            or ArgumentException
            or ObjectDisposedException;
    }

    /// <summary>
    /// Builds two sorted heading maps - one keyed by direct-child paragraph index
    /// (<c>Elements&lt;Paragraph&gt;</c>) and one by all-descendants index
    /// (<c>Descendants&lt;Paragraph&gt;</c>).  Different rules use different traversals,
    /// so we need both to reliably match a result's paragraph index to a heading.
    /// </summary>
    private static (
        List<(int Index, string Text)> ElementsMap,
        List<(int Index, string Text)> DescendantsMap
    ) BuildSectionMaps(WordprocessingDocument doc, UniversityConfig config)
    {
        var elementsMap = new List<(int, string)>();
        var descendantsMap = new List<(int, string)>();

        // Elements-based map (direct body children only - matches FontFamily, list rules, Grammar, FigureCaption, EmptySection rules)
        foreach (var (para, elemIdx) in DocumentAnalysisScope.BodyParagraphs(doc, config))
        {
            var level = HeadingDetectionService.GetHeadingLevel(doc, para);
            if (level is null) continue;

            var text = TextExtractionService.GetParagraphText(para, config).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            elementsMap.Add((elemIdx, text));
        }

        // Descendants-based map (all paragraphs incl. table cells - matches SingleSpace, Justification, NoDots, Indent, Spacing, LineSpacing, Hierarchy rules)
        foreach (var (para, descIdx) in DocumentAnalysisScope.DescendantParagraphs(doc, config))
        {
            var level = HeadingDetectionService.GetHeadingLevel(doc, para);
            if (level is null) continue;

            var text = TextExtractionService.GetParagraphText(para, config).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            descendantsMap.Add((descIdx, text));
        }

        return (elementsMap, descendantsMap);
    }

    /// <summary>
    /// For each validation result, finds the nearest preceding heading and
    /// writes it into <see cref="DocumentLocation.Section"/>.
    /// Uses the correct section map based on which paragraph traversal the rule uses.
    /// </summary>
    private static void PopulateSectionContext(
        List<ValidationResult> results,
        List<(int Index, string Text)> elementsMap,
        List<(int Index, string Text)> descendantsMap)
    {
        foreach (var result in results)
        {
            var paraIdx = result.Location?.Paragraph ?? 0;
            if (paraIdx <= 0) continue;

            var map = result.ParagraphIndexKind == ParagraphIndexKind.BodyElement
                ? elementsMap
                : descendantsMap;

            var section = FindNearestSection(map, paraIdx);

            if (section is not null)
                result.Location!.Section = section;
        }
    }

    private static string? FindNearestSection(List<(int Index, string Text)> map, int paraIdx)
    {
        string? nearest = null;
        foreach (var (idx, text) in map)
        {
            if (idx <= paraIdx)
                nearest = text;
            else
                break;
        }
        return nearest;
    }

    private static RulePolicyResolver CreateDefaultPolicyResolver()
    {
        return new RulePolicyResolver(CreateEmptyConfiguration());
    }

    private static IConfiguration CreateEmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    private sealed class LegacyModernRuleAdapter : ValidationRule<NoRuleOptions>
    {
        private readonly IValidationRule _rule;

        public LegacyModernRuleAdapter(IValidationRule rule)
        {
            _rule = rule;
        }

        public override RuleDescriptor Descriptor
        {
            get
            {
                var definition = RuleCatalog.GetDefinition(_rule.Name);
                return new RuleDescriptor(
                    Name: definition.Id,
                    DisplayName: definition.DisplayName,
                    Description: definition.DisplayName,
                    Category: definition.Category,
                    DefaultAvailability: RuleAvailability.Available,
                    DefaultSeverity: Enum.TryParse<RuleSeverity>(
                        definition.DefaultSeverity,
                        ignoreCase: true,
                        out var severity)
                            ? severity
                            : RuleSeverity.Error);
            }
        }

        public override IEnumerable<RuleProblem> Validate(
            RuleContext context,
            NoRuleOptions options)
        {
            return _rule
                .Validate(context.RawDocument, new UniversityConfig())
                .Select(result => new RuleProblem(
                    result.Message,
                    result.Location,
                    result.ParagraphIndexKind));
        }
    }

}
