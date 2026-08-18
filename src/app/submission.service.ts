import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type SubmissionStatus = 'New' | 'Contacted' | 'Archived';

export interface SubmissionFile {
  id?: string;
  originalName: string;
  contentType?: string;
  size?: number;
  kind: string;
  url: string;
}

export interface ProjectSubmission {
  id: string;
  trackingId: string;
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
  files: SubmissionFile[];
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
  profilePicture?: File | null;
  attachments?: File[];
}

export interface SubmissionResponse {
  id: string;
  trackingId: string;
  createdAtUtc: string;
}

export interface TrackingResponse {
  trackingId: string;
  status: SubmissionStatus;
  createdAtUtc: string;
  name: string;
  files: SubmissionFile[];
}

@Injectable({ providedIn: 'root' })
export class SubmissionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://5080-i7mf8jm6hvf7j3z9mpngi-7d23be11.us2.manus.computer';

  createSubmission(payload: CreateProjectSubmission): Observable<SubmissionResponse> {
    const formData = new FormData();
    Object.entries(payload).forEach(([key, value]) => {
      if (value === undefined || value === null) return;
      if (key === 'attachments' && Array.isArray(value)) {
        value.forEach((file) => formData.append('attachments', file, file.name));
      } else if (key === 'profilePicture' && value instanceof File) {
        formData.append('profilePicture', value, value.name);
      } else {
        formData.append(key, String(value));
      }
    });
    return this.http.post<SubmissionResponse>(`${this.apiUrl}/api/submissions`, formData);
  }

  lookupTracking(trackingId: string): Observable<TrackingResponse> {
    return this.http.get<TrackingResponse>(`${this.apiUrl}/api/tracking/${encodeURIComponent(trackingId.trim())}`);
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
