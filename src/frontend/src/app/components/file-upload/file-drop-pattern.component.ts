import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-file-drop-pattern',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="absolute inset-0 opacity-30 rounded-xl overflow-hidden pointer-events-none"
    >
      <div
        class="absolute inset-0"
        style="
          background-image: repeating-linear-gradient(
            45deg,
            transparent,
            transparent 10px,
            currentColor 10px,
            currentColor 11px
          );
          opacity: 0.03;
        "
      ></div>
    </div>
  `,
})
export class FileDropPatternComponent {}
