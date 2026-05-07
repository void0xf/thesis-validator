import {
  Component,
  Input,
  Output,
  EventEmitter,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import {
  ValidationResponse,
  CategoryGroup,
  ValidationRule,
} from '../../models/validation.models';
import { buildRuleLookup } from '../../models/validation-display.models';
import { ResultStatusBannerComponent } from './result-status-banner.component';
import { ResultStatsGridComponent } from './result-stats-grid.component';
import { ResultCategoryListComponent } from './result-category-list.component';
import { ResultAllClearComponent } from './result-all-clear.component';
import {
  buildCategoryGroups,
  normalizeValidationResultsResponse,
} from './validation-results.view-model';

@Component({
  selector: 'app-validation-results',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    ResultStatusBannerComponent,
    ResultStatsGridComponent,
    ResultCategoryListComponent,
    ResultAllClearComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="paper-card-elevated results-surface overflow-hidden">
        <app-result-status-banner
          [response]="response"
          [downloadingAnnotated]="downloadingAnnotated"
          (downloadAnnotated)="onDownloadAnnotated.emit()"
        />
        <app-result-stats-grid
          [errors]="response.totalErrors"
          [warnings]="response.totalWarnings"
          [categoryCount]="categoryGroups.length"
        />
      </div>

      @if (categoryGroups.length > 0) {
        <app-result-category-list
          [categoryGroups]="categoryGroups"
          [ruleCatalog]="availableRules"
        />
      }

      @if (response.isValid) {
        <app-result-all-clear />
      }

      <div class="flex justify-center gap-4 pt-4">
        <button
          type="button"
          class="btn-secondary flex items-center gap-2"
          (click)="onReset.emit()"
        >
          <lucide-icon name="rotate-ccw" class="w-4 h-4"></lucide-icon>
          Validate Another Document
        </button>
      </div>
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
export class ValidationResultsComponent {
  @Input() downloadingAnnotated = false;
  @Output() onDownloadAnnotated = new EventEmitter<void>();
  @Output() onReset = new EventEmitter<void>();

  private _response!: ValidationResponse;
  private ruleLookup = new Map<string, ValidationRule>();
  availableRules: readonly ValidationRule[] = [];
  categoryGroups: CategoryGroup[] = [];

  @Input()
  set ruleCatalog(value: readonly ValidationRule[] | null) {
    this.availableRules = value ?? [];
    this.ruleLookup = buildRuleLookup(this.availableRules);

    if (this._response) {
      this.categoryGroups = buildCategoryGroups(
        this._response.results,
        this.ruleLookup,
      );
    }
  }

  @Input({ required: true })
  set response(value: ValidationResponse) {
    this._response = normalizeValidationResultsResponse(value);
    this.categoryGroups = buildCategoryGroups(
      this._response.results,
      this.ruleLookup,
    );
  }

  get response(): ValidationResponse {
    return this._response;
  }
}
