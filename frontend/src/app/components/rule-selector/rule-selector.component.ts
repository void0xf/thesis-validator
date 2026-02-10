import { Component, EventEmitter, Output, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, Check, CheckCheck, Type, Layout, ListTree, SpellCheck, ChevronDown, Info } from 'lucide-angular';
import { ValidationService } from '../../services/validation.service';
import { ValidationRule, RuleCategory, RULE_METADATA, CATEGORY_INFO } from '../../models/validation.models';
import { RuleCategoryGroupComponent } from './rule-category-group.component';

@Component({
  selector: 'app-rule-selector',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, RuleCategoryGroupComponent],
  template: `
    <div class="paper-card p-6 animate-slide-up animate-delay-100">
      <div class="flex items-center justify-between mb-5">
        <h2 class="font-display text-xl font-semibold text-ink-900">
          Validation Rules
        </h2>

        <button
          type="button"
          class="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-sans transition-all duration-200"
          [class]="allSelected()
            ? 'bg-academic-green/10 text-academic-green border border-academic-green/20'
            : 'bg-parchment-100 text-ink-600 border border-parchment-300 hover:bg-parchment-200'"
          (click)="toggleAll()"
        >
          <lucide-icon
            [name]="allSelected() ? 'check-check' : 'check'"
            class="w-4 h-4"
          ></lucide-icon>
          {{ allSelected() ? 'Deselect All' : 'Select All' }}
        </button>
      </div>

      @if (loading()) {
        <div class="space-y-3">
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="h-16 bg-parchment-100/50 rounded-lg animate-pulse"></div>
          }
        </div>
      } @else {
        <div class="space-y-4">
          @for (category of categories(); track category) {
            <app-rule-category-group
              [categoryName]="getCategoryDisplayName(category)"
              [icon]="getCategoryIcon(category)"
              [iconClass]="getCategoryIconClass(category)"
              [rules]="getRulesForCategory(category)"
              [selectedCount]="getSelectedCountForCategory(category)"
              [expanded]="expandedCategories().includes(category)"
              (toggleExpanded)="toggleCategory(category)"
              (ruleToggled)="toggleRule($event)"
            />
          }
        </div>

        <div class="flex items-start gap-2 mt-4 p-3 rounded-lg bg-academic-blue/5 border border-academic-blue/10">
          <lucide-icon name="info" class="w-4 h-4 text-academic-blue flex-shrink-0 mt-0.5"></lucide-icon>
          <p class="font-sans text-xs text-academic-blue/80 leading-relaxed">
            Selected rules will be applied during validation. Deselect rules you want to skip.
          </p>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class RuleSelectorComponent implements OnInit {
  @Output() rulesChange = new EventEmitter<string[]>();

  private readonly validationService = inject(ValidationService);

  loading = signal(true);
  rules = signal<ValidationRule[]>([]);
  expandedCategories = signal<RuleCategory[]>(['formatting', 'layout', 'structure', 'language']);

  categories = computed(() => {
    const cats = [...new Set(this.rules().map(r => r.category))];
    return cats.sort((a, b) => CATEGORY_INFO[a].order - CATEGORY_INFO[b].order);
  });

  allSelected = computed(() => this.rules().every(r => r.enabled));

  selectedRules = computed(() =>
    this.rules()
      .filter(r => r.enabled)
      .map(r => r.name)
  );

  ngOnInit(): void {
    this.loadRules();
  }

  private loadRules(): void {
    this.validationService.getRules().subscribe({
      next: (response) => {
        const rules: ValidationRule[] = response.rules.map(r => {
          const metadata = RULE_METADATA[r.name] || {
            displayName: r.name,
            description: 'Validates document formatting',
            category: 'formatting' as RuleCategory
          };
          return {
            name: r.name,
            displayName: metadata.displayName,
            description: metadata.description,
            category: metadata.category,
            enabled: true
          };
        });
        this.rules.set(rules);
        this.loading.set(false);
        this.emitSelectedRules();
      },
      error: () => {
        // Use default rules if service is unavailable
        const defaultRules: ValidationRule[] = Object.entries(RULE_METADATA).map(([name, meta]) => ({
          name,
          displayName: meta.displayName,
          description: meta.description,
          category: meta.category,
          enabled: true
        }));
        this.rules.set(defaultRules);
        this.loading.set(false);
        this.emitSelectedRules();
      }
    });
  }

  getRulesForCategory(category: RuleCategory): ValidationRule[] {
    return this.rules().filter(r => r.category === category);
  }

  getSelectedCountForCategory(category: RuleCategory): number {
    return this.getRulesForCategory(category).filter(r => r.enabled).length;
  }

  getCategoryDisplayName(category: RuleCategory): string {
    return CATEGORY_INFO[category]?.displayName || category;
  }

  getCategoryIcon(category: RuleCategory): string {
    return CATEGORY_INFO[category]?.icon || 'check';
  }

  getCategoryIconClass(category: RuleCategory): string {
    const classes: Record<RuleCategory, string> = {
      'formatting': 'bg-academic-gold/10 text-academic-gold',
      'layout': 'bg-academic-blue/10 text-academic-blue',
      'structure': 'bg-academic-green/10 text-academic-green',
      'language': 'bg-academic-burgundy/10 text-academic-burgundy'
    };
    return classes[category] || 'bg-ink-100 text-ink-600';
  }

  toggleCategory(category: RuleCategory): void {
    const current = this.expandedCategories();
    if (current.includes(category)) {
      this.expandedCategories.set(current.filter(c => c !== category));
    } else {
      this.expandedCategories.set([...current, category]);
    }
  }

  toggleRule(rule: ValidationRule): void {
    const updated = this.rules().map(r =>
      r.name === rule.name ? { ...r, enabled: !r.enabled } : r
    );
    this.rules.set(updated);
    this.emitSelectedRules();
  }

  toggleAll(): void {
    const newState = !this.allSelected();
    const updated = this.rules().map(r => ({ ...r, enabled: newState }));
    this.rules.set(updated);
    this.emitSelectedRules();
  }

  private emitSelectedRules(): void {
    this.rulesChange.emit(this.selectedRules());
  }
}
