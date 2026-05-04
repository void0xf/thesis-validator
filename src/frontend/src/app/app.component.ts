import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { filter } from 'rxjs';
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
    ErrorToastComponent,
  ],
  templateUrl: './app.component.html',
  styles: [
    `
      :host {
        display: block;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  // Injected dependencies
  private readonly validationService = inject(ValidationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  // Constants
  readonly validationSteps = ['Reading', 'Analyzing', 'Reporting'] as const;

  // Internal component state
  readonly appState = signal<AppState>('upload');
  readonly selectedFile = signal<File | null>(null);
  readonly selectedRules = signal<string[]>([]);
  readonly draftSelectedRules = signal<string[]>([]);
  readonly totalRuleCount = signal(0);
  readonly validationResponse = signal<ValidationResponse | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly currentValidationStep = signal(0);
  readonly ruleSettingsOpen = signal(false);
  readonly ruleSelectorSyncKey = signal(0);

  // Derived state
  readonly canValidate = computed(
    () => this.selectedFile() !== null && this.selectedRules().length > 0,
  );
  readonly hasResults = computed(() => this.validationResponse() !== null);

  private stepIntervalId: ReturnType<typeof setInterval> | null = null;
  private rulesInitialized = false;

  constructor() {
    this.destroyRef.onDestroy(() => this.clearStepInterval());

    this.syncStateWithRoute(this.router.url);
    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => {
        this.syncStateWithRoute(event.urlAfterRedirects);
      });
  }

  onFileChange(file: File | null): void {
    this.selectedFile.set(file);
    this.errorMessage.set(null);
  }

  onRulesChange(rules: string[]): void {
    this.draftSelectedRules.set(rules);
    if (!this.rulesInitialized) {
      this.selectedRules.set(rules);
      this.rulesInitialized = true;
    }
  }

  onRuleCountChange(selection: { selected: number; total: number }): void {
    this.totalRuleCount.set(selection.total);
  }

  openRuleSettings(): void {
    this.draftSelectedRules.set([...this.selectedRules()]);
    this.ruleSelectorSyncKey.update((value) => value + 1);
    this.ruleSettingsOpen.set(true);
  }

  closeRuleSettings(): void {
    this.ruleSettingsOpen.set(false);
  }

  applyRuleSettings(): void {
    this.selectedRules.set([...this.draftSelectedRules()]);
    this.closeRuleSettings();
  }

  validateDocument(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.closeRuleSettings();
    this.appState.set('validating');
    void this.router.navigate(['/validate']);
    this.currentValidationStep.set(0);
    this.errorMessage.set(null);

    this.clearStepInterval();
    this.stepIntervalId = setInterval(() => {
      if (this.currentValidationStep() < this.validationSteps.length - 1) {
        this.currentValidationStep.update((v) => v + 1);
      }
    }, 800);

    this.validationService
      .validateDocument(file, this.selectedRules())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.clearStepInterval();
          this.currentValidationStep.set(this.validationSteps.length);
          setTimeout(() => {
            this.validationResponse.set(response);
            this.appState.set('results');
            void this.router.navigate(['/results']);
          }, 500);
        },
        error: (err) => {
          this.clearStepInterval();
          this.appState.set('upload');
          void this.router.navigate(['/validate']);
          this.errorMessage.set(
            err.error?.detail ||
              err.message ||
              'An error occurred during validation',
          );
        },
      });
  }

  downloadAnnotated(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.validationService
      .validateWithComments(file, this.selectedRules())
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
            err.error?.detail || 'Failed to download annotated document',
          );
        },
      });
  }

  reset(): void {
    this.appState.set('upload');
    this.selectedFile.set(null);
    this.validationResponse.set(null);
    this.currentValidationStep.set(0);
    this.errorMessage.set(null);
    void this.router.navigate(['/validate']);
  }

  clearError(): void {
    this.errorMessage.set(null);
  }

  goToValidation(): void {
    if (this.appState() === 'validating') return;
    this.appState.set('upload');
    void this.router.navigate(['/validate']);
  }

  goToResults(): void {
    if (this.appState() === 'validating') return;
    if (!this.validationResponse()) return;
    this.appState.set('results');
    void this.router.navigate(['/results']);
  }

  private clearStepInterval(): void {
    if (this.stepIntervalId !== null) {
      clearInterval(this.stepIntervalId);
      this.stepIntervalId = null;
    }
  }

  private syncStateWithRoute(url: string): void {
    if (this.appState() === 'validating') {
      return;
    }

    if (url.startsWith('/results')) {
      if (this.validationResponse()) {
        this.appState.set('results');
      } else {
        this.appState.set('upload');
        void this.router.navigate(['/validate'], { replaceUrl: true });
      }
      return;
    }

    this.appState.set('upload');
  }
}
