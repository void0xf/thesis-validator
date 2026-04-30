import {
  Component,
  Input,
  Output,
  EventEmitter,
  ChangeDetectionStrategy,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { CategoryGroup } from '../../models/validation.models';
import { ResultItemComponent } from './result-item.component';

@Component({
  selector: 'app-result-category-group',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, ResultItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="paper-card results-surface overflow-hidden animate-slide-up"
      [style.animation-delay]="animationDelay"
    >
      <button
        type="button"
        class="w-full flex items-center justify-between px-5 py-4 hover:bg-parchment-100/50 transition-colors"
        (click)="toggleExpanded.emit(group)"
      >
        <div class="flex items-center gap-4">
          <div
            class="w-10 h-10 rounded-xl flex items-center justify-center"
            [class]="iconClass"
          >
            <lucide-icon [name]="group.icon" class="w-5 h-5"></lucide-icon>
          </div>
          <div class="text-left">
            <h4 class="font-sans font-semibold text-ink-900">
              {{ group.displayName }}
            </h4>
            <p class="font-sans text-xs text-ink-500">
              {{ group.results.length }} issue{{
                group.results.length !== 1 ? 's' : ''
              }}
              found
            </p>
          </div>
        </div>

        <div class="flex items-center gap-3">
          @if (group.errorCount > 0) {
            <span class="badge-error">
              {{ group.errorCount }} error{{
                group.errorCount !== 1 ? 's' : ''
              }}
            </span>
          }
          @if (group.warningCount > 0) {
            <span class="badge-warning">
              {{ group.warningCount }} warning{{
                group.warningCount !== 1 ? 's' : ''
              }}
            </span>
          }
          <lucide-icon
            name="chevron-down"
            class="w-5 h-5 text-ink-400 transition-transform duration-200"
            [class.rotate-180]="expanded"
          ></lucide-icon>
        </div>
      </button>

      @if (expanded) {
        <div class="border-t border-parchment-200/60">
          @if (useVirtualizedList) {
            <div
              class="max-h-[34rem] overflow-y-auto"
              (scroll)="onResultsScroll($event)"
            >
              <div [style.height.px]="topSpacerHeight"></div>
              @for (result of visibleResults; track result) {
                <app-result-item [result]="result" />
              }
              <div [style.height.px]="bottomSpacerHeight"></div>
            </div>
          } @else {
            @for (result of visibleResults; track result) {
              <app-result-item [result]="result" />
            }
          }
          @if (hasMore) {
            <button
              type="button"
              class="w-full px-5 py-3 text-sm font-sans text-ink-500 hover:text-ink-700
                     hover:bg-parchment-50/80 transition-colors border-t border-parchment-100
                     flex items-center justify-center gap-2"
              (click)="showMore($event)"
            >
              <lucide-icon
                name="chevron-down"
                class="w-3.5 h-3.5"
              ></lucide-icon>
              Show {{ remainingCount }} more issue{{
                remainingCount !== 1 ? 's' : ''
              }}
            </button>
          }
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
export class ResultCategoryGroupComponent implements OnChanges {
  @Input({ required: true }) group!: CategoryGroup;
  @Input({ required: true }) iconClass!: string;
  @Input() expanded = false;
  @Input() animationDelay = '0ms';
  @Output() toggleExpanded = new EventEmitter<CategoryGroup>();

  private readonly PAGE_SIZE = 10;
  private readonly VIRTUALIZE_THRESHOLD = 20;
  private readonly ESTIMATED_ROW_HEIGHT = 96;
  private readonly VIEWPORT_BUFFER = 4;
  private readonly VIEWPORT_ROWS = 8;

  displayLimit = this.PAGE_SIZE;
  visibleResults: CategoryGroup['results'] = [];
  hasMore = false;
  remainingCount = 0;
  useVirtualizedList = false;
  topSpacerHeight = 0;
  bottomSpacerHeight = 0;
  private limitedResults: CategoryGroup['results'] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['group']) {
      this.displayLimit = this.PAGE_SIZE;
    }
    this.updateVisibleState();
  }

  showMore(event: Event): void {
    event.stopPropagation();
    this.displayLimit += this.PAGE_SIZE;
    this.updateVisibleState();
  }

  onResultsScroll(event: Event): void {
    if (!this.useVirtualizedList) return;

    const element = event.target as HTMLElement;
    this.updateVirtualWindow(element.scrollTop);
  }

  private updateVisibleState(): void {
    const results = this.group?.results ?? [];
    const limit = Math.min(this.displayLimit, results.length);
    this.limitedResults = results.slice(0, limit);
    this.hasMore = results.length > limit;
    this.remainingCount = Math.max(0, results.length - limit);

    this.useVirtualizedList =
      this.expanded && this.limitedResults.length > this.VIRTUALIZE_THRESHOLD;

    if (!this.useVirtualizedList) {
      this.visibleResults = this.limitedResults;
      this.topSpacerHeight = 0;
      this.bottomSpacerHeight = 0;
      return;
    }

    this.updateVirtualWindow(0);
  }

  private updateVirtualWindow(scrollTop: number): void {
    const approximateStart = Math.floor(scrollTop / this.ESTIMATED_ROW_HEIGHT);
    const start = Math.max(0, approximateStart - this.VIEWPORT_BUFFER);
    const end = Math.min(
      this.limitedResults.length,
      start + this.VIEWPORT_ROWS + this.VIEWPORT_BUFFER * 2,
    );

    this.visibleResults = this.limitedResults.slice(start, end);
    this.topSpacerHeight = start * this.ESTIMATED_ROW_HEIGHT;
    this.bottomSpacerHeight = Math.max(
      0,
      (this.limitedResults.length - end) * this.ESTIMATED_ROW_HEIGHT,
    );
  }
}
