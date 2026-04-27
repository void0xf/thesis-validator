export interface DocumentLocation {
  pageNumber: number;
  lineNumber: number;
  paragraph: number;
  run: number;
  characterOffset: number;
  length: number;
  text: string;
  section: string;
  description: string;
}

export interface ValidationResult {
  ruleName: string;
  message: string;
  isError: boolean;
  severity?: 'Error' | 'Warning' | string;
  category?: string;
  location: DocumentLocation | null;
}

export interface HeadingInfo {
  level: number;
  text: string;
}

export interface ValidationResponse {
  fileName: string;
  fileSize: number;
  validatedAt: string;
  isValid: boolean;
  totalErrors: number;
  totalWarnings: number;
  configUsed: string;
  results: ValidationResult[];
  headings: HeadingInfo[];
}

export interface ValidationOptions {
  skipBeforeTableOfContents: boolean;
  skipTextBoxes: boolean;
}

export interface RuleInfo {
  name: string;
  displayName?: string;
  category?: string;
  defaultSeverity?: string;
  enabled?: boolean;
  selectable?: boolean;
}

export interface RulesResponse {
  rules: RuleInfo[];
  count: number;
}

export interface ValidationRule {
  name: string;
  displayName: string;
  description: string;
  category: RuleCategory;
  enabled: boolean;
}

export type RuleCategory = 'formatting' | 'layout' | 'structure' | 'language';

export interface CategoryGroup {
  category: RuleCategory;
  displayName: string;
  icon: string;
  results: ValidationResult[];
  errorCount: number;
  warningCount: number;
}

export const RULE_METADATA: Record<string, { displayName: string; description: string; category: RuleCategory }> = {
  'FontFamily': {
    displayName: 'Font Family',
    description: 'Checks that the document uses the required font (Times New Roman)',
    category: 'formatting'
  },
  'SingleSpaceRule': {
    displayName: 'Single Space',
    description: 'Detects multiple consecutive spaces between words',
    category: 'formatting'
  },
  'TextJustificationRule': {
    displayName: 'Text Justification',
    description: 'Ensures body text uses full justification alignment',
    category: 'formatting'
  },
  'NoDotsInTitlesRule': {
    displayName: 'Title Punctuation',
    description: 'Checks that headings and titles do not end with periods',
    category: 'formatting'
  },
  'ListConsistencyRule': {
    displayName: 'List Consistency',
    description: 'Validates consistent punctuation and indentation in lists',
    category: 'layout'
  },
  'ParagraphSpacingRule': {
    displayName: 'Paragraph Spacing',
    description: 'Verifies paragraph spacing matches requirements (0pt or 6pt)',
    category: 'layout'
  },
  'RequiredIndentCm': {
    displayName: 'Paragraph Indent',
    description: 'Checks first-line indentation (1.00cm or 1.25cm)',
    category: 'layout'
  },
  'LineSpacingDependencyRule': {
    displayName: 'Line Spacing',
    description: 'Validates line spacing rules and their dependencies',
    category: 'layout'
  },
  'HeadingStyleUsageRule': {
    displayName: 'Heading Style Usage',
    description: 'Detects manually formatted headings that should use proper Heading styles',
    category: 'structure'
  },
  'HierarchyDepthRule': {
    displayName: 'Heading Hierarchy',
    description: 'Checks that heading levels do not exceed 3 levels deep',
    category: 'structure'
  },
  'MissingFigureCaptionRule': {
    displayName: 'Missing Figure Captions',
    description: 'Checks that figure-like objects have captions',
    category: 'structure'
  },
  'FigureCaptionPositionRule': {
    displayName: 'Figure Caption Position',
    description: 'Checks that figure captions are placed below figures',
    category: 'structure'
  },
  'FigureCaptionStyleRule': {
    displayName: 'Figure Caption Style',
    description: 'Checks figure caption style and paragraph formatting',
    category: 'structure'
  },
  'FigureCaptionFormatRule': {
    displayName: 'Figure Caption Format',
    description: 'Checks visible figure caption labels, numbers, and descriptions',
    category: 'structure'
  },
  'FigureCaptionAutomaticNumberingRule': {
    displayName: 'Figure Caption Automatic Numbering',
    description: 'Warns when figure caption numbering appears to be typed manually',
    category: 'structure'
  },
  'CheckTableOfContents': {
    displayName: 'Table of Contents',
    description: 'Ensures the document contains a Table of Contents',
    category: 'structure'
  },
  'Manual table of contents': {
    displayName: 'Manual table of contents',
    description: 'Warns when a table of contents appears to be manually written',
    category: 'structure'
  },
  'EmptySectionStructureRule': {
    displayName: 'Empty Sections',
    description: 'Checks that every chapter has introductory text before its first sub-section',
    category: 'language'
  },
  'Grammar': {
    displayName: 'Grammar & Spelling',
    description: 'Checks for grammar and spelling errors',
    category: 'language'
  }
};

export const CATEGORY_INFO: Record<RuleCategory, { displayName: string; icon: string; order: number }> = {
  'formatting': { displayName: 'Formatting', icon: 'type', order: 1 },
  'layout': { displayName: 'Layout', icon: 'layout', order: 2 },
  'structure': { displayName: 'Structure', icon: 'list-tree', order: 3 },
  'language': { displayName: 'Language', icon: 'spell-check', order: 4 }
};
