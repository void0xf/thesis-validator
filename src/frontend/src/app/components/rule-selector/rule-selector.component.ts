import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  untracked,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ValidationRule } from '../../models/validation.models';
import { CategoryTileListComponent } from '../shared/category-tile-list.component';
import { RuleCategoryRuleListComponent } from './rule-category-rule-list.component';
import { RuleSelectorEmptyComponent } from './rule-selector-empty.component';
import { RuleSelectorHeaderComponent } from './rule-selector-header.component';
import { RuleSelectorHintComponent } from './rule-selector-hint.component';
import { RuleSelectorSkeletonComponent } from './rule-selector-skeleton.component';
import { RuleSelectorStore } from './rule-selector.store';

@Component({
  selector: 'app-rule-selector',
  standalone: true,
  imports: [
    CommonModule,
    CategoryTileListComponent,
    RuleCategoryRuleListComponent,
    RuleSelectorEmptyComponent,
    RuleSelectorHeaderComponent,
    RuleSelectorHintComponent,
    RuleSelectorSkeletonComponent,
  ],
  providers: [RuleSelectorStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="paper-card p-6 animate-slide-up animate-delay-100">
      <app-rule-selector-header
        [selectedCount]="store.selectedRules().length"
        [totalCount]="store.rules().length"
        [allSelected]="store.allSelected()"
        [disabled]="store.loading() || store.rules().length === 0"
        (toggleAll)="store.toggleAll()"
      />

      @if (store.loading()) {
        <app-rule-selector-skeleton />
      } @else if (store.rules().length === 0) {
        <app-rule-selector-empty />
      } @else {
        <app-category-tile-list
          [items]="store.categoryTiles()"
          [selectedCategory]="store.activeCategory()"
          columns="two"
          size="compact"
          [withBottomMargin]="true"
          (categorySelected)="store.setActiveCategory($event)"
        />

        @if (store.activeCategory(); as category) {
          <app-rule-category-rule-list
            [category]="category"
            [rules]="store.getRulesForCategory(category)"
            [selectedCount]="store.getSelectedCountForCategory(category)"
            (ruleToggled)="store.toggleRule($event)"
          />
        }

        <app-rule-selector-hint />
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
    `,
  ],
})
export class RuleSelectorComponent {
  readonly selectedRuleNames = input<string[] | null>(null);
  readonly syncKey = input(0);
  readonly rulesChange = output<string[]>();
  readonly selectionCountChange = output<{
    selected: number;
    total: number;
  }>();
  readonly ruleCatalogChange = output<ValidationRule[]>();
  readonly store = inject(RuleSelectorStore);
  private readonly destroyRef = inject(DestroyRef);
  private hasSeenInitialSyncKey = false;

  constructor() {
    effect(
      () => {
        this.syncKey();
        const selectedRuleNames = this.selectedRuleNames();

        if (!this.hasSeenInitialSyncKey) {
          this.hasSeenInitialSyncKey = true;
          return;
        }

        untracked(() =>
          this.store.syncSelectionFromInput(selectedRuleNames),
        );
      },
      { allowSignalWrites: true },
    );

    this.store.stateChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.publishSelection());
  }

  private publishSelection(): void {
    if (this.store.loading()) return;

    const rules = this.store.rules();
    const selected = this.store.selectedRules();
    this.rulesChange.emit(selected);
    this.selectionCountChange.emit({
      selected: selected.length,
      total: rules.length,
    });
    this.ruleCatalogChange.emit(rules);
  }
}
