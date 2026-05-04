import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  HeadingInfo,
  ValidationResult,
  ValidationResponse,
  RulesResponse,
} from '../models/validation.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ValidationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getRules(): Observable<RulesResponse> {
    return this.http.get<RulesResponse>(`${this.baseUrl}/rules`);
  }

  validateDocument(
    file: File,
    selectedRules?: string[],
  ): Observable<ValidationResponse> {
    const formData = this.createValidationFormData(file, selectedRules);
    return this.http
      .post<ValidationResponse>(`${this.baseUrl}/validate`, formData)
      .pipe(map((response) => this.normalizeValidationResponse(response)));
  }

  validateWithComments(
    file: File,
    selectedRules?: string[],
  ): Observable<Blob> {
    const formData = this.createValidationFormData(file, selectedRules);
    return this.http.post(`${this.baseUrl}/validate-with-comments`, formData, {
      responseType: 'blob'
    });
  }

  healthCheck(): Observable<{ status: string; timestamp: string }> {
    return this.http.get<{ status: string; timestamp: string }>(`${this.baseUrl}/health`);
  }

  private createValidationFormData(
    file: File,
    selectedRules?: string[],
  ): FormData {
    const formData = new FormData();
    formData.append('file', file);

    if (selectedRules && selectedRules.length > 0) {
      formData.append('rules', JSON.stringify(selectedRules));
    }

    return formData;
  }

  private normalizeValidationResponse(
    response: ValidationResponse,
  ): ValidationResponse {
    const results: ValidationResult[] = Array.isArray(response.results)
      ? response.results
      : [];
    const headings: HeadingInfo[] = Array.isArray(response.headings)
      ? response.headings
      : [];
    const totalErrors =
      response.totalErrors ?? results.filter((result) => result.isError).length;
    const totalWarnings =
      response.totalWarnings ??
      results.filter((result) => !result.isError).length;

    return {
      ...response,
      results,
      headings,
      totalErrors,
      totalWarnings,
      isValid: response.isValid ?? totalErrors === 0,
    };
  }
}
