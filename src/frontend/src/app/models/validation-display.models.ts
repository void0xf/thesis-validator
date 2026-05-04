import {
  RuleCategory,
  RuleInfo,
  ValidationRule,
} from './validation.models';

export interface RuleCategoryDisplay {
  displayName: string;
  icon: string;
  order: number;
}

export type RuleCategoryIconTone = 'soft' | 'solid';

const DEFAULT_CATEGORY: RuleCategory = 'formatting';
const UNKNOWN_CATEGORY_ORDER = 99;

const CATEGORY_DISPLAY: Record<RuleCategory, RuleCategoryDisplay> = {
  formatting: { displayName: 'Formatting', icon: 'type', order: 1 },
  layout: { displayName: 'Layout', icon: 'layout', order: 2 },
  structure: { displayName: 'Structure', icon: 'list-tree', order: 3 },
  language: { displayName: 'Language', icon: 'spell-check', order: 4 },
};

const CATEGORY_ICON_CLASSES: Record<
  RuleCategoryIconTone,
  Record<RuleCategory, string>
> = {
  soft: {
    formatting: 'bg-academic-gold/10 text-academic-gold',
    layout: 'bg-academic-blue/10 text-academic-blue',
    structure: 'bg-academic-green/10 text-academic-green',
    language: 'bg-academic-burgundy/10 text-academic-burgundy',
  },
  solid: {
    formatting: 'bg-academic-gold/15 text-academic-gold',
    layout: 'bg-academic-blue/15 text-academic-blue',
    structure: 'bg-academic-green/15 text-academic-green',
    language: 'bg-academic-burgundy/15 text-academic-burgundy',
  },
};

export function normalizeRuleCategory(
  category: string | null | undefined,
): RuleCategory | null {
  const normalized = category?.trim().toLowerCase();

  if (!normalized) {
    return null;
  }

  return isRuleCategory(normalized) ? normalized : null;
}

export function toRuleCategory(
  category: string | null | undefined,
): RuleCategory {
  return normalizeRuleCategory(category) ?? DEFAULT_CATEGORY;
}

export function getRuleCategoryDisplay(
  category: RuleCategory,
): RuleCategoryDisplay {
  return CATEGORY_DISPLAY[category];
}

export function getRuleCategoryIconClass(
  category: RuleCategory,
  tone: RuleCategoryIconTone = 'soft',
): string {
  return CATEGORY_ICON_CLASSES[tone][category];
}

export function compareRuleCategories(
  first: RuleCategory,
  second: RuleCategory,
): number {
  return getRuleCategoryOrder(first) - getRuleCategoryOrder(second);
}

export function buildRuleLookup(
  rules: readonly ValidationRule[],
): Map<string, ValidationRule> {
  const lookup = new Map<string, ValidationRule>();

  for (const rule of rules) {
    lookup.set(rule.name.toLowerCase(), rule);
  }

  return lookup;
}

export function getRuleDisplayName(
  ruleName: string,
  ruleLookup: ReadonlyMap<string, ValidationRule>,
): string {
  return (
    ruleLookup.get(ruleName.toLowerCase())?.displayName ||
    formatRuleDisplayName(ruleName)
  );
}

export function formatRuleDisplayName(ruleName: string): string {
  const trimmed = ruleName.trim();

  if (!trimmed) {
    return 'Unknown Rule';
  }

  return trimmed
    .replace(/Rule$/i, '')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
    .replace(/\s+/g, ' ');
}

export function toValidationRule(rule: RuleInfo): ValidationRule {
  return {
    name: rule.name,
    displayName: rule.displayName?.trim() || formatRuleDisplayName(rule.name),
    description: rule.description?.trim() || '',
    category: toRuleCategory(rule.category),
    severity: rule.defaultSeverity,
    enabled: rule.enabled ?? true,
  };
}

function isRuleCategory(category: string): category is RuleCategory {
  return category in CATEGORY_DISPLAY;
}

function getRuleCategoryOrder(category: RuleCategory): number {
  return CATEGORY_DISPLAY[category]?.order ?? UNKNOWN_CATEGORY_ORDER;
}
