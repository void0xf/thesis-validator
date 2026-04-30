import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-error-toast',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    @if (message) {
      <div class="fixed bottom-6 right-6 max-w-md animate-slide-up z-50">
        <div class="paper-card-elevated flex items-start gap-3 p-4 border-l-4 border-academic-red">
          <lucide-icon name="alert-circle" class="w-5 h-5 text-academic-red flex-shrink-0 mt-0.5"></lucide-icon>
          <div class="flex-1">
            <p class="font-sans font-medium text-ink-900">Validation Error</p>
            <p class="font-body text-sm text-ink-600 mt-1">{{ message }}</p>
          </div>
          <button
            type="button"
            class="p-1 rounded hover:bg-parchment-100 text-ink-400 hover:text-ink-600 transition-colors"
            (click)="dismiss.emit()"
          >
            <lucide-icon name="x" class="w-4 h-4"></lucide-icon>
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorToastComponent {
  @Input() message: string | null = null;
  @Output() dismiss = new EventEmitter<void>();
}
