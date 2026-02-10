export interface DocumentLocation {
  pageNumber: number;
  lineNumber: number;
  paragraph: number;
  run: number;
  characterOffset: number;
  length: number;
  text: string;
  description: string;
}

export interface ValidationResult {
  ruleName: string;
  message: string;
  isError: boolean;
  location: DocumentLocation | null;
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
}

export interface RuleInfo {
  name: string;
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
  expanded: boolean;
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
  'ParagraphIndentRule': {
    displayName: 'Paragraph Indent',
    description: 'Checks first-line indentation (1.00cm or 1.25cm)',
    category: 'layout'
  },
  'LineSpacingDependencyRule': {
    displayName: 'Line Spacing',
    description: 'Validates line spacing rules and their dependencies',
    category: 'layout'
  },
  'CheckTableOfContents': {
    displayName: 'Table of Contents',
    description: 'Ensures the document contains a Table of Contents',
    category: 'structure'
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
