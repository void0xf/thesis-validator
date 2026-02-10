import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-validation-progress',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="flex flex-col items-center justify-center py-20 animate-fade-in">
      <div class="relative">
        <!-- Animated rings -->
        <div class="absolute inset-0 w-24 h-24 rounded-full border-4 border-academic-burgundy/20 animate-ping"></div>
        <div class="w-24 h-24 rounded-full bg-gradient-to-br from-academic-burgundy to-academic-red flex items-center justify-center shadow-lg">
          <lucide-icon name="loader-2" class="w-10 h-10 text-white animate-spin"></lucide-icon>
        </div>
      </div>

      <h2 class="font-display text-2xl font-semibold text-ink-900 mt-8 mb-2">
        Validating Your Thesis
      </h2>
      <p class="font-body text-ink-600 text-center max-w-md">
        Analyzing document formatting, structure, and style against university requirements...
      </p>

      <!-- Progress indicators -->
      <div class="flex items-center gap-3 mt-8">
        @for (step of steps; track step; let i = $index) {
          <div
            class="flex items-center gap-2 px-3 py-1.5 rounded-full transition-all duration-500"
            [class]="currentStep > i
              ? 'bg-academic-green/10 text-academic-green'
              : currentStep === i
                ? 'bg-academic-burgundy/10 text-academic-burgundy animate-pulse-soft'
                : 'bg-parchment-200 text-ink-400'"
          >
            @if (currentStep > i) {
              <lucide-icon name="check-circle-2" class="w-4 h-4"></lucide-icon>
            } @else if (currentStep === i) {
              <lucide-icon name="loader-2" class="w-4 h-4 animate-spin"></lucide-icon>
            }
            <span class="font-sans text-xs font-medium">{{ step }}</span>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ValidationProgressComponent {
  @Input({ required: true }) steps!: readonly string[];
  @Input({ required: true }) currentStep!: number;
}
