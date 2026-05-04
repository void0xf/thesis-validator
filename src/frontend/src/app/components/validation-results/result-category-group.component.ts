import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import {
  CategoryGroup,
  ValidationRule,
} from '../../models/validation.models';
import { CategoryPanelHeadingComponent } from '../shared/category-panel-heading.component';
import { ResultIssueListComponent } from './result-issue-list.component';

@Component({
  selector: 'app-result-category-group',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    CategoryPanelHeadingComponent,
    ResultIssueListComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="paper-card results-surface overflow-hidden animate-slide-up"
      [style.animation-delay]="animationDelay()"
    >
      <button
        type="button"
        class="w-full px-5 py-4 hover:bg-parchment-100/50 transition-colors"
        (click)="toggleExpanded.emit(group())"
      >
        <app-category-panel-heading
          [title]="group().displayName"
          [subtitle]="issueLabel()"
          [icon]="group().icon"
          [iconClass]="iconClass()"
          size="md"
        >
          <div class="flex items-center gap-3">
            @if (group().errorCount > 0) {
              <span class="badge-error">
                {{ group().errorCount }} error{{
                  group().errorCount !== 1 ? 's' : ''
                }}
              </span>
            }
            @if (group().warningCount > 0) {
              <span class="badge-warning">
                {{ group().warningCount }} warning{{
                  group().warningCount !== 1 ? 's' : ''
                }}
              </span>
            }
            <lucide-icon
              name="chevron-down"
              class="w-5 h-5 text-ink-400 transition-transform duration-200"
              [class.rotate-180]="expanded()"
            ></lucide-icon>
          </div>
        </app-category-panel-heading>
      </button>

      @if (expanded()) {
        <div class="border-t border-parchment-200/60">
          <app-result-issue-list
            [results]="group().results"
            [ruleCatalog]="ruleCatalog()"
            [paginated]="true"
          />
        </div>
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
export class ResultCategoryGroupComponent {
  readonly group = input.required<CategoryGroup>();
  readonly iconClass = input.required<string>();
  readonly expanded = input(false);
  readonly animationDelay = input('0ms');
  readonly ruleCatalog = input<readonly ValidationRule[]>([]);
  readonly toggleExpanded = output<CategoryGroup>();

  readonly issueLabel = computed(() => {
    const count = this.group().results.length;
    return `${count} issue${count !== 1 ? 's' : ''} found`;
  });
}
