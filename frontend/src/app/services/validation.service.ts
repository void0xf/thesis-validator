import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ValidationResponse, RulesResponse } from '../models/validation.models';
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

  validateDocument(file: File, selectedRules?: string[]): Observable<ValidationResponse> {
    const formData = new FormData();
    formData.append('file', file);

    if (selectedRules && selectedRules.length > 0) {
      formData.append('rules', JSON.stringify(selectedRules));
    }

    return this.http.post<ValidationResponse>(`${this.baseUrl}/validate`, formData);
  }

  validateWithComments(file: File, selectedRules?: string[]): Observable<Blob> {
    const formData = new FormData();
    formData.append('file', file);

    if (selectedRules && selectedRules.length > 0) {
      formData.append('rules', JSON.stringify(selectedRules));
    }

    return this.http.post(`${this.baseUrl}/validate-with-comments`, formData, {
      responseType: 'blob'
    });
  }

  healthCheck(): Observable<{ status: string; timestamp: string }> {
    return this.http.get<{ status: string; timestamp: string }>(`${this.baseUrl}/health`);
  }
}
