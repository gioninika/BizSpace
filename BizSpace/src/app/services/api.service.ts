import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, catchError, map, throwError } from 'rxjs';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

declare global {
  interface Window {
    bizSpaceConfig?: {
      apiBaseUrl?: string;
      currency?: string;
      contactEmail?: string;
      contactPhone?: string;
      facebookUrl?: string;
      instagramUrl?: string;
    };
  }
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = this.resolveBaseUrl();

  get<T>(path: string, query?: Record<string, string | number | undefined>): Observable<T> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query ?? {})) {
      if (value !== undefined && value !== '') params = params.set(key, value);
    }
    return this.http.get<ApiResponse<T>>(`${this.baseUrl}${path}`, { params }).pipe(
      map((response) => this.unwrap(response)),
      catchError((error) => this.fail(error)),
    );
  }

  post<T>(path: string, body?: unknown): Observable<T> {
    return this.http.post<ApiResponse<T>>(`${this.baseUrl}${path}`, body ?? {}).pipe(
      map((response) => this.unwrap(response)),
      catchError((error) => this.fail(error)),
    );
  }

  put<T>(path: string, body?: unknown): Observable<T> {
    return this.http.put<ApiResponse<T>>(`${this.baseUrl}${path}`, body ?? {}).pipe(
      map((response) => this.unwrap(response)),
      catchError((error) => this.fail(error)),
    );
  }

  delete<T>(path: string): Observable<T> {
    return this.http.delete<ApiResponse<T>>(`${this.baseUrl}${path}`).pipe(
      map((response) => this.unwrap(response)),
      catchError((error) => this.fail(error)),
    );
  }

  upload<T>(path: string, file: File): Observable<T> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<T>>(`${this.baseUrl}${path}`, form).pipe(
      map((response) => this.unwrap(response)),
      catchError((error) => this.fail(error)),
    );
  }

  private unwrap<T>(response: ApiResponse<T>): T {
    if (!response?.success)
      throw new Error(response?.message || 'The request could not be completed.');
    return response.data;
  }

  private fail(error: unknown): Observable<never> {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { message?: string; errors?: Record<string, string[]> } | null;
      const validation = body?.errors ? Object.values(body.errors).flat()[0] : undefined;
      const message =
        body?.message ||
        validation ||
        (error.status === 0
          ? 'BizSpace could not reach the API. Check that the service is running and the API URL is configured.'
          : `Request failed (${error.status}).`);
      return throwError(() => new Error(message));
    }
    return throwError(() =>
      error instanceof Error ? error : new Error('The request could not be completed.'),
    );
  }

  private resolveBaseUrl(): string {
    const configured =
      typeof window !== 'undefined' ? window.bizSpaceConfig?.apiBaseUrl : undefined;
    return (configured || '/api').replace(/\/$/, '');
  }
}
