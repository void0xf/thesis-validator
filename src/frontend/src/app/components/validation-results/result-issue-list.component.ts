import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ValidationResult,
  ValidationRule,
} from '../../models/validation.models';
import {
  buildRuleLookup,
  getRuleDisplayName,
} from '../../models/validation-display.models';
import { ResultItemComponent } from './result-item.component';

@Component({
  selector: 'app-result-issue-list',
  standalone: true,
  imports: [CommonModule, ResultItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-h-[34rem] overflow-y-auto" (scroll)="onScroll($event)">
      @if (useVirtualizedList()) {
        <div [style.height.px]="topSpacerHeight()"></div>
      }

      @for (result of visibleResults(); track result) {
        <app-result-item
          [result]="result"
          [ruleDisplayName]="getDisplayName(result.ruleName)"
        />
      }

      @if (useVirtualizedList()) {
        <div [style.height.px]="bottomSpacerHeight()"></div>
      }
    </div>

    @if (paginated() && hasMore()) {
      <button
        type="button"
        class="w-full px-5 py-3 text-sm font-sans text-ink-500 hover:text-ink-700
               hover:bg-parchment-50/80 transition-colors border-t border-parchment-100
               flex items-center justify-center gap-2"
        (click)="showMore()"
      >
        Show {{ remainingCount() }} more issue{{
          remainingCount() !== 1 ? 's' : ''
        }}
      </button>
    }
  `,
})
export class ResultIssueListComponent {
  readonly results = input.required<readonly ValidationResult[]>();
  readonly ruleCatalog = input<readonly ValidationRule[]>([]);
  readonly paginated = input(false);

  private readonly pageSize = 10;
  private readonly virtualizationThreshold = 20;
  private readonly estimatedRowHeight = 96;
  private readonly viewportBuffer = 4;
  private readonly viewportRows = 8;

  private readonly displayLimit = signal(this.pageSize);
  private readonly virtualStart = signal(0);

  private readonly ruleLookup = computed(() =>
    buildRuleLookup(this.ruleCatalog()),
  );
  private readonly displayedResults = computed(() => {
    const results = this.results();

    if (!this.paginated()) {
      return results;
    }

    return results.slice(0, Math.min(this.displayLimit(), results.length));
  });
  private readonly virtualWindow = computed(() => {
    const results = this.displayedResults();

    if (!this.useVirtualizedList()) {
      return { start: 0, end: results.length };
    }

    const start = Math.min(this.virtualStart(), results.length);
    const end = Math.min(
      results.length,
      start + this.viewportRows + this.viewportBuffer * 2,
    );

    return { start, end };
  });

  readonly hasMore = computed(
    () =>
      this.paginated() &&
      this.results().length > this.displayedResults().length,
  );
  readonly remainingCount = computed(() =>
    Math.max(0, this.results().length - this.displayedResults().length),
  );
  readonly useVirtualizedList = computed(
    () =>
      this.paginated() &&
      this.displayedResults().length > this.virtualizationThreshold,
  );
  readonly visibleResults = computed(() => {
    const results = this.displayedResults();
    const { start, end } = this.virtualWindow();
    return results.slice(start, end);
  });
  readonly topSpacerHeight = computed(
    () => this.virtualWindow().start * this.estimatedRowHeight,
  );
  readonly bottomSpacerHeight = computed(() =>
    Math.max(
      0,
      (this.displayedResults().length - this.virtualWindow().end) *
        this.estimatedRowHeight,
    ),
  );

  constructor() {
    effect(
      () => {
        this.results();
        this.displayLimit.set(this.pageSize);
        this.virtualStart.set(0);
      },
      { allowSignalWrites: true },
    );
  }

  getDisplayName(ruleName: string): string {
    return getRuleDisplayName(ruleName, this.ruleLookup());
  }

  showMore(): void {
    this.displayLimit.update((limit) => limit + this.pageSize);
  }

  onScroll(event: Event): void {
    if (!this.useVirtualizedList()) {
      return;
    }

    const element = event.target as HTMLElement;
    const approximateStart = Math.floor(
      element.scrollTop / this.estimatedRowHeight,
    );
    this.virtualStart.set(Math.max(0, approximateStart - this.viewportBuffer));
  }
}
