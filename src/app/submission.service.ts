import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type SubmissionStatus = 'New' | 'Contacted' | 'Archived';

export interface ProjectSubmission {
  id: string;
  createdAtUtc: string;
  firstName: string;
  lastName: string;
  email: string;
  company?: string | null;
  role: string;
  projectType: string;
  budget: string;
  timeline: string;
  notes?: string | null;
  status: SubmissionStatus;
}

export interface CreateProjectSubmission {
  firstName: string;
  lastName: string;
  email: string;
  company?: string;
  role: string;
  projectType: string;
  budget: string;
  timeline: string;
  notes?: string;
}

export interface SubmissionResponse {
  id: string;
  createdAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class SubmissionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5080';

  createSubmission(payload: CreateProjectSubmission): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(`${this.apiUrl}/api/submissions`, payload);
  }

  listSubmissions(): Observable<ProjectSubmission[]> {
    return this.http.get<ProjectSubmission[]>(`${this.apiUrl}/api/submissions`);
  }

  updateStatus(id: string, status: SubmissionStatus): Observable<ProjectSubmission> {
    return this.http.put<ProjectSubmission>(`${this.apiUrl}/api/submissions/${id}/status`, { status });
  }

  deleteSubmission(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/submissions/${id}`);
  }
}
