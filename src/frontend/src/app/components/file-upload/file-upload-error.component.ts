import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-file-upload-error',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (message()) {
      <div
        class="flex items-center gap-2 mt-3 px-3 py-2 rounded-lg bg-academic-red/5 border border-academic-red/10"
      >
        <lucide-icon
          name="alert-circle"
          class="w-4 h-4 text-academic-red flex-shrink-0"
        ></lucide-icon>
        <p class="font-sans text-sm text-academic-red">
          {{ message() }}
        </p>
      </div>
    }
  `,
})
export class FileUploadErrorComponent {
  readonly message = input<string | null>(null);
}
