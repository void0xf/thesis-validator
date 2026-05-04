import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-result-severity-dot',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex-shrink-0 mt-1">
      <div
        class="w-2 h-2 rounded-full"
        [class.bg-academic-red]="isError()"
        [class.bg-academic-gold]="!isError()"
      ></div>
    </div>
  `,
})
export class ResultSeverityDotComponent {
  readonly isError = input.required<boolean>();
}
