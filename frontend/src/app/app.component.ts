import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LucideAngularModule } from 'lucide-angular';
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';
import { FileUploadComponent } from './components/file-upload/file-upload.component';
import { RuleSelectorComponent } from './components/rule-selector/rule-selector.component';
import { ValidationProgressComponent } from './components/validation-progress/validation-progress.component';
import { ValidationResultsComponent } from './components/validation-results/validation-results.component';
import { ErrorToastComponent } from './components/error-toast/error-toast.component';
import { ValidationService } from './services/validation.service';
import { ValidationResponse } from './models/validation.models';

type AppState = 'upload' | 'validating' | 'results';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    LucideAngularModule,
    HeaderComponent,
    FooterComponent,
    FileUploadComponent,
    RuleSelectorComponent,
    ValidationProgressComponent,
    ValidationResultsComponent,
    ErrorToastComponent
  ],
  templateUrl: './app.component.html',
  styles: [`:host { display: block; }`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  // Injected dependencies
  private readonly validationService = inject(ValidationService);
  private readonly destroyRef = inject(DestroyRef);

  // Constants
  readonly validationSteps = ['Reading', 'Analyzing', 'Reporting'] as const;

  // Internal component state
  readonly appState = signal<AppState>('upload');
  readonly selectedFile = signal<File | null>(null);
  readonly selectedRules = signal<string[]>([]);
  readonly validationResponse = signal<ValidationResponse | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly currentValidationStep = signal(0);

  // Derived state
  readonly canValidate = computed(() =>
    this.selectedFile() !== null && this.selectedRules().length > 0
  );

  private stepIntervalId: ReturnType<typeof setInterval> | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.clearStepInterval());
  }

  onFileChange(file: File | null): void {
    this.selectedFile.set(file);
    this.errorMessage.set(null);
  }

  onRulesChange(rules: string[]): void {
    this.selectedRules.set(rules);
  }

  validateDocument(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.appState.set('validating');
    this.currentValidationStep.set(0);
    this.errorMessage.set(null);

    this.clearStepInterval();
    this.stepIntervalId = setInterval(() => {
      if (this.currentValidationStep() < this.validationSteps.length - 1) {
        this.currentValidationStep.update(v => v + 1);
      }
    }, 800);

    this.validationService.validateDocument(file, this.selectedRules())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.clearStepInterval();
          this.currentValidationStep.set(this.validationSteps.length);
          setTimeout(() => {
            this.validationResponse.set(response);
            this.appState.set('results');
          }, 500);
        },
        error: (err) => {
          this.clearStepInterval();
          this.appState.set('upload');
          this.errorMessage.set(
            err.error?.detail || err.message || 'An error occurred during validation'
          );
        }
      });
  }

  downloadAnnotated(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.validationService.validateWithComments(file, this.selectedRules())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = file.name.replace('.docx', '_annotated.docx');
          link.click();
          URL.revokeObjectURL(url);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.detail || 'Failed to download annotated document'
          );
        }
      });
  }

  reset(): void {
    this.appState.set('upload');
    this.selectedFile.set(null);
    this.validationResponse.set(null);
    this.currentValidationStep.set(0);
    this.errorMessage.set(null);
  }

  clearError(): void {
    this.errorMessage.set(null);
  }

  private clearStepInterval(): void {
    if (this.stepIntervalId !== null) {
      clearInterval(this.stepIntervalId);
      this.stepIntervalId = null;
    }
  }
}
