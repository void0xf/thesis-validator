import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-rule-selector-skeleton',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-3">
      @for (i of placeholders; track i) {
        <div class="h-16 bg-parchment-100/50 rounded-lg animate-pulse"></div>
      }
    </div>
  `,
})
export class RuleSelectorSkeletonComponent {
  readonly placeholders = [1, 2, 3, 4] as const;
}
