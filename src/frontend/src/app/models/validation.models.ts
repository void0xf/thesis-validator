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
  headings?: HeadingInfo[];
}

export interface RuleInfo {
  name: string;
  displayName?: string;
  description: string;
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
  severity?: string;
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
