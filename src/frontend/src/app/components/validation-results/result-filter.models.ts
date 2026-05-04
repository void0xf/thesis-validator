export type SeverityFilter = 'all' | 'errors' | 'warnings';

export interface RuleFilterOption {
  ruleName: string;
  displayName: string;
  count: number;
}

export interface RuleVisibilityChange {
  ruleName: string;
  visible: boolean;
}
