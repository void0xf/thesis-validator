import { Component, Input, Output, EventEmitter, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ValidationResponse } from '../../models/validation.models';

@Component({
  selector: 'app-result-status-banner',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="px-6 py-4" [class]="bannerClass">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 rounded-xl flex items-center justify-center" [class]="iconBgClass">
            <lucide-icon [name]="icon" class="w-6 h-6"></lucide-icon>
          </div>
          <div>
            <h2 class="font-display text-2xl font-bold" [class]="textClass">
              {{ title }}
            </h2>
            <p class="font-sans text-sm" [class]="subtextClass">
              {{ response.fileName }}
            </p>
          </div>
        </div>

        @if (!response.isValid) {
          <button
            type="button"
            class="btn-secondary flex items-center gap-2"
            (click)="downloadAnnotated.emit()"
          >
            <lucide-icon name="file-down" class="w-4 h-4"></lucide-icon>
            Download Annotated
          </button>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ResultStatusBannerComponent {
  @Input({ required: true }) response!: ValidationResponse;
  @Output() downloadAnnotated = new EventEmitter<void>();

  get icon(): string {
    if (this.response.isValid) return 'check-circle-2';
    if (this.response.totalErrors > 0) return 'x-circle';
    return 'alert-triangle';
  }

  get title(): string {
    if (this.response.isValid) return 'Validation Passed';
    if (this.response.totalErrors > 0) return 'Issues Found';
    return 'Warnings Only';
  }

  get bannerClass(): string {
    if (this.response.isValid) return 'bg-gradient-to-r from-academic-green/10 to-academic-green/5';
    if (this.response.totalErrors > 0) return 'bg-gradient-to-r from-academic-red/10 to-academic-red/5';
    return 'bg-gradient-to-r from-academic-gold/10 to-academic-gold/5';
  }

  get iconBgClass(): string {
    if (this.response.isValid) return 'bg-academic-green/20 text-academic-green';
    if (this.response.totalErrors > 0) return 'bg-academic-red/20 text-academic-red';
    return 'bg-academic-gold/20 text-academic-gold';
  }

  get textClass(): string {
    if (this.response.isValid) return 'text-academic-green';
    if (this.response.totalErrors > 0) return 'text-academic-red';
    return 'text-academic-gold';
  }

  get subtextClass(): string {
    if (this.response.isValid) return 'text-academic-green/70';
    if (this.response.totalErrors > 0) return 'text-academic-red/70';
    return 'text-academic-gold/70';
  }
}
