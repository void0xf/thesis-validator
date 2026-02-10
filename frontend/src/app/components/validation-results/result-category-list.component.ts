import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CategoryGroup, RuleCategory } from '../../models/validation.models';
import { ResultCategoryGroupComponent } from './result-category-group.component';

@Component({
  selector: 'app-result-category-list',
  standalone: true,
  imports: [CommonModule, ResultCategoryGroupComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="font-display text-xl font-semibold text-ink-900">
          Issues by Category
        </h3>
        <button
          type="button"
          class="font-sans text-sm text-ink-500 hover:text-ink-700 transition-colors"
          (click)="toggleAll.emit()"
        >
          {{ allExpanded ? 'Collapse All' : 'Expand All' }}
        </button>
      </div>

      @for (group of categoryGroups; track group.category; let i = $index) {
        <app-result-category-group
          [group]="group"
          [iconClass]="getCategoryIconClass(group.category)"
          [animationDelay]="(i * 50) + 'ms'"
          (toggleExpanded)="toggleExpanded.emit($event)"
        />
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ResultCategoryListComponent {
  @Input({ required: true }) categoryGroups!: CategoryGroup[];
  @Input({ required: true }) allExpanded!: boolean;
  @Output() toggleExpanded = new EventEmitter<CategoryGroup>();
  @Output() toggleAll = new EventEmitter<void>();

  getCategoryIconClass(category: RuleCategory): string {
    const classes: Record<RuleCategory, string> = {
      'formatting': 'bg-academic-gold/15 text-academic-gold',
      'layout': 'bg-academic-blue/15 text-academic-blue',
      'structure': 'bg-academic-green/15 text-academic-green',
      'language': 'bg-academic-burgundy/15 text-academic-burgundy'
    };
    return classes[category] || 'bg-ink-100 text-ink-600';
  }
}
