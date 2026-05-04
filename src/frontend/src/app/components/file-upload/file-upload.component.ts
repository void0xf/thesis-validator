import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewChild,
  computed,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileDropEmptyComponent } from './file-drop-empty.component';
import { FileDropPatternComponent } from './file-drop-pattern.component';
import { FileDropPreviewComponent } from './file-drop-preview.component';
import { FileUploadErrorComponent } from './file-upload-error.component';
import { FileUploadHeaderComponent } from './file-upload-header.component';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [
    CommonModule,
    FileDropEmptyComponent,
    FileDropPatternComponent,
    FileDropPreviewComponent,
    FileUploadErrorComponent,
    FileUploadHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="paper-card p-6 animate-slide-up">
      <app-file-upload-header [hasFile]="selectedFile() !== null" />

      <div
        class="relative group"
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave($event)"
        (drop)="onDrop($event)"
      >
        <input
          type="file"
          #fileInput
          id="file-input"
          accept=".docx"
          class="absolute inset-0 w-full h-full opacity-0 cursor-pointer z-10"
          [disabled]="selectedFile() !== null"
          [class.pointer-events-none]="selectedFile() !== null"
          (change)="onFileSelected($event)"
        />

        <div
          class="relative border-2 border-dashed rounded-xl p-8 md:p-12 text-center transition-all duration-300"
          [class]="dropZoneClasses()"
        >
          <app-file-drop-pattern />

          @if (selectedFile(); as file) {
            <app-file-drop-preview
              [file]="file"
              (fileCleared)="clearFile($event)"
            />
          } @else {
            <app-file-drop-empty />
          }
        </div>

        <app-file-upload-error [message]="errorMessage()" />
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
export class FileUploadComponent {
  readonly fileChange = output<File | null>();

  @ViewChild('fileInput') private fileInput?: ElementRef<HTMLInputElement>;

  readonly selectedFile = signal<File | null>(null);
  readonly isDragOver = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly dropZoneClasses = computed(() => {
    const base = 'border-parchment-400/60 bg-parchment-50/50';
    const hover =
      'group-hover:border-academic-burgundy/40 group-hover:bg-parchment-100/50';
    const dragOver = this.isDragOver()
      ? 'border-academic-burgundy bg-academic-burgundy/5 scale-[1.01]'
      : '';
    const hasFile = this.selectedFile()
      ? 'border-academic-blue/30 bg-academic-blue/5'
      : '';

    return `${base} ${hover} ${dragOver} ${hasFile}`;
  });

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);

    const file = event.dataTransfer?.files.item(0);
    if (file) {
      this.processFile(file);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0);
    if (file) {
      this.processFile(file);
    }
  }

  clearFile(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectedFile.set(null);
    this.errorMessage.set(null);
    this.resetNativeInput();
    this.fileChange.emit(null);
  }

  private processFile(file: File): void {
    this.errorMessage.set(null);

    if (!file.name.toLowerCase().endsWith('.docx')) {
      this.errorMessage.set(
        'Please upload a .docx file (Microsoft Word document)',
      );
      this.selectedFile.set(null);
      this.resetNativeInput();
      this.fileChange.emit(null);
      return;
    }

    this.selectedFile.set(file);
    this.fileChange.emit(file);
  }

  private resetNativeInput(): void {
    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }
  }
}
