import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProjectSubmission {
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

  createSubmission(payload: ProjectSubmission): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(`${this.apiUrl}/api/submissions`, payload);
  }
}
