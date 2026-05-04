import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-file-upload-header',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-between mb-5">
      <h2 class="font-display text-xl font-semibold text-ink-900">
        Upload Document
      </h2>
      @if (hasFile()) {
        <span class="badge-success">
          <lucide-icon
            name="check-circle-2"
            class="w-3.5 h-3.5 mr-1.5"
          ></lucide-icon>
          Ready
        </span>
      }
    </div>
  `,
})
export class FileUploadHeaderComponent {
  readonly hasFile = input(false);
}
